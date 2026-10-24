using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentStateTypeConfiguration : IEntityTypeConfiguration<PaymentStateTypeEntity>
{
    public void Configure(EntityTypeBuilder<PaymentStateTypeEntity> builder)
    {
        builder.ToTable("PaymentStateType", "Payment");

        builder.HasKey(paymentStateType => paymentStateType.Id)
               .HasName("PK_PaymentStateType");

        builder.Property(paymentStateType => paymentStateType.Id)
               .ValueGeneratedNever();

        builder.Property(paymentStateType => paymentStateType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = PaymentState.Recorded, Name = nameof(PaymentState.Recorded) },
            new { Id = PaymentState.OutcomeUnknown, Name = nameof(PaymentState.OutcomeUnknown) },
            new { Id = PaymentState.Approved, Name = nameof(PaymentState.Approved) },
            new { Id = PaymentState.Declined, Name = nameof(PaymentState.Declined) },
            new { Id = PaymentState.Failed, Name = nameof(PaymentState.Failed) },
            new { Id = PaymentState.Unresolved, Name = nameof(PaymentState.Unresolved) });
    }
}
