using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class MigrationIntegrationTests : IAsyncDisposable
{
    private const string MigrationTestDatabaseName = "ZapperDbMigrationTest";

    private readonly TemplateDatabaseFixture templateDatabase;

    private readonly ZapperDbContext database;

    public MigrationIntegrationTests(TemplateDatabaseFixture templateDatabase)
    {
        this.templateDatabase = templateDatabase;
        database = templateDatabase.GetContext(MigrationTestDatabaseName);
    }

    [Fact]
    public async Task Migrations_UpThenDownThenUp_DownLeavesNoTableAndUpAppliesEvery()
    {
        var migrator = database.GetService<IMigrator>();

        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await migrator.MigrateAsync(Migration.InitialDatabase, TestContext.Current.CancellationToken);
        var appliedAfterDown = await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        var tablesAfterDown = await GetStoredModelTablesAsync();
        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var appliedAfterUp = await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        appliedAfterDown.ShouldBeEmpty();
        tablesAfterDown.ShouldBeEmpty();
        appliedAfterUp.ShouldBe(database.Database.GetMigrations());
    }

    public async ValueTask DisposeAsync()
    {
        await database.DisposeAsync();
        await templateDatabase.DropDatabaseAsync(MigrationTestDatabaseName);
    }

    private async Task<List<string>> GetStoredModelTablesAsync()
    {
        var modelTables = database.Model.GetEntityTypes()
            .Select(entity => $"{entity.GetSchema()}.{entity.GetTableName()}")
            .ToList();

        var storedTables = await database.Database
            .SqlQuery<string>($"SELECT table_schema || '.' || table_name AS \"Value\" FROM information_schema.tables")
            .ToListAsync(TestContext.Current.CancellationToken);

        return storedTables.Intersect(modelTables).ToList();
    }
}
