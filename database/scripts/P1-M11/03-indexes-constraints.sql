BEGIN;
CREATE INDEX ix_m11_receipt_customer_date_status
    ON m11_receipt ("TenantId","CustomerPartyId","ReceiptDate","Status");
CREATE INDEX ix_m11_allocation_receipt ON m11_receipt_allocation ("TenantId","ReceiptId");
CREATE INDEX ix_m11_allocation_invoice ON m11_receipt_allocation ("TenantId","InvoiceId");
CREATE INDEX ix_m11_promise_branch_date_outcome
    ON m11_promise_to_pay ("TenantId","BranchId","PromiseDate","Outcome");
CREATE INDEX ix_m11_receivable_event_history
    ON m11_receivable_event ("TenantId","ReceiptId","OccurredAt");
COMMIT;
