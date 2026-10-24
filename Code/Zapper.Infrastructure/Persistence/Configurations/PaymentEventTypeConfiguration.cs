using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentEventTypeConfiguration : IEntityTypeConfiguration<PaymentEventTypeEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEventTypeEntity> builder)
    {
        builder.ToTable("PaymentEventType", "Payment");

        builder.HasKey(paymentEventType => paymentEventType.Id)
               .HasName("PK_PaymentEventType");

        builder.Property(paymentEventType => paymentEventType.Id)
               .ValueGeneratedNever();

        builder.Property(paymentEventType => paymentEventType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = PaymentEvent.Reversal, Name = nameof(PaymentEvent.Reversal) },
            new { Id = PaymentEvent.Refund, Name = nameof(PaymentEvent.Refund) });
    }
}
