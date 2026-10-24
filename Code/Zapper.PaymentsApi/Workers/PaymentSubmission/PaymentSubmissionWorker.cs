using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;

namespace Zapper.PaymentsApi.Workers.PaymentSubmission;

public sealed class PaymentSubmissionWorker : BackgroundService
{
    // D-4 fixes the batch, the lease and the polling interval, so they are constants rather than settings
    private const int LargestBatch = 10;

    private const int MostSaves = 3;

    // the fixed marker the call record holds in place of the card token and the email address, R-10
    private const string Removed = "[removed]";

    private const string PaymentService = "CardNotPresent";

    // the customer confirms each payment in the app
    private const string CardOnFileScenario = "CIT";

    private const int UnexpectedFailuresBeforeQuery = 4;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(250);

    // longer than the 35-second call a piece of work makes
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(30);

    // after the first, the second, and the third or fourth unexpected failure in a row, S-2
    private static readonly TimeSpan[] UnexpectedFailureDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15)
    ];

    private readonly IServiceScopeFactory scopeFactory;

    private readonly TimeProvider time;

    private readonly ICryptographyManager cryptography;

    private readonly IOptions<PaymentSubmissionWorkerOptions> options;

    private readonly IOptions<AcmeOptions> acme;

    private readonly IOptions<TokenEncryptionOptions> tokenEncryption;

    private readonly ILogger<PaymentSubmissionWorker> logger;

    private readonly ConcurrentDictionary<Guid, Task> running = new();

    private Task polling = Task.CompletedTask;

    private DateTimeOffset nextReportAt;

    // the options are read only once the worker is on, so a host with it off never needs Acme's settings or the card key
    public PaymentSubmissionWorker(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        ICryptographyManager cryptography,
        IOptions<PaymentSubmissionWorkerOptions> options,
        IOptions<AcmeOptions> acme,
        IOptions<TokenEncryptionOptions> tokenEncryption,
        ILogger<PaymentSubmissionWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.time = time;
        this.cryptography = cryptography;
        this.options = options;
        this.acme = acme;
        this.tokenEncryption = tokenEncryption;
        this.logger = logger;
    }

    // one polling pass and the work it claimed, to their end; the worker's tests run one pass at a time, Testing.md section 5
    public async Task RunPassAsync(CancellationToken stoppingToken)
    {
        await PollAsync(stoppingToken);
        await WaitForWorkAsync();
    }

    // ends once the polling pass in progress and every piece of work running have finished
    public async Task WaitForWorkAsync()
    {
        await Volatile.Read(ref polling);
        await Task.WhenAll(running.Values).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    // claimed work finishes after the stop signal, within the host's shutdown timeout, BackendCode.md section 23
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await WaitForWorkAsync().WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.IsEnabled is not true)
        {
            return;
        }

        using var timer = new PeriodicTimer(PollingInterval, time);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollAsync(stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken stoppingToken)
    {
        // set before the claim, so a wait that sees a claimed payment also waits for the work it started
        var pass = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref polling, pass.Task);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ZapperDbContext>();
            var now = time.GetUtcNow();

            foreach (var paymentId in await ClaimAsync(database, now, stoppingToken))
            {
                StartWork(paymentId);
            }

            await ReportDueAsync(database, now, stoppingToken);
        }
#pragma warning disable CA1031 // around one polling pass, which waits for the next interval, BackendCode.md section 23
        catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
