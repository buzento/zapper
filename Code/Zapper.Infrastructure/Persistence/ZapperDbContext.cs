using Zapper.Domain.Error;
using Zapper.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Zapper.Infrastructure.Persistence;

public sealed class ZapperDbContext : DbContext
{
    public ZapperDbContext(DbContextOptions<ZapperDbContext> options)
        : base(options)
    {
    }

    public DbSet<ErrorStatusEntity> ErrorStatuses => Set<ErrorStatusEntity>();

    public DbSet<ErrorMessageEntity> ErrorMessages => Set<ErrorMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ErrorStatusConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorMessageConfiguration());
    }
}
