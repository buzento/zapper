extern alias PaymentsApi;
using System.Security.Cryptography;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Zapper.IntegrationTests;

public sealed class AcceptanceFixture : IAsyncLifetime
{
    // how long, in real time, the worker is given to take a payment off the due list after the clock moves
    private const int CatchUpTickCount = 200;

    private static readonly TimeSpan CatchUpTick = TimeSpan.FromMilliseconds(10);

    private readonly ClonedDatabaseFixture clonedDatabase;

    private readonly string connectionString;

    private readonly List<IAsyncDisposable> hosts = [];

    private readonly Dictionary<string, HttpClient> clients = [];

    public AcceptanceFixture(TemplateDatabaseFixture templateDatabase)
    {
        clonedDatabase = new ClonedDatabaseFixture(templateDatabase);

        // the clone's context was given its connection string when it was made, so it holds one
        connectionString = clonedDatabase.Database.Database.GetConnectionString()!;
    }

    public ZapperDbContext Database => clonedDatabase.Database;

    public FakeTimeProvider Time { get; } = new();

    public IAcmeService Acme { get; } = Substitute.For<IAcmeService>();

    public byte[] TokenKey { get; } = RandomNumberGenerator.GetBytes(32);

    public async ValueTask InitializeAsync()
    {
        await clonedDatabase.InitializeAsync();

        AnswerUnarrangedPayments();
        StartApi<PaymentsApi::Program>("Zapper.PaymentsApi");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var host in hosts)
        {
            await host.DisposeAsync();
        }

        await clonedDatabase.DisposeAsync();
    }

    public HttpClient GetClient(string api) => clients[api];

    public Task MoveClockAsync(Guid paymentId, TimeSpan step, TimeSpan distance) =>
        MoveClockUntilAsync(paymentId, step, distance, () => Task.FromResult(false));

    // the worker runs apart from the test, so after each step the clock waits until the worker has taken the
    // payment off the due list; a worker that does not stops the clock, and the test's Then reports what it finds
    public async Task MoveClockUntilAsync(Guid paymentId, TimeSpan step, TimeSpan limit, Func<Task<bool>> isReached)
    {
        var moved = TimeSpan.Zero;

        while (moved < limit && !await isReached())
        {
            Time.Advance(step);
            moved += step;

            if (!await WaitUntilNotDueAsync(paymentId))
            {
                return;
            }
        }
    }

    private async Task<bool> WaitUntilNotDueAsync(Guid paymentId)
    {
        using var timer = new PeriodicTimer(CatchUpTick);

        for (var tick = 0; tick < CatchUpTickCount; tick++)
        {
            if (await IsNotDueAsync(paymentId))
            {
                return true;
            }

            await timer.WaitForNextTickAsync(TestContext.Current.CancellationToken);
        }

        return false;
    }

    private Task<bool> IsNotDueAsync(Guid paymentId)
    {
        var now = Time.GetUtcNow();

        return Database.Payments.AnyAsync(
            payment => payment.Id == paymentId && (payment.NextActionAt == null || payment.NextActionAt > now),
            TestContext.Current.CancellationToken);
    }

    // a payment no test arranged, such as one another Feature's test recorded, times out and is never found,
    // so it ends Failed rather than leaving the stand-in to answer nothing
    private void AnswerUnarrangedPayments()
    {
        Acme.SubmitPaymentAsync(Arg.Any<SubmitPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => new SubmitPaymentResponse(
                new AcmeAnswer(AcmeAnswerType.TimedOut),
                AcmeInteractionOutcome.TimedOut,
                null,
                null,
                Time.GetUtcNow(),
                TimeSpan.FromSeconds(35)));

        Acme.GetTransactionByTransactionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new GetTransactionByTransactionIdResponse(
                new AcmeAnswer(AcmeAnswerType.NotFound),
                AcmeInteractionOutcome.Answered,
                404,
                null,
                Time.GetUtcNow(),
                TimeSpan.FromMilliseconds(400)));
    }

    private void StartApi<TProgram>(string api)
        where TProgram : class
    {
        var factory = new AcceptanceApiFactory<TProgram>(connectionString, Time, Acme);
        factory.UseKestrel(0);
        factory.StartServer();

        hosts.Add(factory);
        clients[api] = factory.CreateClient();
    }
}
