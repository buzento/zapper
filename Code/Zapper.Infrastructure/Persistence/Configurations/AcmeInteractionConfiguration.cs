using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class AcmeInteractionConfiguration : IEntityTypeConfiguration<AcmeInteractionEntity>
{
    public void Configure(EntityTypeBuilder<AcmeInteractionEntity> builder)
    {
        builder.ToTable("AcmeInteraction", "Payment");

        builder.HasKey(acmeInteraction => acmeInteraction.Id)
               .HasName("PK_AcmeInteraction");

        builder.Property(acmeInteraction => acmeInteraction.Id)
               .ValueGeneratedNever();

        builder.HasOne<PaymentEntity>()
               .WithMany()
               .HasForeignKey(acmeInteraction => acmeInteraction.PaymentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_AcmeInteraction_PaymentId");

        builder.HasIndex(acmeInteraction => acmeInteraction.PaymentId)
               .HasDatabaseName("IX_AcmeInteraction_PaymentId");

        builder.HasOne<StoredCardEntity>()
               .WithMany()
               .HasForeignKey(acmeInteraction => acmeInteraction.StoredCardId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_AcmeInteraction_StoredCardId");

        builder.HasIndex(acmeInteraction => acmeInteraction.StoredCardId)
               .HasDatabaseName("IX_AcmeInteraction_StoredCardId");

        builder.Property(acmeInteraction => acmeInteraction.Operation)
               .HasColumnName("AcmeOperationTypeId");

        builder.HasOne<AcmeOperationTypeEntity>()
               .WithMany()
               .HasForeignKey(acmeInteraction => acmeInteraction.Operation)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_AcmeInteraction_AcmeOperationTypeId");

        builder.HasIndex(acmeInteraction => acmeInteraction.Operation)
               .HasDatabaseName("IX_AcmeInteraction_AcmeOperationTypeId");

        builder.Property(acmeInteraction => acmeInteraction.Outcome)
               .HasColumnName("AcmeInteractionOutcomeTypeId");

        builder.HasOne<AcmeInteractionOutcomeTypeEntity>()
               .WithMany()
               .HasForeignKey(acmeInteraction => acmeInteraction.Outcome)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_AcmeInteraction_AcmeInteractionOutcomeTypeId");

        builder.HasIndex(acmeInteraction => acmeInteraction.Outcome)
               .HasDatabaseName("IX_AcmeInteraction_AcmeInteractionOutcomeTypeId");

        // Acme adds members without notice, so both bodies are text rather than a length
        builder.Property(acmeInteraction => acmeInteraction.Request)
               .IsRequired();

        builder.Property(acmeInteraction => acmeInteraction.Response);

        builder.Property(acmeInteraction => acmeInteraction.TraceId)
               .HasMaxLength(32)
               .IsRequired();
    }
}
