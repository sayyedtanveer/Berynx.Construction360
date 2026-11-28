# P1-M08 RMC & Material Delivery database delivery status

The assigned receiver-evidence review schema is packaged additively; it is not applied. Apply only after the owner has reconciled and manually applied prerequisites.

## Scripts and execution order

1. Review/apply prerequisite M06/M17 foundation `20261126104710_P1M06M17OperationalFoundation` and preceding M12 posting/mapping migration `20261126151858_P1M12TenantLedgerMappingsAndPostingLinks` using their owner-approved package scripts.
2. Review and manually execute `04-seed.sql` to register optional M08 permissions plus Dispatcher/Supervisor system-role templates. It does not enable the M08 module for a plan or tenant and does not assign roles to users; a tenant admin must explicitly assign roles and branch/plant scopes.
3. Manually execute generated migration SQL `05-receiver-evidence-review-deploy.sql` for migration `20261127081534_P1M08ReceiverEvidenceReview`.
4. Run read-only checks in `07-receiver-evidence-review-verification.sql`.

`06-receiver-evidence-review-rollback.sql` is a separate guarded manual rollback. It refuses to run after any M08 review/resubmission audit event or while reviewer/reason data exists; do not use it to remove audit evidence. Do not execute rollback without an approved backup and owner approval.

No SQL or migration has been executed, no PostgreSQL connection was made, and no live role, migration-history or FK result is claimed. Database-backed integration acceptance is pending an explicitly authorized disposable localhost database ending in `_test`.