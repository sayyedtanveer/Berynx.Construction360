# P1-M12 Accounting — core posting-engine database package

**Not deployable:** no EF migration/model snapshot has been generated. SQL drafts require migration parity review.

This increment adds a source-independent double-entry voucher model, decimal balancing, idempotency, tenant setup-ledger membership, manual Journal/Contra controls, party dimensions for control-account groups, branch/tenant isolation, and locked-period checks. The chart of accounts remains sourced from tenant Setup configuration; editing and system-ledger protection remain pending.

Not implemented: source posting from M10/M11/M07/M14, reversals, unlock workflow, cash/bank/day books, P&L/balance sheet, opening/carry-forward, year-end close, Tally export, bank reconciliation and full account dimensions. Do not mark BR-M12/US-M12 complete.

After migration generation/review, manual order is migration parity review → `01-schema.sql` → `02-alter-existing.sql` → `03-indexes-constraints.sql` → `04-seed.sql` → `05-verification.sql`. Rollback refuses posted vouchers or locked periods. No DB or SQL operations have been performed.
