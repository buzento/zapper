using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Zapper.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddZapperPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
                .BindConfiguration("Zapper:Database")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseValidateOptions>();

        services.AddDbContext<ZapperDbContext>(ConfigureContext);

        return services;
    }

    internal static void SetMigrationHistoryTable(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable("MigrationHistory", "Migration");

    private static void ConfigureContext(IServiceProvider provider, DbContextOptionsBuilder options)
    {
        var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        options.UseNpgsql(
                    DatabaseDataSourceFactory.Get(database.ConnectionString),
                    SetMigrationHistoryTable)
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
