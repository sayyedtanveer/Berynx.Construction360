using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M04M05Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~",
                table: "m03_rmc_rate_card");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_CustomerGroup_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_CustomerPartyId_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.AddColumn<string>(
                name: "PriceBasis",
                table: "m03_rmc_rate_card",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ExPlant");

            migrationBuilder.AddColumn<string>(
                name: "PriceBasis",
                table: "m03_price_list",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ExPlant");

            migrationBuilder.CreateTable(
                name: "m04_assignment_cursor",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    LastOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_assignment_cursor", x => new { x.TenantId, x.Source });
                });

            migrationBuilder.CreateTable(
                name: "m04_assignment_rule",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RegionStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_assignment_rule", x => new { x.TenantId, x.RuleId });
                });

            migrationBuilder.CreateTable(
                name: "m04_enquiry",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedSalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RequiredBy = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpectedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NextActionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstResponseDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstRespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LostReason = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_enquiry", x => new { x.TenantId, x.EnquiryId });
                    table.CheckConstraint("CK_m04_enquiry_open_followup", "\"Status\" IN ('Won','Lost') OR \"NextActionAt\" IS NOT NULL");
                    table.CheckConstraint("CK_m04_enquiry_source", "\"Source\" IN ('Phone','WalkIn','WhatsApp','Referral','SiteVisit','Website','Campaign','Repeat')");
                    table.CheckConstraint("CK_m04_enquiry_status", "\"Status\" IN ('New','Contacted','Follow-up','Quotation Sent','Negotiation','Won','Lost','On hold')");
                    table.CheckConstraint("CK_m04_enquiry_won_order", "\"Status\" <> 'Won' OR \"LinkedSalesOrderId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_m04_enquiry_m03_contact_TenantId_ContactId",
                        columns: x => new { x.TenantId, x.ContactId },
                        principalTable: "m03_contact",
                        principalColumns: new[] { "TenantId", "ContactId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m04_enquiry_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m04_enquiry_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m04_number_counter",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CalendarYear = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_number_counter", x => new { x.TenantId, x.DocumentCode, x.CalendarYear });
                    table.CheckConstraint("CK_m04_number_counter_values", "\"CalendarYear\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "m04_assignment_audit",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousOwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewOwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_assignment_audit", x => new { x.TenantId, x.AssignmentAuditId });
                    table.ForeignKey(
                        name: "FK_m04_assignment_audit_m04_enquiry_TenantId_EnquiryId",
                        columns: x => new { x.TenantId, x.EnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m04_enquiry_activity",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_enquiry_activity", x => new { x.TenantId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_m04_enquiry_activity_m04_enquiry_TenantId_EnquiryId",
                        columns: x => new { x.TenantId, x.EnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m04_enquiry_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RmcGrade = table.Column<string>(type: "text", nullable: true),
                    RmcSlumpBand = table.Column<string>(type: "text", nullable: true),
                    RmcAdmixtureCode = table.Column<string>(type: "text", nullable: true),
                    RmcPumpType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_enquiry_line", x => new { x.TenantId, x.LineId });
                    table.CheckConstraint("CK_m04_enquiry_line_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_m04_enquiry_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m04_enquiry_line_m04_enquiry_TenantId_EnquiryId",
                        columns: x => new { x.TenantId, x.EnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m04_follow_up",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowUpId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_follow_up", x => new { x.TenantId, x.FollowUpId });
                    table.CheckConstraint("CK_m04_follow_up_status", "\"Status\" IN ('Pending','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_m04_follow_up_m04_enquiry_TenantId_EnquiryId",
                        columns: x => new { x.TenantId, x.EnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m04_sla_alert",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m04_sla_alert", x => new { x.TenantId, x.EnquiryId });
                    table.ForeignKey(
                        name: "FK_m04_sla_alert_m04_enquiry_TenantId_EnquiryId",
                        columns: x => new { x.TenantId, x.EnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m05_quotation",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsActiveRevision = table.Column<bool>(type: "boolean", nullable: false),
                    SourceEnquiryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalespersonUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PriceBasis = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CompanyLegalNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SellerGstinSnapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerGstinSnapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CustomerAddressSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SiteAddressSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TermsAndConditions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChargesTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: true),
                    CostStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreditStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreditSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ApprovalSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    RateComparisonJson = table.Column<string>(type: "jsonb", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m05_quotation", x => new { x.TenantId, x.QuotationId });
                    table.CheckConstraint("CK_m05_quotation_amounts", "\"Subtotal\" >= 0 AND \"DiscountTotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0");
                    table.CheckConstraint("CK_m05_quotation_price_basis", "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");
                    table.CheckConstraint("CK_m05_quotation_revision", "\"RevisionNumber\" > 0");
                    table.CheckConstraint("CK_m05_quotation_status", "\"Status\" IN ('Draft','Pending approval','Approved','Sent','Accepted','Rejected','Expired','Revised','Cancelled')");
                    table.ForeignKey(
                        name: "FK_m05_quotation_m03_contact_TenantId_ContactId",
                        columns: x => new { x.TenantId, x.ContactId },
                        principalTable: "m03_contact",
                        principalColumns: new[] { "TenantId", "ContactId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m05_quotation_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m05_quotation_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m05_quotation_m04_enquiry_TenantId_SourceEnquiryId",
                        columns: x => new { x.TenantId, x.SourceEnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m05_quotation_activity",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m05_quotation_activity", x => new { x.TenantId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_m05_quotation_activity_m05_quotation_TenantId_QuotationId",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m05_quotation_charge",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTreatmentSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m05_quotation_charge", x => new { x.TenantId, x.ChargeId });
                    table.CheckConstraint("CK_m05_quotation_charge_values", "\"Amount\" >= 0 AND \"TaxRatePercent\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m05_quotation_charge_m03_charge_type_TenantId_ChargeTypeId",
                        columns: x => new { x.TenantId, x.ChargeTypeId },
                        principalTable: "m03_charge_type",
                        principalColumns: new[] { "TenantId", "ChargeTypeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m05_quotation_charge_m05_quotation_TenantId_QuotationId",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m05_quotation_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    GstPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateSource = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RmcGrade = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RmcSlumpBand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RmcAdmixtureCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RmcPumpType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TaxTreatmentSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m05_quotation_line", x => new { x.TenantId, x.LineId });
                    table.CheckConstraint("CK_m05_quotation_line_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_m05_quotation_line_values", "\"Rate\" >= 0 AND \"DiscountPercent\" BETWEEN 0 AND 100 AND \"GstPercent\" >= 0 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m05_quotation_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m05_quotation_line_m05_quotation_TenantId_QuotationId",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m05_quotation_link",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m05_quotation_link", x => new { x.TenantId, x.LinkId });
                    table.ForeignKey(
                        name: "FK_m05_quotation_link_m05_quotation_TenantId_QuotationId",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~",
                table: "m03_rmc_rate_card",
                columns: new[] { "TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "PriceBasis", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_m03_rmc_rate_card_basis",
                table: "m03_rmc_rate_card",
                sql: "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_CustomerGroup_PriceBasis_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "CustomerGroup", "PriceBasis", "ValidFrom" },
                unique: true,
                filter: "\"CustomerGroup\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_CustomerPartyId_PriceBasis_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "CustomerPartyId", "PriceBasis", "ValidFrom" },
                unique: true,
                filter: "\"CustomerPartyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_PriceBasis_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "PriceBasis", "ValidFrom" },
                unique: true,
                filter: "\"IsDefault\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_m03_price_list_basis",
                table: "m03_price_list",
                sql: "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");

            migrationBuilder.CreateIndex(
                name: "IX_m04_assignment_audit_TenantId_EnquiryId_At",
                table: "m04_assignment_audit",
                columns: new[] { "TenantId", "EnquiryId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_assignment_rule_TenantId_Source_RegionStateCode_Priority",
                table: "m04_assignment_rule",
                columns: new[] { "TenantId", "Source", "RegionStateCode", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_ContactId",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_CustomerPartyId",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "CustomerPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_IdempotencyKey",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_Number",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_OwnerUserId_Status_BranchId",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "OwnerUserId", "Status", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_SiteId",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_TenantId_Status_NextActionAt",
                table: "m04_enquiry",
                columns: new[] { "TenantId", "Status", "NextActionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_activity_TenantId_EnquiryId_At",
                table: "m04_enquiry_activity",
                columns: new[] { "TenantId", "EnquiryId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_line_TenantId_EnquiryId",
                table: "m04_enquiry_line",
                columns: new[] { "TenantId", "EnquiryId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_enquiry_line_TenantId_ProductId",
                table: "m04_enquiry_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_follow_up_TenantId_EnquiryId",
                table: "m04_follow_up",
                columns: new[] { "TenantId", "EnquiryId" });

            migrationBuilder.CreateIndex(
                name: "IX_m04_follow_up_TenantId_Status_DueAt_OwnerUserId",
                table: "m04_follow_up",
                columns: new[] { "TenantId", "Status", "DueAt", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_ContactId",
                table: "m05_quotation",
                columns: new[] { "TenantId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_CustomerPartyId_Status_CreatedAt",
                table: "m05_quotation",
                columns: new[] { "TenantId", "CustomerPartyId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_IdempotencyKey",
                table: "m05_quotation",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_Number",
                table: "m05_quotation",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "\"Number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_SalespersonUserId_Status_BranchId",
                table: "m05_quotation",
                columns: new[] { "TenantId", "SalespersonUserId", "Status", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_SeriesId_RevisionNumber",
                table: "m05_quotation",
                columns: new[] { "TenantId", "SeriesId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_SiteId",
                table: "m05_quotation",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_TenantId_SourceEnquiryId",
                table: "m05_quotation",
                columns: new[] { "TenantId", "SourceEnquiryId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_activity_TenantId_QuotationId_At",
                table: "m05_quotation_activity",
                columns: new[] { "TenantId", "QuotationId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_charge_TenantId_ChargeTypeId",
                table: "m05_quotation_charge",
                columns: new[] { "TenantId", "ChargeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_charge_TenantId_QuotationId",
                table: "m05_quotation_charge",
                columns: new[] { "TenantId", "QuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_line_TenantId_ProductId",
                table: "m05_quotation_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_line_TenantId_QuotationId",
                table: "m05_quotation_line",
                columns: new[] { "TenantId", "QuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_link_TenantId_QuotationId",
                table: "m05_quotation_link",
                columns: new[] { "TenantId", "QuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m05_quotation_link_TenantId_TokenHash",
                table: "m05_quotation_link",
                columns: new[] { "TenantId", "TokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m04_assignment_audit");

            migrationBuilder.DropTable(
                name: "m04_assignment_cursor");

            migrationBuilder.DropTable(
                name: "m04_assignment_rule");

            migrationBuilder.DropTable(
                name: "m04_enquiry_activity");

            migrationBuilder.DropTable(
                name: "m04_enquiry_line");

            migrationBuilder.DropTable(
                name: "m04_follow_up");

            migrationBuilder.DropTable(
                name: "m04_number_counter");

            migrationBuilder.DropTable(
                name: "m04_sla_alert");

            migrationBuilder.DropTable(
                name: "m05_quotation_activity");

            migrationBuilder.DropTable(
                name: "m05_quotation_charge");

            migrationBuilder.DropTable(
                name: "m05_quotation_line");

            migrationBuilder.DropTable(
                name: "m05_quotation_link");

            migrationBuilder.DropTable(
                name: "m05_quotation");

            migrationBuilder.DropTable(
                name: "m04_enquiry");

            migrationBuilder.DropIndex(
                name: "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~",
                table: "m03_rmc_rate_card");

            migrationBuilder.DropCheckConstraint(
                name: "ck_m03_rmc_rate_card_basis",
                table: "m03_rmc_rate_card");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_CustomerGroup_PriceBasis_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_CustomerPartyId_PriceBasis_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.DropIndex(
                name: "IX_m03_price_list_TenantId_PriceBasis_ValidFrom",
                table: "m03_price_list");

            migrationBuilder.DropCheckConstraint(
                name: "ck_m03_price_list_basis",
                table: "m03_price_list");

            migrationBuilder.DropColumn(
                name: "PriceBasis",
                table: "m03_rmc_rate_card");

            migrationBuilder.DropColumn(
                name: "PriceBasis",
                table: "m03_price_list");

            migrationBuilder.CreateIndex(
                name: "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~",
                table: "m03_rmc_rate_card",
                columns: new[] { "TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_CustomerGroup_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "CustomerGroup", "ValidFrom" },
                unique: true,
                filter: "\"CustomerGroup\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_CustomerPartyId_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "CustomerPartyId", "ValidFrom" },
                unique: true,
                filter: "\"CustomerPartyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_TenantId_ValidFrom",
                table: "m03_price_list",
                columns: new[] { "TenantId", "ValidFrom" },
                unique: true,
                filter: "\"IsDefault\"");
        }
    }
}
