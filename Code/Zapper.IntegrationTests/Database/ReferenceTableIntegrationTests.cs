using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class ReferenceTableIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private readonly ZapperDbContext database;

    public ReferenceTableIntegrationTests(ClonedDatabaseFixture clonedDatabase) =>
        database = clonedDatabase.Database;

    [Fact]
    public async Task ReferenceTables_EveryEnumMember_HasRow()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => Enum.GetValues(GetEnum(table.Key)).Cast<object>()
                .Where(member => !table.Value.Select(row => GetColumn(row, "Id")).Contains(member))
                .Select(member => $"{table.Key.GetTableName()}: {member}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReferenceTables_EveryRow_HasEnumMember()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => table.Value
                .Where(row => !Enum.IsDefined(GetEnum(table.Key), GetColumn(row, "Id")))
                .Select(row => $"{table.Key.GetTableName()}: {GetColumn(row, "Id")}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReferenceTables_EveryRowName_IsEnumMemberName()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => table.Value
                .Where(row => (string)GetColumn(row, "Name") != Enum.GetName(GetEnum(table.Key), GetColumn(row, "Id")))
                .Select(row => $"{table.Key.GetTableName()}: {GetColumn(row, "Name")}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ReferenceTables_EveryTable_IsNamedForEnumPlusType()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(IsReferenceTable)
            .Where(table => table.GetTableName() != $"{GetEnum(table).Name}Type")
            .Select(table => $"{GetEnum(table).Name}: {table.GetTableName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static bool IsReferenceTable(IEntityType table) =>
        table.FindPrimaryKey() is { Properties: [{ ClrType.IsEnum: true }] };

    // IsReferenceTable has already selected a table whose one key column is its enum.
    private static Type GetEnum(IEntityType table) => table.FindPrimaryKey()!.Properties[0].ClrType;

    // BackendCode.md section 13 gives every reference table an Id and a Name.
    private static object GetColumn(object row, string column) => row.GetType().GetProperty(column)!.GetValue(row)!;

    private async Task<Dictionary<IEntityType, List<object>>> GetReferenceRowsAsync()
    {
        var rows = new Dictionary<IEntityType, List<object>>();

        foreach (var table in database.Model.GetEntityTypes().Where(IsReferenceTable))
        {
            // DbContext.Set<T>() returns a set for every mapped entity and never null.
            var set = (IQueryable<object>)typeof(DbContext)
                .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(table.ClrType)
                .Invoke(database, null)!;

            rows[table] = await set.ToListAsync(TestContext.Current.CancellationToken);
        }

        return rows;
    }
}
