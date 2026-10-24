using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionStatusErrorMessages : Migration
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
                    { new Guid("29442726-a521-41be-9edc-eb16b56aa053"), "CurrencyCodeRequired", "A currency code is required." },
                    { new Guid("5531ff89-40d9-4098-a66c-cf2a0196b0c2"), "TransactionUuidRequired", "A transaction UUID is required." },
                    { new Guid("665bb989-2572-4ce5-a3b3-4203f9832a31"), "TransactionTypeRequired", "A transaction type is required." },
                    { new Guid("75be5143-d743-4791-90a5-9ed3ce3724a9"), "TransactionDateTimeRequired", "A transaction time is required." },
                    { new Guid("7b5dca6a-e662-44df-8ae8-b5be9e634343"), "TransactionUuidTooLong", "A transaction UUID is at most 50 characters." },
                    { new Guid("82011f00-ade0-49c1-9b18-54c4c0295640"), "TransactionTypeTooLong", "A transaction type is at most 20 characters." },
                    { new Guid("a8579262-a230-4c8e-b3d6-18b20de741e3"), "TransactionResultCodeRequired", "A transaction result code is required." },
                    { new Guid("ba2d0a78-d0a0-4eaf-ba46-1585e259c954"), "IsApprovedRequired", "Whether the event was approved is required." },
                    { new Guid("fbab87af-8ffa-4b7c-9e44-cd8f5e16ae1f"), "TransactionResultCodeTooLong", "A transaction result code is at most 3 characters." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("29442726-a521-41be-9edc-eb16b56aa053"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("5531ff89-40d9-4098-a66c-cf2a0196b0c2"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("665bb989-2572-4ce5-a3b3-4203f9832a31"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("75be5143-d743-4791-90a5-9ed3ce3724a9"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("7b5dca6a-e662-44df-8ae8-b5be9e634343"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("82011f00-ade0-49c1-9b18-54c4c0295640"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("a8579262-a230-4c8e-b3d6-18b20de741e3"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("ba2d0a78-d0a0-4eaf-ba46-1585e259c954"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("fbab87af-8ffa-4b7c-9e44-cd8f5e16ae1f"));
        }
    }
}
