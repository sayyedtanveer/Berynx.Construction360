# P1-M14 Expenses — capture and shared-ledger foundation

No EF migration/model snapshot has been generated. This package is a draft and must not be deployed until migration parity review.

The supported subset captures tenant categories and expense records, sends threshold-triggered approvals through M02, and posts supported approved entries through the shared M12 engine. Supplier bill duplicate check is tenant/supplier/invoice/amount. No default production categories are inserted.

Not implemented: attachments, independent approval inbox integration, petty cash, reimbursements, recurring expenses, other income/capital introduction, ITC, RCM, TDS, supplier freight allocation, category/site analytics, and source-traceable M12 reversals. GST-bearing/RCM cases fail closed as documented in the API/UI.

After migration review, manual order is migration → `01-schema.sql` → `02-alter-existing.sql` → `03-indexes-constraints.sql` → `04-seed.sql` → `05-verification.sql`. Rollback refuses posted expenses. No DB operation occurred.