#pragma warning restore CA1031
        {
            logger.LogError(exception, "A polling pass for the payments due failed");
        }
        finally
        {
            pass.SetResult();
        }
    }

    // a claimed row is always being worked on, so the claim takes only the free places, D-5
    private async Task<List<Guid>> ClaimAsync(
        ZapperDbContext database,
        DateTimeOffset now,
        CancellationToken stoppingToken)
    {
        var places = Math.Min(LargestBatch, acme.Value.MaxConcurrentCalls - running.Count);
        var leaseEnd = now + Lease;

        return await database.Database.SqlQuery<Guid>(
            $"""
             WITH "Due" AS (
                 SELECT "Id"
                 FROM "Payment"."Payment"
                 WHERE "NextActionAt" <= {now}
                 ORDER BY "NextActionAt"
                 LIMIT {places}
                 FOR UPDATE SKIP LOCKED)
             UPDATE "Payment"."Payment" AS "Payment"
             SET "NextActionAt" = {leaseEnd}, "AttemptCount" = "Payment"."AttemptCount" + 1
             FROM "Due"
             WHERE "Payment"."Id" = "Due"."Id"
             RETURNING "Payment"."Id" AS "Value"
             """).ToListAsync(stoppingToken);
    }

    private void StartWork(Guid paymentId)
    {
        var work = WorkAsync(paymentId);
        running[paymentId] = work;

        // removes only this piece of work, so a later claim of the same payment stays counted
        _ = work.ContinueWith(
            _ => running.TryRemove(KeyValuePair.Create(paymentId, work)),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task ReportDueAsync(ZapperDbContext database, DateTimeOffset now, CancellationToken stoppingToken)
    {
        if (now < nextReportAt)
        {
            return;
        }

        nextReportAt = now + ReportInterval;

        var due = database.Payments.Where(payment => payment.NextActionAt <= now);
        var dueCount = await due.CountAsync(stoppingToken);
        var oldestDueAt = await due.MinAsync(payment => payment.NextActionAt, stoppingToken);

        logger.LogInformation(
            "{DueCount} payments are due and not claimed, and the oldest has waited {OldestAge}",
            dueCount,
            now - (oldestDueAt ?? now));
    }

    private async Task WorkAsync(Guid paymentId)
    {
        using var activity = new Activity(nameof(PaymentSubmissionWorker)).Start();
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ZapperDbContext>();

        try
        {
            var acmeService = scope.ServiceProvider.GetRequiredService<IAcmeService>();
            var payment = await LoadPaymentAsync(database, paymentId);
            var traceId = activity.TraceId.ToHexString();

            await (payment.DecideNextCall(time) switch
            {
                NextCallResult.Send => SendAsync(database, acmeService, payment, traceId),
                NextCallResult.Query => QueryAsync(database, acmeService, payment, traceId),
                _ => SaveUnresolvedAsync(database, payment)
            });
        }
#pragma warning disable CA1031 // around one piece of work, so a failure nobody planned for is counted, BackendCode.md section 23
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogError(exception, "The work on payment {PaymentId} failed in a way nobody planned for", paymentId);

            await CountUnexpectedFailureAsync(database, paymentId);
        }
    }

    private async Task SendAsync(
        ZapperDbContext database,
        IAcmeService acmeService,
        PaymentEntity payment,
        string traceId)
    {
        var request = await BuildSubmitPaymentRequestAsync(database, payment);
        var response = await acmeService.SubmitPaymentAsync(request, CancellationToken.None);

        // the outcome is missing only when the circuit was open and no call left the process, R-13
        var acmeInteraction = response.Outcome is { } outcome
            ? new AcmeInteractionEntity(
                payment.Id,
                payment.StoredCardId,
                AcmeOperation.Send,
                outcome,
                request.MessageId,
                payment.AttemptCount,
                JsonSerializer.Serialize(request with { Token = Removed, Email = Removed }),
                response.HttpStatus,
                response.Body,
                response.StartedAt,
                (int)response.Duration.TotalMilliseconds,
                traceId)
            : null;

        AcmeAnswerResult Record(PaymentEntity candidate) =>
            candidate.RecordSendAnswer(response.Answer, acmeInteraction?.Id, time);

        LogResult(await SaveAsync(database, payment, Record(payment), acmeInteraction, Record), response.Answer);
    }

    private async Task QueryAsync(
        ZapperDbContext database,
        IAcmeService acmeService,
        PaymentEntity payment,
        string traceId)
    {
        var response = await acmeService.GetTransactionByTransactionIdAsync(payment.Id, CancellationToken.None);

        var acmeInteraction = response.Outcome is { } outcome
            ? new AcmeInteractionEntity(
                payment.Id,
                payment.StoredCardId,
                AcmeOperation.Query,
                outcome,
                null,
                payment.AttemptCount,
                JsonSerializer.Serialize(new { TransactionId = payment.Id }),
                response.HttpStatus,
                response.Body,
                response.StartedAt,
                (int)response.Duration.TotalMilliseconds,
                traceId)
            : null;

        AcmeAnswerResult Record(PaymentEntity candidate) =>
            candidate.RecordQueryAnswer(response.Answer, acmeInteraction?.Id, time);

        LogResult(await SaveAsync(database, payment, Record(payment), acmeInteraction, Record), response.Answer);
    }

    private async Task SaveUnresolvedAsync(ZapperDbContext database, PaymentEntity payment)
    {
        var saved = await SaveAsync(database, payment, NextCallResult.Unresolved, null, Decide);

        if (saved is { Result: NextCallResult.Unresolved })
        {
            logger.LogError("Payment {PaymentId} is Unresolved, and a person settles it with Acme", payment.Id);
        }

        NextCallResult Decide(PaymentEntity candidate) => candidate.DecideNextCall(time);
    }

    private async Task<SubmitPaymentRequest> BuildSubmitPaymentRequestAsync(ZapperDbContext database, PaymentEntity payment)
    {
        var storedCard = await database.StoredCards.SingleAsync(
            candidate => candidate.Id == payment.StoredCardId, CancellationToken.None);
        var subMerchantReference = await database.SubMerchants
            .Where(candidate => candidate.Id == payment.SubMerchantId)
            .Select(candidate => candidate.MerchantReference)
            .SingleAsync(CancellationToken.None);
        var currencyCode = await database.CurrencyTypes
            .Where(candidate => candidate.Id == payment.Currency)
            .Select(candidate => candidate.Code)
            .SingleAsync(CancellationToken.None);

        // the token is decrypted only in memory, for this one call, and never stored, logged or recorded
        var token = cryptography.Decrypt(
            storedCard.EncryptedToken,
            Convert.FromBase64String(tokenEncryption.Value.Key),
            Encoding.UTF8.GetBytes(storedCard.CustomerReference));

        return new SubmitPaymentRequest(
            Guid.CreateVersion7(),
            payment.Id,
            payment.OrderNumber,
            payment.Amount,
            currencyCode,
            PaymentService,
            token,
            CardOnFileScenario,
            subMerchantReference,
            payment.MerchantCategoryCode,
            payment.CardAcceptorName,
            storedCard.EmailAddress);
    }

    // one save; on a conflict the payment is reloaded and the Domain asked again, up to three saves in all
    private async Task<(PaymentEntity Payment, TResult Result)?> SaveAsync<TResult>(
        ZapperDbContext database,
        PaymentEntity payment,
        TResult result,
        AcmeInteractionEntity? acmeInteraction,
        Func<PaymentEntity, TResult> decide)
    {
        for (var save = 1; ; save++)
        {
            if (acmeInteraction is not null)
            {
                database.AcmeInteractions.Add(acmeInteraction);
            }

            if (await database.SaveChangesWithOutcomeAsync(CancellationToken.None) != SaveOutcome.Conflict)
            {
                return (payment, result);
            }

            if (save == MostSaves)
            {
                logger.LogError(
                    "Payment {PaymentId} was changed by another writer at each of {SaveCount} saves, so it is left to its lease",
                    payment.Id,
                    MostSaves);

                return null;
            }

            database.ChangeTracker.Clear();
            payment = await LoadPaymentAsync(database, payment.Id);
            result = decide(payment);
        }
    }

    private static Task<PaymentEntity> LoadPaymentAsync(ZapperDbContext database, Guid paymentId) =>
        database.Payments.AsTracking().SingleAsync(payment => payment.Id == paymentId, CancellationToken.None);

    // after the fifth failure in a row the payment is due at once, and the Domain decides what follows, R-12
    private async Task CountUnexpectedFailureAsync(ZapperDbContext database, Guid paymentId)
    {
        var now = time.GetUtcNow();
        DateTimeOffset? afterFirst = now + UnexpectedFailureDelays[0];
        DateTimeOffset? afterSecond = now + UnexpectedFailureDelays[1];
        DateTimeOffset? afterThirdOrFourth = now + UnexpectedFailureDelays[2];
        DateTimeOffset? atOnce = now;

        await database.Payments
            .Where(payment => payment.Id == paymentId)
            .ExecuteUpdateAsync(
                payment => payment
                    .SetProperty(row => row.UnexpectedFailureCount, row => row.UnexpectedFailureCount + 1)
                    .SetProperty(
                        row => row.NextActionAt,
                        row => row.UnexpectedFailureCount == 0 ? afterFirst
                            : row.UnexpectedFailureCount == 1 ? afterSecond
                            : row.UnexpectedFailureCount < UnexpectedFailuresBeforeQuery ? afterThirdOrFourth
                            : atOnce),
                CancellationToken.None);
    }

    // each line names the payment by its id only
    private void LogResult((PaymentEntity Payment, AcmeAnswerResult Result)? saved, AcmeAnswer answer)
    {
        if (saved is not var (payment, result))
        {
            return;
        }

        switch (result)
        {
            case AcmeAnswerResult.Approved when payment.AuthorisedAmount > payment.Amount:
                logger.LogError(
                    "Payment {PaymentId} was approved for more than was sent, so the customer is owed a refund",
                    payment.Id);
                break;
            case AcmeAnswerResult.Approved when payment.AuthorisedAmount < payment.Amount:
                logger.LogInformation("Payment {PaymentId} was approved for less than was sent", payment.Id);
                break;
            case AcmeAnswerResult.Declined when payment.AuthorisedAmount > 0:
                logger.LogError("Payment {PaymentId} was declined with an authorised amount above 0", payment.Id);
                break;
            case AcmeAnswerResult.Approved or AcmeAnswerResult.Declined or AcmeAnswerResult.Failed:
                logger.LogInformation("Payment {PaymentId} is {AcmeAnswerResult}", payment.Id, result);
                break;
            case AcmeAnswerResult.FailedNotSubmitted:
                logger.LogWarning("Payment {PaymentId} is {AcmeAnswerResult}", payment.Id, result);
                break;
            case AcmeAnswerResult.FailedRefusedByAcme
                or AcmeAnswerResult.FailedOrderNumberInUse
                or AcmeAnswerResult.FailedAfterUnexpectedFailures
                or AcmeAnswerResult.Unresolved:
                logger.LogError("Payment {PaymentId} is {AcmeAnswerResult}, and a person must act", payment.Id, result);
                break;
            case AcmeAnswerResult.QueryLater
                when answer.Type is AcmeAnswerType.TimedOut or AcmeAnswerType.ConnectionFailed or AcmeAnswerType.Degraded:
                logger.LogWarning(
                    "The call to Acme for payment {PaymentId} ended {AcmeAnswerType}, so Acme is asked by query",
                    payment.Id,
                    answer.Type);
                break;
            default:
                break;
        }
    }
}
