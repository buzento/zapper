using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.ApiCommon.Managers.ErrorMessage;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Zapper.PaymentsApi.Workers.PaymentSubmission;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Zapper.PaymentsApi;

public sealed class StartupHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;

    private readonly IErrorMessageManager errorMessageManager;

    private readonly IOptions<PaymentSubmissionWorkerOptions> paymentSubmissionWorker;

    private readonly IOptions<AcmeOptions> acme;

    private readonly IOptions<TokenEncryptionOptions> tokenEncryption;

    private readonly ILogger<StartupHostedService> logger;

    public StartupHostedService(
        IServiceScopeFactory scopeFactory,
        IErrorMessageManager errorMessageManager,
        IOptions<PaymentSubmissionWorkerOptions> paymentSubmissionWorker,
        IOptions<AcmeOptions> acme,
        IOptions<TokenEncryptionOptions> tokenEncryption,
        ILogger<StartupHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.errorMessageManager = errorMessageManager;
        this.paymentSubmissionWorker = paymentSubmissionWorker;
        this.acme = acme;
        this.tokenEncryption = tokenEncryption;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ZapperDbContext>();
        var pendingMigrations =
            (await database.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            logger.LogCritical(
                "The database is missing migrations this API holds: {PendingMigrationCount}",
                pendingMigrations.Count);

            throw new InvalidOperationException(
                $"The database is missing migrations this API holds: {pendingMigrations.Count}.");
        }

        var statuses = await database.ErrorStatuses.ToListAsync(cancellationToken);
        var messages = await database.ErrorMessages.ToListAsync(cancellationToken);

        errorMessageManager.Populate(statuses, messages);

        // reading the options runs their validation, so a host with the worker on refuses to start without
        // Acme's settings or the card key, and a host with it off never holds either secret, D-4
        if (paymentSubmissionWorker.Value.IsEnabled is true)
        {
            _ = acme.Value;
            _ = tokenEncryption.Value;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
