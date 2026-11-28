using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M12TenantLedgerMappingsAndPostingLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PostedAt",
                table: "m11_receipt",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostedByUserId",
                table: "m11_receipt",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoucherId",
                table: "m11_receipt",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoucherId",
                table: "m10_invoice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiverVerifiedAt",
                table: "m09_delivery_challan",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "m12_ledger_mapping",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    LedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_ledger_mapping", x => new { x.TenantId, x.MappingId });
                    table.CheckConstraint("CK_m12_ledger_mapping_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                });

            migrationBuilder.CreateTable(
                name: "m12_ledger_mapping_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_ledger_mapping_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m12_ledger_mapping_event_m12_ledger_mapping_TenantId_Mappin~",
                        columns: x => new { x.TenantId, x.MappingId },
                        principalTable: "m12_ledger_mapping",
                        principalColumns: new[] { "TenantId", "MappingId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m11_receipt_TenantId_VoucherId",
                table: "m11_receipt",
                columns: new[] { "TenantId", "VoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_TenantId_VoucherId",
                table: "m10_invoice",
                columns: new[] { "TenantId", "VoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_ledger_mapping_TenantId_BranchId_Gstin_MappingCode_Effe~",
                table: "m12_ledger_mapping",
                columns: new[] { "TenantId", "BranchId", "Gstin", "MappingCode", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m12_ledger_mapping_TenantId_BranchId_Gstin_MappingCode_IsAc~",
                table: "m12_ledger_mapping",
                columns: new[] { "TenantId", "BranchId", "Gstin", "MappingCode", "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_ledger_mapping_event_TenantId_MappingId_OccurredAt",
                table: "m12_ledger_mapping_event",
                columns: new[] { "TenantId", "MappingId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_m10_invoice_m12_voucher_TenantId_VoucherId",
                table: "m10_invoice",
                columns: new[] { "TenantId", "VoucherId" },
                principalTable: "m12_voucher",
                principalColumns: new[] { "TenantId", "VoucherId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_m11_receipt_m12_voucher_TenantId_VoucherId",
                table: "m11_receipt",
                columns: new[] { "TenantId", "VoucherId" },
                principalTable: "m12_voucher",
                principalColumns: new[] { "TenantId", "VoucherId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_m10_invoice_m12_voucher_TenantId_VoucherId",
                table: "m10_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_m11_receipt_m12_voucher_TenantId_VoucherId",
                table: "m11_receipt");

            migrationBuilder.DropTable(
                name: "m12_ledger_mapping_event");

            migrationBuilder.DropTable(
                name: "m12_ledger_mapping");

            migrationBuilder.DropIndex(
                name: "IX_m11_receipt_TenantId_VoucherId",
                table: "m11_receipt");

            migrationBuilder.DropIndex(
                name: "IX_m10_invoice_TenantId_VoucherId",
                table: "m10_invoice");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "m11_receipt");

            migrationBuilder.DropColumn(
                name: "PostedByUserId",
                table: "m11_receipt");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "m11_receipt");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "m10_invoice");

            migrationBuilder.DropColumn(
                name: "ReceiverVerifiedAt",
                table: "m09_delivery_challan");
        }
    }
}
