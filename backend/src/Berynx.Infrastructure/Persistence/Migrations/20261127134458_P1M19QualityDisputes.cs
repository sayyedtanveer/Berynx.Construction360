using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M19QualityDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m19_quality_criteria",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Grade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TestAgeDays = table.Column<int>(type: "integer", nullable: false),
                    MinimumStrengthMpa = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    StandardReference = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_quality_criteria", x => new { x.TenantId, x.CriteriaId });
                    table.CheckConstraint("CK_m19_criteria_age", "\"TestAgeDays\" BETWEEN 1 AND 365");
                    table.CheckConstraint("CK_m19_criteria_effective", "\"EffectiveUntil\" IS NULL OR \"EffectiveUntil\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_m19_criteria_strength", "\"MinimumStrengthMpa\" > 0");
                    table.ForeignKey(
                        name: "FK_m19_quality_criteria_m02_user_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_quality_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_quality_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m19_quality_event_m02_user_TenantId_ActorUserId",
                        columns: x => new { x.TenantId, x.ActorUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_quality_sample",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    PourId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    GradeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CastAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CubeCount = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_quality_sample", x => new { x.TenantId, x.SampleId });
                    table.CheckConstraint("CK_m19_sample_cube_count", "\"CubeCount\" IN (3, 6)");
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m02_user_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m02_user_TenantId_OwnerUserId",
                        columns: x => new { x.TenantId, x.OwnerUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m06_sales_order_line_TenantId_SalesOrder~",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m08_pour_TenantId_PourId",
                        columns: x => new { x.TenantId, x.PourId },
                        principalTable: "m08_pour",
                        principalColumns: new[] { "TenantId", "PourId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_sample_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_dispute",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChallanId = table.Column<Guid>(type: "uuid", nullable: true),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_dispute", x => new { x.TenantId, x.DisputeId });
                    table.CheckConstraint("CK_m19_dispute_source", "\"SalesOrderId\" IS NOT NULL OR \"TripId\" IS NOT NULL OR \"ChallanId\" IS NOT NULL OR \"SampleId\" IS NOT NULL OR \"CustomerPartyId\" IS NOT NULL OR \"SupplierPartyId\" IS NOT NULL");
                    table.CheckConstraint("CK_m19_dispute_status", "\"Status\" IN ('Open', 'Investigating', 'Resolved', 'Escalated', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_m19_dispute_m02_user_TenantId_OpenedByUserId",
                        columns: x => new { x.TenantId, x.OpenedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m02_user_TenantId_OwnerUserId",
                        columns: x => new { x.TenantId, x.OwnerUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m02_user_TenantId_ResolvedByUserId",
                        columns: x => new { x.TenantId, x.ResolvedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m09_delivery_challan_TenantId_ChallanId",
                        columns: x => new { x.TenantId, x.ChallanId },
                        principalTable: "m09_delivery_challan",
                        principalColumns: new[] { "TenantId", "ChallanId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_m19_quality_sample_TenantId_SampleId",
                        columns: x => new { x.TenantId, x.SampleId },
                        principalTable: "m19_quality_sample",
                        principalColumns: new[] { "TenantId", "SampleId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_quality_test",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestAgeDays = table.Column<int>(type: "integer", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MinimumStrengthMpaSnapshot = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    AcceptanceReferenceSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StrengthMpa = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReportDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_quality_test", x => new { x.TenantId, x.TestId });
                    table.CheckConstraint("CK_m19_test_age", "\"TestAgeDays\" BETWEEN 1 AND 365");
                    table.CheckConstraint("CK_m19_test_strength", "\"StrengthMpa\" IS NULL OR \"StrengthMpa\" >= 0");
                    table.ForeignKey(
                        name: "FK_m19_quality_test_m02_user_TenantId_RecordedByUserId",
                        columns: x => new { x.TenantId, x.RecordedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_test_m18_document_TenantId_ReportDocumentId",
                        columns: x => new { x.TenantId, x.ReportDocumentId },
                        principalTable: "m18_document",
                        principalColumns: new[] { "TenantId", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_quality_test_m19_quality_sample_TenantId_SampleId",
                        columns: x => new { x.TenantId, x.SampleId },
                        principalTable: "m19_quality_sample",
                        principalColumns: new[] { "TenantId", "SampleId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_dispute_evidence",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_dispute_evidence", x => new { x.TenantId, x.EvidenceId });
                    table.ForeignKey(
                        name: "FK_m19_dispute_evidence_m02_user_TenantId_AddedByUserId",
                        columns: x => new { x.TenantId, x.AddedByUserId },
                        principalTable: "m02_user",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_evidence_m18_document_TenantId_DocumentId",
                        columns: x => new { x.TenantId, x.DocumentId },
                        principalTable: "m18_document",
                        principalColumns: new[] { "TenantId", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_evidence_m19_dispute_TenantId_DisputeId",
                        columns: x => new { x.TenantId, x.DisputeId },
                        principalTable: "m19_dispute",
                        principalColumns: new[] { "TenantId", "DisputeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m19_dispute_invoice",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m19_dispute_invoice", x => new { x.TenantId, x.DisputeId, x.InvoiceId });
                    table.ForeignKey(
                        name: "FK_m19_dispute_invoice_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m19_dispute_invoice_m19_dispute_TenantId_DisputeId",
                        columns: x => new { x.TenantId, x.DisputeId },
                        principalTable: "m19_dispute",
                        principalColumns: new[] { "TenantId", "DisputeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_BranchId_Status_OpenedAt",
                table: "m19_dispute",
                columns: new[] { "TenantId", "BranchId", "Status", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_ChallanId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "ChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_CustomerPartyId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "CustomerPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_IdempotencyKey",
                table: "m19_dispute",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_OpenedByUserId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "OpenedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_OwnerUserId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_ResolvedByUserId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "ResolvedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_SalesOrderId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_SampleId_Status",
                table: "m19_dispute",
                columns: new[] { "TenantId", "SampleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_SiteId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_SupplierPartyId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "SupplierPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_TenantId_TripId",
                table: "m19_dispute",
                columns: new[] { "TenantId", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_evidence_TenantId_AddedByUserId",
                table: "m19_dispute_evidence",
                columns: new[] { "TenantId", "AddedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_evidence_TenantId_DisputeId_AddedAt",
                table: "m19_dispute_evidence",
                columns: new[] { "TenantId", "DisputeId", "AddedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_evidence_TenantId_DisputeId_DocumentId",
                table: "m19_dispute_evidence",
                columns: new[] { "TenantId", "DisputeId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_evidence_TenantId_DocumentId",
                table: "m19_dispute_evidence",
                columns: new[] { "TenantId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_dispute_invoice_TenantId_InvoiceId",
                table: "m19_dispute_invoice",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_criteria_TenantId_CreatedByUserId",
                table: "m19_quality_criteria",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_criteria_TenantId_MaterialType_Grade_TestAgeDay~",
                table: "m19_quality_criteria",
                columns: new[] { "TenantId", "MaterialType", "Grade", "TestAgeDays", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_event_TenantId_ActorUserId",
                table: "m19_quality_event",
                columns: new[] { "TenantId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_event_TenantId_EntityType_EntityId_OccurredAt",
                table: "m19_quality_event",
                columns: new[] { "TenantId", "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_BranchId_Status_CastAt",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "BranchId", "Status", "CastAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_CreatedByUserId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_IdempotencyKey",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_OwnerUserId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_PourId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "PourId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_ProductId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_SalesOrderId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_SalesOrderLineId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "SalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_SampleNumber",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "SampleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_SiteId",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_sample_TenantId_TripId_CreatedAt",
                table: "m19_quality_sample",
                columns: new[] { "TenantId", "TripId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_test_TenantId_RecordedByUserId",
                table: "m19_quality_test",
                columns: new[] { "TenantId", "RecordedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_test_TenantId_ReportDocumentId",
                table: "m19_quality_test",
                columns: new[] { "TenantId", "ReportDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_test_TenantId_SampleId_TestAgeDays",
                table: "m19_quality_test",
                columns: new[] { "TenantId", "SampleId", "TestAgeDays" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m19_quality_test_TenantId_Status_DueAt",
                table: "m19_quality_test",
                columns: new[] { "TenantId", "Status", "DueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m19_dispute_evidence");

            migrationBuilder.DropTable(
                name: "m19_dispute_invoice");

            migrationBuilder.DropTable(
                name: "m19_quality_criteria");

            migrationBuilder.DropTable(
                name: "m19_quality_event");

            migrationBuilder.DropTable(
                name: "m19_quality_test");

            migrationBuilder.DropTable(
                name: "m19_dispute");

            migrationBuilder.DropTable(
                name: "m19_quality_sample");
        }
    }
}
