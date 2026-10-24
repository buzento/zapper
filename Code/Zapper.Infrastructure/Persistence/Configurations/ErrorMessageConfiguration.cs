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

        builder.HasData(
            new
            {
                Id = Guid.Parse("0b051208-d686-4de4-8baa-606a184f7c55"),
                Code = "CustomerReferenceRequired",
                Detail = "A customer reference is required."
            },
            new
            {
                Id = Guid.Parse("61d1daf5-19e9-401e-88b2-8a1ccf9d634a"),
                Code = "CustomerReferenceTooLong",
                Detail = "A customer reference is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("82ea17d2-be0a-4dd0-8dc0-f0652368af69"),
                Code = "SubMerchantReferenceRequired",
                Detail = "A sub-merchant reference is required."
            },
            new
            {
                Id = Guid.Parse("0ec1f88a-a163-469d-8793-9e475027f93a"),
                Code = "SubMerchantReferenceTooLong",
                Detail = "A sub-merchant reference is at most 15 characters."
            },
            new
            {
                Id = Guid.Parse("b45cf55c-b723-4a3c-abf2-fa4ece3bd298"),
                Code = "AmountBelowMinimum",
                Detail = "The amount must be at least 1 in the currency's lowest unit."
            },
            new
            {
                Id = Guid.Parse("589cef0a-f398-400b-927d-b524336eab9e"),
                Code = "CurrencyCodeTooLong",
                Detail = "A currency code is 3 letters."
            },
            new
            {
                Id = Guid.Parse("ab355638-a4ca-4c6c-8ed1-bfd2af56e8a3"),
                Code = "AppPaymentReferenceRequired",
                Detail = "A payment reference is required."
            },
            new
            {
                Id = Guid.Parse("4c4baa8d-4d16-416b-ad63-d828f234c408"),
                Code = "AppPaymentReferenceTooLong",
                Detail = "A payment reference is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("a2dd2ecb-3259-4b8c-bed0-8e6cf32e2cc9"),
                Code = "AppPaymentReferenceReused",
                Detail = "That payment reference is already used for a different payment."
            },
            new
            {
                Id = Guid.Parse("df82ab02-2945-4839-b5f4-155ee2efe9a9"),
                Code = "CustomerHasNoStoredCard",
                Detail = "That customer has no stored card."
            },
            new
            {
                Id = Guid.Parse("74512136-0acb-48ce-8935-aa6a392d8027"),
                Code = "SubMerchantNotRegistered",
                Detail = "That sub-merchant is not registered."
            },
            new
            {
                Id = Guid.Parse("e94d7154-9a10-4134-82fe-90ad7f1fc2d7"),
                Code = "CurrencyCodeNotSupported",
                Detail = "That currency is not supported."
            },
            new
            {
                Id = Guid.Parse("07353e9d-60ae-4c6c-a34b-6acf9c297f7d"),
                Code = "AmountAboveMaximum",
                Detail = "The amount is above the maximum for its currency."
            });
    }
}
