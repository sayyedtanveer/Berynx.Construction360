# Phase 1 manual deployment order

**Deployment status: PARTIAL / NOT READY.** This file records the migration/scripts currently present; it is not deployment approval. No database connection, SQL execution, or EF database update was performed during this remediation.

## Migration inventory

The latest migration that existed before this repair was `20261125092036_P1M04M05Foundation`. Current model additions for the implemented M06–M14, M16 and M17 entity sets are captured by the new additive migration:

- `20261126104710_P1M06M17OperationalFoundation`

The migration adds 52 tables and 124 indexes. Its `Up` has only new table/index creation operations; the generated rollback contains drops for only those new tables. M15 is currently query/report-only in EF. M08 receiver-review migration `20261127081534_P1M08ReceiverEvidenceReview` and M18 secure-evidence migration `20261127105316_P1M18SecureEvidenceStorage` are separate additive migrations; M19 remains outstanding. Do not edit earlier migrations; if the owner has already applied a migration, any later model change must be a new migration.

## Owner-run sequence (manual only)

1. **Confirm prerequisite state.** Back up the target database and verify it is the intended database. Confirm the M01–M05 scripts and migration history are at least through `20261125092036_P1M04M05Foundation`. Resolve any existing M06–M17 tables or migration-history drift before proceeding.
2. **Review the combined schema package.** Review `database/scripts/P1-M06-M17-FOUNDATION/README.md`, `01-apply.sql`, `05-verification.sql`, and `99-rollback.sql`. The package is generated from EF Core and is the current schema source for this model delta.
3. **Do not layer duplicate schema scripts.** Existing `database/scripts/P1-M06/` through `P1-M16/` contain manually authored schema fragments that overlap this new combined script. Until reconciled against the migration, do not execute their `01-schema.sql`, `02-alter-existing.sql`, or `03-indexes-constraints.sql` together with the combined `01-apply.sql`. Those existing files are preserved, not certified as equivalent, and not removed.
4. **Apply the migration script once.** After the owner resolves the above prerequisites, manually execute `database/scripts/P1-M06-M17-FOUNDATION/01-apply.sql` using the approved SQL runner. Do not use `dotnet ef database update` or application startup migration behavior. The script is EF-generated and idempotent only with respect to its migration-history row; tables created outside EF without that row can still conflict.
5. **Apply optional module catalogue/permission seeds** after schema creation, in ascending module order for each enabled module: `database/scripts/P1-M06/04-seed.sql` through `P1-M16/04-seed.sql`, then `database/scripts/P1-M17/04-seed.sql`. Review each package README and prerequisite first. These seeds register optional modules and permission catalogue entries; they do not grant M06–M17 subscription entitlements to existing tenants. Use PlatformAdmin entitlement workflows separately when explicitly authorized.
6. **Verify.** Run `database/scripts/P1-M06-M17-FOUNDATION/05-verification.sql` and `database/scripts/P1-M17/05-verification.sql`. Confirm migration history, expected table/index inventory, module/permission catalogue entries, and tenant entitlement behavior. Save results outside source control in the owner's deployment record.
7. **Rollback only by explicit owner decision.** `database/scripts/P1-M06-M17-FOUNDATION/99-rollback.sql` is guarded to refuse if the migration history row is absent or any of the 52 new tables contains rows. It was not executed. Review live dependencies and take a backup before any authorized rollback.

## Package and readiness gaps

- Individual schema packages for M06–M16 have not yet been reconciled to the combined EF script. Do not run overlapping schema artifacts.
- M17 now has an optional-module/permission seed and its EF tables are in the combined migration, but its BRD campaign, approved-template, wallet, public request-quote, PDF/QR and provider integrations are incomplete.
- M18 source, Angular upload/review wiring, EF mappings/migration, manual SQL and runbook are now present. Its database application, live ClamAV configuration, and PostgreSQL acceptance remain pending. M19 source/migration/schema remain outstanding. See the M18 package sequence below.
- PostgreSQL persistence, tenant isolation under database policies, full golden-path workflows, and all DB-dependent acceptance tests remain **UNVERIFIED / PENDING**. The 24 database-backed tests were skipped because `BERYNX_TEST_POSTGRES` was removed for the safe verification run.

Do not describe Phase 1 as database-deployed, UAT-ready, or production-ready based on these generated artifacts or local build/test results.
## M18 schema package (manual only)

After owner reconciliation of the combined M06-M17 foundation and confirmation that M02/M08 tables plus M08 receiver-review migration `20261127081534_P1M08ReceiverEvidenceReview` are present:

1. Review `database/scripts/P1-M18/README.md` and `docs/modules/P1-M18.md`.
2. Manually execute `database/scripts/P1-M18/01-m18-secure-evidence-deploy.sql` once for migration `20261127105316_P1M18SecureEvidenceStorage`.
3. Run read-only checks in `database/scripts/P1-M18/03-m18-secure-evidence-verification.sql`.
4. The guarded rollback `database/scripts/P1-M18/02-m18-secure-evidence-rollback.sql` refuses if any document/version/audit data exists. Do not alter the guard or use rollback to erase evidence.

M18 file storage and scanner configuration are operational prerequisites separate from the schema. No migration or SQL in this package has been applied; no development PostgreSQL connection was made.
Do not describe Phase 1 as database-deployed, UAT-ready, or production-ready based on these generated artifacts or local build/test results.
