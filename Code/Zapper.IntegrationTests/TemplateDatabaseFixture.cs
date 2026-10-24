using Zapper.Infrastructure.Persistence;
using Zapper.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(TemplateDatabaseFixture))]

namespace Zapper.IntegrationTests;

public sealed class TemplateDatabaseFixture : IAsyncLifetime
{
    private const string TemplateDatabaseName = "ZapperDbTemplate";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.6").Build();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync(TestContext.Current.CancellationToken);

        await ExecuteAsync("CREATE ROLE \"ZapperPaymentsApiRole\"");
        await ExecuteAsync("CREATE ROLE \"ZapperDataReaderRole\"");
        var connectionString = GetConnectionString(TemplateDatabaseName);
        connectionString.Pooling = false;

        await using var database = GetContext(connectionString);

        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    public ZapperDbContext GetContext(string databaseName) =>
        GetContext(GetConnectionString(databaseName));

    public Task CloneTemplateAsync(string databaseName) =>
        ExecuteAsync($"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{TemplateDatabaseName}\"");

    public Task DropDatabaseAsync(string databaseName) =>
        ExecuteAsync($"DROP DATABASE \"{databaseName}\" WITH (FORCE)");

    private static ZapperDbContext GetContext(NpgsqlConnectionStringBuilder connectionString)
    {
        var database = new ZapperDbContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString.ConnectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }

    private NpgsqlConnectionStringBuilder GetConnectionString(string databaseName) =>
        new(container.GetConnectionString()) { Database = databaseName };

    private async Task ExecuteAsync(string statement)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand(statement, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
