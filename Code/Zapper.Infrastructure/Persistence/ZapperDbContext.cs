using Zapper.Domain.Customer;
using Zapper.Domain.Error;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Zapper.Infrastructure.Persistence;

public sealed class ZapperDbContext : DbContext
{
    public ZapperDbContext(DbContextOptions<ZapperDbContext> options)
        : base(options)
    {
    }

    public DbSet<ErrorStatusEntity> ErrorStatuses => Set<ErrorStatusEntity>();

    public DbSet<ErrorMessageEntity> ErrorMessages => Set<ErrorMessageEntity>();

    public DbSet<SubMerchantEntity> SubMerchants => Set<SubMerchantEntity>();

    public DbSet<StoredCardEntity> StoredCards => Set<StoredCardEntity>();

    public DbSet<CurrencyTypeEntity> CurrencyTypes => Set<CurrencyTypeEntity>();

    public DbSet<PaymentStateTypeEntity> PaymentStateTypes => Set<PaymentStateTypeEntity>();

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    public DbSet<AcmeOperationTypeEntity> AcmeOperationTypes => Set<AcmeOperationTypeEntity>();

    public DbSet<AcmeInteractionOutcomeTypeEntity> AcmeInteractionOutcomeTypes => Set<AcmeInteractionOutcomeTypeEntity>();

    public DbSet<PaymentChangeSourceTypeEntity> PaymentChangeSourceTypes => Set<PaymentChangeSourceTypeEntity>();

    public DbSet<AcmeInteractionEntity> AcmeInteractions => Set<AcmeInteractionEntity>();

    public DbSet<PaymentChangeEntity> PaymentChanges => Set<PaymentChangeEntity>();

    public DbSet<NotificationSourceTypeEntity> NotificationSourceTypes => Set<NotificationSourceTypeEntity>();

    public DbSet<NotificationOutcomeTypeEntity> NotificationOutcomeTypes => Set<NotificationOutcomeTypeEntity>();

    public DbSet<PaymentEventTypeEntity> PaymentEventTypes => Set<PaymentEventTypeEntity>();

    public DbSet<PaymentEventStateTypeEntity> PaymentEventStateTypes => Set<PaymentEventStateTypeEntity>();

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    public DbSet<PaymentEventEntity> PaymentEvents => Set<PaymentEventEntity>();

    public async Task<SaveOutcome> SaveChangesWithOutcomeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);

            return SaveOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveOutcome.Conflict;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return SaveOutcome.AlreadyExists;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ErrorStatusConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SubMerchantConfiguration());
        modelBuilder.ApplyConfiguration(new StoredCardConfiguration());
        modelBuilder.ApplyConfiguration(new CurrencyTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentStateTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new AcmeOperationTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AcmeInteractionOutcomeTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentChangeSourceTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AcmeInteractionConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentChangeConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationSourceTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationOutcomeTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentEventTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentEventStateTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentEventConfiguration());
    }
}
