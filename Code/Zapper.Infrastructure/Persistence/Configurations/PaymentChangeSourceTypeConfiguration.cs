using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class PaymentChangeSourceTypeConfiguration : IEntityTypeConfiguration<PaymentChangeSourceTypeEntity>
{
    public void Configure(EntityTypeBuilder<PaymentChangeSourceTypeEntity> builder)
    {
        builder.ToTable("PaymentChangeSourceType", "Payment");

        builder.HasKey(paymentChangeSourceType => paymentChangeSourceType.Id)
               .HasName("PK_PaymentChangeSourceType");

        builder.Property(paymentChangeSourceType => paymentChangeSourceType.Id)
               .ValueGeneratedNever();

        builder.Property(paymentChangeSourceType => paymentChangeSourceType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = PaymentChangeSource.Send, Name = nameof(PaymentChangeSource.Send) },
            new { Id = PaymentChangeSource.Query, Name = nameof(PaymentChangeSource.Query) },
            new { Id = PaymentChangeSource.Worker, Name = nameof(PaymentChangeSource.Worker) },
            new { Id = PaymentChangeSource.PaymentAdvice, Name = nameof(PaymentChangeSource.PaymentAdvice) },
            new { Id = PaymentChangeSource.TransactionStatus, Name = nameof(PaymentChangeSource.TransactionStatus) });
    }
}
