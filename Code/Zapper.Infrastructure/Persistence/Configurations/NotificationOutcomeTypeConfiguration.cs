using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class NotificationOutcomeTypeConfiguration : IEntityTypeConfiguration<NotificationOutcomeTypeEntity>
{
    public void Configure(EntityTypeBuilder<NotificationOutcomeTypeEntity> builder)
    {
        builder.ToTable("NotificationOutcomeType", "Payment");

        builder.HasKey(notificationOutcomeType => notificationOutcomeType.Id)
               .HasName("PK_NotificationOutcomeType");

        builder.Property(notificationOutcomeType => notificationOutcomeType.Id)
               .ValueGeneratedNever();

        builder.Property(notificationOutcomeType => notificationOutcomeType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = NotificationOutcome.Applied, Name = nameof(NotificationOutcome.Applied) },
            new { Id = NotificationOutcome.Confirmed, Name = nameof(NotificationOutcome.Confirmed) },
            new { Id = NotificationOutcome.Held, Name = nameof(NotificationOutcome.Held) },
            new { Id = NotificationOutcome.Recorded, Name = nameof(NotificationOutcome.Recorded) },
            new { Id = NotificationOutcome.Unmatched, Name = nameof(NotificationOutcome.Unmatched) },
            new { Id = NotificationOutcome.Contradiction, Name = nameof(NotificationOutcome.Contradiction) });
    }
}
