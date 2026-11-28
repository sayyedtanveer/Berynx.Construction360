using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M08ReceiverEvidenceReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceDecisionReason",
                table: "m08_trip",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverVerifiedByUserId",
                table: "m08_trip",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_ReceiverVerifiedByUserId",
                table: "m08_trip",
                columns: new[] { "TenantId", "ReceiverVerifiedByUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_m08_trip_m02_user_TenantId_ReceiverVerifiedByUserId",
                table: "m08_trip",
                columns: new[] { "TenantId", "ReceiverVerifiedByUserId" },
                principalTable: "m02_user",
                principalColumns: new[] { "TenantId", "UserId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_m08_trip_m02_user_TenantId_ReceiverVerifiedByUserId",
                table: "m08_trip");

            migrationBuilder.DropIndex(
                name: "IX_m08_trip_TenantId_ReceiverVerifiedByUserId",
                table: "m08_trip");

            migrationBuilder.DropColumn(
                name: "EvidenceDecisionReason",
                table: "m08_trip");

            migrationBuilder.DropColumn(
                name: "ReceiverVerifiedByUserId",
                table: "m08_trip");
        }
    }
}
