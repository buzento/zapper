using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zapper.Infrastructure.Persistence;

public sealed class ZapperDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ZapperDbContext>
{
    public ZapperDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ZapperDbContext>()
            .UseNpgsql(PersistenceRegistration.SetMigrationHistoryTable)
            .Options;

        return new ZapperDbContext(options);
    }
}
