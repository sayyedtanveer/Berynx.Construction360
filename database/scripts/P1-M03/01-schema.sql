-- Generated from EF Core migration 20261124114145_P1M03Masters.
-- Apply once, before 02-alter-existing.sql. Do not run against production without approval.
BEGIN;

CREATE TABLE m03_charge_type (
    "TenantId" uuid NOT NULL,
    "ChargeTypeId" uuid NOT NULL,
    "Code" character varying(48) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "LedgerCode" character varying(64),
    "HsnCode" character varying(16),
    "DefaultSurchargeRate" numeric(18,4),
    "DefaultTaxRatePercent" numeric(7,4),
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_charge_type" PRIMARY KEY ("TenantId", "ChargeTypeId")
);

CREATE TABLE m03_equipment (
    "TenantId" uuid NOT NULL,
    "EquipmentId" uuid NOT NULL,
    "Name" character varying(160) NOT NULL,
    "EquipmentType" character varying(48) NOT NULL,
    "PumpType" character varying(32) NOT NULL,
    "Capacity" numeric(18,3),
    "CapacityUnitCode" character varying(16),
    "OwnerPartyId" uuid,
    "InsuranceExpiresOn" date,
    "FitnessExpiresOn" date,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_equipment" PRIMARY KEY ("TenantId", "EquipmentId")
);

CREATE TABLE m03_import_batch (
    "TenantId" uuid NOT NULL,
    "BatchId" uuid NOT NULL,
    "EntityType" character varying(32) NOT NULL,
    "Status" character varying(24) NOT NULL,
    "SnapshotJson" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ConfirmedAt" timestamp with time zone,
    "UndoneAt" timestamp with time zone,
    CONSTRAINT "PK_m03_import_batch" PRIMARY KEY ("TenantId", "BatchId")
);

CREATE TABLE m03_payment_term (
    "TenantId" uuid NOT NULL,
    "PaymentTermId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "DueDays" integer NOT NULL,
    "LedgerCode" character varying(64),
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_payment_term" PRIMARY KEY ("TenantId", "PaymentTermId"),
    CONSTRAINT ck_m03_payment_term_due_days CHECK ("DueDays" >= 0)
);

CREATE TABLE m03_price_list (
    "TenantId" uuid NOT NULL,
    "PriceListId" uuid NOT NULL,
    "Name" character varying(160) NOT NULL,
    "CustomerPartyId" uuid,
    "CustomerGroup" character varying(100),
    "IsDefault" boolean NOT NULL,
    "ValidFrom" date NOT NULL,
    "ValidTo" date,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_price_list" PRIMARY KEY ("TenantId", "PriceListId"),
    CONSTRAINT ck_m03_price_list_dates CHECK ("ValidTo" IS NULL OR "ValidTo" >= "ValidFrom"),
    CONSTRAINT ck_m03_price_list_scope CHECK ((CASE WHEN "CustomerPartyId" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN "CustomerGroup" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN "IsDefault" THEN 1 ELSE 0 END) = 1)
);

CREATE TABLE m03_tax_rate (
    "TenantId" uuid NOT NULL,
    "TaxRateId" uuid NOT NULL,
    "HsnCode" character varying(16) NOT NULL,
    "RatePercent" numeric(7,4) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "Reason" character varying(500) NOT NULL,
    "IsApproved" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m03_tax_rate" PRIMARY KEY ("TenantId", "TaxRateId"),
    CONSTRAINT ck_m03_tax_rate_dates CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT ck_m03_tax_rate_range CHECK ("RatePercent" >= 0 AND "RatePercent" <= 100)
);

CREATE TABLE m03_unit (
    "TenantId" uuid NOT NULL,
    "UnitCode" character varying(16) NOT NULL,
    "Name" character varying(80) NOT NULL,
    "Symbol" character varying(16) NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_m03_unit" PRIMARY KEY ("TenantId", "UnitCode")
);

