using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentErrorMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "Error",
                table: "ErrorMessage",
                columns: new[] { "Id", "Code", "Detail" },
                values: new object[,]
                {
                    { new Guid("07353e9d-60ae-4c6c-a34b-6acf9c297f7d"), "AmountAboveMaximum", "The amount is above the maximum for its currency." },
                    { new Guid("0b051208-d686-4de4-8baa-606a184f7c55"), "CustomerReferenceRequired", "A customer reference is required." },
                    { new Guid("0ec1f88a-a163-469d-8793-9e475027f93a"), "SubMerchantReferenceTooLong", "A sub-merchant reference is at most 15 characters." },
                    { new Guid("4c4baa8d-4d16-416b-ad63-d828f234c408"), "AppPaymentReferenceTooLong", "A payment reference is at most 50 characters." },
                    { new Guid("589cef0a-f398-400b-927d-b524336eab9e"), "CurrencyCodeTooLong", "A currency code is 3 letters." },
                    { new Guid("61d1daf5-19e9-401e-88b2-8a1ccf9d634a"), "CustomerReferenceTooLong", "A customer reference is at most 50 characters." },
                    { new Guid("74512136-0acb-48ce-8935-aa6a392d8027"), "SubMerchantNotRegistered", "That sub-merchant is not registered." },
                    { new Guid("82ea17d2-be0a-4dd0-8dc0-f0652368af69"), "SubMerchantReferenceRequired", "A sub-merchant reference is required." },
                    { new Guid("a2dd2ecb-3259-4b8c-bed0-8e6cf32e2cc9"), "AppPaymentReferenceReused", "That payment reference is already used for a different payment." },
                    { new Guid("ab355638-a4ca-4c6c-8ed1-bfd2af56e8a3"), "AppPaymentReferenceRequired", "A payment reference is required." },
                    { new Guid("b45cf55c-b723-4a3c-abf2-fa4ece3bd298"), "AmountBelowMinimum", "The amount must be at least 1 in the currency's lowest unit." },
                    { new Guid("df82ab02-2945-4839-b5f4-155ee2efe9a9"), "CustomerHasNoStoredCard", "That customer has no stored card." },
                    { new Guid("e94d7154-9a10-4134-82fe-90ad7f1fc2d7"), "CurrencyCodeNotSupported", "That currency is not supported." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("07353e9d-60ae-4c6c-a34b-6acf9c297f7d"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("0b051208-d686-4de4-8baa-606a184f7c55"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("0ec1f88a-a163-469d-8793-9e475027f93a"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("4c4baa8d-4d16-416b-ad63-d828f234c408"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("589cef0a-f398-400b-927d-b524336eab9e"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("61d1daf5-19e9-401e-88b2-8a1ccf9d634a"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("74512136-0acb-48ce-8935-aa6a392d8027"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("82ea17d2-be0a-4dd0-8dc0-f0652368af69"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("a2dd2ecb-3259-4b8c-bed0-8e6cf32e2cc9"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("ab355638-a4ca-4c6c-8ed1-bfd2af56e8a3"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("b45cf55c-b723-4a3c-abf2-fa4ece3bd298"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("df82ab02-2945-4839-b5f4-155ee2efe9a9"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("e94d7154-9a10-4134-82fe-90ad7f1fc2d7"));
        }
    }
}
