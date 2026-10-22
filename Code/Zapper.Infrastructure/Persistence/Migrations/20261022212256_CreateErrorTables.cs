using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateErrorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Error");

            migrationBuilder.CreateTable(
                name: "ErrorMessage",
                schema: "Error",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorStatus",
                schema: "Error",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorStatus", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "Error",
                table: "ErrorStatus",
                columns: new[] { "Id", "Code", "Detail", "StatusCode", "Title" },
                values: new object[,]
                {
                    { new Guid("09908ee6-b837-4d43-93ba-7298b54275aa"), "TooManyRequests", "Too many requests were sent. Try again later.", 429, "Too Many Requests" },
                    { new Guid("1d4a8187-a0d7-44ab-96e6-143ec5b64b26"), "Forbidden", "You do not have permission to do this.", 403, "Forbidden" },
                    { new Guid("475d7bb9-b876-4573-b357-5dd9cb36774b"), "Internal", "The request could not be served.", 500, "Internal Server Error" },
                    { new Guid("ada439ce-da86-415d-89ed-c228654c13bb"), "UnsupportedMediaType", "That content type is not supported.", 415, "Unsupported Media Type" },
                    { new Guid("b53c6f07-22a7-498c-834b-cdce4fa5913e"), "BadRequest", "The request was not accepted.", 400, "Bad Request" },
                    { new Guid("bfc0cf2d-96a5-4dc4-8e0d-02af8587622d"), "MethodNotAllowed", "That method is not allowed for this address.", 405, "Method Not Allowed" },
                    { new Guid("d05419eb-4175-44ff-924b-c45cc3433e25"), "NotFound", "That record could not be found.", 404, "Not Found" },
                    { new Guid("f2d40348-d6b7-4f61-bbb8-b1104c15449e"), "Unauthorized", "Sign in to continue.", 401, "Unauthorized" },
                    { new Guid("fa7373fb-9f1b-4da7-b360-f955887d320e"), "RequestTooLarge", "The request was too large.", 413, "Content Too Large" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_ErrorMessage_Code",
                schema: "Error",
                table: "ErrorMessage",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ErrorStatus_StatusCode",
                schema: "Error",
                table: "ErrorStatus",
                column: "StatusCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorMessage",
                schema: "Error");

            migrationBuilder.DropTable(
                name: "ErrorStatus",
                schema: "Error");
        }
    }
}
