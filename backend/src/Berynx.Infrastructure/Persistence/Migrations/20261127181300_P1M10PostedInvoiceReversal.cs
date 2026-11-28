using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M10PostedInvoiceReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_m10_invoice_challan_TenantId_ChallanId",
                table: "m10_invoice_challan");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReleasedAt",
                table: "m10_invoice_challan",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_TenantId_ReversalOfVoucherId",
                table: "m12_voucher",
                columns: new[] { "TenantId", "ReversalOfVoucherId" },
                unique: true,
                filter: "\"ReversalOfVoucherId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_challan_TenantId_ChallanId",
                table: "m10_invoice_challan",
                columns: new[] { "TenantId", "ChallanId" },
                unique: true,
                filter: "\"ReleasedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_m12_voucher_m12_voucher_TenantId_ReversalOfVoucherId",
                table: "m12_voucher",
                columns: new[] { "TenantId", "ReversalOfVoucherId" },
                principalTable: "m12_voucher",
                principalColumns: new[] { "TenantId", "VoucherId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_m12_voucher_m12_voucher_TenantId_ReversalOfVoucherId",
                table: "m12_voucher");

            migrationBuilder.DropIndex(
                name: "IX_m12_voucher_TenantId_ReversalOfVoucherId",
                table: "m12_voucher");

            migrationBuilder.DropIndex(
                name: "IX_m10_invoice_challan_TenantId_ChallanId",
                table: "m10_invoice_challan");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "m10_invoice_challan");

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_challan_TenantId_ChallanId",
                table: "m10_invoice_challan",
                columns: new[] { "TenantId", "ChallanId" },
                unique: true);
        }
    }
}
