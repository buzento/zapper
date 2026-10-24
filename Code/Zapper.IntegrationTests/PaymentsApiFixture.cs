extern alias PaymentsApi;

using System.Security.Cryptography;
using Zapper.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Zapper.IntegrationTests;

public sealed class PaymentsApiFixture : WebApplicationFactory<PaymentsApi::Program>, IAsyncLifetime
{
    private readonly ClonedDatabaseFixture clonedDatabase;

    private readonly string connectionString;

    public PaymentsApiFixture(TemplateDatabaseFixture templateDatabase)
    {
        clonedDatabase = new ClonedDatabaseFixture(templateDatabase);

        // the clone's context was given its connection string when it was made, so it holds one
        connectionString = clonedDatabase.Database.Database.GetConnectionString()!;
    }

    public ZapperDbContext Database => clonedDatabase.Database;

    // Acme's notifications are on, with an account and credentials made for the run
    public string AcmeMerchantId { get; } = Guid.NewGuid().ToString();

    public string PaymentAdviceUserName { get; } = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public string PaymentAdvicePassword { get; } = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public string WebhookSecret { get; } = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public async ValueTask InitializeAsync() => await clonedDatabase.InitializeAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await clonedDatabase.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Zapper:Database:ConnectionString", connectionString);

        // an operation's tests read back what they stored, so no worker changes a payment under them
        builder.UseSetting("Zapper:PaymentSubmissionWorker:IsEnabled", "false");
        builder.UseSetting("Zapper:AcmeNotification:IsEnabled", "true");
        builder.UseSetting("Zapper:AcmeNotification:MerchantId", AcmeMerchantId);
        builder.UseSetting("Zapper:AcmeNotification:PaymentAdviceUserName", PaymentAdviceUserName);
        builder.UseSetting("Zapper:AcmeNotification:PaymentAdvicePassword", PaymentAdvicePassword);
        builder.UseSetting("Zapper:AcmeNotification:WebhookSecret", WebhookSecret);
        builder.ConfigureTestServices(ReplaceDatabase);
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseValidateOptions)));
        services.RemoveAll<ZapperDbContext>();
        services.AddScoped(GetContext);
    }

    private ZapperDbContext GetContext(IServiceProvider provider)
    {
        var database = new ZapperDbContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }
}
