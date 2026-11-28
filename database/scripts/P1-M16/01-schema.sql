BEGIN;
CREATE TABLE m16_export_log (
    "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
    "ExportId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ReportCode" varchar(32) NOT NULL,
    "FiltersJson" jsonb NOT NULL,
    "RowCount" integer NOT NULL CHECK ("RowCount" >= 0),
    "ExportedAt" timestamptz NOT NULL,
    PRIMARY KEY ("TenantId","ExportId")
);
COMMIT;
