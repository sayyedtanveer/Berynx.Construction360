BEGIN;
CREATE INDEX ix_m10_invoice_status_date ON m10_invoice ("TenantId","BranchId","Status","InvoiceDate");
CREATE INDEX ix_m10_invoice_customer_due ON m10_invoice ("TenantId","CustomerPartyId","DueDate");
CREATE UNIQUE INDEX ux_m10_invoice_idempotency ON m10_invoice ("TenantId","IdempotencyKey");
CREATE INDEX ix_m10_invoice_line_parent ON m10_invoice_line ("TenantId","InvoiceId");
CREATE UNIQUE INDEX ux_m10_invoice_challan ON m10_invoice_challan ("TenantId","ChallanId");
CREATE INDEX ix_m10_invoice_event_history ON m10_invoice_event ("TenantId","InvoiceId","OccurredAt");
COMMIT;
