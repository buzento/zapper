using Zapper.Domain.Error;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class ErrorMessageConfiguration : IEntityTypeConfiguration<ErrorMessageEntity>
{
    public void Configure(EntityTypeBuilder<ErrorMessageEntity> builder)
    {
        builder.ToTable("ErrorMessage", "Error");

        builder.HasKey(errorMessage => errorMessage.Id)
               .HasName("PK_ErrorMessage");

        builder.Property(errorMessage => errorMessage.Id)
               .ValueGeneratedNever();

        builder.Property(errorMessage => errorMessage.Code)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(errorMessage => errorMessage.Code)
               .IsUnique()
               .HasDatabaseName("UX_ErrorMessage_Code");

        builder.Property(errorMessage => errorMessage.Detail)
               .HasMaxLength(500)
               .IsRequired();
    }
}
