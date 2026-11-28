BEGIN;
CREATE INDEX ix_m13_gst_period_status ON m13_gst_period ("TenantId","Status","PeriodStart");
CREATE INDEX ix_m13_exception_period_invoice ON m13_exception_review ("TenantId","PeriodStart","InvoiceId");
COMMIT;
