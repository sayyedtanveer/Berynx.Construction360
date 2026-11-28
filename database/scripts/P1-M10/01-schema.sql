-- P1-M10 draft-invoice schema only; M12 posting/numbering tables are not implemented here.
-- Requires P1-M06, M08, M09 and M01–M03 prerequisite schemas.
BEGIN;
CREATE TABLE m10_invoice (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "InvoiceId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CustomerPartyId" uuid NOT NULL,
    "SiteId" uuid NULL,
    "Number" varchar(16) NOT NULL DEFAULT '',
    "IdempotencyKey" varchar(100) NOT NULL,
    "InvoiceDate" date NOT NULL,
    "DueDate" date NULL,
    "Status" varchar(24) NOT NULL,
    "InvoiceType" varchar(32) NOT NULL,
    "TaxTreatment" varchar(24) NOT NULL,
    "PlaceOfSupplyStateCode" varchar(8) NOT NULL,
    "CustomerNameSnapshot" varchar(240) NOT NULL,
    "CustomerGstinSnapshot" varchar(15) NOT NULL,
    "BranchGstinSnapshot" varchar(15) NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL CHECK ("TaxableAmount" >= 0),
    "CgstAmount" numeric(18,2) NOT NULL CHECK ("CgstAmount" >= 0),
    "SgstAmount" numeric(18,2) NOT NULL CHECK ("SgstAmount" >= 0),
    "IgstAmount" numeric(18,2) NOT NULL CHECK ("IgstAmount" >= 0),
    "TcsAmount" numeric(18,2) NOT NULL CHECK ("TcsAmount" >= 0),
    "RoundOffAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL CHECK ("TotalAmount" >= 0),
    "Notes" varchar(2000) NOT NULL,
    "PostingSnapshotJson" jsonb NOT NULL,
    "EInvoiceStatus" varchar(24) NOT NULL,
    "Irn" varchar(100) NULL,
    "AckNumber" varchar(100) NULL,
    "AckAt" timestamptz NULL,
    "EwayBillNumber" varchar(32) NULL,
    "EwayBillValidUntil" timestamptz NULL,
    "DisputeHold" boolean NOT NULL DEFAULT false,
    "Version" bigint NOT NULL DEFAULT 1,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "PostedAt" timestamptz NULL,
    "PostedByUserId" uuid NULL,
    PRIMARY KEY ("TenantId","InvoiceId"),
    CONSTRAINT "FK_m10_invoice_customer" FOREIGN KEY ("TenantId","CustomerPartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT
);
CREATE TABLE m10_invoice_line (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "InvoiceLineId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "SalesOrderId" uuid NOT NULL,
    "SalesOrderLineId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductNameSnapshot" varchar(240) NOT NULL,
    "HsnCodeSnapshot" varchar(16) NOT NULL,
    "UnitCodeSnapshot" varchar(24) NOT NULL,
    "ChallanReferencesJson" jsonb NOT NULL,
    "Quantity" numeric(18,3) NOT NULL CHECK ("Quantity" > 0),
    "Rate" numeric(18,4) NOT NULL CHECK ("Rate" >= 0),
    "TaxableAmount" numeric(18,2) NOT NULL CHECK ("TaxableAmount" >= 0),
    "TaxPercent" numeric(7,4) NOT NULL CHECK ("TaxPercent" >= 0),
    "CgstAmount" numeric(18,2) NOT NULL,
    "SgstAmount" numeric(18,2) NOT NULL,
    "IgstAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL CHECK ("TotalAmount" >= 0),
    "TaxSnapshotJson" jsonb NOT NULL,
    PRIMARY KEY ("TenantId","InvoiceLineId"),
    CONSTRAINT "FK_m10_invoice_line_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m10_invoice_line_order" FOREIGN KEY ("TenantId","SalesOrderId")
        REFERENCES m06_sales_order("TenantId","SalesOrderId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m10_invoice_line_order_line" FOREIGN KEY ("TenantId","SalesOrderLineId")
        REFERENCES m06_sales_order_line("TenantId","LineId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m10_invoice_line_product" FOREIGN KEY ("TenantId","ProductId")
        REFERENCES m03_product("TenantId","ProductId") ON DELETE RESTRICT
);
CREATE TABLE m10_invoice_challan (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "InvoiceChallanId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "ChallanId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL CHECK ("Quantity" > 0),
    "DeliveredValueSnapshot" numeric(18,2) NOT NULL CHECK ("DeliveredValueSnapshot" >= 0),
    PRIMARY KEY ("TenantId","InvoiceChallanId"),
    CONSTRAINT "FK_m10_invoice_challan_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m10_invoice_challan_challan" FOREIGN KEY ("TenantId","ChallanId")
        REFERENCES m09_delivery_challan("TenantId","ChallanId") ON DELETE RESTRICT
);
CREATE TABLE m10_invoice_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId"),
    CONSTRAINT "FK_m10_invoice_event_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT
);
COMMIT;
