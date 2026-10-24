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

    public AcceptanceApiFactory(string connectionString, FakeTimeProvider time, IAcmeService acme)
    {
        this.connectionString = connectionString;
        this.time = time;
        this.acme = acme;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Zapper:Database:ConnectionString", connectionString);
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
