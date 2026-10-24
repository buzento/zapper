using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
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

        // one data source, and so one connection pool, for the host; the container disposes it when the host stops
        services.AddSingleton(GetDataSource);
        services.AddDbContext<ZapperDbContext>(ConfigureContext);

        return services;
    }

    internal static void SetMigrationHistoryTable(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable("MigrationHistory", "Migration");

    private static NpgsqlDataSource GetDataSource(IServiceProvider provider) =>
        DatabaseDataSourceFactory.Get(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString);

    private static void ConfigureContext(IServiceProvider provider, DbContextOptionsBuilder options) =>
        options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>(), SetMigrationHistoryTable)
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
