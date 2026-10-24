extern alias PaymentsApi;

using System.Security.Cryptography;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PaymentsApi::Zapper.PaymentsApi.Workers.PaymentSubmission;
using Xunit;

namespace Zapper.IntegrationTests;

// the worker's loop is off, so each test runs one polling pass at a time, Testing.md section 5
public sealed class PaymentSubmissionWorkerFixture : WebApplicationFactory<PaymentsApi::Program>, IAsyncLifetime
{
    // small, so a test can fill every place the worker has
    public const int MaxConcurrentCalls = 3;

    private readonly ClonedDatabaseFixture clonedDatabase;

    private readonly string connectionString;

    public PaymentSubmissionWorkerFixture(TemplateDatabaseFixture templateDatabase)
    {
        clonedDatabase = new ClonedDatabaseFixture(templateDatabase);

        // the clone's context was given its connection string when it was made, so it holds one
        connectionString = clonedDatabase.Database.Database.GetConnectionString()!;
    }

    public ZapperDbContext Database => clonedDatabase.Database;

    public FakeTimeProvider Time { get; } = new();

    public IAcmeService Acme { get; } = Substitute.For<IAcmeService>();

    public byte[] TokenKey { get; } = RandomNumberGenerator.GetBytes(32);

    // notifications are on, so a test can send one through the API while the worker's call is in flight
    public string WebhookSecret { get; } = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public PaymentSubmissionWorker Worker =>
        Services.GetServices<IHostedService>().OfType<PaymentSubmissionWorker>().Single();

    public async ValueTask InitializeAsync() => await clonedDatabase.InitializeAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await clonedDatabase.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Zapper:Database:ConnectionString", connectionString);
        builder.UseSetting("Zapper:PaymentSubmissionWorker:IsEnabled", "false");
        builder.UseSetting("Zapper:AcmeNotification:IsEnabled", "true");
        builder.UseSetting("Zapper:AcmeNotification:MerchantId", Guid.NewGuid().ToString());
        builder.UseSetting("Zapper:AcmeNotification:PaymentAdviceUserName", Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Zapper:AcmeNotification:PaymentAdvicePassword", Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Zapper:AcmeNotification:WebhookSecret", WebhookSecret);

        // the client is substituted, so the settings are made for the run and no call leaves the process
        builder.UseSetting("Zapper:TokenEncryption:Key", Convert.ToBase64String(TokenKey));
        builder.UseSetting("Zapper:Acme:BaseAddress", "https://acme.test");
        builder.UseSetting("Zapper:Acme:MerchantId", Guid.NewGuid().ToString());
        builder.UseSetting("Zapper:Acme:ApiKey", $"sk_test_{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24))}");
        builder.UseSetting("Zapper:Acme:RequestsPerSecond", "20");
        builder.UseSetting("Zapper:Acme:MaxConcurrentCalls", MaxConcurrentCalls.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureTestServices(ReplaceDatabase);
        builder.ConfigureTestServices(ReplaceTimeAndAcme);
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseValidateOptions)));
        services.RemoveAll<ZapperDbContext>();
        services.AddScoped(GetContext);
    }

    private void ReplaceTimeAndAcme(IServiceCollection services)
    {
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(Time);
        services.RemoveAll<IAcmeService>();
        services.AddSingleton(Acme);
    }

    private ZapperDbContext GetContext(IServiceProvider provider)
    {
        var database = new ZapperDbContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }
}
