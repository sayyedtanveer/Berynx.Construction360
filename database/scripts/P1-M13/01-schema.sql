-- M13 GST filing/review metadata only. Transaction totals remain sourced from M10/M07/M12.
BEGIN;
CREATE TABLE m13_gst_period (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "PeriodStart" date NOT NULL,
    "PeriodEnd" date NOT NULL,
    "Status" varchar(24) NOT NULL,
    "FiledAt" timestamptz NULL,
    "FiledByUserId" uuid NULL,
    "ExternalAcknowledgement" varchar(160) NOT NULL,
    "FilingSnapshotJson" jsonb NOT NULL,
    PRIMARY KEY ("TenantId","PeriodStart"),
    CONSTRAINT "CK_m13_gst_period_dates" CHECK ("PeriodEnd" >= "PeriodStart")
);
CREATE TABLE m13_exception_review (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "ReviewId" uuid NOT NULL,
    "PeriodStart" date NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "ExceptionCode" varchar(48) NOT NULL,
    "ReviewNote" varchar(1000) NOT NULL,
    "ReviewedByUserId" uuid NOT NULL,
    "ReviewedAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","ReviewId"),
    CONSTRAINT "UQ_m13_exception_review" UNIQUE ("TenantId","PeriodStart","InvoiceId","ExceptionCode"),
    CONSTRAINT "FK_m13_exception_invoice" FOREIGN KEY ("TenantId","InvoiceId")
        REFERENCES m10_invoice("TenantId","InvoiceId") ON DELETE RESTRICT
);
COMMIT;
