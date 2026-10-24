extern alias PaymentsApi;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PaymentsApi::Zapper.PaymentsApi.Workers.PaymentSubmission;
using Shouldly;
using Xunit;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;

namespace Zapper.IntegrationTests.PaymentsApi.Workers.PaymentSubmission;

// the tests share the clock and the database, so each one leaves no payment of its own due for the next
public sealed class PaymentSubmissionWorkerIntegrationTests : IClassFixture<PaymentSubmissionWorkerFixture>
{
    private const string AccountMerchantId = "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67";

    private static readonly TimeSpan QueryGap = TimeSpan.FromSeconds(5);

    private readonly PaymentSubmissionWorker worker;

    private readonly ZapperDbContext database;

    private readonly IAcmeService acme;

    private readonly FakeTimeProvider time;

    private readonly byte[] tokenKey;

    public PaymentSubmissionWorkerIntegrationTests(PaymentSubmissionWorkerFixture fixture)
    {
        worker = fixture.Worker;
        database = fixture.Database;
        acme = fixture.Acme;
        time = fixture.Time;
        tokenKey = fixture.TokenKey;
    }

    [Fact]
    public async Task RunPass_SendApproved_SavesApprovedWithItsCallRecordAndChange()
    {
        var id = await RecordPaymentAsync("0501");
        var sends = new ConcurrentQueue<SubmitPaymentRequest>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue(call.Arg<SubmitPaymentRequest>()));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Approved);
        payment.ResponseCode.ShouldBe("00");
        payment.ClientMessage.ShouldBe("Approved.");
        payment.AuthorisedAmount.ShouldBe(1999);
        payment.NextActionAt.ShouldBeNull();
        payment.AttemptCount.ShouldBe(1);
        payment.SendCount.ShouldBe(1);
        var sent = sends.ShouldHaveSingleItem();
        sent.Token.ShouldBe("test-token-0501");
        sent.Email.ShouldBe("cus-0501@example.test");
        sent.SubMerchantReference.ShouldBe("ZAP0000501");
        sent.CurrencyCode.ShouldBe("ZAR");
        sent.PaymentService.ShouldBe("CardNotPresent");
        sent.CardOnFileScenario.ShouldBe("CIT");
        var acmeInteraction = (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem();
        acmeInteraction.Operation.ShouldBe(AcmeOperation.Send);
        acmeInteraction.Outcome.ShouldBe(AcmeInteractionOutcome.Answered);
        acmeInteraction.MessageId.ShouldBe(sent.MessageId);
        acmeInteraction.Attempt.ShouldBe(1);
        acmeInteraction.HttpStatus.ShouldBe(200);
        acmeInteraction.TraceId.Length.ShouldBe(32);
        var request = JsonNode.Parse(acmeInteraction.Request)!;
        request["TransactionId"]!.GetValue<Guid>().ShouldBe(id);
        request["Token"]!.GetValue<string>().ShouldBe("[removed]");
        request["Email"]!.GetValue<string>().ShouldBe("[removed]");
        acmeInteraction.Request.ShouldNotContain("test-token-0501");
        acmeInteraction.Request.ShouldNotContain("cus-0501@example.test");
        var change = (await GetChangesAsync(id)).ShouldHaveSingleItem();
        (change.FromState, change.ToState, change.Source, change.AcmeInteractionId)
            .ShouldBe((PaymentState.Recorded, PaymentState.Approved, PaymentChangeSource.Send, acmeInteraction.Id));
    }

    [Fact]
    public async Task RunPass_SendDeclined_SavesDeclined()
    {
        var id = await RecordPaymentAsync("0502");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Declined(), 200));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Declined);
        payment.ResponseCode.ShouldBe("51");
        payment.AuthorisedAmount.ShouldBe(0);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunPass_SendFailed_SavesFailed()
    {
        var id = await RecordPaymentAsync("0503");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(new AcmeAnswer(AcmeAnswerType.Failure, "Acquirer", "91", "Issuer or switch inoperative", "The bank could not be reached."), 200));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("91");
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunPass_SendRefused_SavesFailedWithAcmesCode()
    {
        var id = await RecordPaymentAsync("0504");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(new AcmeAnswer(AcmeAnswerType.Refused, "Application", "E02", "API Key not valid", "We could not process that payment."), 401));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("E02");
        payment.NextActionAt.ShouldBeNull();
        (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem().HttpStatus.ShouldBe(401);
    }

    [Fact]
    public async Task RunPass_SendOrderNumberHeldByAnother_SavesFailedWithAcmesCode()
    {
        var id = await RecordPaymentAsync("0505");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(new AcmeAnswer(AcmeAnswerType.OrderNumberHeldByAnother, "Application", "E09", "Retrieval Reference Number already in use", "We could not process that payment."), 409));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("E09");
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunPass_SendTimedOut_SavesOutcomeUnknownAndQueriesLater()
    {
        var id = await RecordPaymentAsync("0506");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut());

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.NextActionAt.ShouldBe(time.GetUtcNow() + QueryGap);
        var acmeInteraction = (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem();
        acmeInteraction.Outcome.ShouldBe(AcmeInteractionOutcome.TimedOut);
        acmeInteraction.HttpStatus.ShouldBeNull();
        acmeInteraction.Response.ShouldBeNull();
        await FinishAsync(id);
    }

    [Fact]
    public async Task RunPass_SendRateLimited_PostponesByRetryAfterWithoutCountingTheSend()
    {
        var id = await RecordPaymentAsync("0507");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(new AcmeAnswer(AcmeAnswerType.RateLimited, "Application", "E29", "Too many requests", retryAfter: TimeSpan.FromSeconds(2)), 429));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.SendCount.ShouldBe(0);
        payment.NextActionAt.ShouldBe(time.GetUtcNow() + TimeSpan.FromSeconds(2));
        (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem().HttpStatus.ShouldBe(429);
        await FinishAsync(id);
    }

    [Fact]
    public async Task RunPass_CircuitOpen_PostponesWithNoCallRecord()
    {
        var id = await RecordPaymentAsync("0508");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => new SubmitPaymentResponse(
                new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: TimeSpan.FromSeconds(30)),
                null,
                null,
                null,
                time.GetUtcNow(),
                TimeSpan.Zero));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.NextActionAt.ShouldBe(time.GetUtcNow() + TimeSpan.FromSeconds(30));
        (await GetAcmeInteractionsAsync(id)).ShouldBeEmpty();
        await FinishAsync(id);
    }

    [Fact]
    public async Task RunPass_QueryNotFound_MakesThePaymentDueToSendAgain()
    {
        var id = await RecordPaymentAsync("0509", AnswerSendTimedOut);
        time.Advance(QueryGap);
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(new AcmeAnswer(AcmeAnswerType.NotFound), 404));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.NextActionAt.ShouldBe(time.GetUtcNow());
        var acmeInteraction = (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem();
        acmeInteraction.Operation.ShouldBe(AcmeOperation.Query);
        acmeInteraction.MessageId.ShouldBeNull();
        acmeInteraction.HttpStatus.ShouldBe(404);
        JsonNode.Parse(acmeInteraction.Request)!["TransactionId"]!.GetValue<Guid>().ShouldBe(id);
        await acme.DidNotReceive().SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>());
        await FinishAsync(id);
    }

    [Fact]
    public async Task RunPass_QueryNotFoundAfterThreeSends_SavesFailedAsNotSubmitted()
    {
        var id = await RecordPaymentAsync("0510", AnswerThreeSendsNeverReceived);
        time.Advance(QueryGap);
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(new AcmeAnswer(AcmeAnswerType.NotFound), 404));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBeNull();
        payment.ClientMessage.ShouldBeNull();
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunPass_QueryRefused_SavesUnresolved()
    {
        var id = await RecordPaymentAsync("0511", AnswerSendTimedOut);
        time.Advance(QueryGap);
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(new AcmeAnswer(AcmeAnswerType.Refused, "Application", "E02", "API Key not valid", "We could not process that payment."), 401));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunPass_NoFinalAnswerAfter24Hours_SavesUnresolvedWithoutACall()
    {
        var id = await RecordPaymentAsync("0512", AnswerSendTimedOut);
        time.Advance(TimeSpan.FromHours(24));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
        await acme.DidNotReceive().GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>());
        (await GetChangesAsync(id)).Last().Source.ShouldBe(PaymentChangeSource.Worker);
    }

    [Fact]
    public async Task RunPass_TwoPassesAtTheSameMoment_SendEachPaymentOnce()
    {
        string[] numbers = ["0513", "0514", "0515"];
        var ids = new List<Guid>();
        foreach (var number in numbers)
        {
            ids.Add(await RecordPaymentAsync(number));
        }

        var sends = new ConcurrentQueue<Guid>();
        acme.SubmitPaymentAsync(Arg.Is<SubmitPaymentRequest>(request => ids.Contains(request.TransactionId)), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue(call.Arg<SubmitPaymentRequest>().TransactionId));

        await Task.WhenAll(
            worker.RunPassAsync(TestContext.Current.CancellationToken),
            worker.RunPassAsync(TestContext.Current.CancellationToken));

        sends.Order().ShouldBe(ids.Order());
        foreach (var id in ids)
        {
            var payment = await GetPaymentAsync(id);
            payment.State.ShouldBe(PaymentState.Approved);
            payment.AttemptCount.ShouldBe(1);
        }
    }

    [Fact]
    public async Task RunPass_LeaseLapsed_ClaimsThePaymentAgain()
    {
        var id = await RecordPaymentAsync("0516");
        var firstAnswer = new TaskCompletionSource<SubmitPaymentResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendsStarted = new[] { NewSignal(), NewSignal() };
        var sends = new ConcurrentQueue<SubmitPaymentRequest>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => firstAnswer.Task, _ => Task.FromResult(SendAnswered(Approved(), 200)))
            .AndDoes(call =>
            {
                sends.Enqueue(call.Arg<SubmitPaymentRequest>());
                sendsStarted[sends.Count - 1].SetResult();
            });

        var firstPass = worker.RunPassAsync(TestContext.Current.CancellationToken);
        await sendsStarted[0].Task;
        time.Advance(TimeSpan.FromSeconds(61));
        var secondPass = worker.RunPassAsync(TestContext.Current.CancellationToken);
        await sendsStarted[1].Task;

        (await GetPaymentAsync(id)).AttemptCount.ShouldBe(2);

        firstAnswer.SetResult(SendAnswered(Approved(), 200));
        await Task.WhenAll(firstPass, secondPass);

        sends.Select(sent => (sent.TransactionId, sent.OrderNumber)).Distinct().ShouldHaveSingleItem();
        (await GetPaymentAsync(id)).State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RunPass_ChangedByAnotherWriterDuringTheCall_ReloadsAndSavesTheAnswer()
    {
        var id = await RecordPaymentAsync("0517");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                // another writer saves the payment while the call is in flight
                await database.Payments
                    .Where(payment => payment.Id == id)
                    .ExecuteUpdateAsync(payment => payment.SetProperty(row => row.AttemptCount, 7), CancellationToken.None);

                return SendAnswered(Approved(), 200);
            });

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Approved);
        payment.AttemptCount.ShouldBe(7);
        (await GetAcmeInteractionsAsync(id)).ShouldHaveSingleItem();
        (await GetChangesAsync(id)).ShouldHaveSingleItem().ToState.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RunPass_EveryPlaceTaken_ClaimsOnlyTheFreePlaces()
    {
        string[] numbers = ["0518", "0519", "0520", "0521"];
        var ids = new List<Guid>();
        foreach (var number in numbers)
        {
            ids.Add(await RecordPaymentAsync(number));
        }

        var answers = new TaskCompletionSource<SubmitPaymentResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var placesTaken = NewSignal();
        var sendCount = 0;
        acme.SubmitPaymentAsync(Arg.Is<SubmitPaymentRequest>(request => ids.Contains(request.TransactionId)), Arg.Any<CancellationToken>())
            .Returns(_ => answers.Task)
            .AndDoes(_ =>
            {
                if (Interlocked.Increment(ref sendCount) == PaymentSubmissionWorkerFixture.MaxConcurrentCalls)
                {
                    placesTaken.SetResult();
                }
            });

        var pass = worker.RunPassAsync(TestContext.Current.CancellationToken);
        await placesTaken.Task;

        var attemptCounts = new List<int>();
        foreach (var id in ids)
        {
            attemptCounts.Add((await GetPaymentAsync(id)).AttemptCount);
        }

        answers.SetResult(SendAnswered(Approved(), 200));
        await pass;
        attemptCounts.Order().ShouldBe(new[] { 0, 1, 1, 1 });

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        foreach (var id in ids)
        {
            (await GetPaymentAsync(id)).State.ShouldBe(PaymentState.Approved);
        }
    }

    [Fact]
    public async Task RunPass_FifthUnexpectedFailure_QueriesAndFailsWhatAcmeDoesNotHold()
    {
        var id = await RecordPaymentAsync("0522");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The send failed in a way nobody planned for."));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(new AcmeAnswer(AcmeAnswerType.NotFound), 404));

        TimeSpan[] delays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15)];
        for (var failure = 0; failure < delays.Length; failure++)
        {
            await worker.RunPassAsync(TestContext.Current.CancellationToken);

            var failing = await GetPaymentAsync(id);
            failing.UnexpectedFailureCount.ShouldBe(failure + 1);
            failing.NextActionAt.ShouldBe(time.GetUtcNow() + delays[failure]);
            time.Advance(delays[failure]);
        }

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var afterFifth = await GetPaymentAsync(id);
        afterFifth.UnexpectedFailureCount.ShouldBe(5);
        afterFifth.NextActionAt.ShouldBe(time.GetUtcNow());

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.UnexpectedFailureCount.ShouldBe(0);
        payment.NextActionAt.ShouldBeNull();
        await acme.Received(5).SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>());
        await acme.Received(1).GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunPass_SixthUnexpectedFailure_SavesUnresolved()
    {
        var id = await RecordPaymentAsync("0523");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The send failed in a way nobody planned for."));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The query failed in a way nobody planned for."));

        for (var failure = 0; failure < 6; failure++)
        {
            await worker.RunPassAsync(TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromMinutes(15));
        }

        (await GetPaymentAsync(id)).UnexpectedFailureCount.ShouldBe(6);

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
        await acme.Received(1).GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunPass_SendApprovedWithHeldReversal_SavesTheReversalApplied()
    {
        var id = await RecordPaymentAsync("0540", (payment, clock) => HoldTransactionStatus(payment, "ZAR", clock));
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Reversed);
        payment.ReversedAmount.ShouldBe(1999);
        var paymentEvent = await database.PaymentEvents.AsNoTracking().SingleAsync(
            candidate => candidate.PaymentId == id, TestContext.Current.CancellationToken);
        paymentEvent.State.ShouldBe(PaymentEventState.Applied);
        (await GetChangesAsync(id)).Select(change => (change.ToState, change.Source)).ShouldBe(
            [(PaymentState.Approved, PaymentChangeSource.Send), (PaymentState.Reversed, PaymentChangeSource.TransactionStatus)],
            ignoreOrder: true);
    }

    [Fact]
    public async Task RunPass_QueryApprovedWithHeldAdvice_SavesItsConfirmation()
    {
        var id = await RecordPaymentAsync("0541", (payment, clock) =>
        {
            AnswerSendTimedOut(payment, clock);
            HoldPaymentAdvice(payment, clock);
        });
        time.Advance(QueryGap);
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200));

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Approved);
        var notification = await database.Notifications.AsNoTracking().SingleAsync(
            candidate => candidate.PaymentId == id, TestContext.Current.CancellationToken);
        notification.Outcome.ShouldBe(NotificationOutcome.Held);
        // the answer and the confirmation are made at the same moment, so the confirmation is found by its source
        var confirmation = (await GetChangesAsync(id)).Single(change => change.Source == PaymentChangeSource.PaymentAdvice);
        (confirmation.FromState, confirmation.ToState, confirmation.Source, confirmation.NotificationId).ShouldBe(
            (PaymentState.Approved, PaymentState.Approved, PaymentChangeSource.PaymentAdvice, notification.Id));
    }

    [Fact]
    public async Task RunPass_MadeUnresolvedByNotification_StopsWithoutACall()
    {
        var id = await RecordPaymentAsync("0542", (payment, clock) =>
        {
            AnswerSendTimedOut(payment, clock);
            HoldTransactionStatus(payment, "USD", clock);
        });
        time.Advance(QueryGap);

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var payment = await GetPaymentAsync(id);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
        (await GetAcmeInteractionsAsync(id)).ShouldBeEmpty();
        await acme.DidNotReceive().GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>());
    }

    // a reversal Acme Switch sent before the outcome was known; one in another currency makes the payment Unresolved
    private static void HoldTransactionStatus(PaymentEntity payment, string currencyCode, TimeProvider time)
    {
        var status = new AcmeTransactionStatus(
            $"EVT-{payment.AppPaymentReference}", "Reversal", time.GetUtcNow(), payment.OrderNumber, 1999, currencyCode, "00", true);
        var notification = new NotificationEntity(
            NotificationSource.TransactionStatus,
            status.TransactionUuid,
            null,
            SHA256.HashData(Encoding.UTF8.GetBytes(status.GetContent())),
            "{}",
            null,
            null,
            "4bf92f3577b34da6a3ce929d0e0e4736",
            time);
        payment.RecordTransactionStatus(notification, status, [], time);
    }

    // a Payment Advice Acme sent before the worker's answer, which holds it
    private static void HoldPaymentAdvice(PaymentEntity payment, TimeProvider time)
    {
        var advice = new AcmePaymentAdvice(
            "Payment",
            AccountMerchantId,
            payment.Id.ToString(),
            time.GetUtcNow(),
            payment.OrderNumber,
            "MR747891457",
            payment.OrderNumber,
            "7543",
            payment.Amount,
            "ZAR",
            "CardNotPresent",
            "Success",
            "Acquirer",
            "00",
            "Approved",
            "238699",
            "REST");
        var notification = new NotificationEntity(
            NotificationSource.PaymentAdvice,
            advice.TransactionId,
            advice.RequestType,
            SHA256.HashData(Encoding.UTF8.GetBytes(advice.GetContent())),
            "{}",
            advice.TransactionStatus,
            advice.ResponseCode,
            "4bf92f3577b34da6a3ce929d0e0e4736",
            time);
        payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);
    }

    private static SubmitPaymentRequest SentFor(Guid id) =>
        Arg.Is<SubmitPaymentRequest>(request => request.TransactionId == id);

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AnswerSendTimedOut(PaymentEntity payment, TimeProvider time) =>
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, time);

    // three sends, each followed by a query that found nothing, the last not yet asked about
    private static void AnswerThreeSendsNeverReceived(PaymentEntity payment, TimeProvider time)
    {
        AnswerSendTimedOut(payment, time);
        payment.RecordQueryAnswer(new AcmeAnswer(AcmeAnswerType.NotFound), null, time);
        AnswerSendTimedOut(payment, time);
        payment.RecordQueryAnswer(new AcmeAnswer(AcmeAnswerType.NotFound), null, time);
        AnswerSendTimedOut(payment, time);
    }

    private AcmeAnswer Approved() =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "00",
            "Approved or completed successfully",
            "Approved.",
            "238699",
            1999,
            "MR747891457",
            "MR43851259",
            time.GetUtcNow());

    private AcmeAnswer Declined() =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "51",
            "Insufficient funds",
            "There was not enough money in the account.",
            acmeTransactionAt: time.GetUtcNow());

    private SubmitPaymentResponse SendAnswered(AcmeAnswer answer, int httpStatus) =>
        new(answer, AcmeInteractionOutcome.Answered, httpStatus, "{}", time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private SubmitPaymentResponse SendTimedOut() =>
        new(new AcmeAnswer(AcmeAnswerType.TimedOut), AcmeInteractionOutcome.TimedOut, null, null, time.GetUtcNow(), TimeSpan.FromSeconds(35));

    private GetTransactionByTransactionIdResponse QueryAnswered(AcmeAnswer answer, int httpStatus) =>
        new(answer, AcmeInteractionOutcome.Answered, httpStatus, "{}", time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private async Task<Guid> RecordPaymentAsync(string number, Action<PaymentEntity, TimeProvider>? arrange = null)
    {
        var customerReference = $"CUS-{number}";
        var storedCard = new StoredCardEntity(
            customerReference,
            new CryptographyManager().Encrypt($"test-token-{number}", tokenKey, Encoding.UTF8.GetBytes(customerReference)),
            $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, $"APP-{number}", time);
        arrange?.Invoke(payment, time);
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();

        return payment.Id;
    }

    // a payment left due would be claimed by a later test's pass, so it is given a final answer
    private async Task FinishAsync(Guid id)
    {
        var payment = await database.Payments.AsTracking().SingleAsync(
            candidate => candidate.Id == id, TestContext.Current.CancellationToken);
        payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.Failure, "Acquirer", "91", "Issuer or switch inoperative", "The bank could not be reached."),
            null,
            time);
        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();
    }

    private Task<PaymentEntity> GetPaymentAsync(Guid id) =>
        database.Payments.AsNoTracking().SingleAsync(candidate => candidate.Id == id, TestContext.Current.CancellationToken);

    private Task<List<AcmeInteractionEntity>> GetAcmeInteractionsAsync(Guid id) =>
        database.AcmeInteractions.AsNoTracking()
            .Where(acmeInteraction => acmeInteraction.PaymentId == id)
            .OrderBy(acmeInteraction => acmeInteraction.StartedAt)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<PaymentChangeEntity>> GetChangesAsync(Guid id) =>
        database.PaymentChanges.AsNoTracking()
            .Where(change => change.PaymentId == id)
            .OrderBy(change => change.ChangedAt)
            .ToListAsync(TestContext.Current.CancellationToken);
}
