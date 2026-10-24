using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateNotificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationId",
                schema: "Payment",
                table: "PaymentChange",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationOutcomeType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutcomeType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSourceType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSourceType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEventStateType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEventStateType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEventType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEventType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationSourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    AcmeReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ContentHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationOutcomeTypeId = table.Column<int>(type: "integer", nullable: false),
                    TransactionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResponseCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_NotificationOutcomeTypeId",
                        column: x => x.NotificationOutcomeTypeId,
                        principalSchema: "Payment",
                        principalTable: "NotificationOutcomeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_NotificationSourceTypeId",
                        column: x => x.NotificationSourceTypeId,
                        principalSchema: "Payment",
                        principalTable: "NotificationSourceType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Payment",
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEvent",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentEventTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AcmeProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaymentEventStateTypeId = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentEvent_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "Payment",
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentEvent_PaymentEventStateTypeId",
                        column: x => x.PaymentEventStateTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentEventStateType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentEvent_PaymentEventTypeId",
                        column: x => x.PaymentEventTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentEventType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentEvent_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Payment",
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "NotificationOutcomeType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Applied" },
                    { 2, "Confirmed" },
                    { 3, "Held" },
                    { 4, "Recorded" },
                    { 5, "Unmatched" },
                    { 6, "Contradiction" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "NotificationSourceType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "PaymentAdvice" },
                    { 2, "TransactionStatus" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentChangeSourceType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 4, "PaymentAdvice" },
                    { 5, "TransactionStatus" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentEventStateType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Held" },
                    { 2, "Applied" },
                    { 3, "NotApplied" },
                    { 4, "Contradiction" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentEventType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Reversal" },
                    { 2, "Refund" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentStateType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 7, "Reversed" },
                    { 8, "Refunded" },
                    { 9, "PartiallyRefunded" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_NotificationId",
                schema: "Payment",
                table: "PaymentChange",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_NotificationOutcomeTypeId",
                schema: "Payment",
                table: "Notification",
                column: "NotificationOutcomeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_NotificationSourceTypeId",
                schema: "Payment",
                table: "Notification",
                column: "NotificationSourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_PaymentId",
                schema: "Payment",
                table: "Notification",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_Notification_NotificationSourceTypeId_AcmeReference_RequestType_ContentHash",
                schema: "Payment",
                table: "Notification",
                columns: new[] { "NotificationSourceTypeId", "AcmeReference", "RequestType", "ContentHash" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvent_PaymentEventStateTypeId",
                schema: "Payment",
                table: "PaymentEvent",
                column: "PaymentEventStateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvent_PaymentEventTypeId",
                schema: "Payment",
                table: "PaymentEvent",
                column: "PaymentEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvent_PaymentId",
                schema: "Payment",
                table: "PaymentEvent",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentEvent_NotificationId",
                schema: "Payment",
                table: "PaymentEvent",
                column: "NotificationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentChange_NotificationId",
                schema: "Payment",
                table: "PaymentChange",
                column: "NotificationId",
                principalSchema: "Payment",
                principalTable: "Notification",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("GRANT SELECT, INSERT ON \"Payment\".\"Notification\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE ON \"Payment\".\"PaymentEvent\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"NotificationSourceType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"NotificationOutcomeType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"PaymentEventType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"PaymentEventStateType\" TO \"ZapperPaymentsApiRole\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"PaymentEventStateType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"PaymentEventType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"NotificationOutcomeType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"NotificationSourceType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT, INSERT, UPDATE ON \"Payment\".\"PaymentEvent\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT, INSERT ON \"Payment\".\"Notification\" FROM \"ZapperPaymentsApiRole\";");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentChange_NotificationId",
                schema: "Payment",
                table: "PaymentChange");

            migrationBuilder.DropTable(
                name: "PaymentEvent",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentEventStateType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentEventType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "NotificationOutcomeType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "NotificationSourceType",
                schema: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_PaymentChange_NotificationId",
                schema: "Payment",
                table: "PaymentChange");

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentChangeSourceType",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentChangeSourceType",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "NotificationId",
                schema: "Payment",
                table: "PaymentChange");
        }
    }
}
