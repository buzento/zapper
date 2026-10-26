using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentEventStateTypeConfiguration : IEntityTypeConfiguration<PaymentEventStateTypeEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEventStateTypeEntity> builder)
    {
        builder.ToTable("PaymentEventStateType", "Payment");

        builder.HasKey(paymentEventStateType => paymentEventStateType.Id)
               .HasName("PK_PaymentEventStateType");

        builder.Property(paymentEventStateType => paymentEventStateType.Id)
               .ValueGeneratedNever();

        builder.Property(paymentEventStateType => paymentEventStateType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = PaymentEventState.Held, Name = nameof(PaymentEventState.Held) },
            new { Id = PaymentEventState.Applied, Name = nameof(PaymentEventState.Applied) },
            new { Id = PaymentEventState.NotApplied, Name = nameof(PaymentEventState.NotApplied) },
            new { Id = PaymentEventState.Contradiction, Name = nameof(PaymentEventState.Contradiction) });
    }
}
