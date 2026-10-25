using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("Payment", "Payment");

        builder.HasKey(payment => payment.Id)
               .HasName("PK_Payment");

        builder.Property(payment => payment.Id)
               .ValueGeneratedNever();

        builder.Property(payment => payment.AppPaymentReference)
               .HasMaxLength(50)
               .IsRequired();

        builder.HasIndex(payment => payment.AppPaymentReference)
               .IsUnique()
               .HasDatabaseName("UX_Payment_AppPaymentReference");

        builder.HasOne<StoredCardEntity>()
               .WithMany()
               .HasForeignKey(payment => payment.StoredCardId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Payment_StoredCardId");

        builder.HasIndex(payment => payment.StoredCardId)
               .HasDatabaseName("IX_Payment_StoredCardId");

        builder.HasOne<SubMerchantEntity>()
               .WithMany()
               .HasForeignKey(payment => payment.SubMerchantId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Payment_SubMerchantId");

        builder.HasIndex(payment => payment.SubMerchantId)
               .HasDatabaseName("IX_Payment_SubMerchantId");

        builder.Property(payment => payment.Currency)
               .HasColumnName("CurrencyTypeId");

        builder.HasOne<CurrencyTypeEntity>()
               .WithMany()
               .HasForeignKey(payment => payment.Currency)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Payment_CurrencyTypeId");

        builder.HasIndex(payment => payment.Currency)
               .HasDatabaseName("IX_Payment_CurrencyTypeId");

        builder.Property(payment => payment.Amount)
               .IsRequired();

        builder.Property(payment => payment.OrderNumber)
               .HasMaxLength(12)
               .IsRequired();

        builder.HasIndex(payment => payment.OrderNumber)
               .IsUnique()
               .HasDatabaseName("UX_Payment_OrderNumber");

        builder.Property(payment => payment.CardAcceptorName)
               .HasMaxLength(40)
               .IsRequired();

        builder.Property(payment => payment.MerchantCategoryCode)
               .HasMaxLength(4)
               .IsRequired();

        builder.Property(payment => payment.State)
               .HasColumnName("PaymentStateTypeId");

        builder.HasOne<PaymentStateTypeEntity>()
               .WithMany()
               .HasForeignKey(payment => payment.State)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Payment_PaymentStateTypeId");

        builder.HasIndex(payment => payment.State)
               .HasDatabaseName("IX_Payment_PaymentStateTypeId");

        builder.HasIndex(payment => payment.NextActionAt)
               .HasDatabaseName("IX_Payment_NextActionAt");

        // Appendix A's maximum lengths
        builder.Property(payment => payment.ResponseSource)
               .HasMaxLength(50);

        builder.Property(payment => payment.ResponseCode)
               .HasMaxLength(3);

        builder.Property(payment => payment.ResponseDescription)
               .HasMaxLength(50);

        builder.Property(payment => payment.ClientMessage)
               .HasMaxLength(100);

        builder.Property(payment => payment.AuthorisationCode)
               .HasMaxLength(6);

        builder.Property(payment => payment.ReconciliationId)
               .HasMaxLength(50);

        builder.Property(payment => payment.RetrievalReferenceNumber)
               .HasMaxLength(12);

        // the daily summary reads one day of payments by these two times, rather than 5 years, D-16
        builder.HasIndex(payment => payment.AcmeTransactionAt)
               .HasDatabaseName("IX_Payment_AcmeTransactionAt");

        builder.HasIndex(payment => payment.RecordedAt)
               .HasDatabaseName("IX_Payment_RecordedAt");

        // the worker and, from Acme Notifications on, a notification both change a payment, D-6
        builder.Property<uint>("Version")
               .IsRowVersion();
    }
}
