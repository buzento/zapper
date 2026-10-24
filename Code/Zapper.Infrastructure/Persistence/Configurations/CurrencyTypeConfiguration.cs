using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class CurrencyTypeConfiguration : IEntityTypeConfiguration<CurrencyTypeEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyTypeEntity> builder)
    {
        builder.ToTable("CurrencyType", "Payment");

        builder.HasKey(currencyType => currencyType.Id)
               .HasName("PK_CurrencyType");

        builder.Property(currencyType => currencyType.Id)
               .ValueGeneratedNever();

        builder.Property(currencyType => currencyType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(currencyType => currencyType.Code)
               .HasMaxLength(3)
               .IsRequired();

        builder.HasIndex(currencyType => currencyType.Code)
               .IsUnique()
               .HasDatabaseName("UX_CurrencyType_Code");

        builder.Property(currencyType => currencyType.Symbol)
               .HasMaxLength(5)
               .IsRequired();

        builder.Property(currencyType => currencyType.MaximumAmount)
               .IsRequired();

        builder.HasData(
            new CurrencyTypeEntity(Currency.Zar, "ZAR", "R", 100000000),
            new CurrencyTypeEntity(Currency.Usd, "USD", "$", 5000000),
            new CurrencyTypeEntity(Currency.Eur, "EUR", "€", 5000000),
            new CurrencyTypeEntity(Currency.Gbp, "GBP", "£", 4000000));
    }
}
