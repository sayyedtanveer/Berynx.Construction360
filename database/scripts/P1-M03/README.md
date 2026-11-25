# P1-M03 SQL deployment

These scripts are generated/reviewed for EF migrations:

- `20261124114145_P1M03Masters`
- `20261124115631_P1M03MasterConcurrency`
- `20261124121636_P1M03VersionedMasters`
- `20261124122738_P1M03PartyOwnership`

They are **not executed by the agent**. Do not run them against production or an unknown database. The owner is responsible for confirming target identity, reviewing the database backup, and manually executing each script.

## M01/M02 prerequisites

Before M03, confirm the target already contains the M01/M02 schema and these migration-history entries:

1. `20261124000100_Onboarding`
2. `20261124000200_OnboardingDelivery`
3. `20261124000300_UsersRolesApprovals`
4. `20261124092204_P1M02IdentityAuthentication`
5. `20261124094727_P1M02SessionSecurityStamp`

The M03 deployment assumes `onboarding_tenant`, `__EFMigrationsHistory`, `m02_permission_catalog`, `m02_system_role_permission`, and the M02 `m02_seed_tenant(uuid)` function exist. M03 does not create replacement tenant, user, role, audit, approval, GST-registration, bank/cash, or onboarding tables. If any prerequisite is missing, stop and reconcile M01/M02 first; do not edit migration history by hand.

## Manual execution order

For an existing authorized development database, first verify its actual identity/schema/migration history and take a verified backup. For a fresh database, run the M01/M02 scripts and required seeds in the order in `database/README.md` before proceeding.

Run these files once, in order:

1. [`01-schema.sql`](01-schema.sql) — creates the 22 M03 tables, tenant-aware primary/foreign keys, and table check constraints.
2. [`02-alter-existing.sql`](02-alter-existing.sql) — adds concurrency `Version` columns to `m03_price_list_line`, `m03_import_batch`, `m03_contact`, `m03_unit`, and `m03_tax_rate`, plus the M03 party salesperson and tag fields. It also broadens the M02 permission-catalog `Action` check to allow M03's `deactivate`, `import`, and `override` permission actions.
3. [`03-indexes-constraints.sql`](03-indexes-constraints.sql) — creates the EF indexes and salesperson membership foreign key, then records all four M03 migration IDs in `__EFMigrationsHistory`. Other table-level constraints are already in file 1.
4. [`04-seed.sql`](04-seed.sql) — idempotently adds M03 permissions and standard-role grants, then tenant-scoped units/conversions, an Immediate payment term, and RMC surcharge charge types. It does not seed GST rates, prices, tenant-specific bank data, or credentials.
5. [`05-verification.sql`](05-verification.sql) — read-only checks for migration history, tables, constraints, indexes, reference seeds, and M02 permissions. Retain the output with the deployment record.

Do not launch application code between schema and index/history steps. Do not run `dotnet ef database update` or assume a script succeeded from its exit status alone. The EF model/migration static check should be run from the backend after code updates; the owner verifies live schema synchronization after applying SQL.

[`99-rollback.sql`](99-rollback.sql) is **not** part of deployment. It requires the owner to take a verified backup, explicitly set `berynx.allow_p1_m03_rollback=on`, and have no rows in any `m03_%` table (including reference seeds). It only permits the authorized development database or a database ending in `_test`. It retains additive M02 permission rows and the broadened M02 permission-action check for separate review. Rollback of any populated M03 installation requires an individually reviewed data-migration plan instead.

If an earlier M03 seed attempt failed on `m02_permission_catalog_Action_check`,
run `02-alter-existing.sql` again before retrying `04-seed.sql`. Its column
additions are idempotent, and it replaces the old action check with the M03
compatible set.

## Current status

The agent generated/reviewed these files and did not connect to PostgreSQL or execute SQL. Live schema state, migration application, seed results, and PostgreSQL integration tests remain **PENDING owner verification**. Integration tests require `BERYNX_TEST_POSTGRES` configured locally to a disposable localhost database ending in `_test`; credentials must not be sent in chat or committed.
