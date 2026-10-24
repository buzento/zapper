using Zapper.Domain.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class AcmeInteractionOutcomeTypeConfiguration : IEntityTypeConfiguration<AcmeInteractionOutcomeTypeEntity>
{
    public void Configure(EntityTypeBuilder<AcmeInteractionOutcomeTypeEntity> builder)
    {
        builder.ToTable("AcmeInteractionOutcomeType", "Payment");

        builder.HasKey(acmeInteractionOutcomeType => acmeInteractionOutcomeType.Id)
               .HasName("PK_AcmeInteractionOutcomeType");

        builder.Property(acmeInteractionOutcomeType => acmeInteractionOutcomeType.Id)
               .ValueGeneratedNever();

        builder.Property(acmeInteractionOutcomeType => acmeInteractionOutcomeType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = AcmeInteractionOutcome.Answered, Name = nameof(AcmeInteractionOutcome.Answered) },
            new { Id = AcmeInteractionOutcome.TimedOut, Name = nameof(AcmeInteractionOutcome.TimedOut) },
            new { Id = AcmeInteractionOutcome.ConnectionFailed, Name = nameof(AcmeInteractionOutcome.ConnectionFailed) });
    }
}
