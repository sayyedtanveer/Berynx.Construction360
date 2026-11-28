-- P1-M06 schema package. The EF migration is pending generation/review.
-- Prerequisites and exact manual order are in README.md.
BEGIN;

CREATE TABLE IF NOT EXISTS m06_sales_order (
    "TenantId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL, "Number" varchar(40) NOT NULL,
    "OrderDate" date NOT NULL, "BranchId" uuid, "CustomerPartyId" uuid NOT NULL,
    "CustomerNameSnapshot" varchar(200) NOT NULL, "SiteId" uuid,
    "SiteAddressSnapshot" varchar(2000) NOT NULL, "CustomerPoNumber" varchar(100),
    "CustomerPoDate" date, "CustomerPoAttachmentReference" varchar(500),
    "SourceQuotationId" uuid, "SourceQuotationNumber" varchar(40), "SourceEnquiryId" uuid,
    "OrderType" varchar(16) NOT NULL, "Status" varchar(24) NOT NULL,
    "SalesOwnerUserId" uuid NOT NULL, "RateValidityUntil" date, "PaymentTermId" uuid,
    "PaymentTermsSnapshot" varchar(2000) NOT NULL, "DeliveryTermsSnapshot" varchar(2000) NOT NULL,
    "Notes" varchar(4000) NOT NULL, "Subtotal" numeric(18,2) NOT NULL,
    "DiscountTotal" numeric(18,2) NOT NULL, "ChargesTotal" numeric(18,2) NOT NULL,
    "TaxTotal" numeric(18,2) NOT NULL, "Total" numeric(18,2) NOT NULL,
    "CreditStatus" varchar(24) NOT NULL, "CreditSnapshotJson" jsonb NOT NULL,
    "CostStatus" varchar(24) NOT NULL, "EstimatedMarginPercent" numeric(7,4),
    "ApprovalSnapshotJson" jsonb NOT NULL, "ApprovalRequestId" uuid,
    "IdempotencyKey" varchar(80) NOT NULL, "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL, "ConfirmedAt" timestamptz, "CompletedAt" timestamptz,
    "CancelledAt" timestamptz, "ShortClosedAt" timestamptz, "ClosureReason" varchar(1000),
    "Version" bigint NOT NULL, CONSTRAINT "PK_m06_sales_order" PRIMARY KEY ("TenantId","SalesOrderId")
);

CREATE TABLE IF NOT EXISTS m06_sales_order_line (
    "TenantId" uuid NOT NULL, "LineId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "SourceQuotationLineId" uuid, "ProductId" uuid NOT NULL, "ProductNameSnapshot" varchar(200) NOT NULL,
    "ProductCategorySnapshot" varchar(48) NOT NULL, "UnitCode" varchar(16) NOT NULL,
    "OrderedQuantity" numeric(18,3) NOT NULL, "Rate" numeric(18,4) NOT NULL,
    "DiscountPercent" numeric(7,4) NOT NULL, "GstPercent" numeric(7,4) NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL, "CgstAmount" numeric(18,2) NOT NULL,
    "SgstAmount" numeric(18,2) NOT NULL, "IgstAmount" numeric(18,2) NOT NULL,
    "TaxAmount" numeric(18,2) NOT NULL, "Total" numeric(18,2) NOT NULL,
    "TaxSnapshotJson" jsonb NOT NULL, "DeliveredQuantity" numeric(18,3) NOT NULL,
    "InvoicedQuantity" numeric(18,3) NOT NULL, "PaidAmount" numeric(18,2) NOT NULL,
    CONSTRAINT "PK_m06_sales_order_line" PRIMARY KEY ("TenantId","LineId")
);

CREATE TABLE IF NOT EXISTS m06_sales_order_charge (
    "TenantId" uuid NOT NULL, "ChargeId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "SourceQuotationChargeId" uuid, "Description" varchar(200) NOT NULL,
    "Amount" numeric(18,2) NOT NULL, "TaxRatePercent" numeric(7,4) NOT NULL,
    "CgstAmount" numeric(18,2) NOT NULL, "SgstAmount" numeric(18,2) NOT NULL,
    "IgstAmount" numeric(18,2) NOT NULL, "TaxAmount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL, "TaxSnapshotJson" jsonb NOT NULL,
    CONSTRAINT "PK_m06_sales_order_charge" PRIMARY KEY ("TenantId","ChargeId")
);

CREATE TABLE IF NOT EXISTS m06_quotation_allocation (
    "TenantId" uuid NOT NULL, "AllocationId" uuid NOT NULL, "QuotationId" uuid NOT NULL,
    "QuotationLineId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL, "SalesOrderLineId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL, "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_m06_quotation_allocation" PRIMARY KEY ("TenantId","AllocationId")
);

CREATE TABLE IF NOT EXISTS m06_quotation_charge_allocation (
    "TenantId" uuid NOT NULL, "AllocationId" uuid NOT NULL, "QuotationId" uuid NOT NULL,
    "QuotationChargeId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL, "SalesOrderChargeId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL, "CgstAmount" numeric(18,2) NOT NULL,
    "SgstAmount" numeric(18,2) NOT NULL, "IgstAmount" numeric(18,2) NOT NULL,
    "TaxAmount" numeric(18,2) NOT NULL, "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_m06_quotation_charge_allocation" PRIMARY KEY ("TenantId","AllocationId")
);

CREATE TABLE IF NOT EXISTS m06_delivery_schedule (
    "TenantId" uuid NOT NULL, "ScheduleId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "SalesOrderLineId" uuid NOT NULL, "ProductId" uuid NOT NULL, "Quantity" numeric(18,3) NOT NULL,
    "DeliveryDate" date NOT NULL, "TimeWindow" varchar(80), "SiteId" uuid,
    "PreferredPlantId" uuid, "OperationalNotes" varchar(2000) NOT NULL, "Status" varchar(16) NOT NULL,
    "ApprovalRequestId" uuid, "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_m06_delivery_schedule" PRIMARY KEY ("TenantId","ScheduleId")
);

CREATE TABLE IF NOT EXISTS m06_amendment (
    "TenantId" uuid NOT NULL, "AmendmentId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "RevisionNumber" integer NOT NULL, "Reason" varchar(1000) NOT NULL,
    "BeforeSnapshotJson" jsonb NOT NULL, "ProposedSnapshotJson" jsonb NOT NULL,
    "Status" varchar(24) NOT NULL, "ApprovalRequestId" uuid, "RequestedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_m06_amendment" PRIMARY KEY ("TenantId","AmendmentId")
);

CREATE TABLE IF NOT EXISTS m06_activity (
    "TenantId" uuid NOT NULL, "ActivityId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL, "Action" varchar(40) NOT NULL, "DetailJson" jsonb NOT NULL,
    "At" timestamptz NOT NULL, CONSTRAINT "PK_m06_activity" PRIMARY KEY ("TenantId","ActivityId")
);

CREATE TABLE IF NOT EXISTS m06_linked_document (
    "TenantId" uuid NOT NULL, "LinkId" uuid NOT NULL, "SalesOrderId" uuid NOT NULL,
    "SourceModule" varchar(8) NOT NULL, "DocumentType" varchar(32) NOT NULL,
    "DocumentId" uuid NOT NULL, "DocumentNumber" varchar(80) NOT NULL,
    "Status" varchar(32) NOT NULL, "LinkedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_m06_linked_document" PRIMARY KEY ("TenantId","LinkId")
);

COMMIT;
