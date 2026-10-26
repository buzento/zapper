using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantDataReaderRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Error\", \"Migration\", \"Merchant\", \"Customer\", \"Payment\" TO \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("GRANT SELECT ON ALL TABLES IN SCHEMA \"Error\", \"Migration\", \"Merchant\", \"Customer\", \"Payment\" TO \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES GRANT SELECT ON TABLES TO \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES GRANT USAGE ON SCHEMAS TO \"ZapperDataReaderRole\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES REVOKE USAGE ON SCHEMAS FROM \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES REVOKE SELECT ON TABLES FROM \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("REVOKE SELECT ON ALL TABLES IN SCHEMA \"Error\", \"Migration\", \"Merchant\", \"Customer\", \"Payment\" FROM \"ZapperDataReaderRole\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Error\", \"Migration\", \"Merchant\", \"Customer\", \"Payment\" FROM \"ZapperDataReaderRole\";");
        }
    }
}
