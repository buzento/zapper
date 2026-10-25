using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

// the test container gives the reader no certificate, so the container's superuser, which ran the migrations, takes
// the reader's role, D-18
public sealed class DataReaderRoleIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private const string TakeDataReaderRole = "SET ROLE \"ZapperDataReaderRole\"";

    private readonly ZapperDbContext database;

    public DataReaderRoleIntegrationTests(ClonedDatabaseFixture clonedDatabase) =>
        database = clonedDatabase.Database;

    [Fact]
    public async Task ReadTables_EveryTableTheModelMaps_Succeeds()
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, TakeDataReaderRole);

        var read = () => ExecuteAsync(connection, GetReadOfEveryTable());

        await read.ShouldNotThrowAsync();
    }

    [Theory]
    [InlineData("INSERT INTO \"Payment\".\"Payment\" (\"Id\") VALUES ('0198f3c1-1a2b-7c3d-8e4f-5a6b7c8d9e0f')")]
    [InlineData("UPDATE \"Payment\".\"Payment\" SET \"Amount\" = \"Amount\" + 1")]
    [InlineData("DELETE FROM \"Payment\".\"Payment\"")]
    public async Task ChangePayment_AsDataReader_IsRefused(string change)
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, TakeDataReaderRole);

        var execute = () => ExecuteAsync(connection, change);

        (await execute.ShouldThrowAsync<PostgresException>()).SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
    }

    // a later plan's migration creates its schema and its tables as the role that ran these, and grants the reader
    // nothing
    [Fact]
    public async Task ReadTable_CreatedAfterMigrationsByTheirRole_Succeeds()
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "CREATE SCHEMA \"AddedLater\"");
        await ExecuteAsync(connection, "CREATE TABLE \"AddedLater\".\"AddedLater\" (\"Id\" uuid PRIMARY KEY)");
        await ExecuteAsync(connection, TakeDataReaderRole);

        var read = () => ExecuteAsync(connection, "SELECT count(*) FROM \"AddedLater\".\"AddedLater\"");

        await read.ShouldNotThrowAsync();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string statement)
    {
        await using var command = new NpgsqlCommand(statement, connection);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    // never pooled, so the reader's role never reaches the class's context
    private async Task<NpgsqlConnection> OpenAsync()
    {
        // the clone's context was given its connection string when it was made, so it holds one
        var connectionString = new NpgsqlConnectionStringBuilder(database.Database.GetConnectionString()!) { Pooling = false };
        var connection = new NpgsqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        return connection;
    }

    // one statement reading every table the model maps, so a table the reader cannot read fails it
    private string GetReadOfEveryTable() =>
        "SELECT " + string.Join(
            ", ",
            database.Model.GetEntityTypes()
                .Select(entity => $"(SELECT count(*) FROM \"{entity.GetSchema()}\".\"{entity.GetTableName()}\")"));
}
