using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentChangeConfiguration : IEntityTypeConfiguration<PaymentChangeEntity>
{
    public void Configure(EntityTypeBuilder<PaymentChangeEntity> builder)
    {
        builder.ToTable("PaymentChange", "Payment");

        builder.HasKey(paymentChange => paymentChange.Id)
               .HasName("PK_PaymentChange");

        builder.Property(paymentChange => paymentChange.Id)
               .ValueGeneratedNever();

        builder.HasOne<PaymentEntity>()
               .WithMany(payment => payment.Changes)
               .HasForeignKey(paymentChange => paymentChange.PaymentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_PaymentId");

        builder.HasIndex(paymentChange => paymentChange.PaymentId)
               .HasDatabaseName("IX_PaymentChange_PaymentId");

        builder.Property(paymentChange => paymentChange.FromState)
               .HasColumnName("FromPaymentStateTypeId");

        builder.HasOne<PaymentStateTypeEntity>()
               .WithMany()
               .HasForeignKey(paymentChange => paymentChange.FromState)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_FromPaymentStateTypeId");

        builder.HasIndex(paymentChange => paymentChange.FromState)
               .HasDatabaseName("IX_PaymentChange_FromPaymentStateTypeId");

        builder.Property(paymentChange => paymentChange.ToState)
               .HasColumnName("ToPaymentStateTypeId");

        builder.HasOne<PaymentStateTypeEntity>()
               .WithMany()
               .HasForeignKey(paymentChange => paymentChange.ToState)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_ToPaymentStateTypeId");

        builder.HasIndex(paymentChange => paymentChange.ToState)
               .HasDatabaseName("IX_PaymentChange_ToPaymentStateTypeId");

        builder.Property(paymentChange => paymentChange.Source)
               .HasColumnName("PaymentChangeSourceTypeId");

        builder.HasOne<PaymentChangeSourceTypeEntity>()
               .WithMany()
               .HasForeignKey(paymentChange => paymentChange.Source)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_PaymentChangeSourceTypeId");

        builder.HasIndex(paymentChange => paymentChange.Source)
               .HasDatabaseName("IX_PaymentChange_PaymentChangeSourceTypeId");

        builder.HasOne<AcmeInteractionEntity>()
               .WithMany()
               .HasForeignKey(paymentChange => paymentChange.AcmeInteractionId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_AcmeInteractionId");

        builder.HasIndex(paymentChange => paymentChange.AcmeInteractionId)
               .HasDatabaseName("IX_PaymentChange_AcmeInteractionId");

        builder.HasOne<NotificationEntity>()
               .WithMany()
               .HasForeignKey(paymentChange => paymentChange.NotificationId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PaymentChange_NotificationId");

        builder.HasIndex(paymentChange => paymentChange.NotificationId)
               .HasDatabaseName("IX_PaymentChange_NotificationId");
    }
}
