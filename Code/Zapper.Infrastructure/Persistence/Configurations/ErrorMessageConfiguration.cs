using Zapper.Domain.Entities.Error;
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
            },
            new
            {
                Id = Guid.Parse("521f44e9-3fca-444b-bb4c-bede12370e03"),
                Code = "RequestTypeRequired",
                Detail = "A request type is required."
            },
            new
            {
                Id = Guid.Parse("7c15dfbf-0f38-49ed-9861-ec6382efeeb6"),
                Code = "RequestTypeTooLong",
                Detail = "A request type is at most 20 characters."
            },
            new
            {
                Id = Guid.Parse("e0afec99-ce72-433c-ab63-b990605084b6"),
                Code = "MerchantIdRequired",
                Detail = "A merchant id is required."
            },
            new
            {
                Id = Guid.Parse("3d140158-89dd-4e70-a32f-2a7374f8d6e3"),
                Code = "MerchantIdTooLong",
                Detail = "A merchant id is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("642587bd-d057-4210-8f55-d9f7c0d2c33b"),
                Code = "TransactionIdRequired",
                Detail = "A transaction id is required."
            },
            new
            {
                Id = Guid.Parse("1707ad45-896d-4a7b-91d6-7c7eef64b95e"),
                Code = "TransactionIdTooLong",
                Detail = "A transaction id is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("12f42775-f7bf-496e-887a-f3daf23c5817"),
                Code = "TransactionDateTimeMalformed",
                Detail = "A transaction time is written as yyyy-MM-ddTHH:mm:ss.fff, with no offset."
            },
            new
            {
                Id = Guid.Parse("4b843870-266e-4c9f-b943-fcd4f405d944"),
                Code = "OrderNumberTooLong",
                Detail = "An order number is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("67022baa-19b8-4f34-a769-b9c1a02a32b7"),
                Code = "ReconciliationIdTooLong",
                Detail = "A reconciliation id is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("c181e099-2da9-4716-a47d-af6b9f2c3509"),
                Code = "RetrievalReferenceNumberRequired",
                Detail = "A retrieval reference number is required."
            },
            new
            {
                Id = Guid.Parse("9f17bd4c-303c-47be-a1ce-6ce787bab137"),
                Code = "RetrievalReferenceNumberTooLong",
                Detail = "A retrieval reference number is at most 12 characters."
            },
            new
            {
                Id = Guid.Parse("502e6848-f6b0-4c31-a2f6-78d961f2a733"),
                Code = "SystemsTraceAuditNumberRequired",
                Detail = "A systems trace audit number is required."
            },
            new
            {
                Id = Guid.Parse("5d53b994-5c54-4b78-9c55-be1ffe1d447b"),
                Code = "SystemsTraceAuditNumberTooLong",
                Detail = "A systems trace audit number is at most 6 characters."
            },
            new
            {
                Id = Guid.Parse("a2f5155c-50ce-4893-a113-9b4d8c8cf45b"),
                Code = "PaymentServiceRequired",
                Detail = "A payment service is required."
            },
            new
            {
                Id = Guid.Parse("d39e3508-8331-49b6-974c-dd05e7eef504"),
                Code = "PaymentServiceTooLong",
                Detail = "A payment service is at most 30 characters."
            },
            new
            {
                Id = Guid.Parse("49a83a97-084c-491a-8d91-39282d9c116f"),
                Code = "TransactionStatusRequired",
                Detail = "A transaction status is required."
            },
            new
            {
                Id = Guid.Parse("2f950496-09cb-4ec4-9526-db0380c8e9b5"),
                Code = "TransactionStatusTooLong",
                Detail = "A transaction status is at most 20 characters."
            },
            new
            {
                Id = Guid.Parse("af5ec77f-69b4-4e8b-bfbe-924ae491c824"),
                Code = "ResponseSourceRequired",
                Detail = "A response source is required."
            },
            new
            {
                Id = Guid.Parse("f516dda3-6c4d-40ce-9c9c-2a7d56d48abd"),
                Code = "ResponseSourceTooLong",
                Detail = "A response source is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("eb0570dd-6d15-40ed-b99c-a8b8aa4f0d2b"),
                Code = "ResponseCodeRequired",
                Detail = "A response code is required."
            },
            new
            {
                Id = Guid.Parse("cdb902cd-d366-4896-a077-023f9111c3e2"),
                Code = "ResponseCodeTooLong",
                Detail = "A response code is at most 3 characters."
            },
            new
            {
                Id = Guid.Parse("987601ba-13d3-4716-a19a-bb52cacf9598"),
                Code = "ResponseDescriptionTooLong",
                Detail = "A response description is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("5522e4e1-c314-46ba-8268-754177bb658e"),
                Code = "AuthorisationCodeTooLong",
                Detail = "An authorisation code is at most 6 characters."
            },
            new
            {
                Id = Guid.Parse("6b873b11-b172-467d-ad29-50bcc1b7a7f8"),
                Code = "CommunicationProtocolRequired",
                Detail = "A communication protocol is required."
            },
            new
            {
                Id = Guid.Parse("b344770e-44a6-4127-8bce-861f1a690f21"),
                Code = "CommunicationProtocolTooLong",
                Detail = "A communication protocol is at most 4 characters."
            },
            new
            {
                Id = Guid.Parse("a4bb9587-84ea-47ab-8dba-db6ccad3c1e7"),
                Code = "ConcurrentChange",
                Detail = "The payment changed while this was applied. Send it again."
            },
            new
            {
                Id = Guid.Parse("75be5143-d743-4791-90a5-9ed3ce3724a9"),
                Code = "TransactionDateTimeRequired",
                Detail = "A transaction time is required."
            },
            new
            {
                Id = Guid.Parse("29442726-a521-41be-9edc-eb16b56aa053"),
                Code = "CurrencyCodeRequired",
                Detail = "A currency code is required."
            },
            new
            {
                Id = Guid.Parse("5531ff89-40d9-4098-a66c-cf2a0196b0c2"),
                Code = "TransactionUuidRequired",
                Detail = "A transaction UUID is required."
            },
            new
            {
                Id = Guid.Parse("7b5dca6a-e662-44df-8ae8-b5be9e634343"),
                Code = "TransactionUuidTooLong",
                Detail = "A transaction UUID is at most 50 characters."
            },
            new
            {
                Id = Guid.Parse("665bb989-2572-4ce5-a3b3-4203f9832a31"),
                Code = "TransactionTypeRequired",
                Detail = "A transaction type is required."
            },
            new
            {
                Id = Guid.Parse("82011f00-ade0-49c1-9b18-54c4c0295640"),
                Code = "TransactionTypeTooLong",
                Detail = "A transaction type is at most 20 characters."
            },
            new
            {
                Id = Guid.Parse("a8579262-a230-4c8e-b3d6-18b20de741e3"),
                Code = "TransactionResultCodeRequired",
                Detail = "A transaction result code is required."
            },
            new
            {
                Id = Guid.Parse("fbab87af-8ffa-4b7c-9e44-cd8f5e16ae1f"),
                Code = "TransactionResultCodeTooLong",
                Detail = "A transaction result code is at most 3 characters."
            },
            new
            {
                Id = Guid.Parse("ba2d0a78-d0a0-4eaf-ba46-1585e259c954"),
                Code = "IsApprovedRequired",
                Detail = "Whether the event was approved is required."
            });
    }
}
