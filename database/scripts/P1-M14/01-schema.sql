BEGIN;
CREATE TABLE m14_expense_category (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "CategoryId" uuid NOT NULL,
    "Name" varchar(120) NOT NULL,
    "Type" varchar(24) NOT NULL,
    "ApprovalThreshold" numeric(18,2) NULL,
    "ExpenseLedgerCode" varchar(64) NOT NULL,
    "IsActive" boolean NOT NULL,
    PRIMARY KEY ("TenantId","CategoryId"),
    CONSTRAINT "CK_m14_expense_category_type" CHECK
        ("Type" IN ('Direct','Indirect','Capital','Other income')),
    CONSTRAINT "UQ_m14_expense_category_name" UNIQUE ("TenantId","Name")
);
CREATE TABLE m14_expense (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "ExpenseId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CategoryId" uuid NOT NULL,
    "VendorPartyId" uuid NULL,
    "SiteId" uuid NULL,
    "SalesOrderId" uuid NULL,
    "TripId" uuid NULL,
    "ExpenseDate" date NOT NULL,
    "Status" varchar(32) NOT NULL,
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "Description" varchar(1000) NOT NULL,
    "PaidFromLedgerCode" varchar(64) NOT NULL,
    "EmployeePaid" boolean NOT NULL,
    "SupplierGstin" varchar(15) NOT NULL,
    "SupplierBillNumber" varchar(64) NOT NULL,
    "GstAmount" numeric(18,2) NOT NULL CHECK ("GstAmount" >= 0 AND "GstAmount" <= "Amount"),
    "ItcEligible" boolean NOT NULL,
    "ReverseCharge" boolean NOT NULL,
    "AttachmentReference" varchar(500) NOT NULL,
    "ApprovalComment" varchar(1000) NOT NULL,
    "ApprovalRequestId" uuid NULL,
    "VoucherId" uuid NULL,
    "IdempotencyKey" varchar(100) NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "PostedAt" timestamptz NULL,
    PRIMARY KEY ("TenantId","ExpenseId"),
    CONSTRAINT "FK_m14_expense_category" FOREIGN KEY ("TenantId","CategoryId")
        REFERENCES m14_expense_category("TenantId","CategoryId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m14_expense_vendor" FOREIGN KEY ("TenantId","VendorPartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m14_expense_voucher" FOREIGN KEY ("TenantId","VoucherId")
        REFERENCES m12_voucher("TenantId","VoucherId") ON DELETE RESTRICT,
    CONSTRAINT "UQ_m14_expense_idempotency" UNIQUE ("TenantId","IdempotencyKey")
);
CREATE TABLE m14_expense_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "ExpenseId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "Detail" varchar(1000) NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId"),
    CONSTRAINT "FK_m14_expense_event_expense" FOREIGN KEY ("TenantId","ExpenseId")
        REFERENCES m14_expense("TenantId","ExpenseId") ON DELETE RESTRICT
);
COMMIT;
