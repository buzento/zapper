using Zapper.Domain.Entities.Error;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zapper.Infrastructure.Persistence.Configurations;

public sealed class ErrorStatusConfiguration : IEntityTypeConfiguration<ErrorStatusEntity>
{
    public void Configure(EntityTypeBuilder<ErrorStatusEntity> builder)
    {
        builder.ToTable("ErrorStatus", "Error");

        builder.HasKey(errorStatus => errorStatus.Id)
               .HasName("PK_ErrorStatus");

        builder.Property(errorStatus => errorStatus.Id)
               .ValueGeneratedNever();

        builder.Property(errorStatus => errorStatus.StatusCode)
               .IsRequired();

        builder.HasIndex(errorStatus => errorStatus.StatusCode)
               .IsUnique()
               .HasDatabaseName("UX_ErrorStatus_StatusCode");

        builder.Property(errorStatus => errorStatus.Code)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(errorStatus => errorStatus.Title)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(errorStatus => errorStatus.Detail)
               .HasMaxLength(500)
               .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("b53c6f07-22a7-498c-834b-cdce4fa5913e"),
                StatusCode = 400,
                Code = "BadRequest",
                Title = "Bad Request",
                Detail = "The request was not accepted."
            },
            new
            {
                Id = Guid.Parse("f2d40348-d6b7-4f61-bbb8-b1104c15449e"),
                StatusCode = 401,
                Code = "Unauthorized",
                Title = "Unauthorized",
                Detail = "Sign in to continue."
            },
            new
            {
                Id = Guid.Parse("1d4a8187-a0d7-44ab-96e6-143ec5b64b26"),
                StatusCode = 403,
                Code = "Forbidden",
                Title = "Forbidden",
                Detail = "You do not have permission to do this."
            },
            new
            {
                Id = Guid.Parse("d05419eb-4175-44ff-924b-c45cc3433e25"),
                StatusCode = 404,
                Code = "NotFound",
                Title = "Not Found",
                Detail = "That record could not be found."
            },
            new
            {
                Id = Guid.Parse("bfc0cf2d-96a5-4dc4-8e0d-02af8587622d"),
                StatusCode = 405,
                Code = "MethodNotAllowed",
                Title = "Method Not Allowed",
                Detail = "That method is not allowed for this address."
            },
            new
            {
                Id = Guid.Parse("fa7373fb-9f1b-4da7-b360-f955887d320e"),
                StatusCode = 413,
                Code = "RequestTooLarge",
                Title = "Content Too Large",
                Detail = "The request was too large."
            },
            new
            {
                Id = Guid.Parse("ada439ce-da86-415d-89ed-c228654c13bb"),
                StatusCode = 415,
                Code = "UnsupportedMediaType",
                Title = "Unsupported Media Type",
                Detail = "That content type is not supported."
            },
            new
            {
                Id = Guid.Parse("09908ee6-b837-4d43-93ba-7298b54275aa"),
                StatusCode = 429,
                Code = "TooManyRequests",
                Title = "Too Many Requests",
                Detail = "Too many requests were sent. Try again later."
            },
            new
            {
                Id = Guid.Parse("475d7bb9-b876-4573-b357-5dd9cb36774b"),
                StatusCode = 500,
                Code = "Internal",
                Title = "Internal Server Error",
                Detail = "The request could not be served."
            });
    }
}
