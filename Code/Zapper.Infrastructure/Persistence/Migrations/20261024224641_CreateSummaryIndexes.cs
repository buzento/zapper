using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateSummaryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvent_AcmeProcessedAt",
                schema: "Payment",
                table: "PaymentEvent",
                column: "AcmeProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_AcmeTransactionAt",
                schema: "Payment",
                table: "Payment",
                column: "AcmeTransactionAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RecordedAt",
                schema: "Payment",
                table: "Payment",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentEvent_AcmeProcessedAt",
                schema: "Payment",
                table: "PaymentEvent");

            migrationBuilder.DropIndex(
                name: "IX_Payment_AcmeTransactionAt",
                schema: "Payment",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_RecordedAt",
                schema: "Payment",
                table: "Payment");
        }
    }
}
