START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_sales_order (
        "TenantId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "Number" character varying(40) NOT NULL,
        "OrderDate" date NOT NULL,
        "BranchId" uuid,
        "CustomerPartyId" uuid NOT NULL,
        "CustomerNameSnapshot" character varying(200) NOT NULL,
        "SiteId" uuid,
        "SiteAddressSnapshot" character varying(2000) NOT NULL,
        "CustomerPoNumber" character varying(100),
        "CustomerPoDate" date,
        "CustomerPoAttachmentReference" character varying(500),
        "SourceQuotationId" uuid,
        "SourceQuotationNumber" character varying(40),
        "SourceEnquiryId" uuid,
        "OrderType" character varying(16) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "SalesOwnerUserId" uuid NOT NULL,
        "RateValidityUntil" date,
        "PaymentTermId" uuid,
        "PaymentTermsSnapshot" character varying(2000) NOT NULL,
        "DeliveryTermsSnapshot" character varying(2000) NOT NULL,
        "Notes" character varying(4000) NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "DiscountTotal" numeric(18,2) NOT NULL,
        "ChargesTotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "CreditStatus" character varying(24) NOT NULL,
        "CreditSnapshotJson" jsonb NOT NULL,
        "CostStatus" character varying(24) NOT NULL,
        "EstimatedMarginPercent" numeric(7,4),
        "ApprovalSnapshotJson" jsonb NOT NULL,
        "ApprovalRequestId" uuid,
        "IdempotencyKey" character varying(80) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "ConfirmedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CancelledAt" timestamp with time zone,
        "ShortClosedAt" timestamp with time zone,
        "ClosureReason" character varying(1000),
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m06_sales_order" PRIMARY KEY ("TenantId", "SalesOrderId"),
        CONSTRAINT "CK_m06_sales_order_amounts" CHECK ("Subtotal" >= 0 AND "DiscountTotal" >= 0 AND "ChargesTotal" >= 0 AND "TaxTotal" >= 0 AND "Total" >= 0),
        CONSTRAINT "CK_m06_sales_order_status" CHECK ("Status" IN ('Draft','Pending approval','Confirmed','In fulfilment','Completed','Short-closed','On hold','Cancelled')),
        CONSTRAINT "CK_m06_sales_order_type" CHECK ("OrderType" IN ('RMC','Material','Mixed')),
        CONSTRAINT "FK_m06_sales_order_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_m04_enquiry_TenantId_SourceEnquiryId" FOREIGN KEY ("TenantId", "SourceEnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_m05_quotation_TenantId_SourceQuotationId" FOREIGN KEY ("TenantId", "SourceQuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_activity (
        "TenantId" uuid NOT NULL,
        "ActivityId" uuid NOT NULL,
        "EntityType" character varying(40) NOT NULL,
        "EntityId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Action" character varying(48) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "At" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m07_activity" PRIMARY KEY ("TenantId", "ActivityId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_number_counter (
        "TenantId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "DocumentCode" character varying(32) NOT NULL,
        "FinancialYearStart" integer NOT NULL,
        "NextNumber" bigint NOT NULL,
        CONSTRAINT "PK_m07_number_counter" PRIMARY KEY ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart"),
        CONSTRAINT "CK_m07_number_counter_values" CHECK ("FinancialYearStart" BETWEEN 2000 AND 2200 AND "NextNumber" > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_supplier_payment (
        "TenantId" uuid NOT NULL,
        "SupplierPaymentId" uuid NOT NULL,
        "Number" character varying(48) NOT NULL,
        "SupplierPartyId" uuid NOT NULL,
        "PaymentDate" date NOT NULL,
        "PaymentMode" character varying(32) NOT NULL,
        "ReferenceNumber" character varying(100),
        "DepositAccountId" uuid,
        "GrossAmount" numeric(18,2) NOT NULL,
        "TdsAmount" numeric(18,2) NOT NULL,
        "BankCharges" numeric(18,2) NOT NULL,
        "NetBankAmount" numeric(18,2) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "JournalId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m07_supplier_payment" PRIMARY KEY ("TenantId", "SupplierPaymentId"),
        CONSTRAINT "CK_m07_supplier_payment_amounts" CHECK ("GrossAmount" > 0 AND "TdsAmount" >= 0 AND "BankCharges" >= 0 AND "NetBankAmount" >= 0 AND "TdsAmount" + "BankCharges" <= "GrossAmount"),
        CONSTRAINT "FK_m07_supplier_payment_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m09_challan_number_counter (
        "TenantId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "DocumentCode" character varying(32) NOT NULL,
        "FinancialYearStart" integer NOT NULL,
        "NextNumber" bigint NOT NULL,
        CONSTRAINT "PK_m09_challan_number_counter" PRIMARY KEY ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart"),
        CONSTRAINT "CK_m09_challan_number_counter_values" CHECK ("FinancialYearStart" BETWEEN 2000 AND 2200 AND "NextNumber" > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m10_invoice (
        "TenantId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "CustomerPartyId" uuid NOT NULL,
        "SiteId" uuid,
        "Number" character varying(16) NOT NULL,
        "IdempotencyKey" character varying(100) NOT NULL,
        "InvoiceDate" date NOT NULL,
        "DueDate" date,
        "Status" character varying(24) NOT NULL,
        "InvoiceType" character varying(32) NOT NULL,
        "TaxTreatment" character varying(24) NOT NULL,
        "PlaceOfSupplyStateCode" character varying(8) NOT NULL,
        "CustomerNameSnapshot" character varying(240) NOT NULL,
        "CustomerGstinSnapshot" character varying(15) NOT NULL,
        "BranchGstinSnapshot" character varying(15) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TcsAmount" numeric(18,2) NOT NULL,
        "RoundOffAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Notes" character varying(2000) NOT NULL,
        "PostingSnapshotJson" jsonb NOT NULL,
        "EInvoiceStatus" character varying(24) NOT NULL,
        "Irn" character varying(100),
        "AckNumber" character varying(100),
        "AckAt" timestamp with time zone,
        "EwayBillNumber" character varying(32),
        "EwayBillValidUntil" timestamp with time zone,
        "DisputeHold" boolean NOT NULL,
        "Version" bigint NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        "PostedByUserId" uuid,
        CONSTRAINT "PK_m10_invoice" PRIMARY KEY ("TenantId", "InvoiceId"),
        CONSTRAINT "CK_m10_invoice_amounts" CHECK ("TaxableAmount" >= 0 AND "CgstAmount" >= 0 AND "SgstAmount" >= 0 AND "IgstAmount" >= 0 AND "TcsAmount" >= 0 AND "TotalAmount" >= 0),
        CONSTRAINT "FK_m10_invoice_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m11_receipt (
        "TenantId" uuid NOT NULL,
        "ReceiptId" uuid NOT NULL,
        "CustomerPartyId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "Number" character varying(16) NOT NULL,
        "ReceiptDate" date NOT NULL,
        "Status" character varying(24) NOT NULL,
        "PaymentMethod" character varying(24) NOT NULL,
        "ReferenceNumber" character varying(100) NOT NULL,
        "DepositAccountId" uuid,
        "Amount" numeric(18,2) NOT NULL,
        "TdsAmount" numeric(18,2) NOT NULL,
        "BankCharges" numeric(18,2) NOT NULL,
        "AllocatedAmount" numeric(18,2) NOT NULL,
        "AdvanceAmount" numeric(18,2) NOT NULL,
        "Notes" character varying(2000) NOT NULL,
        "IdempotencyKey" character varying(100) NOT NULL,
        "Version" bigint NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m11_receipt" PRIMARY KEY ("TenantId", "ReceiptId"),
        CONSTRAINT "CK_m11_receipt_amounts" CHECK ("Amount" > 0 AND "TdsAmount" >= 0 AND "BankCharges" >= 0 AND "AllocatedAmount" >= 0 AND "AdvanceAmount" >= 0),
        CONSTRAINT "FK_m11_receipt_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_financial_period (
        "TenantId" uuid NOT NULL,
        "PeriodStart" date NOT NULL,
        "PeriodEnd" date NOT NULL,
        "IsLocked" boolean NOT NULL,
        "LockReason" character varying(1000),
        "LockedByUserId" uuid,
        "LockedAt" timestamp with time zone,
        CONSTRAINT "PK_m12_financial_period" PRIMARY KEY ("TenantId", "PeriodStart"),
        CONSTRAINT "CK_m12_financial_period_dates" CHECK ("PeriodEnd" >= "PeriodStart")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_period_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "PeriodStart" date NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "Reason" character varying(1000) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m12_period_event" PRIMARY KEY ("TenantId", "EventId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_voucher (
        "TenantId" uuid NOT NULL,
        "VoucherId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "Number" character varying(32) NOT NULL,
        "VoucherType" character varying(24) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "VoucherDate" date NOT NULL,
        "SourceModule" character varying(32) NOT NULL,
        "SourceEntityId" uuid,
        "IdempotencyKey" character varying(100) NOT NULL,
        "Narration" character varying(1000) NOT NULL,
        "TotalDebit" numeric(18,2) NOT NULL,
        "TotalCredit" numeric(18,2) NOT NULL,
        "ReversalOfVoucherId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        CONSTRAINT "PK_m12_voucher" PRIMARY KEY ("TenantId", "VoucherId"),
        CONSTRAINT "CK_m12_voucher_amounts" CHECK ("TotalDebit" > 0 AND "TotalDebit" = "TotalCredit")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_voucher_number_counter (
        "TenantId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "VoucherType" character varying(24) NOT NULL,
        "CalendarYear" integer NOT NULL,
        "NextNumber" bigint NOT NULL,
        CONSTRAINT "PK_m12_voucher_number_counter" PRIMARY KEY ("TenantId", "BranchId", "VoucherType", "CalendarYear"),
        CONSTRAINT "CK_m12_voucher_number_counter_values" CHECK ("CalendarYear" BETWEEN 2000 AND 2200 AND "NextNumber" > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m13_gst_period (
        "TenantId" uuid NOT NULL,
        "PeriodStart" date NOT NULL,
        "PeriodEnd" date NOT NULL,
        "Status" character varying(24) NOT NULL,
        "FiledAt" timestamp with time zone,
        "FiledByUserId" uuid,
        "ExternalAcknowledgement" character varying(160) NOT NULL,
        "FilingSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m13_gst_period" PRIMARY KEY ("TenantId", "PeriodStart"),
        CONSTRAINT "CK_m13_gst_period_dates" CHECK ("PeriodEnd" >= "PeriodStart")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m14_expense_category (
        "TenantId" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "Type" character varying(24) NOT NULL,
        "ApprovalThreshold" numeric,
        "ExpenseLedgerCode" character varying(64) NOT NULL,
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_m14_expense_category" PRIMARY KEY ("TenantId", "CategoryId"),
        CONSTRAINT "CK_m14_expense_category_type" CHECK ("Type" IN ('Direct','Indirect','Capital','Other income'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m16_export_log (
        "TenantId" uuid NOT NULL,
        "ExportId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ReportCode" character varying(32) NOT NULL,
        "FiltersJson" jsonb NOT NULL,
        "RowCount" integer NOT NULL,
        "ExportedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m16_export_log" PRIMARY KEY ("TenantId", "ExportId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m17_catalogue_item (
        "TenantId" uuid NOT NULL,
        "CatalogueItemId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "ImageReference" character varying(500) NOT NULL,
        "KeySpecs" character varying(2000) NOT NULL,
        "IndicativePrice" numeric,
        "IsPublished" boolean NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        CONSTRAINT "PK_m17_catalogue_item" PRIMARY KEY ("TenantId", "CatalogueItemId"),
        CONSTRAINT "CK_m17_catalogue_price" CHECK ("IndicativePrice" IS NULL OR "IndicativePrice" >= 0),
        CONSTRAINT "FK_m17_catalogue_item_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m17_consent_record (
        "TenantId" uuid NOT NULL,
        "ConsentId" uuid NOT NULL,
        "ContactId" uuid NOT NULL,
        "Channel" character varying(16) NOT NULL,
        "IsOptedIn" boolean NOT NULL,
        "CapturedAt" timestamp with time zone NOT NULL,
        "Source" character varying(120) NOT NULL,
        "Wording" character varying(1000) NOT NULL,
        "OptedOutAt" timestamp with time zone,
        "OptOutSource" character varying(120) NOT NULL,
        CONSTRAINT "PK_m17_consent_record" PRIMARY KEY ("TenantId", "ConsentId"),
        CONSTRAINT "FK_m17_consent_record_m03_contact_TenantId_ContactId" FOREIGN KEY ("TenantId", "ContactId") REFERENCES m03_contact ("TenantId", "ContactId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m17_promotion_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "ConsentId" uuid,
        "CatalogueItemId" uuid,
        "EventType" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m17_promotion_event" PRIMARY KEY ("TenantId", "EventId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_activity (
        "TenantId" uuid NOT NULL,
        "ActivityId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Action" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "At" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_activity" PRIMARY KEY ("TenantId", "ActivityId"),
        CONSTRAINT "FK_m06_activity_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_amendment (
        "TenantId" uuid NOT NULL,
        "AmendmentId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "RevisionNumber" integer NOT NULL,
        "Reason" character varying(1000) NOT NULL,
        "BeforeSnapshotJson" jsonb NOT NULL,
        "ProposedSnapshotJson" jsonb NOT NULL,
        "Status" character varying(24) NOT NULL,
        "ApprovalRequestId" uuid,
        "RequestedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_amendment" PRIMARY KEY ("TenantId", "AmendmentId"),
        CONSTRAINT "CK_m06_amendment_revision" CHECK ("RevisionNumber" > 0),
        CONSTRAINT "CK_m06_amendment_status" CHECK ("Status" IN ('Pending approval','Approved','Rejected','Cancelled')),
        CONSTRAINT "FK_m06_amendment_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_linked_document (
        "TenantId" uuid NOT NULL,
        "LinkId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SourceModule" character varying(8) NOT NULL,
        "DocumentType" character varying(32) NOT NULL,
        "DocumentId" uuid NOT NULL,
        "DocumentNumber" character varying(80) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "LinkedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_linked_document" PRIMARY KEY ("TenantId", "LinkId"),
        CONSTRAINT "FK_m06_linked_document_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_sales_order_charge (
        "TenantId" uuid NOT NULL,
        "ChargeId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SourceQuotationChargeId" uuid,
        "Description" character varying(200) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "TaxRatePercent" numeric(7,4) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "TaxSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m06_sales_order_charge" PRIMARY KEY ("TenantId", "ChargeId"),
        CONSTRAINT "CK_m06_sales_order_charge_values" CHECK ("Amount" >= 0 AND "TaxRatePercent" >= 0 AND "TaxAmount" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m06_sales_order_charge_m05_quotation_charge_TenantId_Source~" FOREIGN KEY ("TenantId", "SourceQuotationChargeId") REFERENCES m05_quotation_charge ("TenantId", "ChargeId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_charge_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_sales_order_line (
        "TenantId" uuid NOT NULL,
        "LineId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SourceQuotationLineId" uuid,
        "ProductId" uuid NOT NULL,
        "ProductNameSnapshot" character varying(200) NOT NULL,
        "ProductCategorySnapshot" character varying(48) NOT NULL,
        "UnitCode" character varying(16) NOT NULL,
        "OrderedQuantity" numeric(18,3) NOT NULL,
        "Rate" numeric(18,4) NOT NULL,
        "DiscountPercent" numeric(7,4) NOT NULL,
        "GstPercent" numeric(7,4) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "TaxSnapshotJson" jsonb NOT NULL,
        "DeliveredQuantity" numeric(18,3) NOT NULL,
        "InvoicedQuantity" numeric(18,3) NOT NULL,
        "PaidAmount" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m06_sales_order_line" PRIMARY KEY ("TenantId", "LineId"),
        CONSTRAINT "CK_m06_sales_order_line_values" CHECK ("OrderedQuantity" > 0 AND "Rate" >= 0 AND "DiscountPercent" BETWEEN 0 AND 100 AND "GstPercent" >= 0 AND "DeliveredQuantity" >= 0 AND "InvoicedQuantity" >= 0 AND "PaidAmount" >= 0),
        CONSTRAINT "FK_m06_sales_order_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_line_m05_quotation_line_TenantId_SourceQuot~" FOREIGN KEY ("TenantId", "SourceQuotationLineId") REFERENCES m05_quotation_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_sales_order_line_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_purchase_order (
        "TenantId" uuid NOT NULL,
        "PurchaseOrderId" uuid NOT NULL,
        "Number" character varying(48) NOT NULL,
        "OrderDate" date NOT NULL,
        "BranchId" uuid,
        "SupplierPartyId" uuid NOT NULL,
        "SourceSalesOrderId" uuid,
        "SiteId" uuid,
        "SupplierNameSnapshot" character varying(240) NOT NULL,
        "SupplierGstinSnapshot" character varying(15) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "ExpectedDate" date,
        "PaymentTermsSnapshot" character varying(1000) NOT NULL,
        "DeliveryTermsSnapshot" character varying(1000) NOT NULL,
        "Notes" character varying(4000) NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "ChargesTotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "ApprovalRequestId" uuid,
        "ApprovalSnapshotJson" jsonb NOT NULL,
        "IdempotencyKey" character varying(100) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "ClosureReason" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m07_purchase_order" PRIMARY KEY ("TenantId", "PurchaseOrderId"),
        CONSTRAINT "CK_m07_purchase_order_amounts" CHECK ("Subtotal" >= 0 AND "ChargesTotal" >= 0 AND "TaxTotal" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m07_purchase_order_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_purchase_order_m06_sales_order_TenantId_SourceSalesOrde~" FOREIGN KEY ("TenantId", "SourceSalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m10_invoice_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m10_invoice_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m10_invoice_event_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m11_promise_to_pay (
        "TenantId" uuid NOT NULL,
        "PromiseId" uuid NOT NULL,
        "CustomerPartyId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "InvoiceId" uuid,
        "PromiseDate" date NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Outcome" character varying(24) NOT NULL,
        "Notes" character varying(1000) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m11_promise_to_pay" PRIMARY KEY ("TenantId", "PromiseId"),
        CONSTRAINT "CK_m11_promise_amount" CHECK ("Amount" > 0),
        CONSTRAINT "FK_m11_promise_to_pay_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m11_promise_to_pay_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m13_exception_review (
        "TenantId" uuid NOT NULL,
        "ReviewId" uuid NOT NULL,
        "PeriodStart" date NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "ExceptionCode" character varying(48) NOT NULL,
        "ReviewNote" character varying(1000) NOT NULL,
        "ReviewedByUserId" uuid NOT NULL,
        "ReviewedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m13_exception_review" PRIMARY KEY ("TenantId", "ReviewId"),
        CONSTRAINT "FK_m13_exception_review_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m11_receipt_allocation (
        "TenantId" uuid NOT NULL,
        "AllocationId" uuid NOT NULL,
        "ReceiptId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m11_receipt_allocation" PRIMARY KEY ("TenantId", "AllocationId"),
        CONSTRAINT "CK_m11_receipt_allocation_amount" CHECK ("Amount" > 0),
        CONSTRAINT "FK_m11_receipt_allocation_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m11_receipt_allocation_m11_receipt_TenantId_ReceiptId" FOREIGN KEY ("TenantId", "ReceiptId") REFERENCES m11_receipt ("TenantId", "ReceiptId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m11_receivable_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ReceiptId" uuid,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m11_receivable_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m11_receivable_event_m11_receipt_TenantId_ReceiptId" FOREIGN KEY ("TenantId", "ReceiptId") REFERENCES m11_receipt ("TenantId", "ReceiptId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_voucher_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "VoucherId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m12_voucher_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m12_voucher_event_m12_voucher_TenantId_VoucherId" FOREIGN KEY ("TenantId", "VoucherId") REFERENCES m12_voucher ("TenantId", "VoucherId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m12_voucher_line (
        "TenantId" uuid NOT NULL,
        "VoucherLineId" uuid NOT NULL,
        "VoucherId" uuid NOT NULL,
        "LedgerCode" character varying(64) NOT NULL,
        "PartyId" uuid,
        "BranchId" uuid,
        "SiteId" uuid,
        "SalesOrderId" uuid,
        "TripId" uuid,
        "Narration" character varying(500) NOT NULL,
        "Debit" numeric(18,2) NOT NULL,
        "Credit" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m12_voucher_line" PRIMARY KEY ("TenantId", "VoucherLineId"),
        CONSTRAINT "CK_m12_voucher_line_side" CHECK (("Debit" > 0 AND "Credit" = 0) OR ("Credit" > 0 AND "Debit" = 0)),
        CONSTRAINT "FK_m12_voucher_line_m03_party_TenantId_PartyId" FOREIGN KEY ("TenantId", "PartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m12_voucher_line_m12_voucher_TenantId_VoucherId" FOREIGN KEY ("TenantId", "VoucherId") REFERENCES m12_voucher ("TenantId", "VoucherId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m14_expense (
        "TenantId" uuid NOT NULL,
        "ExpenseId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "VendorPartyId" uuid,
        "SiteId" uuid,
        "SalesOrderId" uuid,
        "TripId" uuid,
        "ExpenseDate" date NOT NULL,
        "Status" character varying(32) NOT NULL,
        "Amount" numeric NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "PaidFromLedgerCode" character varying(64) NOT NULL,
        "EmployeePaid" boolean NOT NULL,
        "SupplierGstin" character varying(15) NOT NULL,
        "SupplierBillNumber" character varying(64) NOT NULL,
        "GstAmount" numeric NOT NULL,
        "ItcEligible" boolean NOT NULL,
        "ReverseCharge" boolean NOT NULL,
        "AttachmentReference" character varying(500) NOT NULL,
        "ApprovalComment" character varying(1000) NOT NULL,
        "ApprovalRequestId" uuid,
        "VoucherId" uuid,
        "IdempotencyKey" character varying(100) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        CONSTRAINT "PK_m14_expense" PRIMARY KEY ("TenantId", "ExpenseId"),
        CONSTRAINT "CK_m14_expense_amount" CHECK ("Amount" > 0),
        CONSTRAINT "CK_m14_expense_gst" CHECK ("GstAmount" >= 0 AND "GstAmount" <= "Amount"),
        CONSTRAINT "FK_m14_expense_m03_party_TenantId_VendorPartyId" FOREIGN KEY ("TenantId", "VendorPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m14_expense_m12_voucher_TenantId_VoucherId" FOREIGN KEY ("TenantId", "VoucherId") REFERENCES m12_voucher ("TenantId", "VoucherId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m14_expense_m14_expense_category_TenantId_CategoryId" FOREIGN KEY ("TenantId", "CategoryId") REFERENCES m14_expense_category ("TenantId", "CategoryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_quotation_charge_allocation (
        "TenantId" uuid NOT NULL,
        "AllocationId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "QuotationChargeId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderChargeId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_quotation_charge_allocation" PRIMARY KEY ("TenantId", "AllocationId"),
        CONSTRAINT "CK_m06_quotation_charge_allocation_values" CHECK ("Amount" >= 0 AND "CgstAmount" >= 0 AND "SgstAmount" >= 0 AND "IgstAmount" >= 0 AND "TaxAmount" >= 0),
        CONSTRAINT "FK_m06_quotation_charge_allocation_m05_quotation_TenantId_Quot~" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_charge_allocation_m05_quotation_charge_Tenant~" FOREIGN KEY ("TenantId", "QuotationChargeId") REFERENCES m05_quotation_charge ("TenantId", "ChargeId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_charge_allocation_m06_sales_order_TenantId_Sa~" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_charge_allocation_m06_sales_order_charge_Tena~" FOREIGN KEY ("TenantId", "SalesOrderChargeId") REFERENCES m06_sales_order_charge ("TenantId", "ChargeId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_delivery_schedule (
        "TenantId" uuid NOT NULL,
        "ScheduleId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "DeliveryDate" date NOT NULL,
        "TimeWindow" character varying(80),
        "SiteId" uuid,
        "PreferredPlantId" uuid,
        "OperationalNotes" character varying(2000) NOT NULL,
        "Status" character varying(16) NOT NULL,
        "ApprovalRequestId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_delivery_schedule" PRIMARY KEY ("TenantId", "ScheduleId"),
        CONSTRAINT "CK_m06_delivery_schedule_quantity" CHECK ("Quantity" > 0),
        CONSTRAINT "CK_m06_delivery_schedule_status" CHECK ("Status" IN ('Pending approval','Planned','Released','Cancelled')),
        CONSTRAINT "FK_m06_delivery_schedule_m03_plant_TenantId_PreferredPlantId" FOREIGN KEY ("TenantId", "PreferredPlantId") REFERENCES m03_plant ("TenantId", "PlantId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_delivery_schedule_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_delivery_schedule_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_delivery_schedule_m06_sales_order_line_TenantId_SalesOr~" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m06_quotation_allocation (
        "TenantId" uuid NOT NULL,
        "AllocationId" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "QuotationLineId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m06_quotation_allocation" PRIMARY KEY ("TenantId", "AllocationId"),
        CONSTRAINT "CK_m06_quotation_allocation_quantity" CHECK ("Quantity" > 0),
        CONSTRAINT "FK_m06_quotation_allocation_m05_quotation_TenantId_QuotationId" FOREIGN KEY ("TenantId", "QuotationId") REFERENCES m05_quotation ("TenantId", "QuotationId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_allocation_m05_quotation_line_TenantId_Quotat~" FOREIGN KEY ("TenantId", "QuotationLineId") REFERENCES m05_quotation_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_allocation_m06_sales_order_TenantId_SalesOrde~" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m06_quotation_allocation_m06_sales_order_line_TenantId_Sale~" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m10_invoice_line (
        "TenantId" uuid NOT NULL,
        "InvoiceLineId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "ProductNameSnapshot" character varying(240) NOT NULL,
        "HsnCodeSnapshot" character varying(16) NOT NULL,
        "UnitCodeSnapshot" character varying(24) NOT NULL,
        "ChallanReferencesJson" jsonb NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "Rate" numeric(18,4) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "TaxPercent" numeric(7,4) NOT NULL,
        "CgstAmount" numeric(18,2) NOT NULL,
        "SgstAmount" numeric(18,2) NOT NULL,
        "IgstAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "TaxSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m10_invoice_line" PRIMARY KEY ("TenantId", "InvoiceLineId"),
        CONSTRAINT "CK_m10_invoice_line_amounts" CHECK ("Quantity" > 0 AND "Rate" >= 0 AND "TaxableAmount" >= 0 AND "TaxPercent" >= 0 AND "TotalAmount" >= 0),
        CONSTRAINT "FK_m10_invoice_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m10_invoice_line_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m10_invoice_line_m06_sales_order_line_TenantId_SalesOrderLi~" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m10_invoice_line_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_goods_receipt (
        "TenantId" uuid NOT NULL,
        "GoodsReceiptId" uuid NOT NULL,
        "Number" character varying(48) NOT NULL,
        "ReceiptDate" date NOT NULL,
        "BranchId" uuid NOT NULL,
        "PurchaseOrderId" uuid NOT NULL,
        "SupplierPartyId" uuid NOT NULL,
        "SiteId" uuid,
        "Status" character varying(24) NOT NULL,
        "VehicleNumber" character varying(32),
        "DriverName" character varying(120),
        "WeighbridgeSlipNumber" character varying(80),
        "GrossWeight" numeric(18,3),
        "TareWeight" numeric(18,3),
        "NetWeight" numeric(18,3),
        "AttachmentReference" character varying(500),
        "Remarks" character varying(2000),
        "ReceivedByUserId" uuid NOT NULL,
        "ApprovalRequestId" uuid,
        "IdempotencyKey" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m07_goods_receipt" PRIMARY KEY ("TenantId", "GoodsReceiptId"),
        CONSTRAINT "CK_m07_goods_receipt_status" CHECK ("Status" IN ('Draft', 'Pending approval', 'Posted', 'Cancelled')),
        CONSTRAINT "CK_m07_goods_receipt_weights" CHECK (("GrossWeight" IS NULL OR "GrossWeight" >= 0) AND ("TareWeight" IS NULL OR "TareWeight" >= 0) AND ("NetWeight" IS NULL OR "NetWeight" >= 0)),
        CONSTRAINT "FK_m07_goods_receipt_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_goods_receipt_m07_purchase_order_TenantId_PurchaseOrder~" FOREIGN KEY ("TenantId", "PurchaseOrderId") REFERENCES m07_purchase_order ("TenantId", "PurchaseOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_purchase_order_charge (
        "TenantId" uuid NOT NULL,
        "ChargeId" uuid NOT NULL,
        "PurchaseOrderId" uuid NOT NULL,
        "Description" character varying(240) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "GstPercent" numeric(7,4) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m07_purchase_order_charge" PRIMARY KEY ("TenantId", "ChargeId"),
        CONSTRAINT "CK_m07_purchase_order_charge_amounts" CHECK ("Amount" >= 0 AND "GstPercent" BETWEEN 0 AND 100 AND "TaxAmount" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m07_purchase_order_charge_m07_purchase_order_TenantId_Purch~" FOREIGN KEY ("TenantId", "PurchaseOrderId") REFERENCES m07_purchase_order ("TenantId", "PurchaseOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_purchase_order_line (
        "TenantId" uuid NOT NULL,
        "PurchaseOrderLineId" uuid NOT NULL,
        "PurchaseOrderId" uuid NOT NULL,
        "SourceSalesOrderLineId" uuid,
        "ProductId" uuid NOT NULL,
        "ProductCodeSnapshot" character varying(48) NOT NULL,
        "ProductNameSnapshot" character varying(240) NOT NULL,
        "ProductCategorySnapshot" character varying(40) NOT NULL,
        "UnitCode" character varying(24) NOT NULL,
        "Traceable" boolean NOT NULL,
        "OrderedQuantity" numeric(18,3) NOT NULL,
        "ReceivedQuantity" numeric(18,3) NOT NULL,
        "Rate" numeric(18,4) NOT NULL,
        "GstPercent" numeric(7,4) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "TaxSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m07_purchase_order_line" PRIMARY KEY ("TenantId", "PurchaseOrderLineId"),
        CONSTRAINT "CK_m07_purchase_order_line_amounts" CHECK ("OrderedQuantity" > 0 AND "ReceivedQuantity" >= 0 AND "Rate" >= 0 AND "GstPercent" BETWEEN 0 AND 100 AND "TaxableAmount" >= 0 AND "TaxAmount" >= 0 AND "Total" >= 0),
        CONSTRAINT "FK_m07_purchase_order_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_purchase_order_line_m06_sales_order_line_TenantId_Sourc~" FOREIGN KEY ("TenantId", "SourceSalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_purchase_order_line_m07_purchase_order_TenantId_Purchas~" FOREIGN KEY ("TenantId", "PurchaseOrderId") REFERENCES m07_purchase_order ("TenantId", "PurchaseOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_supplier_invoice (
        "TenantId" uuid NOT NULL,
        "SupplierInvoiceId" uuid NOT NULL,
        "Number" character varying(48) NOT NULL,
        "SupplierPartyId" uuid NOT NULL,
        "SupplierInvoiceNumber" character varying(80) NOT NULL,
        "InvoiceDate" date NOT NULL,
        "DueDate" date NOT NULL,
        "PurchaseOrderId" uuid,
        "Status" character varying(24) NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "PaidAmount" numeric(18,2) NOT NULL,
        "TdsWithheld" numeric(18,2) NOT NULL,
        "AttachmentReference" character varying(500),
        "Notes" character varying(2000),
        "JournalId" uuid,
        "ApprovalRequestId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PostedAt" timestamp with time zone,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m07_supplier_invoice" PRIMARY KEY ("TenantId", "SupplierInvoiceId"),
        CONSTRAINT "CK_m07_supplier_invoice_amounts" CHECK ("Subtotal" >= 0 AND "TaxTotal" >= 0 AND "Total" >= 0 AND "PaidAmount" >= 0 AND "PaidAmount" <= "Total" AND "TdsWithheld" >= 0),
        CONSTRAINT "CK_m07_supplier_invoice_status" CHECK ("Status" IN ('Draft', 'Matched', 'Pending approval', 'Posted', 'Part-paid', 'Paid', 'Cancelled')),
        CONSTRAINT "FK_m07_supplier_invoice_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_supplier_invoice_m07_purchase_order_TenantId_PurchaseOr~" FOREIGN KEY ("TenantId", "PurchaseOrderId") REFERENCES m07_purchase_order ("TenantId", "PurchaseOrderId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m14_expense_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ExpenseId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "Detail" character varying(1000) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m14_expense_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m14_expense_event_m14_expense_TenantId_ExpenseId" FOREIGN KEY ("TenantId", "ExpenseId") REFERENCES m14_expense ("TenantId", "ExpenseId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m08_pour (
        "TenantId" uuid NOT NULL,
        "PourId" uuid NOT NULL,
        "ScheduleId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "SiteId" uuid,
        "BranchId" uuid NOT NULL,
        "PlantId" uuid,
        "ProductNameSnapshot" character varying(240) NOT NULL,
        "CustomerNameSnapshot" character varying(240) NOT NULL,
        "SiteNameSnapshot" character varying(240) NOT NULL,
        "ProductCategorySnapshot" character varying(40) NOT NULL,
        "WindowStart" timestamp with time zone NOT NULL,
        "WindowEnd" timestamp with time zone NOT NULL,
        "ScheduledQuantity" numeric(18,3) NOT NULL,
        "DeliveredQuantity" numeric(18,3) NOT NULL,
        "ReturnedQuantity" numeric(18,3) NOT NULL,
        "Slump" character varying(40) NOT NULL,
        "PlacementMethod" character varying(24) NOT NULL,
        "PumpType" character varying(80) NOT NULL,
        "PumpQuantity" numeric(18,3) NOT NULL,
        "CustomerEngineer" character varying(120) NOT NULL,
        "Coordinator" character varying(120) NOT NULL,
        "SpecialInstructions" character varying(2000) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "ClosureReason" character varying(1000),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m08_pour" PRIMARY KEY ("TenantId", "PourId"),
        CONSTRAINT "CK_m08_pour_quantities" CHECK ("ScheduledQuantity" > 0 AND "DeliveredQuantity" >= 0 AND "ReturnedQuantity" >= 0 AND "PumpQuantity" >= 0),
        CONSTRAINT "CK_m08_pour_window" CHECK ("WindowEnd" > "WindowStart"),
        CONSTRAINT "FK_m08_pour_m03_plant_TenantId_PlantId" FOREIGN KEY ("TenantId", "PlantId") REFERENCES m03_plant ("TenantId", "PlantId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_pour_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_pour_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_pour_m06_delivery_schedule_TenantId_ScheduleId" FOREIGN KEY ("TenantId", "ScheduleId") REFERENCES m06_delivery_schedule ("TenantId", "ScheduleId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_pour_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_pour_m06_sales_order_line_TenantId_SalesOrderLineId" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_goods_receipt_line (
        "TenantId" uuid NOT NULL,
        "GoodsReceiptLineId" uuid NOT NULL,
        "GoodsReceiptId" uuid NOT NULL,
        "PurchaseOrderLineId" uuid NOT NULL,
        "ReceivedQuantity" numeric(18,3) NOT NULL,
        "DamagedQuantity" numeric(18,3) NOT NULL,
        "ShortQuantity" numeric(18,3) NOT NULL,
        "ExceptionReason" character varying(500),
        "LotOrHeatNumber" character varying(100),
        "ManufacturerName" character varying(160),
        "MtcReference" character varying(500),
        CONSTRAINT "PK_m07_goods_receipt_line" PRIMARY KEY ("TenantId", "GoodsReceiptLineId"),
        CONSTRAINT "CK_m07_goods_receipt_line_quantities" CHECK ("ReceivedQuantity" > 0 AND "DamagedQuantity" >= 0 AND "DamagedQuantity" <= "ReceivedQuantity" AND "ShortQuantity" >= 0),
        CONSTRAINT "FK_m07_goods_receipt_line_m07_goods_receipt_TenantId_GoodsRece~" FOREIGN KEY ("TenantId", "GoodsReceiptId") REFERENCES m07_goods_receipt ("TenantId", "GoodsReceiptId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_goods_receipt_line_m07_purchase_order_line_TenantId_Pur~" FOREIGN KEY ("TenantId", "PurchaseOrderLineId") REFERENCES m07_purchase_order_line ("TenantId", "PurchaseOrderLineId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_supplier_payment_allocation (
        "TenantId" uuid NOT NULL,
        "AllocationId" uuid NOT NULL,
        "SupplierPaymentId" uuid NOT NULL,
        "SupplierInvoiceId" uuid,
        "Amount" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m07_supplier_payment_allocation" PRIMARY KEY ("TenantId", "AllocationId"),
        CONSTRAINT "CK_m07_supplier_payment_allocation_amount" CHECK ("Amount" > 0),
        CONSTRAINT "FK_m07_supplier_payment_allocation_m07_supplier_invoice_Tenant~" FOREIGN KEY ("TenantId", "SupplierInvoiceId") REFERENCES m07_supplier_invoice ("TenantId", "SupplierInvoiceId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_supplier_payment_allocation_m07_supplier_payment_Tenant~" FOREIGN KEY ("TenantId", "SupplierPaymentId") REFERENCES m07_supplier_payment ("TenantId", "SupplierPaymentId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m08_trip (
        "TenantId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "PourId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "PlantId" uuid,
        "VehicleId" uuid,
        "DriverId" uuid,
        "Number" character varying(48) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "PlannedQuantity" numeric(18,3) NOT NULL,
        "LoadedQuantity" numeric(18,3) NOT NULL,
        "DeliveredQuantity" numeric(18,3) NOT NULL,
        "ReturnedQuantity" numeric(18,3) NOT NULL,
        "BatchSlipNumber" character varying(80),
        "LotOrHeatNumber" character varying(100),
        "MtcReference" character varying(500),
        "WeighbridgeSlipNumber" character varying(80),
        "GrossWeight" numeric(18,3),
        "TareWeight" numeric(18,3),
        "NetWeight" numeric(18,3),
        "LoadedAt" timestamp with time zone,
        "DispatchedAt" timestamp with time zone,
        "ArrivedAt" timestamp with time zone,
        "UnloadingStartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "ReceiverName" character varying(160),
        "ReceiverMobile" character varying(24),
        "SignatureReference" character varying(500),
        "PhotoReference" character varying(500),
        "ReceiverVerifiedAt" timestamp with time zone,
        "EvidenceState" character varying(24) NOT NULL,
        "ExceptionType" character varying(40) NOT NULL,
        "ExceptionReason" character varying(1000),
        "WaitingMinutes" numeric(18,2) NOT NULL,
        "WaitingCharge" numeric(18,2) NOT NULL,
        "PartLoadCharge" numeric(18,2) NOT NULL,
        "PurchaseRateSnapshot" numeric(18,4),
        "PurchaseCostSnapshot" numeric(18,2),
        "CostSnapshotJson" jsonb NOT NULL,
        "ReplacedByTripId" text,
        "IdempotencyKey" character varying(100) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m08_trip" PRIMARY KEY ("TenantId", "TripId"),
        CONSTRAINT "CK_m08_trip_quantities" CHECK ("PlannedQuantity" > 0 AND "LoadedQuantity" >= 0 AND "DeliveredQuantity" >= 0 AND "ReturnedQuantity" >= 0 AND "WaitingMinutes" >= 0 AND "WaitingCharge" >= 0 AND "PartLoadCharge" >= 0),
        CONSTRAINT "CK_m08_trip_weights" CHECK (("GrossWeight" IS NULL OR "GrossWeight" >= 0) AND ("TareWeight" IS NULL OR "TareWeight" >= 0) AND ("NetWeight" IS NULL OR "NetWeight" >= 0)),
        CONSTRAINT "FK_m08_trip_m03_driver_TenantId_DriverId" FOREIGN KEY ("TenantId", "DriverId") REFERENCES m03_driver ("TenantId", "DriverId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_trip_m03_plant_TenantId_PlantId" FOREIGN KEY ("TenantId", "PlantId") REFERENCES m03_plant ("TenantId", "PlantId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_trip_m03_vehicle_TenantId_VehicleId" FOREIGN KEY ("TenantId", "VehicleId") REFERENCES m03_vehicle ("TenantId", "VehicleId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_trip_m08_pour_TenantId_PourId" FOREIGN KEY ("TenantId", "PourId") REFERENCES m08_pour ("TenantId", "PourId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m07_supplier_invoice_line (
        "TenantId" uuid NOT NULL,
        "InvoiceLineId" uuid NOT NULL,
        "SupplierInvoiceId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "PurchaseOrderLineId" uuid,
        "GoodsReceiptLineId" uuid,
        "Description" character varying(240) NOT NULL,
        "HsnCode" character varying(8) NOT NULL,
        "UnitCode" character varying(24) NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "Rate" numeric(18,4) NOT NULL,
        "GstPercent" numeric(7,4) NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "ItcEligible" boolean NOT NULL,
        "ReverseCharge" boolean NOT NULL,
        "TaxSnapshotJson" jsonb NOT NULL,
        CONSTRAINT "PK_m07_supplier_invoice_line" PRIMARY KEY ("TenantId", "InvoiceLineId"),
        CONSTRAINT "CK_m07_supplier_invoice_line_amounts" CHECK ("Quantity" > 0 AND "Rate" >= 0 AND "GstPercent" BETWEEN 0 AND 100 AND "TaxableAmount" >= 0 AND "TaxAmount" >= 0),
        CONSTRAINT "FK_m07_supplier_invoice_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_supplier_invoice_line_m07_goods_receipt_line_TenantId_G~" FOREIGN KEY ("TenantId", "GoodsReceiptLineId") REFERENCES m07_goods_receipt_line ("TenantId", "GoodsReceiptLineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_supplier_invoice_line_m07_purchase_order_line_TenantId_~" FOREIGN KEY ("TenantId", "PurchaseOrderLineId") REFERENCES m07_purchase_order_line ("TenantId", "PurchaseOrderLineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m07_supplier_invoice_line_m07_supplier_invoice_TenantId_Sup~" FOREIGN KEY ("TenantId", "SupplierInvoiceId") REFERENCES m07_supplier_invoice ("TenantId", "SupplierInvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m08_driver_action (
        "TenantId" uuid NOT NULL,
        "DriverActionId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "DriverId" uuid NOT NULL,
        "ActionId" character varying(100) NOT NULL,
        "ActionType" character varying(40) NOT NULL,
        "State" character varying(24) NOT NULL,
        "RejectionReason" character varying(1000),
        "DeviceAt" timestamp with time zone NOT NULL,
        "ReceivedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m08_driver_action" PRIMARY KEY ("TenantId", "DriverActionId"),
        CONSTRAINT "FK_m08_driver_action_m03_driver_TenantId_DriverId" FOREIGN KEY ("TenantId", "DriverId") REFERENCES m03_driver ("TenantId", "DriverId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m08_driver_action_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m08_receiver_challenge (
        "TenantId" uuid NOT NULL,
        "ChallengeId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "DestinationMobileProtected" character varying(500) NOT NULL,
        "CodeHash" character varying(200) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "ResendCount" integer NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "FailedAttempts" integer NOT NULL,
        CONSTRAINT "PK_m08_receiver_challenge" PRIMARY KEY ("TenantId", "ChallengeId"),
        CONSTRAINT "FK_m08_receiver_challenge_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m08_trip_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "FromStatus" character varying(24) NOT NULL,
        "ToStatus" character varying(24) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "DeviceAt" timestamp with time zone NOT NULL,
        "ReceivedAt" timestamp with time zone NOT NULL,
        "ActionId" character varying(100) NOT NULL,
        CONSTRAINT "PK_m08_trip_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m08_trip_event_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m09_delivery_challan (
        "TenantId" uuid NOT NULL,
        "ChallanId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "PourId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "CustomerPartyId" uuid NOT NULL,
        "SiteId" uuid,
        "ProductId" uuid NOT NULL,
        "Number" character varying(48) NOT NULL,
        "ChallanDate" date NOT NULL,
        "DeliveredAt" timestamp with time zone NOT NULL,
        "Status" character varying(24) NOT NULL,
        "CustomerNameSnapshot" character varying(240) NOT NULL,
        "SiteAddressSnapshot" character varying(1000) NOT NULL,
        "OrderNumberSnapshot" character varying(48) NOT NULL,
        "CustomerPoNumberSnapshot" character varying(80),
        "ProductNameSnapshot" character varying(240) NOT NULL,
        "UnitCodeSnapshot" character varying(24) NOT NULL,
        "DeliveredQuantity" numeric(18,3) NOT NULL,
        "ReturnedQuantity" numeric(18,3) NOT NULL,
        "UnitRateSnapshot" numeric(18,4) NOT NULL,
        "TaxPercentSnapshot" numeric(7,4) NOT NULL,
        "DeliveredValueSnapshot" numeric(18,2) NOT NULL,
        "VehicleNumberSnapshot" character varying(32) NOT NULL,
        "DriverNameSnapshot" character varying(160) NOT NULL,
        "PlantNameSnapshot" character varying(160) NOT NULL,
        "BatchSlipNumber" character varying(80) NOT NULL,
        "ReceiverName" character varying(160),
        "ReceiverMobile" character varying(24),
        "EvidenceState" character varying(24) NOT NULL,
        "SignatureReference" character varying(500),
        "PhotoReference" character varying(500),
        "EwayBillNumber" character varying(32),
        "InvoiceId" uuid,
        "Remarks" character varying(2000) NOT NULL,
        "Version" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        CONSTRAINT "PK_m09_delivery_challan" PRIMARY KEY ("TenantId", "ChallanId"),
        CONSTRAINT "CK_m09_delivery_challan_quantities" CHECK ("DeliveredQuantity" > 0 AND "ReturnedQuantity" >= 0 AND "UnitRateSnapshot" >= 0 AND "DeliveredValueSnapshot" >= 0),
        CONSTRAINT "FK_m09_delivery_challan_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m06_sales_order_line_TenantId_SalesOrd~" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m08_pour_TenantId_PourId" FOREIGN KEY ("TenantId", "PourId") REFERENCES m08_pour ("TenantId", "PourId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m09_delivery_challan_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m09_challan_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ChallanId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(40) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m09_challan_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m09_challan_event_m09_delivery_challan_TenantId_ChallanId" FOREIGN KEY ("TenantId", "ChallanId") REFERENCES m09_delivery_challan ("TenantId", "ChallanId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m09_challan_revision (
        "TenantId" uuid NOT NULL,
        "RevisionId" uuid NOT NULL,
        "ChallanId" uuid NOT NULL,
        "RevisionNumber" integer NOT NULL,
        "RequestedByUserId" uuid NOT NULL,
        "ApprovalRequestId" uuid,
        "Reason" character varying(1000) NOT NULL,
        "OriginalQuantity" numeric(18,3) NOT NULL,
        "CorrectedQuantity" numeric(18,3) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "AppliedAt" timestamp with time zone,
        CONSTRAINT "PK_m09_challan_revision" PRIMARY KEY ("TenantId", "RevisionId"),
        CONSTRAINT "CK_m09_challan_revision_quantities" CHECK ("RevisionNumber" > 0 AND "OriginalQuantity" > 0 AND "CorrectedQuantity" > 0),
        CONSTRAINT "FK_m09_challan_revision_m09_delivery_challan_TenantId_ChallanId" FOREIGN KEY ("TenantId", "ChallanId") REFERENCES m09_delivery_challan ("TenantId", "ChallanId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE TABLE m10_invoice_challan (
        "TenantId" uuid NOT NULL,
        "InvoiceChallanId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "ChallanId" uuid NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "DeliveredValueSnapshot" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_m10_invoice_challan" PRIMARY KEY ("TenantId", "InvoiceChallanId"),
        CONSTRAINT "CK_m10_invoice_challan_values" CHECK ("Quantity" > 0 AND "DeliveredValueSnapshot" >= 0),
        CONSTRAINT "FK_m10_invoice_challan_m09_delivery_challan_TenantId_ChallanId" FOREIGN KEY ("TenantId", "ChallanId") REFERENCES m09_delivery_challan ("TenantId", "ChallanId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m10_invoice_challan_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_activity_TenantId_SalesOrderId_At" ON m06_activity ("TenantId", "SalesOrderId", "At");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_amendment_TenantId_SalesOrderId_RevisionNumber" ON m06_amendment ("TenantId", "SalesOrderId", "RevisionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_delivery_schedule_TenantId_PreferredPlantId" ON m06_delivery_schedule ("TenantId", "PreferredPlantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_delivery_schedule_TenantId_SalesOrderId_DeliveryDate" ON m06_delivery_schedule ("TenantId", "SalesOrderId", "DeliveryDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_delivery_schedule_TenantId_SalesOrderLineId" ON m06_delivery_schedule ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_delivery_schedule_TenantId_SiteId" ON m06_delivery_schedule ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_linked_document_TenantId_SalesOrderId_SourceModule_Docu~" ON m06_linked_document ("TenantId", "SalesOrderId", "SourceModule", "DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_allocation_TenantId_QuotationId" ON m06_quotation_allocation ("TenantId", "QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_allocation_TenantId_QuotationLineId" ON m06_quotation_allocation ("TenantId", "QuotationLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_allocation_TenantId_SalesOrderId" ON m06_quotation_allocation ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_quotation_allocation_TenantId_SalesOrderLineId" ON m06_quotation_allocation ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_charge_allocation_TenantId_QuotationChargeId" ON m06_quotation_charge_allocation ("TenantId", "QuotationChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_charge_allocation_TenantId_QuotationId" ON m06_quotation_charge_allocation ("TenantId", "QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_quotation_charge_allocation_TenantId_SalesOrderChargeId" ON m06_quotation_charge_allocation ("TenantId", "SalesOrderChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_quotation_charge_allocation_TenantId_SalesOrderId" ON m06_quotation_charge_allocation ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_TenantId_CustomerPartyId_Status" ON m06_sales_order ("TenantId", "CustomerPartyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_sales_order_TenantId_IdempotencyKey" ON m06_sales_order ("TenantId", "IdempotencyKey") WHERE "IdempotencyKey" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m06_sales_order_TenantId_Number" ON m06_sales_order ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_TenantId_SiteId" ON m06_sales_order ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_TenantId_SourceEnquiryId" ON m06_sales_order ("TenantId", "SourceEnquiryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_TenantId_SourceQuotationId" ON m06_sales_order ("TenantId", "SourceQuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_TenantId_Status_OrderDate_BranchId" ON m06_sales_order ("TenantId", "Status", "OrderDate", "BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_charge_TenantId_SalesOrderId" ON m06_sales_order_charge ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_charge_TenantId_SourceQuotationChargeId" ON m06_sales_order_charge ("TenantId", "SourceQuotationChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_line_TenantId_ProductId" ON m06_sales_order_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_line_TenantId_SalesOrderId" ON m06_sales_order_line ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m06_sales_order_line_TenantId_SourceQuotationLineId" ON m06_sales_order_line ("TenantId", "SourceQuotationLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_activity_TenantId_EntityType_EntityId_At" ON m07_activity ("TenantId", "EntityType", "EntityId", "At");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_goods_receipt_TenantId_IdempotencyKey" ON m07_goods_receipt ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_goods_receipt_TenantId_Number" ON m07_goods_receipt ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_goods_receipt_TenantId_PurchaseOrderId_Status" ON m07_goods_receipt ("TenantId", "PurchaseOrderId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_goods_receipt_TenantId_SupplierPartyId" ON m07_goods_receipt ("TenantId", "SupplierPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_goods_receipt_line_TenantId_GoodsReceiptId" ON m07_goods_receipt_line ("TenantId", "GoodsReceiptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_goods_receipt_line_TenantId_PurchaseOrderLineId" ON m07_goods_receipt_line ("TenantId", "PurchaseOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_purchase_order_TenantId_IdempotencyKey" ON m07_purchase_order ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_purchase_order_TenantId_Number" ON m07_purchase_order ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_TenantId_SourceSalesOrderId" ON m07_purchase_order ("TenantId", "SourceSalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_TenantId_SupplierPartyId_OrderDate_Status" ON m07_purchase_order ("TenantId", "SupplierPartyId", "OrderDate", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_charge_TenantId_PurchaseOrderId" ON m07_purchase_order_charge ("TenantId", "PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_line_TenantId_ProductId" ON m07_purchase_order_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_line_TenantId_PurchaseOrderId" ON m07_purchase_order_line ("TenantId", "PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_purchase_order_line_TenantId_SourceSalesOrderLineId" ON m07_purchase_order_line ("TenantId", "SourceSalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_supplier_invoice_TenantId_Number" ON m07_supplier_invoice ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_TenantId_PurchaseOrderId" ON m07_supplier_invoice ("TenantId", "PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_TenantId_SupplierPartyId_DueDate_Status" ON m07_supplier_invoice ("TenantId", "SupplierPartyId", "DueDate", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_supplier_invoice_TenantId_SupplierPartyId_SupplierInvoi~" ON m07_supplier_invoice ("TenantId", "SupplierPartyId", "SupplierInvoiceNumber", "InvoiceDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_line_TenantId_GoodsReceiptLineId" ON m07_supplier_invoice_line ("TenantId", "GoodsReceiptLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_line_TenantId_ProductId" ON m07_supplier_invoice_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_line_TenantId_PurchaseOrderLineId" ON m07_supplier_invoice_line ("TenantId", "PurchaseOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_invoice_line_TenantId_SupplierInvoiceId" ON m07_supplier_invoice_line ("TenantId", "SupplierInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_supplier_payment_TenantId_Number" ON m07_supplier_payment ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_payment_TenantId_SupplierPartyId_PaymentDate" ON m07_supplier_payment ("TenantId", "SupplierPartyId", "PaymentDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m07_supplier_payment_allocation_TenantId_SupplierInvoiceId" ON m07_supplier_payment_allocation ("TenantId", "SupplierInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m07_supplier_payment_allocation_TenantId_SupplierPaymentId_~" ON m07_supplier_payment_allocation ("TenantId", "SupplierPaymentId", "SupplierInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m08_driver_action_TenantId_ActionId" ON m08_driver_action ("TenantId", "ActionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_driver_action_TenantId_DriverId_State_DeviceAt" ON m08_driver_action ("TenantId", "DriverId", "State", "DeviceAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_driver_action_TenantId_TripId" ON m08_driver_action ("TenantId", "TripId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_BranchId_WindowStart_Status" ON m08_pour ("TenantId", "BranchId", "WindowStart", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_PlantId_WindowStart_WindowEnd" ON m08_pour ("TenantId", "PlantId", "WindowStart", "WindowEnd");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_ProductId" ON m08_pour ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_SalesOrderId" ON m08_pour ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_SalesOrderLineId" ON m08_pour ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m08_pour_TenantId_ScheduleId" ON m08_pour ("TenantId", "ScheduleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_pour_TenantId_SiteId" ON m08_pour ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_receiver_challenge_TenantId_TripId_CreatedAt" ON m08_receiver_challenge ("TenantId", "TripId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_trip_TenantId_DriverId_Status" ON m08_trip ("TenantId", "DriverId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m08_trip_TenantId_IdempotencyKey" ON m08_trip ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m08_trip_TenantId_Number" ON m08_trip ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_trip_TenantId_PlantId" ON m08_trip ("TenantId", "PlantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_trip_TenantId_PourId_Status" ON m08_trip ("TenantId", "PourId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_trip_TenantId_VehicleId_Status" ON m08_trip ("TenantId", "VehicleId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m08_trip_event_TenantId_ActionId" ON m08_trip_event ("TenantId", "ActionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m08_trip_event_TenantId_TripId_ReceivedAt" ON m08_trip_event ("TenantId", "TripId", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_challan_event_TenantId_ChallanId_OccurredAt" ON m09_challan_event ("TenantId", "ChallanId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m09_challan_revision_TenantId_ChallanId_RevisionNumber" ON m09_challan_revision ("TenantId", "ChallanId", "RevisionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m09_delivery_challan_TenantId_BranchId_ChallanDate_Number" ON m09_delivery_challan ("TenantId", "BranchId", "ChallanDate", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_CustomerPartyId" ON m09_delivery_challan ("TenantId", "CustomerPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_PourId" ON m09_delivery_challan ("TenantId", "PourId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_ProductId" ON m09_delivery_challan ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_SalesOrderId" ON m09_delivery_challan ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_SalesOrderLineId" ON m09_delivery_challan ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_SiteId" ON m09_delivery_challan ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m09_delivery_challan_TenantId_Status_ChallanDate" ON m09_delivery_challan ("TenantId", "Status", "ChallanDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m09_delivery_challan_TenantId_TripId" ON m09_delivery_challan ("TenantId", "TripId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_TenantId_BranchId_Status_InvoiceDate" ON m10_invoice ("TenantId", "BranchId", "Status", "InvoiceDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_TenantId_CustomerPartyId_DueDate" ON m10_invoice ("TenantId", "CustomerPartyId", "DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m10_invoice_TenantId_IdempotencyKey" ON m10_invoice ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m10_invoice_challan_TenantId_ChallanId" ON m10_invoice_challan ("TenantId", "ChallanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_challan_TenantId_InvoiceId" ON m10_invoice_challan ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_event_TenantId_InvoiceId_OccurredAt" ON m10_invoice_event ("TenantId", "InvoiceId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_line_TenantId_InvoiceId" ON m10_invoice_line ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_line_TenantId_ProductId" ON m10_invoice_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_line_TenantId_SalesOrderId" ON m10_invoice_line ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m10_invoice_line_TenantId_SalesOrderLineId" ON m10_invoice_line ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_promise_to_pay_TenantId_BranchId_PromiseDate_Outcome" ON m11_promise_to_pay ("TenantId", "BranchId", "PromiseDate", "Outcome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_promise_to_pay_TenantId_CustomerPartyId" ON m11_promise_to_pay ("TenantId", "CustomerPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_promise_to_pay_TenantId_InvoiceId" ON m11_promise_to_pay ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_receipt_TenantId_CustomerPartyId_ReceiptDate_Status" ON m11_receipt ("TenantId", "CustomerPartyId", "ReceiptDate", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m11_receipt_TenantId_IdempotencyKey" ON m11_receipt ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_receipt_allocation_TenantId_InvoiceId" ON m11_receipt_allocation ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_receipt_allocation_TenantId_ReceiptId" ON m11_receipt_allocation ("TenantId", "ReceiptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m11_receivable_event_TenantId_ReceiptId_OccurredAt" ON m11_receivable_event ("TenantId", "ReceiptId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_financial_period_TenantId_PeriodStart_PeriodEnd_IsLocked" ON m12_financial_period ("TenantId", "PeriodStart", "PeriodEnd", "IsLocked");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_period_event_TenantId_PeriodStart_OccurredAt" ON m12_period_event ("TenantId", "PeriodStart", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m12_voucher_TenantId_BranchId_Number" ON m12_voucher ("TenantId", "BranchId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m12_voucher_TenantId_IdempotencyKey" ON m12_voucher ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_TenantId_VoucherDate_VoucherType" ON m12_voucher ("TenantId", "VoucherDate", "VoucherType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_event_TenantId_VoucherId_OccurredAt" ON m12_voucher_event ("TenantId", "VoucherId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_line_TenantId_LedgerCode_BranchId" ON m12_voucher_line ("TenantId", "LedgerCode", "BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_line_TenantId_PartyId" ON m12_voucher_line ("TenantId", "PartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_line_TenantId_SalesOrderId_TripId" ON m12_voucher_line ("TenantId", "SalesOrderId", "TripId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m12_voucher_line_TenantId_VoucherId" ON m12_voucher_line ("TenantId", "VoucherId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m13_exception_review_TenantId_InvoiceId" ON m13_exception_review ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m13_exception_review_TenantId_PeriodStart_InvoiceId_Excepti~" ON m13_exception_review ("TenantId", "PeriodStart", "InvoiceId", "ExceptionCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m14_expense_TenantId_CategoryId" ON m14_expense ("TenantId", "CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m14_expense_TenantId_IdempotencyKey" ON m14_expense ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m14_expense_TenantId_VendorPartyId_SupplierBillNumber_Amount" ON m14_expense ("TenantId", "VendorPartyId", "SupplierBillNumber", "Amount") WHERE "SupplierBillNumber" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m14_expense_TenantId_VoucherId" ON m14_expense ("TenantId", "VoucherId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m14_expense_category_TenantId_Name" ON m14_expense_category ("TenantId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m14_expense_event_TenantId_ExpenseId_OccurredAt" ON m14_expense_event ("TenantId", "ExpenseId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m16_export_log_TenantId_UserId_ExportedAt" ON m16_export_log ("TenantId", "UserId", "ExportedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m17_catalogue_item_TenantId_ProductId" ON m17_catalogue_item ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE UNIQUE INDEX "IX_m17_consent_record_TenantId_ContactId_Channel" ON m17_consent_record ("TenantId", "ContactId", "Channel");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    CREATE INDEX "IX_m17_promotion_event_TenantId_OccurredAt" ON m17_promotion_event ("TenantId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261126104710_P1M06M17OperationalFoundation', '10.0.15');
    END IF;
END $EF$;
COMMIT;

