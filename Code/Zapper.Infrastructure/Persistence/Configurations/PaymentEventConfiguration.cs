using Zapper.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEventEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEventEntity> builder)
    {
        builder.ToTable("PaymentEvent", "Payment");

        builder.HasKey(paymentEvent => paymentEvent.Id)
               .HasName("PK_PaymentEvent");

        builder.Property(paymentEvent => paymentEvent.Id)
               .ValueGeneratedNever();

        builder.HasOne<PaymentEntity>()
               .WithMany(payment => payment.Events)
               .HasForeignKey(paymentEvent => paymentEvent.PaymentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentEvent_PaymentId");

        builder.HasIndex(paymentEvent => paymentEvent.PaymentId)
               .HasDatabaseName("IX_PaymentEvent_PaymentId");

        builder.HasOne<NotificationEntity>()
               .WithMany()
               .HasForeignKey(paymentEvent => paymentEvent.NotificationId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentEvent_NotificationId");

        // a Transaction Status names one event
        builder.HasIndex(paymentEvent => paymentEvent.NotificationId)
               .IsUnique()
               .HasDatabaseName("UX_PaymentEvent_NotificationId");

        builder.Property(paymentEvent => paymentEvent.Event)
               .HasColumnName("PaymentEventTypeId");

        builder.HasOne<PaymentEventTypeEntity>()
               .WithMany()
               .HasForeignKey(paymentEvent => paymentEvent.Event)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentEvent_PaymentEventTypeId");

        builder.HasIndex(paymentEvent => paymentEvent.Event)
               .HasDatabaseName("IX_PaymentEvent_PaymentEventTypeId");

        builder.Property(paymentEvent => paymentEvent.CurrencyCode)
               .HasMaxLength(3)
               .IsRequired();

        // the daily summary reads one day of reversals and refunds by this time, rather than 5 years, D-16
        builder.HasIndex(paymentEvent => paymentEvent.AcmeProcessedAt)
               .HasDatabaseName("IX_PaymentEvent_AcmeProcessedAt");

        builder.Property(paymentEvent => paymentEvent.State)
               .HasColumnName("PaymentEventStateTypeId");

        builder.HasOne<PaymentEventStateTypeEntity>()
               .WithMany()
               .HasForeignKey(paymentEvent => paymentEvent.State)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentEvent_PaymentEventStateTypeId");

        builder.HasIndex(paymentEvent => paymentEvent.State)
               .HasDatabaseName("IX_PaymentEvent_PaymentEventStateTypeId");
    }
}
