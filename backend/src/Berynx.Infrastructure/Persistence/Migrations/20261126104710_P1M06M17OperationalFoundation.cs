using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M06M17OperationalFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m06_sales_order",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteAddressSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CustomerPoNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerPoDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomerPoAttachmentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceQuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceQuotationNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SourceEnquiryId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SalesOwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateValidityUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChargesTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreditSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CostStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    EstimatedMarginPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: true),
                    ApprovalSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShortClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_sales_order", x => new { x.TenantId, x.SalesOrderId });
                    table.CheckConstraint("CK_m06_sales_order_amounts", "\"Subtotal\" >= 0 AND \"DiscountTotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0");
                    table.CheckConstraint("CK_m06_sales_order_status", "\"Status\" IN ('Draft','Pending approval','Confirmed','In fulfilment','Completed','Short-closed','On hold','Cancelled')");
                    table.CheckConstraint("CK_m06_sales_order_type", "\"OrderType\" IN ('RMC','Material','Mixed')");
                    table.ForeignKey(
                        name: "FK_m06_sales_order_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_m04_enquiry_TenantId_SourceEnquiryId",
                        columns: x => new { x.TenantId, x.SourceEnquiryId },
                        principalTable: "m04_enquiry",
                        principalColumns: new[] { "TenantId", "EnquiryId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_m05_quotation_TenantId_SourceQuotationId",
                        columns: x => new { x.TenantId, x.SourceQuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_activity",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_activity", x => new { x.TenantId, x.ActivityId });
                });

            migrationBuilder.CreateTable(
                name: "m07_number_counter",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FinancialYearStart = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_number_counter", x => new { x.TenantId, x.BranchId, x.DocumentCode, x.FinancialYearStart });
                    table.CheckConstraint("CK_m07_number_counter_values", "\"FinancialYearStart\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "m07_supplier_payment",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DepositAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TdsAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BankCharges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBankAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_supplier_payment", x => new { x.TenantId, x.SupplierPaymentId });
                    table.CheckConstraint("CK_m07_supplier_payment_amounts", "\"GrossAmount\" > 0 AND \"TdsAmount\" >= 0 AND \"BankCharges\" >= 0 AND \"NetBankAmount\" >= 0 AND \"TdsAmount\" + \"BankCharges\" <= \"GrossAmount\"");
                    table.ForeignKey(
                        name: "FK_m07_supplier_payment_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m09_challan_number_counter",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FinancialYearStart = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m09_challan_number_counter", x => new { x.TenantId, x.BranchId, x.DocumentCode, x.FinancialYearStart });
                    table.CheckConstraint("CK_m09_challan_number_counter_values", "\"FinancialYearStart\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "m10_invoice",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    InvoiceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TaxTreatment = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CustomerGstinSnapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    BranchGstinSnapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TcsAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RoundOffAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PostingSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    EInvoiceStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Irn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AckNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EwayBillNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EwayBillValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisputeHold = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m10_invoice", x => new { x.TenantId, x.InvoiceId });
                    table.CheckConstraint("CK_m10_invoice_amounts", "\"TaxableAmount\" >= 0 AND \"CgstAmount\" >= 0 AND \"SgstAmount\" >= 0 AND \"IgstAmount\" >= 0 AND \"TcsAmount\" >= 0 AND \"TotalAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m10_invoice_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m11_receipt",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepositAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TdsAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BankCharges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m11_receipt", x => new { x.TenantId, x.ReceiptId });
                    table.CheckConstraint("CK_m11_receipt_amounts", "\"Amount\" > 0 AND \"TdsAmount\" >= 0 AND \"BankCharges\" >= 0 AND \"AllocatedAmount\" >= 0 AND \"AdvanceAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m11_receipt_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m12_financial_period",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_financial_period", x => new { x.TenantId, x.PeriodStart });
                    table.CheckConstraint("CK_m12_financial_period_dates", "\"PeriodEnd\" >= \"PeriodStart\"");
                });

            migrationBuilder.CreateTable(
                name: "m12_period_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_period_event", x => new { x.TenantId, x.EventId });
                });

            migrationBuilder.CreateTable(
                name: "m12_voucher",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VoucherType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    VoucherDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Narration = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TotalDebit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReversalOfVoucherId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_voucher", x => new { x.TenantId, x.VoucherId });
                    table.CheckConstraint("CK_m12_voucher_amounts", "\"TotalDebit\" > 0 AND \"TotalDebit\" = \"TotalCredit\"");
                });

            migrationBuilder.CreateTable(
                name: "m12_voucher_number_counter",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CalendarYear = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_voucher_number_counter", x => new { x.TenantId, x.BranchId, x.VoucherType, x.CalendarYear });
                    table.CheckConstraint("CK_m12_voucher_number_counter_values", "\"CalendarYear\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "m13_gst_period",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FiledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalAcknowledgement = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FilingSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m13_gst_period", x => new { x.TenantId, x.PeriodStart });
                    table.CheckConstraint("CK_m13_gst_period_dates", "\"PeriodEnd\" >= \"PeriodStart\"");
                });

            migrationBuilder.CreateTable(
                name: "m14_expense_category",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ApprovalThreshold = table.Column<decimal>(type: "numeric", nullable: true),
                    ExpenseLedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m14_expense_category", x => new { x.TenantId, x.CategoryId });
                    table.CheckConstraint("CK_m14_expense_category_type", "\"Type\" IN ('Direct','Indirect','Capital','Other income')");
                });

            migrationBuilder.CreateTable(
                name: "m16_export_log",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FiltersJson = table.Column<string>(type: "jsonb", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m16_export_log", x => new { x.TenantId, x.ExportId });
                });

            migrationBuilder.CreateTable(
                name: "m17_catalogue_item",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImageReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    KeySpecs = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IndicativePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m17_catalogue_item", x => new { x.TenantId, x.CatalogueItemId });
                    table.CheckConstraint("CK_m17_catalogue_price", "\"IndicativePrice\" IS NULL OR \"IndicativePrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_m17_catalogue_item_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m17_consent_record",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsOptedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Wording = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OptedOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OptOutSource = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m17_consent_record", x => new { x.TenantId, x.ConsentId });
                    table.ForeignKey(
                        name: "FK_m17_consent_record_m03_contact_TenantId_ContactId",
                        columns: x => new { x.TenantId, x.ContactId },
                        principalTable: "m03_contact",
                        principalColumns: new[] { "TenantId", "ContactId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m17_promotion_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CatalogueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m17_promotion_event", x => new { x.TenantId, x.EventId });
                });

            migrationBuilder.CreateTable(
                name: "m06_activity",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_activity", x => new { x.TenantId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_m06_activity_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_amendment",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmendmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BeforeSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProposedSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_amendment", x => new { x.TenantId, x.AmendmentId });
                    table.CheckConstraint("CK_m06_amendment_revision", "\"RevisionNumber\" > 0");
                    table.CheckConstraint("CK_m06_amendment_status", "\"Status\" IN ('Pending approval','Approved','Rejected','Cancelled')");
                    table.ForeignKey(
                        name: "FK_m06_amendment_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_linked_document",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_linked_document", x => new { x.TenantId, x.LinkId });
                    table.ForeignKey(
                        name: "FK_m06_linked_document_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_sales_order_charge",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuotationChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_sales_order_charge", x => new { x.TenantId, x.ChargeId });
                    table.CheckConstraint("CK_m06_sales_order_charge_values", "\"Amount\" >= 0 AND \"TaxRatePercent\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m06_sales_order_charge_m05_quotation_charge_TenantId_Source~",
                        columns: x => new { x.TenantId, x.SourceQuotationChargeId },
                        principalTable: "m05_quotation_charge",
                        principalColumns: new[] { "TenantId", "ChargeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_charge_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_sales_order_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuotationLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductCategorySnapshot = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    GstPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_sales_order_line", x => new { x.TenantId, x.LineId });
                    table.CheckConstraint("CK_m06_sales_order_line_values", "\"OrderedQuantity\" > 0 AND \"Rate\" >= 0 AND \"DiscountPercent\" BETWEEN 0 AND 100 AND \"GstPercent\" >= 0 AND \"DeliveredQuantity\" >= 0 AND \"InvoicedQuantity\" >= 0 AND \"PaidAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m06_sales_order_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_line_m05_quotation_line_TenantId_SourceQuot~",
                        columns: x => new { x.TenantId, x.SourceQuotationLineId },
                        principalTable: "m05_quotation_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_sales_order_line_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_purchase_order",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SupplierGstinSnapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChargesTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_purchase_order", x => new { x.TenantId, x.PurchaseOrderId });
                    table.CheckConstraint("CK_m07_purchase_order_amounts", "\"Subtotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_m06_sales_order_TenantId_SourceSalesOrde~",
                        columns: x => new { x.TenantId, x.SourceSalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m10_invoice_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m10_invoice_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m10_invoice_event_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m11_promise_to_pay",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PromiseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m11_promise_to_pay", x => new { x.TenantId, x.PromiseId });
                    table.CheckConstraint("CK_m11_promise_amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_m11_promise_to_pay_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m11_promise_to_pay_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m13_exception_review",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExceptionCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m13_exception_review", x => new { x.TenantId, x.ReviewId });
                    table.ForeignKey(
                        name: "FK_m13_exception_review_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m11_receipt_allocation",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m11_receipt_allocation", x => new { x.TenantId, x.AllocationId });
                    table.CheckConstraint("CK_m11_receipt_allocation_amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_m11_receipt_allocation_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m11_receipt_allocation_m11_receipt_TenantId_ReceiptId",
                        columns: x => new { x.TenantId, x.ReceiptId },
                        principalTable: "m11_receipt",
                        principalColumns: new[] { "TenantId", "ReceiptId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m11_receivable_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m11_receivable_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m11_receivable_event_m11_receipt_TenantId_ReceiptId",
                        columns: x => new { x.TenantId, x.ReceiptId },
                        principalTable: "m11_receipt",
                        principalColumns: new[] { "TenantId", "ReceiptId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m12_voucher_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_voucher_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m12_voucher_event_m12_voucher_TenantId_VoucherId",
                        columns: x => new { x.TenantId, x.VoucherId },
                        principalTable: "m12_voucher",
                        principalColumns: new[] { "TenantId", "VoucherId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m12_voucher_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    Narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m12_voucher_line", x => new { x.TenantId, x.VoucherLineId });
                    table.CheckConstraint("CK_m12_voucher_line_side", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)");
                    table.ForeignKey(
                        name: "FK_m12_voucher_line_m03_party_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m12_voucher_line_m12_voucher_TenantId_VoucherId",
                        columns: x => new { x.TenantId, x.VoucherId },
                        principalTable: "m12_voucher",
                        principalColumns: new[] { "TenantId", "VoucherId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m14_expense",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PaidFromLedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmployeePaid = table.Column<bool>(type: "boolean", nullable: false),
                    SupplierGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    SupplierBillNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ItcEligible = table.Column<bool>(type: "boolean", nullable: false),
                    ReverseCharge = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApprovalComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m14_expense", x => new { x.TenantId, x.ExpenseId });
                    table.CheckConstraint("CK_m14_expense_amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_m14_expense_gst", "\"GstAmount\" >= 0 AND \"GstAmount\" <= \"Amount\"");
                    table.ForeignKey(
                        name: "FK_m14_expense_m03_party_TenantId_VendorPartyId",
                        columns: x => new { x.TenantId, x.VendorPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m14_expense_m12_voucher_TenantId_VoucherId",
                        columns: x => new { x.TenantId, x.VoucherId },
                        principalTable: "m12_voucher",
                        principalColumns: new[] { "TenantId", "VoucherId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m14_expense_m14_expense_category_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "m14_expense_category",
                        principalColumns: new[] { "TenantId", "CategoryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_quotation_charge_allocation",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_quotation_charge_allocation", x => new { x.TenantId, x.AllocationId });
                    table.CheckConstraint("CK_m06_quotation_charge_allocation_values", "\"Amount\" >= 0 AND \"CgstAmount\" >= 0 AND \"SgstAmount\" >= 0 AND \"IgstAmount\" >= 0 AND \"TaxAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m06_quotation_charge_allocation_m05_quotation_TenantId_Quot~",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_charge_allocation_m05_quotation_charge_Tenant~",
                        columns: x => new { x.TenantId, x.QuotationChargeId },
                        principalTable: "m05_quotation_charge",
                        principalColumns: new[] { "TenantId", "ChargeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_charge_allocation_m06_sales_order_TenantId_Sa~",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_charge_allocation_m06_sales_order_charge_Tena~",
                        columns: x => new { x.TenantId, x.SalesOrderChargeId },
                        principalTable: "m06_sales_order_charge",
                        principalColumns: new[] { "TenantId", "ChargeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_delivery_schedule",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeWindow = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreferredPlantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_delivery_schedule", x => new { x.TenantId, x.ScheduleId });
                    table.CheckConstraint("CK_m06_delivery_schedule_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_m06_delivery_schedule_status", "\"Status\" IN ('Pending approval','Planned','Released','Cancelled')");
                    table.ForeignKey(
                        name: "FK_m06_delivery_schedule_m03_plant_TenantId_PreferredPlantId",
                        columns: x => new { x.TenantId, x.PreferredPlantId },
                        principalTable: "m03_plant",
                        principalColumns: new[] { "TenantId", "PlantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_delivery_schedule_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_delivery_schedule_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_delivery_schedule_m06_sales_order_line_TenantId_SalesOr~",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m06_quotation_allocation",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m06_quotation_allocation", x => new { x.TenantId, x.AllocationId });
                    table.CheckConstraint("CK_m06_quotation_allocation_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_m06_quotation_allocation_m05_quotation_TenantId_QuotationId",
                        columns: x => new { x.TenantId, x.QuotationId },
                        principalTable: "m05_quotation",
                        principalColumns: new[] { "TenantId", "QuotationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_allocation_m05_quotation_line_TenantId_Quotat~",
                        columns: x => new { x.TenantId, x.QuotationLineId },
                        principalTable: "m05_quotation_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_allocation_m06_sales_order_TenantId_SalesOrde~",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m06_quotation_allocation_m06_sales_order_line_TenantId_Sale~",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m10_invoice_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    HsnCodeSnapshot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ChallanReferencesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m10_invoice_line", x => new { x.TenantId, x.InvoiceLineId });
                    table.CheckConstraint("CK_m10_invoice_line_amounts", "\"Quantity\" > 0 AND \"Rate\" >= 0 AND \"TaxableAmount\" >= 0 AND \"TaxPercent\" >= 0 AND \"TotalAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m10_invoice_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m10_invoice_line_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m10_invoice_line_m06_sales_order_line_TenantId_SalesOrderLi~",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m10_invoice_line_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_goods_receipt",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DriverName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    WeighbridgeSlipNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    TareWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    NetWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    AttachmentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_goods_receipt", x => new { x.TenantId, x.GoodsReceiptId });
                    table.CheckConstraint("CK_m07_goods_receipt_status", "\"Status\" IN ('Draft', 'Pending approval', 'Posted', 'Cancelled')");
                    table.CheckConstraint("CK_m07_goods_receipt_weights", "(\"GrossWeight\" IS NULL OR \"GrossWeight\" >= 0) AND (\"TareWeight\" IS NULL OR \"TareWeight\" >= 0) AND (\"NetWeight\" IS NULL OR \"NetWeight\" >= 0)");
                    table.ForeignKey(
                        name: "FK_m07_goods_receipt_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_goods_receipt_m07_purchase_order_TenantId_PurchaseOrder~",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "m07_purchase_order",
                        principalColumns: new[] { "TenantId", "PurchaseOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_purchase_order_charge",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GstPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_purchase_order_charge", x => new { x.TenantId, x.ChargeId });
                    table.CheckConstraint("CK_m07_purchase_order_charge_amounts", "\"Amount\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_charge_m07_purchase_order_TenantId_Purch~",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "m07_purchase_order",
                        principalColumns: new[] { "TenantId", "PurchaseOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_purchase_order_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ProductCategorySnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Traceable = table.Column<bool>(type: "boolean", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GstPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_purchase_order_line", x => new { x.TenantId, x.PurchaseOrderLineId });
                    table.CheckConstraint("CK_m07_purchase_order_line_amounts", "\"OrderedQuantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"Rate\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_line_m06_sales_order_line_TenantId_Sourc~",
                        columns: x => new { x.TenantId, x.SourceSalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_purchase_order_line_m07_purchase_order_TenantId_Purchas~",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "m07_purchase_order",
                        principalColumns: new[] { "TenantId", "PurchaseOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_supplier_invoice",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TdsWithheld = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AttachmentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_supplier_invoice", x => new { x.TenantId, x.SupplierInvoiceId });
                    table.CheckConstraint("CK_m07_supplier_invoice_amounts", "\"Subtotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0 AND \"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"Total\" AND \"TdsWithheld\" >= 0");
                    table.CheckConstraint("CK_m07_supplier_invoice_status", "\"Status\" IN ('Draft', 'Matched', 'Pending approval', 'Posted', 'Part-paid', 'Paid', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_m07_purchase_order_TenantId_PurchaseOr~",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "m07_purchase_order",
                        principalColumns: new[] { "TenantId", "PurchaseOrderId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m14_expense_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m14_expense_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m14_expense_event_m14_expense_TenantId_ExpenseId",
                        columns: x => new { x.TenantId, x.ExpenseId },
                        principalTable: "m14_expense",
                        principalColumns: new[] { "TenantId", "ExpenseId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m08_pour",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PourId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SiteNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ProductCategorySnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Slump = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PlacementMethod = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PumpType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PumpQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CustomerEngineer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Coordinator = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SpecialInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ClosureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m08_pour", x => new { x.TenantId, x.PourId });
                    table.CheckConstraint("CK_m08_pour_quantities", "\"ScheduledQuantity\" > 0 AND \"DeliveredQuantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"PumpQuantity\" >= 0");
                    table.CheckConstraint("CK_m08_pour_window", "\"WindowEnd\" > \"WindowStart\"");
                    table.ForeignKey(
                        name: "FK_m08_pour_m03_plant_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "m03_plant",
                        principalColumns: new[] { "TenantId", "PlantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_pour_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_pour_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_pour_m06_delivery_schedule_TenantId_ScheduleId",
                        columns: x => new { x.TenantId, x.ScheduleId },
                        principalTable: "m06_delivery_schedule",
                        principalColumns: new[] { "TenantId", "ScheduleId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_pour_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_pour_m06_sales_order_line_TenantId_SalesOrderLineId",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_goods_receipt_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DamagedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ShortQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ExceptionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LotOrHeatNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ManufacturerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    MtcReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_goods_receipt_line", x => new { x.TenantId, x.GoodsReceiptLineId });
                    table.CheckConstraint("CK_m07_goods_receipt_line_quantities", "\"ReceivedQuantity\" > 0 AND \"DamagedQuantity\" >= 0 AND \"DamagedQuantity\" <= \"ReceivedQuantity\" AND \"ShortQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_m07_goods_receipt_line_m07_goods_receipt_TenantId_GoodsRece~",
                        columns: x => new { x.TenantId, x.GoodsReceiptId },
                        principalTable: "m07_goods_receipt",
                        principalColumns: new[] { "TenantId", "GoodsReceiptId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_goods_receipt_line_m07_purchase_order_line_TenantId_Pur~",
                        columns: x => new { x.TenantId, x.PurchaseOrderLineId },
                        principalTable: "m07_purchase_order_line",
                        principalColumns: new[] { "TenantId", "PurchaseOrderLineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_supplier_payment_allocation",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_supplier_payment_allocation", x => new { x.TenantId, x.AllocationId });
                    table.CheckConstraint("CK_m07_supplier_payment_allocation_amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_m07_supplier_payment_allocation_m07_supplier_invoice_Tenant~",
                        columns: x => new { x.TenantId, x.SupplierInvoiceId },
                        principalTable: "m07_supplier_invoice",
                        principalColumns: new[] { "TenantId", "SupplierInvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_supplier_payment_allocation_m07_supplier_payment_Tenant~",
                        columns: x => new { x.TenantId, x.SupplierPaymentId },
                        principalTable: "m07_supplier_payment",
                        principalColumns: new[] { "TenantId", "SupplierPaymentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m08_trip",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    PourId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LoadedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BatchSlipNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LotOrHeatNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MtcReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WeighbridgeSlipNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    TareWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    NetWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    LoadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnloadingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ReceiverMobile = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    SignatureReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceiverVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvidenceState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExceptionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WaitingMinutes = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WaitingCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PartLoadCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseRateSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    PurchaseCostSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CostSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReplacedByTripId = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m08_trip", x => new { x.TenantId, x.TripId });
                    table.CheckConstraint("CK_m08_trip_quantities", "\"PlannedQuantity\" > 0 AND \"LoadedQuantity\" >= 0 AND \"DeliveredQuantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"WaitingMinutes\" >= 0 AND \"WaitingCharge\" >= 0 AND \"PartLoadCharge\" >= 0");
                    table.CheckConstraint("CK_m08_trip_weights", "(\"GrossWeight\" IS NULL OR \"GrossWeight\" >= 0) AND (\"TareWeight\" IS NULL OR \"TareWeight\" >= 0) AND (\"NetWeight\" IS NULL OR \"NetWeight\" >= 0)");
                    table.ForeignKey(
                        name: "FK_m08_trip_m03_driver_TenantId_DriverId",
                        columns: x => new { x.TenantId, x.DriverId },
                        principalTable: "m03_driver",
                        principalColumns: new[] { "TenantId", "DriverId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_trip_m03_plant_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "m03_plant",
                        principalColumns: new[] { "TenantId", "PlantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_trip_m03_vehicle_TenantId_VehicleId",
                        columns: x => new { x.TenantId, x.VehicleId },
                        principalTable: "m03_vehicle",
                        principalColumns: new[] { "TenantId", "VehicleId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_trip_m08_pour_TenantId_PourId",
                        columns: x => new { x.TenantId, x.PourId },
                        principalTable: "m08_pour",
                        principalColumns: new[] { "TenantId", "PourId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m07_supplier_invoice_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    HsnCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GstPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ItcEligible = table.Column<bool>(type: "boolean", nullable: false),
                    ReverseCharge = table.Column<bool>(type: "boolean", nullable: false),
                    TaxSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m07_supplier_invoice_line", x => new { x.TenantId, x.InvoiceLineId });
                    table.CheckConstraint("CK_m07_supplier_invoice_line_amounts", "\"Quantity\" > 0 AND \"Rate\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_line_m07_goods_receipt_line_TenantId_G~",
                        columns: x => new { x.TenantId, x.GoodsReceiptLineId },
                        principalTable: "m07_goods_receipt_line",
                        principalColumns: new[] { "TenantId", "GoodsReceiptLineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_line_m07_purchase_order_line_TenantId_~",
                        columns: x => new { x.TenantId, x.PurchaseOrderLineId },
                        principalTable: "m07_purchase_order_line",
                        principalColumns: new[] { "TenantId", "PurchaseOrderLineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m07_supplier_invoice_line_m07_supplier_invoice_TenantId_Sup~",
                        columns: x => new { x.TenantId, x.SupplierInvoiceId },
                        principalTable: "m07_supplier_invoice",
                        principalColumns: new[] { "TenantId", "SupplierInvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m08_driver_action",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeviceAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m08_driver_action", x => new { x.TenantId, x.DriverActionId });
                    table.ForeignKey(
                        name: "FK_m08_driver_action_m03_driver_TenantId_DriverId",
                        columns: x => new { x.TenantId, x.DriverId },
                        principalTable: "m03_driver",
                        principalColumns: new[] { "TenantId", "DriverId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m08_driver_action_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m08_receiver_challenge",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationMobileProtected = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResendCount = table.Column<int>(type: "integer", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m08_receiver_challenge", x => new { x.TenantId, x.ChallengeId });
                    table.ForeignKey(
                        name: "FK_m08_receiver_challenge_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m08_trip_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeviceAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m08_trip_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m08_trip_event_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m09_delivery_challan",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    PourId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ChallanDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SiteAddressSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OrderNumberSnapshot = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    CustomerPoNumberSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitRateSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercentSnapshot = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    DeliveredValueSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VehicleNumberSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DriverNameSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PlantNameSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    BatchSlipNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ReceiverMobile = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    EvidenceState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SignatureReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EwayBillNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m09_delivery_challan", x => new { x.TenantId, x.ChallanId });
                    table.CheckConstraint("CK_m09_delivery_challan_quantities", "\"DeliveredQuantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"UnitRateSnapshot\" >= 0 AND \"DeliveredValueSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m03_site_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalTable: "m03_site",
                        principalColumns: new[] { "TenantId", "SiteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m06_sales_order_TenantId_SalesOrderId",
                        columns: x => new { x.TenantId, x.SalesOrderId },
                        principalTable: "m06_sales_order",
                        principalColumns: new[] { "TenantId", "SalesOrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m06_sales_order_line_TenantId_SalesOrd~",
                        columns: x => new { x.TenantId, x.SalesOrderLineId },
                        principalTable: "m06_sales_order_line",
                        principalColumns: new[] { "TenantId", "LineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m08_pour_TenantId_PourId",
                        columns: x => new { x.TenantId, x.PourId },
                        principalTable: "m08_pour",
                        principalColumns: new[] { "TenantId", "PourId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m09_delivery_challan_m08_trip_TenantId_TripId",
                        columns: x => new { x.TenantId, x.TripId },
                        principalTable: "m08_trip",
                        principalColumns: new[] { "TenantId", "TripId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m09_challan_event",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m09_challan_event", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_m09_challan_event_m09_delivery_challan_TenantId_ChallanId",
                        columns: x => new { x.TenantId, x.ChallanId },
                        principalTable: "m09_delivery_challan",
                        principalColumns: new[] { "TenantId", "ChallanId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m09_challan_revision",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CorrectedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m09_challan_revision", x => new { x.TenantId, x.RevisionId });
                    table.CheckConstraint("CK_m09_challan_revision_quantities", "\"RevisionNumber\" > 0 AND \"OriginalQuantity\" > 0 AND \"CorrectedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_m09_challan_revision_m09_delivery_challan_TenantId_ChallanId",
                        columns: x => new { x.TenantId, x.ChallanId },
                        principalTable: "m09_delivery_challan",
                        principalColumns: new[] { "TenantId", "ChallanId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m10_invoice_challan",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DeliveredValueSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m10_invoice_challan", x => new { x.TenantId, x.InvoiceChallanId });
                    table.CheckConstraint("CK_m10_invoice_challan_values", "\"Quantity\" > 0 AND \"DeliveredValueSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_m10_invoice_challan_m09_delivery_challan_TenantId_ChallanId",
                        columns: x => new { x.TenantId, x.ChallanId },
                        principalTable: "m09_delivery_challan",
                        principalColumns: new[] { "TenantId", "ChallanId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m10_invoice_challan_m10_invoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "m10_invoice",
                        principalColumns: new[] { "TenantId", "InvoiceId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m06_activity_TenantId_SalesOrderId_At",
                table: "m06_activity",
                columns: new[] { "TenantId", "SalesOrderId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_amendment_TenantId_SalesOrderId_RevisionNumber",
                table: "m06_amendment",
                columns: new[] { "TenantId", "SalesOrderId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m06_delivery_schedule_TenantId_PreferredPlantId",
                table: "m06_delivery_schedule",
                columns: new[] { "TenantId", "PreferredPlantId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_delivery_schedule_TenantId_SalesOrderId_DeliveryDate",
                table: "m06_delivery_schedule",
                columns: new[] { "TenantId", "SalesOrderId", "DeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_delivery_schedule_TenantId_SalesOrderLineId",
                table: "m06_delivery_schedule",
                columns: new[] { "TenantId", "SalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_delivery_schedule_TenantId_SiteId",
                table: "m06_delivery_schedule",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_linked_document_TenantId_SalesOrderId_SourceModule_Docu~",
                table: "m06_linked_document",
                columns: new[] { "TenantId", "SalesOrderId", "SourceModule", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_allocation_TenantId_QuotationId",
                table: "m06_quotation_allocation",
                columns: new[] { "TenantId", "QuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_allocation_TenantId_QuotationLineId",
                table: "m06_quotation_allocation",
                columns: new[] { "TenantId", "QuotationLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_allocation_TenantId_SalesOrderId",
                table: "m06_quotation_allocation",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_allocation_TenantId_SalesOrderLineId",
                table: "m06_quotation_allocation",
                columns: new[] { "TenantId", "SalesOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_charge_allocation_TenantId_QuotationChargeId",
                table: "m06_quotation_charge_allocation",
                columns: new[] { "TenantId", "QuotationChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_charge_allocation_TenantId_QuotationId",
                table: "m06_quotation_charge_allocation",
                columns: new[] { "TenantId", "QuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_charge_allocation_TenantId_SalesOrderChargeId",
                table: "m06_quotation_charge_allocation",
                columns: new[] { "TenantId", "SalesOrderChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m06_quotation_charge_allocation_TenantId_SalesOrderId",
                table: "m06_quotation_charge_allocation",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_CustomerPartyId_Status",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "CustomerPartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_IdempotencyKey",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_Number",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_SiteId",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_SourceEnquiryId",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "SourceEnquiryId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_SourceQuotationId",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "SourceQuotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_TenantId_Status_OrderDate_BranchId",
                table: "m06_sales_order",
                columns: new[] { "TenantId", "Status", "OrderDate", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_charge_TenantId_SalesOrderId",
                table: "m06_sales_order_charge",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_charge_TenantId_SourceQuotationChargeId",
                table: "m06_sales_order_charge",
                columns: new[] { "TenantId", "SourceQuotationChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_line_TenantId_ProductId",
                table: "m06_sales_order_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_line_TenantId_SalesOrderId",
                table: "m06_sales_order_line",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m06_sales_order_line_TenantId_SourceQuotationLineId",
                table: "m06_sales_order_line",
                columns: new[] { "TenantId", "SourceQuotationLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_activity_TenantId_EntityType_EntityId_At",
                table: "m07_activity",
                columns: new[] { "TenantId", "EntityType", "EntityId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_TenantId_IdempotencyKey",
                table: "m07_goods_receipt",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_TenantId_Number",
                table: "m07_goods_receipt",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_TenantId_PurchaseOrderId_Status",
                table: "m07_goods_receipt",
                columns: new[] { "TenantId", "PurchaseOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_TenantId_SupplierPartyId",
                table: "m07_goods_receipt",
                columns: new[] { "TenantId", "SupplierPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_line_TenantId_GoodsReceiptId",
                table: "m07_goods_receipt_line",
                columns: new[] { "TenantId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_goods_receipt_line_TenantId_PurchaseOrderLineId",
                table: "m07_goods_receipt_line",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_TenantId_IdempotencyKey",
                table: "m07_purchase_order",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_TenantId_Number",
                table: "m07_purchase_order",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_TenantId_SourceSalesOrderId",
                table: "m07_purchase_order",
                columns: new[] { "TenantId", "SourceSalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_TenantId_SupplierPartyId_OrderDate_Status",
                table: "m07_purchase_order",
                columns: new[] { "TenantId", "SupplierPartyId", "OrderDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_charge_TenantId_PurchaseOrderId",
                table: "m07_purchase_order_charge",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_line_TenantId_ProductId",
                table: "m07_purchase_order_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_line_TenantId_PurchaseOrderId",
                table: "m07_purchase_order_line",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_purchase_order_line_TenantId_SourceSalesOrderLineId",
                table: "m07_purchase_order_line",
                columns: new[] { "TenantId", "SourceSalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_TenantId_Number",
                table: "m07_supplier_invoice",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_TenantId_PurchaseOrderId",
                table: "m07_supplier_invoice",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_TenantId_SupplierPartyId_DueDate_Status",
                table: "m07_supplier_invoice",
                columns: new[] { "TenantId", "SupplierPartyId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_TenantId_SupplierPartyId_SupplierInvoi~",
                table: "m07_supplier_invoice",
                columns: new[] { "TenantId", "SupplierPartyId", "SupplierInvoiceNumber", "InvoiceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_line_TenantId_GoodsReceiptLineId",
                table: "m07_supplier_invoice_line",
                columns: new[] { "TenantId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_line_TenantId_ProductId",
                table: "m07_supplier_invoice_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_line_TenantId_PurchaseOrderLineId",
                table: "m07_supplier_invoice_line",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_invoice_line_TenantId_SupplierInvoiceId",
                table: "m07_supplier_invoice_line",
                columns: new[] { "TenantId", "SupplierInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_payment_TenantId_Number",
                table: "m07_supplier_payment",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_payment_TenantId_SupplierPartyId_PaymentDate",
                table: "m07_supplier_payment",
                columns: new[] { "TenantId", "SupplierPartyId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_payment_allocation_TenantId_SupplierInvoiceId",
                table: "m07_supplier_payment_allocation",
                columns: new[] { "TenantId", "SupplierInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m07_supplier_payment_allocation_TenantId_SupplierPaymentId_~",
                table: "m07_supplier_payment_allocation",
                columns: new[] { "TenantId", "SupplierPaymentId", "SupplierInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_driver_action_TenantId_ActionId",
                table: "m08_driver_action",
                columns: new[] { "TenantId", "ActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_driver_action_TenantId_DriverId_State_DeviceAt",
                table: "m08_driver_action",
                columns: new[] { "TenantId", "DriverId", "State", "DeviceAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_driver_action_TenantId_TripId",
                table: "m08_driver_action",
                columns: new[] { "TenantId", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_BranchId_WindowStart_Status",
                table: "m08_pour",
                columns: new[] { "TenantId", "BranchId", "WindowStart", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_PlantId_WindowStart_WindowEnd",
                table: "m08_pour",
                columns: new[] { "TenantId", "PlantId", "WindowStart", "WindowEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_ProductId",
                table: "m08_pour",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_SalesOrderId",
                table: "m08_pour",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_SalesOrderLineId",
                table: "m08_pour",
                columns: new[] { "TenantId", "SalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_ScheduleId",
                table: "m08_pour",
                columns: new[] { "TenantId", "ScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_pour_TenantId_SiteId",
                table: "m08_pour",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_receiver_challenge_TenantId_TripId_CreatedAt",
                table: "m08_receiver_challenge",
                columns: new[] { "TenantId", "TripId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_DriverId_Status",
                table: "m08_trip",
                columns: new[] { "TenantId", "DriverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_IdempotencyKey",
                table: "m08_trip",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_Number",
                table: "m08_trip",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_PlantId",
                table: "m08_trip",
                columns: new[] { "TenantId", "PlantId" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_PourId_Status",
                table: "m08_trip",
                columns: new[] { "TenantId", "PourId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_TenantId_VehicleId_Status",
                table: "m08_trip",
                columns: new[] { "TenantId", "VehicleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_event_TenantId_ActionId",
                table: "m08_trip_event",
                columns: new[] { "TenantId", "ActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m08_trip_event_TenantId_TripId_ReceivedAt",
                table: "m08_trip_event",
                columns: new[] { "TenantId", "TripId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_challan_event_TenantId_ChallanId_OccurredAt",
                table: "m09_challan_event",
                columns: new[] { "TenantId", "ChallanId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_challan_revision_TenantId_ChallanId_RevisionNumber",
                table: "m09_challan_revision",
                columns: new[] { "TenantId", "ChallanId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_BranchId_ChallanDate_Number",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "BranchId", "ChallanDate", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_CustomerPartyId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "CustomerPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_PourId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "PourId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_ProductId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_SalesOrderId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_SalesOrderLineId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "SalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_SiteId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_Status_ChallanDate",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "Status", "ChallanDate" });

            migrationBuilder.CreateIndex(
                name: "IX_m09_delivery_challan_TenantId_TripId",
                table: "m09_delivery_challan",
                columns: new[] { "TenantId", "TripId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_TenantId_BranchId_Status_InvoiceDate",
                table: "m10_invoice",
                columns: new[] { "TenantId", "BranchId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_TenantId_CustomerPartyId_DueDate",
                table: "m10_invoice",
                columns: new[] { "TenantId", "CustomerPartyId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_TenantId_IdempotencyKey",
                table: "m10_invoice",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_challan_TenantId_ChallanId",
                table: "m10_invoice_challan",
                columns: new[] { "TenantId", "ChallanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_challan_TenantId_InvoiceId",
                table: "m10_invoice_challan",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_event_TenantId_InvoiceId_OccurredAt",
                table: "m10_invoice_event",
                columns: new[] { "TenantId", "InvoiceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_line_TenantId_InvoiceId",
                table: "m10_invoice_line",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_line_TenantId_ProductId",
                table: "m10_invoice_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_line_TenantId_SalesOrderId",
                table: "m10_invoice_line",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_m10_invoice_line_TenantId_SalesOrderLineId",
                table: "m10_invoice_line",
                columns: new[] { "TenantId", "SalesOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_promise_to_pay_TenantId_BranchId_PromiseDate_Outcome",
                table: "m11_promise_to_pay",
                columns: new[] { "TenantId", "BranchId", "PromiseDate", "Outcome" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_promise_to_pay_TenantId_CustomerPartyId",
                table: "m11_promise_to_pay",
                columns: new[] { "TenantId", "CustomerPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_promise_to_pay_TenantId_InvoiceId",
                table: "m11_promise_to_pay",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_receipt_TenantId_CustomerPartyId_ReceiptDate_Status",
                table: "m11_receipt",
                columns: new[] { "TenantId", "CustomerPartyId", "ReceiptDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_receipt_TenantId_IdempotencyKey",
                table: "m11_receipt",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m11_receipt_allocation_TenantId_InvoiceId",
                table: "m11_receipt_allocation",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_receipt_allocation_TenantId_ReceiptId",
                table: "m11_receipt_allocation",
                columns: new[] { "TenantId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_m11_receivable_event_TenantId_ReceiptId_OccurredAt",
                table: "m11_receivable_event",
                columns: new[] { "TenantId", "ReceiptId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_financial_period_TenantId_PeriodStart_PeriodEnd_IsLocked",
                table: "m12_financial_period",
                columns: new[] { "TenantId", "PeriodStart", "PeriodEnd", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_period_event_TenantId_PeriodStart_OccurredAt",
                table: "m12_period_event",
                columns: new[] { "TenantId", "PeriodStart", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_TenantId_BranchId_Number",
                table: "m12_voucher",
                columns: new[] { "TenantId", "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_TenantId_IdempotencyKey",
                table: "m12_voucher",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_TenantId_VoucherDate_VoucherType",
                table: "m12_voucher",
                columns: new[] { "TenantId", "VoucherDate", "VoucherType" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_event_TenantId_VoucherId_OccurredAt",
                table: "m12_voucher_event",
                columns: new[] { "TenantId", "VoucherId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_line_TenantId_LedgerCode_BranchId",
                table: "m12_voucher_line",
                columns: new[] { "TenantId", "LedgerCode", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_line_TenantId_PartyId",
                table: "m12_voucher_line",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_line_TenantId_SalesOrderId_TripId",
                table: "m12_voucher_line",
                columns: new[] { "TenantId", "SalesOrderId", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_m12_voucher_line_TenantId_VoucherId",
                table: "m12_voucher_line",
                columns: new[] { "TenantId", "VoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_m13_exception_review_TenantId_InvoiceId",
                table: "m13_exception_review",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_m13_exception_review_TenantId_PeriodStart_InvoiceId_Excepti~",
                table: "m13_exception_review",
                columns: new[] { "TenantId", "PeriodStart", "InvoiceId", "ExceptionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_TenantId_CategoryId",
                table: "m14_expense",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_TenantId_IdempotencyKey",
                table: "m14_expense",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_TenantId_VendorPartyId_SupplierBillNumber_Amount",
                table: "m14_expense",
                columns: new[] { "TenantId", "VendorPartyId", "SupplierBillNumber", "Amount" },
                unique: true,
                filter: "\"SupplierBillNumber\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_TenantId_VoucherId",
                table: "m14_expense",
                columns: new[] { "TenantId", "VoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_category_TenantId_Name",
                table: "m14_expense_category",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m14_expense_event_TenantId_ExpenseId_OccurredAt",
                table: "m14_expense_event",
                columns: new[] { "TenantId", "ExpenseId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m16_export_log_TenantId_UserId_ExportedAt",
                table: "m16_export_log",
                columns: new[] { "TenantId", "UserId", "ExportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_m17_catalogue_item_TenantId_ProductId",
                table: "m17_catalogue_item",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m17_consent_record_TenantId_ContactId_Channel",
                table: "m17_consent_record",
                columns: new[] { "TenantId", "ContactId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m17_promotion_event_TenantId_OccurredAt",
                table: "m17_promotion_event",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m06_activity");

            migrationBuilder.DropTable(
                name: "m06_amendment");

            migrationBuilder.DropTable(
                name: "m06_linked_document");

            migrationBuilder.DropTable(
                name: "m06_quotation_allocation");

            migrationBuilder.DropTable(
                name: "m06_quotation_charge_allocation");

            migrationBuilder.DropTable(
                name: "m07_activity");

            migrationBuilder.DropTable(
                name: "m07_number_counter");

            migrationBuilder.DropTable(
                name: "m07_purchase_order_charge");

            migrationBuilder.DropTable(
                name: "m07_supplier_invoice_line");

            migrationBuilder.DropTable(
                name: "m07_supplier_payment_allocation");

            migrationBuilder.DropTable(
                name: "m08_driver_action");

            migrationBuilder.DropTable(
                name: "m08_receiver_challenge");

            migrationBuilder.DropTable(
                name: "m08_trip_event");

            migrationBuilder.DropTable(
                name: "m09_challan_event");

            migrationBuilder.DropTable(
                name: "m09_challan_number_counter");

            migrationBuilder.DropTable(
                name: "m09_challan_revision");

            migrationBuilder.DropTable(
                name: "m10_invoice_challan");

            migrationBuilder.DropTable(
                name: "m10_invoice_event");

            migrationBuilder.DropTable(
                name: "m10_invoice_line");

            migrationBuilder.DropTable(
                name: "m11_promise_to_pay");

            migrationBuilder.DropTable(
                name: "m11_receipt_allocation");

            migrationBuilder.DropTable(
                name: "m11_receivable_event");

            migrationBuilder.DropTable(
                name: "m12_financial_period");

            migrationBuilder.DropTable(
                name: "m12_period_event");

            migrationBuilder.DropTable(
                name: "m12_voucher_event");

            migrationBuilder.DropTable(
                name: "m12_voucher_line");

            migrationBuilder.DropTable(
                name: "m12_voucher_number_counter");

            migrationBuilder.DropTable(
                name: "m13_exception_review");

            migrationBuilder.DropTable(
                name: "m13_gst_period");

            migrationBuilder.DropTable(
                name: "m14_expense_event");

            migrationBuilder.DropTable(
                name: "m16_export_log");

            migrationBuilder.DropTable(
                name: "m17_catalogue_item");

            migrationBuilder.DropTable(
                name: "m17_consent_record");

            migrationBuilder.DropTable(
                name: "m17_promotion_event");

            migrationBuilder.DropTable(
                name: "m06_sales_order_charge");

            migrationBuilder.DropTable(
                name: "m07_goods_receipt_line");

            migrationBuilder.DropTable(
                name: "m07_supplier_invoice");

            migrationBuilder.DropTable(
                name: "m07_supplier_payment");

            migrationBuilder.DropTable(
                name: "m09_delivery_challan");

            migrationBuilder.DropTable(
                name: "m11_receipt");

            migrationBuilder.DropTable(
                name: "m10_invoice");

            migrationBuilder.DropTable(
                name: "m14_expense");

            migrationBuilder.DropTable(
                name: "m07_goods_receipt");

            migrationBuilder.DropTable(
                name: "m07_purchase_order_line");

            migrationBuilder.DropTable(
                name: "m08_trip");

            migrationBuilder.DropTable(
                name: "m12_voucher");

            migrationBuilder.DropTable(
                name: "m14_expense_category");

            migrationBuilder.DropTable(
                name: "m07_purchase_order");

            migrationBuilder.DropTable(
                name: "m08_pour");

            migrationBuilder.DropTable(
                name: "m06_delivery_schedule");

            migrationBuilder.DropTable(
                name: "m06_sales_order_line");

            migrationBuilder.DropTable(
                name: "m06_sales_order");
        }
    }
}
