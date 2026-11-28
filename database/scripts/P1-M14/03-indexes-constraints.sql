BEGIN;
CREATE INDEX ix_m14_expense_status_date ON m14_expense ("TenantId","Status","ExpenseDate");
CREATE INDEX ix_m14_expense_branch_date ON m14_expense ("TenantId","BranchId","ExpenseDate");
CREATE INDEX ix_m14_expense_dimensions ON m14_expense ("TenantId","SalesOrderId","TripId");
CREATE INDEX ix_m14_expense_event_history ON m14_expense_event ("TenantId","ExpenseId","OccurredAt");
CREATE UNIQUE INDEX uq_m14_expense_supplier_bill ON m14_expense
    ("TenantId","VendorPartyId","SupplierBillNumber","Amount")
    WHERE "SupplierBillNumber" <> '';
COMMIT;
