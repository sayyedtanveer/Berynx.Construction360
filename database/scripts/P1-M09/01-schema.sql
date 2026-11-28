-- P1-M09 schema. Requires M01–M03, M06, M08 schema and tenant seed infrastructure.
BEGIN;

CREATE TABLE m09_challan_number_counter (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "BranchId" uuid NOT NULL,
    "DocumentCode" varchar(32) NOT NULL,
    "FinancialYearStart" integer NOT NULL CHECK ("FinancialYearStart" BETWEEN 2000 AND 2200),
    "NextNumber" bigint NOT NULL CHECK ("NextNumber" > 0),
    PRIMARY KEY ("TenantId","BranchId","DocumentCode","FinancialYearStart")
);

CREATE TABLE m09_delivery_challan (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "ChallanId" uuid NOT NULL,
    "TripId" uuid NOT NULL,
    "PourId" uuid NOT NULL,
    "SalesOrderId" uuid NOT NULL,
    "SalesOrderLineId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CustomerPartyId" uuid NOT NULL,
    "SiteId" uuid NULL,
    "ProductId" uuid NOT NULL,
    "Number" varchar(48) NOT NULL,
    "ChallanDate" date NOT NULL,
    "DeliveredAt" timestamptz NOT NULL,
    "Status" varchar(24) NOT NULL,
    "CustomerNameSnapshot" varchar(240) NOT NULL,
    "SiteAddressSnapshot" varchar(1000) NOT NULL,
    "OrderNumberSnapshot" varchar(48) NOT NULL,
    "CustomerPoNumberSnapshot" varchar(80) NULL,
    "ProductNameSnapshot" varchar(240) NOT NULL,
    "UnitCodeSnapshot" varchar(24) NOT NULL,
    "DeliveredQuantity" numeric(18,3) NOT NULL CHECK ("DeliveredQuantity" > 0),
    "ReturnedQuantity" numeric(18,3) NOT NULL CHECK ("ReturnedQuantity" >= 0),
    "UnitRateSnapshot" numeric(18,4) NOT NULL CHECK ("UnitRateSnapshot" >= 0),
    "TaxPercentSnapshot" numeric(7,4) NOT NULL,
    "DeliveredValueSnapshot" numeric(18,2) NOT NULL CHECK ("DeliveredValueSnapshot" >= 0),
    "VehicleNumberSnapshot" varchar(32) NOT NULL,
    "DriverNameSnapshot" varchar(160) NOT NULL,
    "PlantNameSnapshot" varchar(160) NOT NULL,
    "BatchSlipNumber" varchar(80) NOT NULL,
    "ReceiverName" varchar(160) NULL,
    "ReceiverMobile" varchar(24) NULL,
    "EvidenceState" varchar(24) NOT NULL,
    "SignatureReference" varchar(500) NULL,
    "PhotoReference" varchar(500) NULL,
    "EwayBillNumber" varchar(32) NULL,
    "InvoiceId" uuid NULL,
    "Remarks" varchar(2000) NOT NULL,
    "Version" bigint NOT NULL DEFAULT 1,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    PRIMARY KEY ("TenantId","ChallanId"),
    CONSTRAINT "UQ_m09_challan_trip" UNIQUE ("TenantId","TripId"),
    CONSTRAINT "UQ_m09_challan_number" UNIQUE ("TenantId","BranchId","ChallanDate","Number"),
    CONSTRAINT "FK_m09_challan_trip" FOREIGN KEY ("TenantId","TripId")
        REFERENCES m08_trip("TenantId","TripId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_pour" FOREIGN KEY ("TenantId","PourId")
        REFERENCES m08_pour("TenantId","PourId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_order" FOREIGN KEY ("TenantId","SalesOrderId")
        REFERENCES m06_sales_order("TenantId","SalesOrderId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_line" FOREIGN KEY ("TenantId","SalesOrderLineId")
        REFERENCES m06_sales_order_line("TenantId","LineId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_customer" FOREIGN KEY ("TenantId","CustomerPartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_product" FOREIGN KEY ("TenantId","ProductId")
        REFERENCES m03_product("TenantId","ProductId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m09_challan_site" FOREIGN KEY ("TenantId","SiteId")
        REFERENCES m03_site("TenantId","SiteId") ON DELETE RESTRICT
);

CREATE TABLE m09_challan_revision (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "RevisionId" uuid NOT NULL,
    "ChallanId" uuid NOT NULL,
    "RevisionNumber" integer NOT NULL CHECK ("RevisionNumber" > 0),
    "RequestedByUserId" uuid NOT NULL,
    "ApprovalRequestId" uuid NULL,
    "Reason" varchar(1000) NOT NULL,
    "OriginalQuantity" numeric(18,3) NOT NULL CHECK ("OriginalQuantity" > 0),
    "CorrectedQuantity" numeric(18,3) NOT NULL CHECK ("CorrectedQuantity" > 0),
    "Status" varchar(24) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "AppliedAt" timestamptz NULL,
    PRIMARY KEY ("TenantId","RevisionId"),
    CONSTRAINT "UQ_m09_challan_revision_number" UNIQUE ("TenantId","ChallanId","RevisionNumber"),
    CONSTRAINT "FK_m09_challan_revision_challan" FOREIGN KEY ("TenantId","ChallanId")
        REFERENCES m09_delivery_challan("TenantId","ChallanId") ON DELETE RESTRICT
);

CREATE TABLE m09_challan_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "ChallanId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId"),
    CONSTRAINT "FK_m09_challan_event_challan" FOREIGN KEY ("TenantId","ChallanId")
        REFERENCES m09_delivery_challan("TenantId","ChallanId") ON DELETE RESTRICT
);

COMMIT;
