using System.Security.Cryptography;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Zapper.IntegrationTests;

public sealed class AcceptanceApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string connectionString;

    private readonly FakeTimeProvider time;

    private readonly IAcmeService acme;

    private readonly byte[] tokenKey;

    public AcceptanceApiFactory(string connectionString, FakeTimeProvider time, IAcmeService acme, byte[] tokenKey)
    {
        this.connectionString = connectionString;
        this.time = time;
        this.acme = acme;
        this.tokenKey = tokenKey;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Zapper:Database:ConnectionString", connectionString);

        // the worker is on, so the startup check reads Acme's settings and the card key; the client is substituted,
        // so the settings are made for the run and no call leaves the process
        builder.UseSetting("Zapper:PaymentSubmissionWorker:IsEnabled", "true");
        builder.UseSetting("Zapper:TokenEncryption:Key", Convert.ToBase64String(tokenKey));
        builder.UseSetting("Zapper:Acme:BaseAddress", "https://acme.test");
        builder.UseSetting("Zapper:Acme:MerchantId", Guid.NewGuid().ToString());
        builder.UseSetting("Zapper:Acme:ApiKey", $"sk_test_{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24))}");
        builder.UseSetting("Zapper:Acme:RequestsPerSecond", "20");
        builder.UseSetting("Zapper:Acme:MaxConcurrentCalls", "50");

        // a test reads the payment once for each step of the clock, all from one address, so it would pass the
        // rate limit that guards recording; no acceptance test is about that limit
        builder.UseSetting("Zapper:Security:RateLimit", "1000000");
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
        services.AddSingleton<TimeProvider>(time);
        services.RemoveAll<IAcmeService>();
        services.AddSingleton(acme);
    }

    private ZapperDbContext GetContext(IServiceProvider provider)
    {
        var database = new ZapperDbContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }
}
