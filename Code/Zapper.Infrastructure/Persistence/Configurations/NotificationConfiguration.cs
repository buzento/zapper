using Zapper.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("Notification", "Payment");

        builder.HasKey(notification => notification.Id)
               .HasName("PK_Notification");

        builder.Property(notification => notification.Id)
               .ValueGeneratedNever();

        builder.Property(notification => notification.Source)
               .HasColumnName("NotificationSourceTypeId");

        builder.HasOne<NotificationSourceTypeEntity>()
               .WithMany()
               .HasForeignKey(notification => notification.Source)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Notification_NotificationSourceTypeId");

        builder.HasIndex(notification => notification.Source)
               .HasDatabaseName("IX_Notification_NotificationSourceTypeId");

        builder.Property(notification => notification.AcmeReference)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(notification => notification.RequestType)
               .HasMaxLength(20);

        builder.Property(notification => notification.ContentHash)
               .IsRequired();

        // a Transaction Status has no request type, so a missing one counts as the same value, D-8
        builder.HasIndex(
                   notification => new
                   {
                       notification.Source,
                       notification.AcmeReference,
                       notification.RequestType,
                       notification.ContentHash
                   })
               .IsUnique()
               .AreNullsDistinct(false)
               .HasDatabaseName("UX_Notification_NotificationSourceTypeId_AcmeReference_RequestType_ContentHash");

        builder.HasOne<PaymentEntity>()
               .WithMany(payment => payment.Notifications)
               .HasForeignKey(notification => notification.PaymentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Notification_PaymentId");

        builder.HasIndex(notification => notification.PaymentId)
               .HasDatabaseName("IX_Notification_PaymentId");

        builder.Property(notification => notification.Outcome)
               .HasColumnName("NotificationOutcomeTypeId");

        builder.HasOne<NotificationOutcomeTypeEntity>()
               .WithMany()
               .HasForeignKey(notification => notification.Outcome)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Notification_NotificationOutcomeTypeId");

        builder.HasIndex(notification => notification.Outcome)
               .HasDatabaseName("IX_Notification_NotificationOutcomeTypeId");

        builder.Property(notification => notification.TransactionStatus)
               .HasMaxLength(20);

        builder.Property(notification => notification.ResponseCode)
               .HasMaxLength(3);

        // Acme adds members without notice, so the body is text rather than a length
        builder.Property(notification => notification.Body)
               .IsRequired();

        builder.Property(notification => notification.TraceId)
               .HasMaxLength(32)
               .IsRequired();
    }
}
