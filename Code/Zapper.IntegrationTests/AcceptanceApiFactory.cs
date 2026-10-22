using Zapper.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Zapper.IntegrationTests;

public sealed class AcceptanceApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string connectionString;

    public AcceptanceApiFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Zapper:Database:ConnectionString", connectionString);
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