CREATE TABLE m03_vehicle (
    "TenantId" uuid NOT NULL,
    "VehicleId" uuid NOT NULL,
    "RegistrationNumber" character varying(20) NOT NULL,
    "Type" character varying(32) NOT NULL,
    "Capacity" numeric(18,3) NOT NULL,
    "CapacityUnitCode" character varying(16) NOT NULL,
    "Ownership" character varying(16) NOT NULL,
    "TransporterPartyId" uuid,
    "InsuranceExpiresOn" date,
    "PucExpiresOn" date,
    "FitnessExpiresOn" date,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_vehicle" PRIMARY KEY ("TenantId", "VehicleId"),
    CONSTRAINT ck_m03_vehicle_capacity CHECK ("Capacity" > 0)
);

CREATE TABLE m03_import_row_error (
    "TenantId" uuid NOT NULL,
    "RowErrorId" uuid NOT NULL,
    "BatchId" uuid NOT NULL,
    "RowNumber" integer NOT NULL,
    "Message" character varying(1000) NOT NULL,
    "RowJson" jsonb NOT NULL,
    CONSTRAINT "PK_m03_import_row_error" PRIMARY KEY ("TenantId", "RowErrorId"),
    CONSTRAINT "FK_m03_import_row_error_m03_import_batch_TenantId_BatchId" FOREIGN KEY ("TenantId", "BatchId") REFERENCES m03_import_batch ("TenantId", "BatchId") ON DELETE RESTRICT
);

