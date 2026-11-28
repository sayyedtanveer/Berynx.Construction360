BEGIN;
CREATE INDEX ix_m16_export_audit_user_date ON m16_export_log ("TenantId","UserId","ExportedAt");
CREATE INDEX ix_m16_export_audit_code_date ON m16_export_log ("TenantId","ReportCode","ExportedAt");
COMMIT;
