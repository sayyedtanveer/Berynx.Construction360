using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M02TenantMfaEnforcement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaSatisfied",
                table: "m02_auth_session",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MfaSatisfied",
                table: "m02_auth_session");
        }
    }
}
