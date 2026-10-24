using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class NotificationSourceTypeConfiguration : IEntityTypeConfiguration<NotificationSourceTypeEntity>
{
    public void Configure(EntityTypeBuilder<NotificationSourceTypeEntity> builder)
    {
        builder.ToTable("NotificationSourceType", "Payment");

        builder.HasKey(notificationSourceType => notificationSourceType.Id)
               .HasName("PK_NotificationSourceType");

        builder.Property(notificationSourceType => notificationSourceType.Id)
               .ValueGeneratedNever();

        builder.Property(notificationSourceType => notificationSourceType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = NotificationSource.PaymentAdvice, Name = nameof(NotificationSource.PaymentAdvice) },
            new { Id = NotificationSource.TransactionStatus, Name = nameof(NotificationSource.TransactionStatus) });
    }
}
