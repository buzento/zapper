extern alias PaymentsApi;

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

    public async ValueTask InitializeAsync() => await clonedDatabase.InitializeAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await clonedDatabase.DisposeAsync();
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
