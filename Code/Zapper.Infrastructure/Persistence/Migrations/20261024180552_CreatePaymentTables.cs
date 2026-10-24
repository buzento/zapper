using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreatePaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Payment");

            migrationBuilder.EnsureSchema(
                name: "Customer");

            migrationBuilder.EnsureSchema(
                name: "Merchant");

            migrationBuilder.CreateTable(
                name: "CurrencyType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    MaximumAmount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStateType",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStateType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredCard",
                schema: "Customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncryptedToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredCard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubMerchant",
                schema: "Merchant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantReference = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    MerchantCategoryCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubMerchant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppPaymentReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StoredCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubMerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CardAcceptorName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MerchantCategoryCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    PaymentStateTypeId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextActionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResponseCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ClientMessage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorisedAmount = table.Column<long>(type: "bigint", nullable: true),
                    ReversedAmount = table.Column<long>(type: "bigint", nullable: false),
                    RefundedAmount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalSchema: "Payment",
                        principalTable: "CurrencyType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_PaymentStateTypeId",
                        column: x => x.PaymentStateTypeId,
                        principalSchema: "Payment",
                        principalTable: "PaymentStateType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_StoredCardId",
                        column: x => x.StoredCardId,
                        principalSchema: "Customer",
                        principalTable: "StoredCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_SubMerchantId",
                        column: x => x.SubMerchantId,
                        principalSchema: "Merchant",
                        principalTable: "SubMerchant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "CurrencyType",
                columns: new[] { "Id", "Code", "MaximumAmount", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "ZAR", 100000000L, "Zar", "R" },
                    { 2, "USD", 5000000L, "Usd", "$" },
                    { 3, "EUR", 5000000L, "Eur", "€" },
                    { 4, "GBP", 4000000L, "Gbp", "£" }
                });

            migrationBuilder.InsertData(
                schema: "Payment",
                table: "PaymentStateType",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Recorded" });

            migrationBuilder.CreateIndex(
                name: "UX_CurrencyType_Code",
                schema: "Payment",
                table: "CurrencyType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CurrencyTypeId",
                schema: "Payment",
                table: "Payment",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentStateTypeId",
                schema: "Payment",
                table: "Payment",
                column: "PaymentStateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_StoredCardId",
                schema: "Payment",
                table: "Payment",
                column: "StoredCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_SubMerchantId",
                schema: "Payment",
                table: "Payment",
                column: "SubMerchantId");

            migrationBuilder.CreateIndex(
                name: "UX_Payment_AppPaymentReference",
                schema: "Payment",
                table: "Payment",
                column: "AppPaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payment_OrderNumber",
                schema: "Payment",
                table: "Payment",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_StoredCard_CustomerReference",
                schema: "Customer",
                table: "StoredCard",
                column: "CustomerReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SubMerchant_MerchantReference",
                schema: "Merchant",
                table: "SubMerchant",
                column: "MerchantReference",
                unique: true);

            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Merchant\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Customer\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Payment\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Merchant\".\"SubMerchant\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Customer\".\"StoredCard\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"CurrencyType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Payment\".\"PaymentStateType\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT, INSERT ON \"Payment\".\"Payment\" TO \"ZapperPaymentsApiRole\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT, INSERT ON \"Payment\".\"Payment\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"PaymentStateType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Payment\".\"CurrencyType\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Customer\".\"StoredCard\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Merchant\".\"SubMerchant\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Payment\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Customer\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Merchant\" FROM \"ZapperPaymentsApiRole\";");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "CurrencyType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentStateType",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "StoredCard",
                schema: "Customer");

            migrationBuilder.DropTable(
                name: "SubMerchant",
                schema: "Merchant");
        }
    }
}
