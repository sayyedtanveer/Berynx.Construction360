-- P1-M12 core posting ledger; account definitions remain in tenant setup configuration.
BEGIN;
CREATE TABLE m12_voucher (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "VoucherId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "Number" varchar(32) NOT NULL,
    "VoucherType" varchar(24) NOT NULL,
    "Status" varchar(24) NOT NULL,
    "VoucherDate" date NOT NULL,
    "SourceModule" varchar(32) NOT NULL,
    "SourceEntityId" uuid NULL,
    "IdempotencyKey" varchar(100) NOT NULL,
    "Narration" varchar(1000) NOT NULL,
    "TotalDebit" numeric(18,2) NOT NULL CHECK ("TotalDebit" > 0),
    "TotalCredit" numeric(18,2) NOT NULL CHECK ("TotalDebit" = "TotalCredit"),
    "ReversalOfVoucherId" uuid NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "PostedAt" timestamptz NULL,
    PRIMARY KEY ("TenantId","VoucherId"),
    CONSTRAINT "UQ_m12_voucher_idempotency" UNIQUE ("TenantId","IdempotencyKey"),
    CONSTRAINT "UQ_m12_voucher_number" UNIQUE ("TenantId","BranchId","Number")
);
CREATE TABLE m12_voucher_line (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "VoucherLineId" uuid NOT NULL,
    "VoucherId" uuid NOT NULL,
    "LedgerCode" varchar(64) NOT NULL,
    "PartyId" uuid NULL,
    "BranchId" uuid NULL,
    "SiteId" uuid NULL,
    "SalesOrderId" uuid NULL,
    "TripId" uuid NULL,
    "Narration" varchar(500) NOT NULL,
    "Debit" numeric(18,2) NOT NULL DEFAULT 0,
    "Credit" numeric(18,2) NOT NULL DEFAULT 0,
    PRIMARY KEY ("TenantId","VoucherLineId"),
    CONSTRAINT "CK_m12_voucher_line_side" CHECK
        (("Debit" > 0 AND "Credit" = 0) OR ("Credit" > 0 AND "Debit" = 0)),
    CONSTRAINT "FK_m12_voucher_line_voucher" FOREIGN KEY ("TenantId","VoucherId")
        REFERENCES m12_voucher("TenantId","VoucherId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m12_voucher_line_party" FOREIGN KEY ("TenantId","PartyId")
        REFERENCES m03_party("TenantId","PartyId") ON DELETE RESTRICT
);
CREATE TABLE m12_financial_period (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "PeriodStart" date NOT NULL,
    "PeriodEnd" date NOT NULL,
    "IsLocked" boolean NOT NULL,
    "LockReason" varchar(1000) NULL,
    "LockedByUserId" uuid NULL,
    "LockedAt" timestamptz NULL,
    PRIMARY KEY ("TenantId","PeriodStart"),
    CONSTRAINT "CK_m12_financial_period_dates" CHECK ("PeriodEnd" >= "PeriodStart")
);
CREATE TABLE m12_voucher_number_counter (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "BranchId" uuid NOT NULL,
    "VoucherType" varchar(24) NOT NULL,
    "CalendarYear" integer NOT NULL CHECK ("CalendarYear" BETWEEN 2000 AND 2200),
    "NextNumber" bigint NOT NULL CHECK ("NextNumber" > 0),
    PRIMARY KEY ("TenantId","BranchId","VoucherType","CalendarYear")
);
CREATE TABLE m12_voucher_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "VoucherId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId"),
    CONSTRAINT "FK_m12_voucher_event_voucher" FOREIGN KEY ("TenantId","VoucherId")
        REFERENCES m12_voucher("TenantId","VoucherId") ON DELETE RESTRICT
);
CREATE TABLE m12_period_event (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL,
    "PeriodStart" date NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" varchar(40) NOT NULL,
    "Reason" varchar(1000) NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","EventId")
);
COMMIT;
