using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M03PartyOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalespersonUserId",
                table: "m03_party",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "m03_party",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_SalespersonUserId",
                table: "m03_party",
                columns: new[] { "TenantId", "SalespersonUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_m03_party_m02_user_TenantId_SalespersonUserId",
                table: "m03_party",
                columns: new[] { "TenantId", "SalespersonUserId" },
                principalTable: "m02_user",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_m03_party_m02_user_TenantId_SalespersonUserId",
                table: "m03_party");

            migrationBuilder.DropIndex(
                name: "IX_m03_party_TenantId_SalespersonUserId",
                table: "m03_party");

            migrationBuilder.DropColumn(
                name: "SalespersonUserId",
                table: "m03_party");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "m03_party");
        }
    }
}
