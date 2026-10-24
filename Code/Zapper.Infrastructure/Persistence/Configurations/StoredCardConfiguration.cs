using Zapper.Domain.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class StoredCardConfiguration : IEntityTypeConfiguration<StoredCardEntity>
{
    public void Configure(EntityTypeBuilder<StoredCardEntity> builder)
    {
        builder.ToTable("StoredCard", "Customer");

        builder.HasKey(storedCard => storedCard.Id)
               .HasName("PK_StoredCard");

        builder.Property(storedCard => storedCard.Id)
               .ValueGeneratedNever();

        builder.Property(storedCard => storedCard.CustomerReference)
               .HasMaxLength(50)
               .IsRequired();

        // one stored card per customer, D-9
        builder.HasIndex(storedCard => storedCard.CustomerReference)
               .IsUnique()
               .HasDatabaseName("UX_StoredCard_CustomerReference");

        builder.Property(storedCard => storedCard.EncryptedToken)
               .IsRequired();

        builder.Property(storedCard => storedCard.EmailAddress)
               .HasMaxLength(254)
               .IsRequired();
    }
}
