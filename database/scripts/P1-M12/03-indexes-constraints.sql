BEGIN;
CREATE INDEX ix_m12_voucher_date_type ON m12_voucher ("TenantId","VoucherDate","VoucherType");
CREATE INDEX ix_m12_voucher_branch_date ON m12_voucher ("TenantId","BranchId","VoucherDate");
CREATE INDEX ix_m12_voucher_line_ledger_branch ON m12_voucher_line ("TenantId","LedgerCode","BranchId");
CREATE INDEX ix_m12_voucher_line_dimensions ON m12_voucher_line ("TenantId","SalesOrderId","TripId");
CREATE INDEX ix_m12_period_locked ON m12_financial_period ("TenantId","PeriodStart","PeriodEnd","IsLocked");
CREATE INDEX ix_m12_voucher_event_history ON m12_voucher_event ("TenantId","VoucherId","OccurredAt");
CREATE INDEX ix_m12_period_event_history ON m12_period_event ("TenantId","PeriodStart","OccurredAt");
COMMIT;
