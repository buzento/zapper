using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateSubmissionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcmeTransactionAt",
                schema: "Payment",
                table: "Payment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "Payment",
                table: "Payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AuthorisationCode",
                schema: "Payment",
                table: "Payment",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueryCount",
                schema: "Payment",
                table: "Payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationId",
                schema: "Payment",
                table: "Payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseDescription",
                schema: "Payment",
                table: "Payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseSource",
                schema: "Payment",
                table: "Payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetrievalReferenceNumber",
                schema: "Payment",
                table: "Payment",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SendCount",
                schema: "Payment",
                table: "Payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnexpectedFailureCount",
                schema: "Payment",
                table: "Payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "Payment",
                table: "Payment",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "AcmeInteractionOutcomeType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeInteractionOutcomeType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcmeOperationType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeOperationType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentChangeSourceType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentChangeSourceType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcmeInteraction",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcmeOperationTypeId = table.Column<int>(type: "integer", nullable: false),
                    AcmeInteractionOutcomeTypeId = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    Request = table.Column<string>(type: "text", nullable: false),
                    HttpStatus = table.Column<int>(type: "integer", nullable: true),
                    Response = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeInteraction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcmeInteraction_AcmeInteractionOutcomeTypeId",
                        column: x => x.AcmeInteractionOutcomeTypeId,
                        principalSchema: "Payment",
                        principalTable: "AcmeInteractionOutcomeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcmeInteraction_AcmeOperationTypeId",
                        column: x => x.AcmeOperationTypeId,
                        principalSchema: "Payment",
                        principalTable: "AcmeOperationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcmeInteraction_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Payment",
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcmeInteraction_StoredCardId",
                        column: x => x.StoredCardId,
                        principalSchema: "Customer",
                        principalTable: "StoredCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentChange",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPaymentStateTypeId = table.Column<int>(type: "integer", nullable: false),
                    ToPaymentStateTypeId = table.Column<int>(type: "integer", nullable: false),
                    PaymentChangeSourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    AcmeInteractionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentChange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentChange_AcmeInteractionId",
                        column: x => x.AcmeInteractionId,
                        principalSchema: "Payment",
                        principalTable: "AcmeInteraction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentChange_FromPaymentStateTypeId",
                        column: x => x.FromPaymentStateTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentStateType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentChange_PaymentChangeSourceTypeId",
                        column: x => x.PaymentChangeSourceTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentChangeSourceType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentChange_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Payment",
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentChange_ToPaymentStateTypeId",
                        column: x => x.ToPaymentStateTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentStateType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "AcmeInteractionOutcomeType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Answered" },
                    { 2, "TimedOut" },
                    { 3, "ConnectionFailed" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "AcmeOperationType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Send" },
                    { 2, "Query" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentChangeSourceType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Send" },
                    { 2, "Query" },
                    { 3, "Worker" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentStateType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 2, "OutcomeUnknown" },
                    { 3, "Approved" },
                    { 4, "Declined" },
                    { 5, "Failed" },
                    { 6, "Unresolved" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_NextActionAt",
                schema: "Payment",
                table: "Payment",
                column: "NextActionAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeInteraction_AcmeInteractionOutcomeTypeId",
                schema: "Payment",
                table: "AcmeInteraction",
                column: "AcmeInteractionOutcomeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeInteraction_AcmeOperationTypeId",
                schema: "Payment",
                table: "AcmeInteraction",
                column: "AcmeOperationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeInteraction_PaymentId",
                schema: "Payment",
                table: "AcmeInteraction",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeInteraction_StoredCardId",
                schema: "Payment",
                table: "AcmeInteraction",
                column: "StoredCardId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_AcmeInteractionId",
                schema: "Payment",
                table: "PaymentChange",
                column: "AcmeInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_FromPaymentStateTypeId",
                schema: "Payment",
                table: "PaymentChange",
                column: "FromPaymentStateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_PaymentChangeSourceTypeId",
                schema: "Payment",
                table: "PaymentChange",
                column: "PaymentChangeSourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_PaymentId",
                schema: "Payment",
                table: "PaymentChange",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChange_ToPaymentStateTypeId",
                schema: "Payment",
                table: "PaymentChange",
                column: "ToPaymentStateTypeId");

            migrationBuilder.Sql("GRANT UPDATE ON \"Payment\".\"Payment\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT, INSERT ON \"Payment\".\"AcmeInteraction\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT, INSERT ON \"Payment\".\"PaymentChange\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"AcmeOperationType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"AcmeInteractionOutcomeType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"PaymentChangeSourceType\" TO \"ZapperPaymentsApiRole\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"PaymentChangeSourceType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"AcmeInteractionOutcomeType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"AcmeOperationType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT, INSERT ON \"Payment\".\"PaymentChange\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT, INSERT ON \"Payment\".\"AcmeInteraction\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE UPDATE ON \"Payment\".\"Payment\" FROM \"ZapperPaymentsApiRole\";");

            migrationBuilder.DropTable(
                name: "PaymentChange",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "AcmeInteraction",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentChangeSourceType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "AcmeInteractionOutcomeType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "AcmeOperationType",
                schema: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_NextActionAt",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Payment",
                table: "PaymentStateType",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "AcmeTransactionAt",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "AuthorisationCode",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "QueryCount",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReconciliationId",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ResponseDescription",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ResponseSource",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RetrievalReferenceNumber",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "SendCount",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "UnexpectedFailureCount",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "Payment",
                table: "Payment");
        }
    }
}
