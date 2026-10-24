using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAdviceErrorMessages : Migration
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
                    { new Guid("12f42775-f7bf-496e-887a-f3daf23c5817"), "TransactionDateTimeMalformed", "A transaction time is written as yyyy-MM-ddTHH:mm:ss.fff, with no offset." },
                    { new Guid("1707ad45-896d-4a7b-91d6-7c7eef64b95e"), "TransactionIdTooLong", "A transaction id is at most 50 characters." },
                    { new Guid("2f950496-09cb-4ec4-9526-db0380c8e9b5"), "TransactionStatusTooLong", "A transaction status is at most 20 characters." },
                    { new Guid("3d140158-89dd-4e70-a32f-2a7374f8d6e3"), "MerchantIdTooLong", "A merchant id is at most 50 characters." },
                    { new Guid("49a83a97-084c-491a-8d91-39282d9c116f"), "TransactionStatusRequired", "A transaction status is required." },
                    { new Guid("4b843870-266e-4c9f-b943-fcd4f405d944"), "OrderNumberTooLong", "An order number is at most 50 characters." },
                    { new Guid("502e6848-f6b0-4c31-a2f6-78d961f2a733"), "SystemsTraceAuditNumberRequired", "A systems trace audit number is required." },
                    { new Guid("521f44e9-3fca-444b-bb4c-bede12370e03"), "RequestTypeRequired", "A request type is required." },
                    { new Guid("5522e4e1-c314-46ba-8268-754177bb658e"), "AuthorisationCodeTooLong", "An authorisation code is at most 6 characters." },
                    { new Guid("5d53b994-5c54-4b78-9c55-be1ffe1d447b"), "SystemsTraceAuditNumberTooLong", "A systems trace audit number is at most 6 characters." },
                    { new Guid("642587bd-d057-4210-8f55-d9f7c0d2c33b"), "TransactionIdRequired", "A transaction id is required." },
                    { new Guid("67022baa-19b8-4f34-a769-b9c1a02a32b7"), "ReconciliationIdTooLong", "A reconciliation id is at most 50 characters." },
                    { new Guid("6b873b11-b172-467d-ad29-50bcc1b7a7f8"), "CommunicationProtocolRequired", "A communication protocol is required." },
                    { new Guid("7c15dfbf-0f38-49ed-9861-ec6382efeeb6"), "RequestTypeTooLong", "A request type is at most 20 characters." },
                    { new Guid("987601ba-13d3-4716-a19a-bb52cacf9598"), "ResponseDescriptionTooLong", "A response description is at most 50 characters." },
                    { new Guid("9f17bd4c-303c-47be-a1ce-6ce787bab137"), "RetrievalReferenceNumberTooLong", "A retrieval reference number is at most 12 characters." },
                    { new Guid("a2f5155c-50ce-4893-a113-9b4d8c8cf45b"), "PaymentServiceRequired", "A payment service is required." },
                    { new Guid("a4bb9587-84ea-47ab-8dba-db6ccad3c1e7"), "ConcurrentChange", "The payment changed while this was applied. Send it again." },
                    { new Guid("af5ec77f-69b4-4e8b-bfbe-924ae491c824"), "ResponseSourceRequired", "A response source is required." },
                    { new Guid("b344770e-44a6-4127-8bce-861f1a690f21"), "CommunicationProtocolTooLong", "A communication protocol is at most 4 characters." },
                    { new Guid("c181e099-2da9-4716-a47d-af6b9f2c3509"), "RetrievalReferenceNumberRequired", "A retrieval reference number is required." },
                    { new Guid("cdb902cd-d366-4896-a077-023f9111c3e2"), "ResponseCodeTooLong", "A response code is at most 3 characters." },
                    { new Guid("d39e3508-8331-49b6-974c-dd05e7eef504"), "PaymentServiceTooLong", "A payment service is at most 30 characters." },
                    { new Guid("e0afec99-ce72-433c-ab63-b990605084b6"), "MerchantIdRequired", "A merchant id is required." },
                    { new Guid("eb0570dd-6d15-40ed-b99c-a8b8aa4f0d2b"), "ResponseCodeRequired", "A response code is required." },
                    { new Guid("f516dda3-6c4d-40ce-9c9c-2a7d56d48abd"), "ResponseSourceTooLong", "A response source is at most 50 characters." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("12f42775-f7bf-496e-887a-f3daf23c5817"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("1707ad45-896d-4a7b-91d6-7c7eef64b95e"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("2f950496-09cb-4ec4-9526-db0380c8e9b5"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("3d140158-89dd-4e70-a32f-2a7374f8d6e3"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("49a83a97-084c-491a-8d91-39282d9c116f"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("4b843870-266e-4c9f-b943-fcd4f405d944"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("502e6848-f6b0-4c31-a2f6-78d961f2a733"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("521f44e9-3fca-444b-bb4c-bede12370e03"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("5522e4e1-c314-46ba-8268-754177bb658e"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("5d53b994-5c54-4b78-9c55-be1ffe1d447b"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("642587bd-d057-4210-8f55-d9f7c0d2c33b"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("67022baa-19b8-4f34-a769-b9c1a02a32b7"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("6b873b11-b172-467d-ad29-50bcc1b7a7f8"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("7c15dfbf-0f38-49ed-9861-ec6382efeeb6"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("987601ba-13d3-4716-a19a-bb52cacf9598"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("9f17bd4c-303c-47be-a1ce-6ce787bab137"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("a2f5155c-50ce-4893-a113-9b4d8c8cf45b"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("a4bb9587-84ea-47ab-8dba-db6ccad3c1e7"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("af5ec77f-69b4-4e8b-bfbe-924ae491c824"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("b344770e-44a6-4127-8bce-861f1a690f21"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("c181e099-2da9-4716-a47d-af6b9f2c3509"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("cdb902cd-d366-4896-a077-023f9111c3e2"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("d39e3508-8331-49b6-974c-dd05e7eef504"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("e0afec99-ce72-433c-ab63-b990605084b6"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("eb0570dd-6d15-40ed-b99c-a8b8aa4f0d2b"));

            migrationBuilder.DeleteData(
                schema: "Error",
                table: "ErrorMessage",
                keyColumn: "Id",
                keyValue: new Guid("f516dda3-6c4d-40ce-9c9c-2a7d56d48abd"));
        }
    }
}