CREATE TABLE m03_party (
    "TenantId" uuid NOT NULL,
    "PartyId" uuid NOT NULL,
    "LegalName" character varying(200) NOT NULL,
    "TradeName" character varying(200),
    "PartyType" character varying(32) NOT NULL,
    "Gstin" character varying(15),
    "Pan" character varying(10),
    "CustomerCategory" character varying(80),
    "City" character varying(100),
    "StateCode" character varying(2),
    "BillingAddress" text,
    "CustomerGroup" character varying(100),
    "CreditLimit" numeric(18,2) NOT NULL,
    "CreditDays" integer NOT NULL,
    "PaymentTermId" uuid,
    "CreditPolicy" text NOT NULL,
    "CreditStatus" text NOT NULL,
    "DefaultPriceListId" uuid,
    "IsMsme" boolean NOT NULL,
    "UdyamNumber" text,
    "TdsCategory" text,
    "Version" bigint NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m03_party" PRIMARY KEY ("TenantId", "PartyId"),
    CONSTRAINT "FK_m03_party_m03_payment_term_TenantId_PaymentTermId" FOREIGN KEY ("TenantId", "PaymentTermId") REFERENCES m03_payment_term ("TenantId", "PaymentTermId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m03_party_m03_price_list_TenantId_DefaultPriceListId" FOREIGN KEY ("TenantId", "DefaultPriceListId") REFERENCES m03_price_list ("TenantId", "PriceListId") ON DELETE RESTRICT
);

CREATE TABLE m03_product (
    "TenantId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Code" character varying(64) NOT NULL,
    "Name" character varying(180) NOT NULL,
    "Category" character varying(48) NOT NULL,
    "HsnCode" character varying(16) NOT NULL,
    "BaseUnitCode" character varying(16) NOT NULL,
    "CategoryAttributesJson" jsonb NOT NULL,
    "QualityReference" character varying(500) NOT NULL,
    "Traceable" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m03_product" PRIMARY KEY ("TenantId", "ProductId"),
    CONSTRAINT "FK_m03_product_m03_unit_TenantId_BaseUnitCode" FOREIGN KEY ("TenantId", "BaseUnitCode") REFERENCES m03_unit ("TenantId", "UnitCode") ON DELETE RESTRICT
);

CREATE TABLE m03_unit_conversion (
    "TenantId" uuid NOT NULL,
    "ConversionId" uuid NOT NULL,
    "FromUnitCode" character varying(16) NOT NULL,
    "ToUnitCode" character varying(16) NOT NULL,
    "ToUnitsPerFromUnit" numeric(18,9) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_unit_conversion" PRIMARY KEY ("TenantId", "ConversionId"),
    CONSTRAINT ck_m03_unit_conversion_canonical CHECK ("FromUnitCode" < "ToUnitCode"),
    CONSTRAINT ck_m03_unit_conversion_positive CHECK ("ToUnitsPerFromUnit" > 0),
    CONSTRAINT "FK_m03_unit_conversion_m03_unit_TenantId_FromUnitCode" FOREIGN KEY ("TenantId", "FromUnitCode") REFERENCES m03_unit ("TenantId", "UnitCode") ON DELETE RESTRICT,
    CONSTRAINT "FK_m03_unit_conversion_m03_unit_TenantId_ToUnitCode" FOREIGN KEY ("TenantId", "ToUnitCode") REFERENCES m03_unit ("TenantId", "UnitCode") ON DELETE RESTRICT
);

CREATE TABLE m03_driver (
    "TenantId" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Mobile" character varying(20) NOT NULL,
    "LicenceNumber" character varying(40) NOT NULL,
    "LicenceExpiresOn" date NOT NULL,
    "VehicleId" uuid,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_driver" PRIMARY KEY ("TenantId", "DriverId"),
    CONSTRAINT "FK_m03_driver_m03_vehicle_TenantId_VehicleId" FOREIGN KEY ("TenantId", "VehicleId") REFERENCES m03_vehicle ("TenantId", "VehicleId") ON DELETE RESTRICT
);

CREATE TABLE m03_contact (
    "TenantId" uuid NOT NULL,
    "ContactId" uuid NOT NULL,
    "PartyId" uuid NOT NULL,
    "SiteId" uuid,
    "Name" character varying(150) NOT NULL,
    "Role" character varying(80) NOT NULL,
    "Email" character varying(254),
    "Mobile" character varying(20),
    "EmailConsent" boolean NOT NULL,
    "SmsConsent" boolean NOT NULL,
    "WhatsAppConsent" boolean NOT NULL,
    "ConsentSource" character varying(200),
    "ConsentUpdatedAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_m03_contact" PRIMARY KEY ("TenantId", "ContactId"),
    CONSTRAINT "FK_m03_contact_m03_party_TenantId_PartyId" FOREIGN KEY ("TenantId", "PartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_party_bank_account (
    "TenantId" uuid NOT NULL,
    "BankAccountId" uuid NOT NULL,
    "PartyId" uuid NOT NULL,
    "AccountHolder" character varying(200) NOT NULL,
    "AccountNumberProtected" character varying(512) NOT NULL,
    "Ifsc" character varying(11) NOT NULL,
    "BankName" text,
    "IsPrimary" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_party_bank_account" PRIMARY KEY ("TenantId", "BankAccountId"),
    CONSTRAINT "FK_m03_party_bank_account_m03_party_TenantId_PartyId" FOREIGN KEY ("TenantId", "PartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_party_merge (
    "TenantId" uuid NOT NULL,
    "MergeId" uuid NOT NULL,
    "SourcePartyId" uuid NOT NULL,
    "SurvivorPartyId" uuid NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "MergedBy" character varying(200) NOT NULL,
    "MergedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m03_party_merge" PRIMARY KEY ("TenantId", "MergeId"),
    CONSTRAINT "FK_m03_party_merge_m03_party_TenantId_SourcePartyId" FOREIGN KEY ("TenantId", "SourcePartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m03_party_merge_m03_party_TenantId_SurvivorPartyId" FOREIGN KEY ("TenantId", "SurvivorPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_party_role (
    "TenantId" uuid NOT NULL,
    "PartyId" uuid NOT NULL,
    "RoleCode" character varying(32) NOT NULL,
    CONSTRAINT "PK_m03_party_role" PRIMARY KEY ("TenantId", "PartyId", "RoleCode"),
    CONSTRAINT "FK_m03_party_role_m03_party_TenantId_PartyId" FOREIGN KEY ("TenantId", "PartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_plant (
    "TenantId" uuid NOT NULL,
    "PlantId" uuid NOT NULL,
    "SupplierPartyId" uuid NOT NULL,
    "BranchId" uuid,
    "Name" character varying(160) NOT NULL,
    "Address" text NOT NULL,
    "City" text NOT NULL,
    "StateCode" text NOT NULL,
    "CapacityPerHour" numeric(18,3),
    "GradesJson" jsonb NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_plant" PRIMARY KEY ("TenantId", "PlantId"),
    CONSTRAINT "FK_m03_plant_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_site (
    "TenantId" uuid NOT NULL,
    "SiteId" uuid NOT NULL,
    "CustomerPartyId" uuid NOT NULL,
    "BranchId" uuid,
    "Name" character varying(160) NOT NULL,
    "Address" text NOT NULL,
    "City" character varying(100) NOT NULL,
    "StateCode" character varying(2) NOT NULL,
    "Gstin" character varying(15),
    "Latitude" numeric(9,6),
    "Longitude" numeric(9,6),
    "DefaultPlantId" uuid,
    "AccessNotes" text NOT NULL,
    "Status" text NOT NULL,
    "Version" bigint NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m03_site" PRIMARY KEY ("TenantId", "SiteId"),
    CONSTRAINT "FK_m03_site_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
);

CREATE TABLE m03_category_tax_override (
    "TenantId" uuid NOT NULL,
    "OverrideId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "CustomerCategory" character varying(80) NOT NULL,
    "RatePercent" numeric(7,4) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "Reason" character varying(500) NOT NULL,
    "IsApproved" boolean NOT NULL,
    "ApprovedBy" character varying(200) NOT NULL,
    "ApprovedAt" timestamp with time zone,
    CONSTRAINT "PK_m03_category_tax_override" PRIMARY KEY ("TenantId", "OverrideId"),
    CONSTRAINT ck_m03_category_tax_override_dates CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT ck_m03_category_tax_override_range CHECK ("RatePercent" >= 0 AND "RatePercent" <= 100),
    CONSTRAINT ck_m03_category_tax_override_reason CHECK (length(trim("Reason")) > 0),
    CONSTRAINT "FK_m03_category_tax_override_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT
);

CREATE TABLE m03_price_list_line (
    "TenantId" uuid NOT NULL,
    "PriceListLineId" uuid NOT NULL,
    "PriceListId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Rate" numeric(18,4) NOT NULL,
    "MinimumRate" numeric(18,4),
    "MinimumMarginPercent" numeric(7,4),
    CONSTRAINT "PK_m03_price_list_line" PRIMARY KEY ("TenantId", "PriceListLineId"),
    CONSTRAINT ck_m03_price_line_margin CHECK ("MinimumMarginPercent" IS NULL OR ("MinimumMarginPercent" >= 0 AND "MinimumMarginPercent" <= 100)),
    CONSTRAINT ck_m03_price_line_rates CHECK ("Rate" >= 0 AND ("MinimumRate" IS NULL OR "MinimumRate" >= 0)),
    CONSTRAINT "FK_m03_price_list_line_m03_price_list_TenantId_PriceListId" FOREIGN KEY ("TenantId", "PriceListId") REFERENCES m03_price_list ("TenantId", "PriceListId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m03_price_list_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT
);

CREATE TABLE m03_rmc_rate_card (
    "TenantId" uuid NOT NULL,
    "RateCardId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Grade" character varying(32) NOT NULL,
    "SlumpBand" character varying(32) NOT NULL,
    "AdmixtureCode" character varying(64) NOT NULL,
    "PumpType" character varying(32) NOT NULL,
    "Rate" numeric(18,4) NOT NULL,
    "AdmixtureChargeTypeId" uuid,
    "PumpChargeTypeId" uuid,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsApproved" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m03_rmc_rate_card" PRIMARY KEY ("TenantId", "RateCardId"),
    CONSTRAINT ck_m03_rmc_rate_card_dates CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT ck_m03_rmc_rate_card_rate CHECK ("Rate" >= 0),
    CONSTRAINT "FK_m03_rmc_rate_card_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT
);

COMMIT;
