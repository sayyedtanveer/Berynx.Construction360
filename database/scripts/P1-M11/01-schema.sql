-- P1-M11 draft receipt, allocation and promise-to-pay schema; no ledger posting.
-- Requires M01–M03 and P1-M10 schema.
BEGIN;
CREATE TABLE m11_receipt (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "ReceiptId" uuid NOT NULL,
    "CustomerPartyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "Number" varchar(16) NOT NULL DEFAULT '',
    "ReceiptDate" date NOT NULL,
    "Status" varchar(24) NOT NULL,
    "PaymentMethod" varchar(24) NOT NULL,
    "ReferenceNumber" varchar(100) NOT NULL,
    "DepositAccountId" uuid NULL,
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "TdsAmount" numeric(18,2) NOT NULL CHECK ("TdsAmount" >= 0),
    "BankCharges" numeric(18,2) NOT NULL CHECK ("BankCharges" >= 0),
    "AllocatedAmount" numeric(18,2) NOT NULL CHECK ("AllocatedAmount" >= 0),
    "AdvanceAmount" numeric(18,2) NOT NULL CHECK ("AdvanceAmount" >= 0),
    "Notes" varchar(2000) NOT NULL,
    "IdempotencyKey" varchar(100) NOT NULL,
    "Version" bigint NOT NULL DEFAULT 1,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","ReceiptId"),
    CONSTRAINT "UQ_m11_receipt_idempotency" UNIQUE ("TenantId","IdempotencyKey"),
    CONSTRAINT "FK_m11_receipt_customer" FOREIGN KEY ("TenantId","CustomerPartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT
);
CREATE TABLE m11_receipt_allocation (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "AllocationId" uuid NOT NULL,
    "ReceiptId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    PRIMARY KEY ("TenantId","AllocationId"),
    CONSTRAINT "FK_m11_allocation_receipt" FOREIGN KEY ("TenantId","ReceiptId")
        REFERENCES m11_receipt("TenantId","ReceiptId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m11_allocation_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT
);
CREATE TABLE m11_promise_to_pay (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "PromiseId" uuid NOT NULL,
    "CustomerPartyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "InvoiceId" uuid NULL,
    "PromiseDate" date NOT NULL,
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "Outcome" varchar(24) NOT NULL,
    "Notes" varchar(1000) NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","PromiseId"),
    CONSTRAINT "FK_m11_promise_customer" FOREIGN KEY ("TenantId","CustomerPartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m11_promise_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT
);
CREATE TABLE m11_receivable_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "ReceiptId" uuid NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId"),
    CONSTRAINT "FK_m11_receivable_event_receipt" FOREIGN KEY ("TenantId","ReceiptId")
        REFERENCES m11_receipt("TenantId","ReceiptId") ON DELETE RESTRICT
);
COMMIT;
