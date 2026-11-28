# P1-M10 Sales Invoicing — SQL status

**The original `01`–`06` scripts remain unreconciled drafts and are not deployment-ready.** An additive EF Core migration has now been generated for posted-invoice cancellation:
`20261127181300_P1M10PostedInvoiceReversal`.

The migration and matching manual scripts are:

- `07-posted-invoice-reversal.sql` — adds the released-reservation timestamp, active-only challan reservation index and tenant-scoped voucher reversal relationship.
- `08-posted-invoice-reversal-rollback.sql` — guarded rollback; refuses if reservations have been released or reversal vouchers exist.

The M10 source now supports transactional cancellation of a posted invoice only when it has no receipt allocations and no IRN. It posts an exact reversal through the shared M12 engine, releases linked challans for re-invoicing, restores M06 invoiced quantities and records audit events. IRN-bearing invoices remain blocked until portal cancellation is implemented. This change has not been exercised against PostgreSQL.

Dependencies remain M01–M03, M06, M08, M09 and M12 schemas, branch ledger mappings and verified challans. M10 billing modes, charges/SAC tax, TCS policy, full e-invoice/e-way bill handling, pro-forma, and other BRD workflows remain incomplete.

Review sequence: EF migration/model diff → compare this migration with `07` → apply the full dependency-ordered deployment package only after owner review. Do not apply the original `01`–`06` drafts as a substitute for migration parity. No database connection, SQL execution or migration application occurred.
