using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M18SecureEvidenceStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m18_document",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScanStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StorageStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m18_document", x => new { x.TenantId, x.DocumentId });
                    table.CheckConstraint("CK_m18_document_purpose", "\"Purpose\" IN ('Signature', 'Photo')");
                    table.CheckConstraint("CK_m18_document_scan_status", "\"ScanStatus\" IN ('Pending', 'Clean', 'Rejected')");
                    table.CheckConstraint("CK_m18_document_size", "\"SizeBytes\" > 0");
                    table.CheckConstraint("CK_m18_document_storage_status", "\"StorageStatus\" IN ('Quarantined', 'Available', 'Deleted')");
                    table.CheckConstraint("CK_m18_document_version", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_m18_document_m02_user_TenantId_UploadedByUserId",
                        columns: x => new { x.TenantId, x.UploadedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m18_document_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m18_document_m18_document_TenantId_PreviousDocumentId",
                        columns: x => new { x.TenantId, x.PreviousDocumentId },
                        principalTable: "m18_document",
                        principalColumns: new[] { "TenantId", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m18_document_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m18_document_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m18_document_event_m02_user_TenantId_ActorUserId",
                        columns: x => new { x.TenantId, x.ActorUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m18_document_event_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m18_document_event_m18_document_TenantId_DocumentId",
                        columns: x => new { x.TenantId, x.DocumentId },
                        principalTable: "m18_document",
                        principalColumns: new[] { "TenantId", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_TenantId_PreviousDocumentId",
                table: "m18_document",
                columns: new[] { "TenantId", "PreviousDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_TenantId_TripId_Purpose_IsCurrent",
                table: "m18_document",
                columns: new[] { "TenantId", "TripId", "Purpose", "IsCurrent" },
                unique: true,
                filter: "\"IsCurrent\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_TenantId_TripId_Purpose_VersionNumber",
                table: "m18_document",
                columns: new[] { "TenantId", "TripId", "Purpose", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_TenantId_UploadedAt",
                table: "m18_document",
                columns: new[] { "TenantId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_TenantId_UploadedByUserId",
                table: "m18_document",
                columns: new[] { "TenantId", "UploadedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_event_TenantId_ActorUserId",
                table: "m18_document_event",
                columns: new[] { "TenantId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_event_TenantId_DocumentId_OccurredAt",
                table: "m18_document_event",
                columns: new[] { "TenantId", "DocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m18_document_event_TenantId_TripId_OccurredAt",
                table: "m18_document_event",
                columns: new[] { "TenantId", "TripId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m18_document_event");

            migrationBuilder.DropTable(
                name: "m18_document");
        }
    }
}
