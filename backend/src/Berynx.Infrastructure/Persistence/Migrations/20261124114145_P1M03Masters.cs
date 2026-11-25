using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P1M03Masters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m03_charge_type",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    HsnCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    DefaultSurchargeRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DefaultTaxRatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_charge_type", x => new { x.TenantId, x.ChargeTypeId });
                });

            migrationBuilder.CreateTable(
                name: "m03_equipment",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EquipmentType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    PumpType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    CapacityUnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    OwnerPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsuranceExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    FitnessExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_equipment", x => new { x.TenantId, x.EquipmentId });
                });

            migrationBuilder.CreateTable(
                name: "m03_import_batch",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UndoneAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_import_batch", x => new { x.TenantId, x.BatchId });
                });

            migrationBuilder.CreateTable(
                name: "m03_payment_term",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DueDays = table.Column<int>(type: "integer", nullable: false),
                    LedgerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_payment_term", x => new { x.TenantId, x.PaymentTermId });
                    table.CheckConstraint("ck_m03_payment_term_due_days", "\"DueDays\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "m03_price_list",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_price_list", x => new { x.TenantId, x.PriceListId });
                    table.CheckConstraint("ck_m03_price_list_dates", "\"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                    table.CheckConstraint("ck_m03_price_list_scope", "(CASE WHEN \"CustomerPartyId\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"CustomerGroup\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"IsDefault\" THEN 1 ELSE 0 END) = 1");
                });

            migrationBuilder.CreateTable(
                name: "m03_tax_rate",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    HsnCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_tax_rate", x => new { x.TenantId, x.TaxRateId });
                    table.CheckConstraint("ck_m03_tax_rate_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("ck_m03_tax_rate_range", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "m03_unit",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_unit", x => new { x.TenantId, x.UnitCode });
                });

            migrationBuilder.CreateTable(
                name: "m03_vehicle",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CapacityUnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Ownership = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TransporterPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsuranceExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PucExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    FitnessExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_vehicle", x => new { x.TenantId, x.VehicleId });
                    table.CheckConstraint("ck_m03_vehicle_capacity", "\"Capacity\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "m03_import_row_error",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowErrorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RowJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_import_row_error", x => new { x.TenantId, x.RowErrorId });
                    table.ForeignKey(
                        name: "FK_m03_import_row_error_m03_import_batch_TenantId_BatchId",
                        columns: x => new { x.TenantId, x.BatchId },
                        principalTable: "m03_import_batch",
                        principalColumns: new[] { "TenantId", "BatchId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_party",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PartyType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Pan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CustomerCategory = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BillingAddress = table.Column<string>(type: "text", nullable: true),
                    CustomerGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditDays = table.Column<int>(type: "integer", nullable: false),
                    PaymentTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditPolicy = table.Column<string>(type: "text", nullable: false),
                    CreditStatus = table.Column<string>(type: "text", nullable: false),
                    DefaultPriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMsme = table.Column<bool>(type: "boolean", nullable: false),
                    UdyamNumber = table.Column<string>(type: "text", nullable: true),
                    TdsCategory = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_party", x => new { x.TenantId, x.PartyId });
                    table.ForeignKey(
                        name: "FK_m03_party_m03_payment_term_TenantId_PaymentTermId",
                        columns: x => new { x.TenantId, x.PaymentTermId },
                        principalTable: "m03_payment_term",
                        principalColumns: new[] { "TenantId", "PaymentTermId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m03_party_m03_price_list_TenantId_DefaultPriceListId",
                        columns: x => new { x.TenantId, x.DefaultPriceListId },
                        principalTable: "m03_price_list",
                        principalColumns: new[] { "TenantId", "PriceListId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_product",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Category = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    HsnCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BaseUnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CategoryAttributesJson = table.Column<string>(type: "jsonb", nullable: false),
                    QualityReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Traceable = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_product", x => new { x.TenantId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_m03_product_m03_unit_TenantId_BaseUnitCode",
                        columns: x => new { x.TenantId, x.BaseUnitCode },
                        principalTable: "m03_unit",
                        principalColumns: new[] { "TenantId", "UnitCode" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_unit_conversion",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ToUnitCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ToUnitsPerFromUnit = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_unit_conversion", x => new { x.TenantId, x.ConversionId });
                    table.CheckConstraint("ck_m03_unit_conversion_canonical", "\"FromUnitCode\" < \"ToUnitCode\"");
                    table.CheckConstraint("ck_m03_unit_conversion_positive", "\"ToUnitsPerFromUnit\" > 0");
                    table.ForeignKey(
                        name: "FK_m03_unit_conversion_m03_unit_TenantId_FromUnitCode",
                        columns: x => new { x.TenantId, x.FromUnitCode },
                        principalTable: "m03_unit",
                        principalColumns: new[] { "TenantId", "UnitCode" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m03_unit_conversion_m03_unit_TenantId_ToUnitCode",
                        columns: x => new { x.TenantId, x.ToUnitCode },
                        principalTable: "m03_unit",
                        principalColumns: new[] { "TenantId", "UnitCode" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_driver",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LicenceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LicenceExpiresOn = table.Column<DateOnly>(type: "date", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_driver", x => new { x.TenantId, x.DriverId });
                    table.ForeignKey(
                        name: "FK_m03_driver_m03_vehicle_TenantId_VehicleId",
                        columns: x => new { x.TenantId, x.VehicleId },
                        principalTable: "m03_vehicle",
                        principalColumns: new[] { "TenantId", "VehicleId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_contact",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmailConsent = table.Column<bool>(type: "boolean", nullable: false),
                    SmsConsent = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppConsent = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConsentUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_contact", x => new { x.TenantId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_m03_contact_m03_party_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_party_bank_account",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountHolder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountNumberProtected = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Ifsc = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_party_bank_account", x => new { x.TenantId, x.BankAccountId });
                    table.ForeignKey(
                        name: "FK_m03_party_bank_account_m03_party_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_party_merge",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MergeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurvivorPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MergedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MergedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_party_merge", x => new { x.TenantId, x.MergeId });
                    table.ForeignKey(
                        name: "FK_m03_party_merge_m03_party_TenantId_SourcePartyId",
                        columns: x => new { x.TenantId, x.SourcePartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m03_party_merge_m03_party_TenantId_SurvivorPartyId",
                        columns: x => new { x.TenantId, x.SurvivorPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_party_role",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_party_role", x => new { x.TenantId, x.PartyId, x.RoleCode });
                    table.ForeignKey(
                        name: "FK_m03_party_role_m03_party_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_plant",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    StateCode = table.Column<string>(type: "text", nullable: false),
                    CapacityPerHour = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    GradesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_plant", x => new { x.TenantId, x.PlantId });
                    table.ForeignKey(
                        name: "FK_m03_plant_m03_party_TenantId_SupplierPartyId",
                        columns: x => new { x.TenantId, x.SupplierPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_site",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    DefaultPlantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessNotes = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_site", x => new { x.TenantId, x.SiteId });
                    table.ForeignKey(
                        name: "FK_m03_site_m03_party_TenantId_CustomerPartyId",
                        columns: x => new { x.TenantId, x.CustomerPartyId },
                        principalTable: "m03_party",
                        principalColumns: new[] { "TenantId", "PartyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_category_tax_override",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverrideId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCategory = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_category_tax_override", x => new { x.TenantId, x.OverrideId });
                    table.CheckConstraint("ck_m03_category_tax_override_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("ck_m03_category_tax_override_range", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100");
                    table.CheckConstraint("ck_m03_category_tax_override_reason", "length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_m03_category_tax_override_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_price_list_line",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MinimumRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MinimumMarginPercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_price_list_line", x => new { x.TenantId, x.PriceListLineId });
                    table.CheckConstraint("ck_m03_price_line_margin", "\"MinimumMarginPercent\" IS NULL OR (\"MinimumMarginPercent\" >= 0 AND \"MinimumMarginPercent\" <= 100)");
                    table.CheckConstraint("ck_m03_price_line_rates", "\"Rate\" >= 0 AND (\"MinimumRate\" IS NULL OR \"MinimumRate\" >= 0)");
                    table.ForeignKey(
                        name: "FK_m03_price_list_line_m03_price_list_TenantId_PriceListId",
                        columns: x => new { x.TenantId, x.PriceListId },
                        principalTable: "m03_price_list",
                        principalColumns: new[] { "TenantId", "PriceListId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_m03_price_list_line_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "m03_rmc_rate_card",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SlumpBand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AdmixtureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PumpType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AdmixtureChargeTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PumpChargeTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m03_rmc_rate_card", x => new { x.TenantId, x.RateCardId });
                    table.CheckConstraint("ck_m03_rmc_rate_card_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("ck_m03_rmc_rate_card_rate", "\"Rate\" >= 0");
                    table.ForeignKey(
                        name: "FK_m03_rmc_rate_card_m03_product_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "m03_product",
                        principalColumns: new[] { "TenantId", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m03_category_tax_override_TenantId_ProductId_CustomerCatego~",
                table: "m03_category_tax_override",
                columns: new[] { "TenantId", "ProductId", "CustomerCategory", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_charge_type_TenantId_Code",
                table: "m03_charge_type",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_contact_TenantId_PartyId_Mobile",
                table: "m03_contact",
                columns: new[] { "TenantId", "PartyId", "Mobile" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_driver_TenantId_LicenceNumber",
                table: "m03_driver",
                columns: new[] { "TenantId", "LicenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_driver_TenantId_VehicleId",
                table: "m03_driver",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_equipment_TenantId_EquipmentType_Name",
                table: "m03_equipment",
                columns: new[] { "TenantId", "EquipmentType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_equipment_TenantId_OwnerPartyId",
                table: "m03_equipment",
                columns: new[] { "TenantId", "OwnerPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_import_row_error_TenantId_BatchId",
                table: "m03_import_row_error",
                columns: new[] { "TenantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_DefaultPriceListId",
                table: "m03_party",
                columns: new[] { "TenantId", "DefaultPriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_Gstin",
                table: "m03_party",
                columns: new[] { "TenantId", "Gstin" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_LegalName_City",
                table: "m03_party",
                columns: new[] { "TenantId", "LegalName", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_Pan",
                table: "m03_party",
                columns: new[] { "TenantId", "Pan" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_TenantId_PaymentTermId",
                table: "m03_party",
                columns: new[] { "TenantId", "PaymentTermId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_bank_account_TenantId_PartyId",
                table: "m03_party_bank_account",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_merge_TenantId_SourcePartyId",
                table: "m03_party_merge",
                columns: new[] { "TenantId", "SourcePartyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_party_merge_TenantId_SurvivorPartyId",
                table: "m03_party_merge",
                columns: new[] { "TenantId", "SurvivorPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_payment_term_TenantId_Name",
                table: "m03_payment_term",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_plant_TenantId_BranchId",
                table: "m03_plant",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_plant_TenantId_SupplierPartyId_Name",
                table: "m03_plant",
                columns: new[] { "TenantId", "SupplierPartyId", "Name" },
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

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_line_TenantId_PriceListId_ProductId",
                table: "m03_price_list_line",
                columns: new[] { "TenantId", "PriceListId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_price_list_line_TenantId_ProductId",
                table: "m03_price_list_line",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_product_TenantId_BaseUnitCode",
                table: "m03_product",
                columns: new[] { "TenantId", "BaseUnitCode" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_product_TenantId_Code",
                table: "m03_product",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~",
                table: "m03_rmc_rate_card",
                columns: new[] { "TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_site_TenantId_BranchId",
                table: "m03_site",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_site_TenantId_CustomerPartyId_Name",
                table: "m03_site",
                columns: new[] { "TenantId", "CustomerPartyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_tax_rate_TenantId_HsnCode_EffectiveFrom",
                table: "m03_tax_rate",
                columns: new[] { "TenantId", "HsnCode", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_unit_conversion_TenantId_FromUnitCode_ToUnitCode",
                table: "m03_unit_conversion",
                columns: new[] { "TenantId", "FromUnitCode", "ToUnitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_unit_conversion_TenantId_ToUnitCode",
                table: "m03_unit_conversion",
                columns: new[] { "TenantId", "ToUnitCode" });

            migrationBuilder.CreateIndex(
                name: "IX_m03_vehicle_TenantId_RegistrationNumber",
                table: "m03_vehicle",
                columns: new[] { "TenantId", "RegistrationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_m03_vehicle_TenantId_TransporterPartyId",
                table: "m03_vehicle",
                columns: new[] { "TenantId", "TransporterPartyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m03_category_tax_override");

            migrationBuilder.DropTable(
                name: "m03_charge_type");

            migrationBuilder.DropTable(
                name: "m03_contact");

            migrationBuilder.DropTable(
                name: "m03_driver");

            migrationBuilder.DropTable(
                name: "m03_equipment");

            migrationBuilder.DropTable(
                name: "m03_import_row_error");

            migrationBuilder.DropTable(
                name: "m03_party_bank_account");

            migrationBuilder.DropTable(
                name: "m03_party_merge");

            migrationBuilder.DropTable(
                name: "m03_party_role");

            migrationBuilder.DropTable(
                name: "m03_plant");

            migrationBuilder.DropTable(
                name: "m03_price_list_line");

            migrationBuilder.DropTable(
                name: "m03_rmc_rate_card");

            migrationBuilder.DropTable(
                name: "m03_site");

            migrationBuilder.DropTable(
                name: "m03_tax_rate");

            migrationBuilder.DropTable(
                name: "m03_unit_conversion");

            migrationBuilder.DropTable(
                name: "m03_vehicle");

            migrationBuilder.DropTable(
                name: "m03_import_batch");

            migrationBuilder.DropTable(
                name: "m03_product");

            migrationBuilder.DropTable(
                name: "m03_party");

            migrationBuilder.DropTable(
                name: "m03_unit");

            migrationBuilder.DropTable(
                name: "m03_payment_term");

            migrationBuilder.DropTable(
                name: "m03_price_list");
        }
    }
}
