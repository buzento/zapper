using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class AcmeOperationTypeConfiguration : IEntityTypeConfiguration<AcmeOperationTypeEntity>
{
    public void Configure(EntityTypeBuilder<AcmeOperationTypeEntity> builder)
    {
        builder.ToTable("AcmeOperationType", "Payment");

        builder.HasKey(acmeOperationType => acmeOperationType.Id)
               .HasName("PK_AcmeOperationType");

        builder.Property(acmeOperationType => acmeOperationType.Id)
               .ValueGeneratedNever();

        builder.Property(acmeOperationType => acmeOperationType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = AcmeOperation.Send, Name = nameof(AcmeOperation.Send) },
            new { Id = AcmeOperation.Query, Name = nameof(AcmeOperation.Query) });
    }
}
