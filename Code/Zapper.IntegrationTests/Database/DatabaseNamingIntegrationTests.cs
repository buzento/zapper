using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class DatabaseNamingIntegrationTests : IAsyncDisposable
{
    private readonly ZapperDbContext database = new ZapperDbContextDesignTimeFactory().CreateDbContext([]);

    [Fact]
    public void Tables_EveryTable_IsNamedForItsEntity()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(entity => entity.GetTableName() != entity.ClrType.Name[..^"Entity".Length])
            .Select(entity => $"{entity.ClrType.Name}: {entity.GetTableName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Schemas_EveryTable_HasNamedSchemaOtherThanPublic()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(entity => entity.GetSchema() is null or "public")
            .Select(entity => $"{entity.GetTableName()}: {entity.GetSchema()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void PrimaryKeys_EveryKeyColumn_IsNamedId()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetKeys().Where(key => key.IsPrimaryKey()))
            .SelectMany(key => key.Properties
                .Where(column => column.GetColumnName() != "Id")
                .Select(column => $"{key.DeclaringEntityType.GetTableName()}: {column.GetColumnName()}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void PrimaryKeys_EveryKey_FollowsPkPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetKeys().Where(key => key.IsPrimaryKey()))
            .Where(key => key.GetName() != $"PK_{key.DeclaringEntityType.GetTableName()}")
            .Select(key => $"{key.DeclaringEntityType.GetTableName()}: {key.GetName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ForeignKeys_EveryKey_FollowsFkPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.GetConstraintName()
                != $"FK_{foreignKey.DeclaringEntityType.GetTableName()}_{GetColumns(foreignKey.Properties)}")
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}: {foreignKey.GetConstraintName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ForeignKeys_EveryColumn_IsNamedReferencedTableId()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => GetColumns(foreignKey.Properties)
                != $"{foreignKey.PrincipalEntityType.GetTableName()}Id")
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}: {GetColumns(foreignKey.Properties)}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Indexes_EveryIndex_FollowsIxPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Where(index => !index.IsUnique))
            .Where(index => index.GetDatabaseName()
                != $"IX_{index.DeclaringEntityType.GetTableName()}_{GetColumns(index.Properties)}")
            .Select(index => $"{index.DeclaringEntityType.GetTableName()}: {index.GetDatabaseName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Indexes_EveryUniqueIndex_FollowsUxPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Where(index => index.IsUnique))
            .Where(index => index.GetDatabaseName()
                != $"UX_{index.DeclaringEntityType.GetTableName()}_{GetColumns(index.Properties)}")
            .Select(index => $"{index.DeclaringEntityType.GetTableName()}: {index.GetDatabaseName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    private static string GetColumns(IEnumerable<IProperty> properties) =>
        string.Join("_", properties.Select(property => property.GetColumnName()));
}
