using Zapper.Infrastructure.Persistence;
using Xunit;

namespace Zapper.IntegrationTests;

public sealed class ClonedDatabaseFixture : IAsyncLifetime
{
    private readonly TemplateDatabaseFixture templateDatabase;

    private readonly string databaseName = Guid.CreateVersion7().ToString("N");

    public ClonedDatabaseFixture(TemplateDatabaseFixture templateDatabase)
    {
        this.templateDatabase = templateDatabase;
        Database = templateDatabase.GetContext(databaseName);
    }

    public ZapperDbContext Database { get; }

    public async ValueTask InitializeAsync() => await templateDatabase.CloneTemplateAsync(databaseName);

    public async ValueTask DisposeAsync()
    {
        await Database.DisposeAsync();
        await templateDatabase.DropDatabaseAsync(databaseName);
    }
}
