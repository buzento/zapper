using Zapper.ApiCommon.Managers.ErrorMessage;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Zapper.PaymentsApi;

public sealed class StartupHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;

    private readonly IErrorMessageManager errorMessageManager;

    private readonly ILogger<StartupHostedService> logger;

    public StartupHostedService(
        IServiceScopeFactory scopeFactory,
        IErrorMessageManager errorMessageManager,
        ILogger<StartupHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.errorMessageManager = errorMessageManager;
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
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
