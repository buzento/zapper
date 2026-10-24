using Zapper.Domain.Merchant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class SubMerchantConfiguration : IEntityTypeConfiguration<SubMerchantEntity>
{
    public void Configure(EntityTypeBuilder<SubMerchantEntity> builder)
    {
        builder.ToTable("SubMerchant", "Merchant");

        builder.HasKey(subMerchant => subMerchant.Id)
               .HasName("PK_SubMerchant");

        builder.Property(subMerchant => subMerchant.Id)
               .ValueGeneratedNever();

        builder.Property(subMerchant => subMerchant.MerchantReference)
               .HasMaxLength(15)
               .IsRequired();

        builder.HasIndex(subMerchant => subMerchant.MerchantReference)
               .IsUnique()
               .HasDatabaseName("UX_SubMerchant_MerchantReference");

        builder.Property(subMerchant => subMerchant.TradingName)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(subMerchant => subMerchant.City)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(subMerchant => subMerchant.Region)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(subMerchant => subMerchant.CountryCode)
               .HasMaxLength(2)
               .IsRequired();

        builder.Property(subMerchant => subMerchant.MerchantCategoryCode)
               .HasMaxLength(4)
               .IsRequired();
    }
}
