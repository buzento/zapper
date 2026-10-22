extern alias PaymentsApi;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Zapper.IntegrationTests;

public sealed class AcceptanceFixture : IAsyncLifetime
{
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

    public async ValueTask InitializeAsync()
    {
        await clonedDatabase.InitializeAsync();

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

    private void StartApi<TProgram>(string api)
        where TProgram : class
    {
        var factory = new AcceptanceApiFactory<TProgram>(connectionString);
        factory.UseKestrel(0);
        factory.StartServer();

        hosts.Add(factory);
        clients[api] = factory.CreateClient();
    }
}
