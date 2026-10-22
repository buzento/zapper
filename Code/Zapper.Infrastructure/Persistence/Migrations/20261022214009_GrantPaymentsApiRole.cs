using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantPaymentsApiRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Error\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Migration\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Error\".\"ErrorStatus\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Error\".\"ErrorMessage\" TO \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Migration\".\"MigrationHistory\" TO \"ZapperPaymentsApiRole\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT ON \"Migration\".\"MigrationHistory\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Error\".\"ErrorMessage\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Error\".\"ErrorStatus\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Migration\" FROM \"ZapperPaymentsApiRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Error\" FROM \"ZapperPaymentsApiRole\";");
        }
    }
}
