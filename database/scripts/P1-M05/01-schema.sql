-- P1-M05 quotation table creation
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m05_quotation (
        "TenantId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "SeriesId" uuid NOT NULL,
        "RevisionNumber" integer NOT NULL,
        "Number" character varying(32),
        "IsActiveRevision" boolean NOT NULL,
        "SourceEnquiryId" uuid,
        "CustomerPartyId" uuid NOT NULL,
        "SiteId" uuid,
        "ContactId" uuid,
        "BranchId" uuid,
        "SalespersonUserId" uuid NOT NULL,
        "Status" character varying(24) NOT NULL,
        "PriceBasis" character varying(16) NOT NULL,
        "ValidUntil" date NOT NULL,
        "PaymentTermId" uuid,
        "PlaceOfSupplyStateCode" character varying(2) NOT NULL,
        "CompanyLegalNameSnapshot" character varying(200) NOT NULL,
        "SellerGstinSnapshot" character varying(15) NOT NULL,
        "CustomerNameSnapshot" character varying(200) NOT NULL,
        "CustomerGstinSnapshot" character varying(15),
        "CustomerAddressSnapshot" character varying(2000) NOT NULL,
        "SiteAddressSnapshot" character varying(2000) NOT NULL,
        "DeliveryTerms" character varying(2000) NOT NULL,
        "TermsAndConditions" character varying(8000) NOT NULL,
        "InternalNotes" character varying(4000) NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "DiscountTotal" numeric(18,2) NOT NULL,
        "ChargesTotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "MarginPercent" numeric(7,4),
        "CostStatus" character varying(24) NOT NULL,
        "CreditStatus" character varying(24) NOT NULL,
        "CreditSnapshotJson" jsonb NOT NULL,
        "ApprovalSnapshotJson" jsonb NOT NULL,
        "RateComparisonJson" jsonb NOT NULL,
        "ApprovalRequestId" uuid,
        "IdempotencyKey" character varying(80) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IssuedAt" timestamp with time zone,
        "AcceptedAt" timestamp with time zone,
        "RejectedAt" timestamp with time zone,
        "CancelledAt" timestamp with time zone,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m05_quotation" PRIMARY KEY ("TenantId", "QuotationId"),
        CONSTRAINT "CK_m05_quotation_amounts" CHECK ("Subtotal" >= 0 AND "DiscountTotal" >= 0 AND "ChargesTotal" >= 0 AND "TaxTotal" >= 0 AND "Total" >= 0),
        CONSTRAINT "CK_m05_quotation_price_basis" CHECK ("PriceBasis" IN ('ExPlant','Delivered','Site')),
        CONSTRAINT "CK_m05_quotation_revision" CHECK ("RevisionNumber" > 0),
        CONSTRAINT "CK_m05_quotation_status" CHECK ("Status" IN ('Draft','Pending approval','Approved','Sent','Accepted','Rejected','Expired','Revised','Cancelled')),
        CONSTRAINT "FK_m05_quotation_m03_contact_TenantId_ContactId" FOREIGN KEY ("TenantId", "ContactId") REFERENCES m03_contact ("TenantId", "ContactId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m05_quotation_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m05_quotation_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m05_quotation_m04_enquiry_TenantId_SourceEnquiryId" FOREIGN KEY ("TenantId", "SourceEnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m05_quotation_activity (
        "TenantId" uuid NOT NULL,
        "ActivityId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "ActorUserId" uuid,
        "Action" character varying(32) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "At" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m05_quotation_activity" PRIMARY KEY ("TenantId", "ActivityId"),
        CONSTRAINT "FK_m05_quotation_activity_m05_quotation_TenantId_QuotationId" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m05_quotation_charge (
        "TenantId" uuid NOT NULL,
        "ChargeId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "ChargeTypeId" uuid,
        "Description" character varying(200) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "TaxRatePercent" numeric(7,4) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "TaxTreatmentSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m05_quotation_charge" PRIMARY KEY ("TenantId", "ChargeId"),
        CONSTRAINT "CK_m05_quotation_charge_values" CHECK ("Amount" >= 0 AND "TaxRatePercent" >= 0 AND "TaxAmount" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m05_quotation_charge_m03_charge_type_TenantId_ChargeTypeId" FOREIGN KEY ("TenantId", "ChargeTypeId") REFERENCES m03_charge_type ("TenantId", "ChargeTypeId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m05_quotation_charge_m05_quotation_TenantId_QuotationId" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m05_quotation_line (
        "TenantId" uuid NOT NULL,
        "LineId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "UnitCode" character varying(16) NOT NULL,
        "Rate" numeric(18,4) NOT NULL,
        "DiscountPercent" numeric(7,4) NOT NULL,
        "GstPercent" numeric(7,4) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "PriceListId" uuid,
        "RateSource" character varying(64) NOT NULL,
        "RmcGrade" character varying(32),
        "RmcSlumpBand" character varying(32),
        "RmcAdmixtureCode" character varying(32),
        "RmcPumpType" character varying(32),
        "TaxSnapshotJson" jsonb NOT NULL,
        "TaxTreatmentSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m05_quotation_line" PRIMARY KEY ("TenantId", "LineId"),
        CONSTRAINT "CK_m05_quotation_line_quantity" CHECK ("Quantity" > 0),
        CONSTRAINT "CK_m05_quotation_line_values" CHECK ("Rate" >= 0 AND "DiscountPercent" BETWEEN 0 AND 100 AND "GstPercent" >= 0 AND "TaxableAmount" >= 0 AND "TaxAmount" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m05_quotation_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m05_quotation_line_m05_quotation_TenantId_QuotationId" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m05_quotation_link (
        "TenantId" uuid NOT NULL,
        "LinkId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        CONSTRAINT "PK_m05_quotation_link" PRIMARY KEY ("TenantId", "LinkId"),
        CONSTRAINT "FK_m05_quotation_link_m05_quotation_TenantId_QuotationId" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

COMMIT;
