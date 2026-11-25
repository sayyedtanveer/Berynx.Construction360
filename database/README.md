# PostgreSQL SQL artifacts and manual deployment

EF Core migrations under `backend/src/Berynx.Infrastructure/Persistence/Migrations/` are the model/schema source of truth. The agent generates and reviews deployment and rollback SQL but **does not connect to, inspect, back up, or modify any database and does not execute SQL**. The owner manually reviews and runs the scripts. Application startup must never call `Database.Migrate`, `EnsureCreated` or execute DDL; do not use `dotnet ef database update`.

## Required per-module SQL deliverables

For a module that changes the schema, generate SQL matching its EF migration definitions for:

1. New tables and tenant-aware keys.
2. Alterations to existing tables and columns.
3. Indexes, foreign keys, checks, uniqueness and triggers/constraints.
4. Required reference/configuration seeds, with no real credentials or tenant-specific secrets.
5. Verification queries for expected objects and migration history.
6. A separate guarded rollback script that stops if it would destroy module or referenced business data.

Keep scripts ordered, deterministic, reviewed against generated EF SQL, and document migration IDs plus exact execution order in the module guide and the module's `database/scripts/<module>/README.md`. Do not silently baseline, edit or delete migration history. Mark actual application/verification as pending until the owner reports execution evidence.

## P1-M01 and P1-M02 SQL bundles

For a fresh database, use the ordered bundles and their module READMEs:

1. `scripts/P1-M01/` — migrations `20261124000100_Onboarding` and `20261124000200_OnboardingDelivery`.
2. `scripts/P1-M02/` — migrations `20261124000300_UsersRolesApprovals`, `20261124092204_P1M02IdentityAuthentication`, and `20261124094727_P1M02SessionSecurityStamp`; includes the permission catalog and system-role reference seeds.
3. `scripts/P1-M03/` — follow its README after M01 and M02 schema and seeds are verified.

The M01/M02 bundles contain direct SQL copied from their reviewed EF-generated migration scripts; the original flat scripts and M02 seed remain available as source/reference files. The bundle files do not depend on `psql` include commands and can be run individually in a PostgreSQL SQL client. Do not delete or treat the legacy source scripts as obsolete.

### Reported M01 baseline with M02 missing

The owner previously reported that the M01 schema is present while M02 objects such as `m02_permission_catalog` are missing. This is a reported state, not an inspected database state. Before applying anything, the owner must verify the target database, actual objects, and `__EFMigrationsHistory`:

- If both `20261124000100_Onboarding` and `20261124000200_OnboardingDelivery` are recorded and verified, do **not** rerun M01; run the M02 bundle in the order above, then M03.
- If either M01 migration is absent, stop and reconcile schema/history with the M01 README before continuing. Do not apply M02/M03 on a partial M01 baseline.
- Once M01 is confirmed, run M02's direct DDL, indexes/constraints and permission/system-role seed before any M03 seed. M03 seeds call the M02 seed function and require its catalog/role tables.
- Then run the M03 bundle in its README order, followed by verification. Do not rerun M03 schema files blindly on a partially applied database; compare each script's idempotency notes and current history first.

For any existing database, the owner must verify backup, actual schema, and migration history before execution. Do not infer live state from these source files. M01/M02/M03 live synchronization and Git finalization are owner-managed.

## P1-M03 script sequence

The P1-M03 SQL deliverables are separate files under `scripts/P1-M03/`:

1. `01-schema.sql`
2. `02-alter-existing.sql`
3. `03-indexes-constraints.sql`
4. `04-seed.sql`
5. `05-verification.sql`
6. `99-rollback.sql` — run only as a separately reviewed owner action; it must be guarded and is not a routine deployment step.

The M03 README must state M01/M02 schema/seed dependencies and their required migration history, explain execution on a fresh versus existing database, and describe the verification output the owner should retain. **No P1-M03 SQL is run by the agent.**

## Local development authentication fixture

After all M01–M03 migrations and module seeds are manually verified, the optional local-only package is `scripts/DEV-AUTH/`. Execute `01-prerequisites.sql`, `02-roles-permissions.sql`, `03-development-tenant.sql`, and `04-verification.sql` in order. `03-development-tenant.sql` inserts a labelled tenant and branch only when the target contains no tenants; it does not alter existing M01 company/branch data. The package intentionally has no cleanup script because deleting tenant-scoped Identity users, audit events, or sessions is not a safe generic rollback.

Then the owner can explicitly run `backend/tools/Berynx.DevAuthProvisioner/` as documented in `scripts/DEV-AUTH/README.md`. The console app uses ASP.NET Core Identity `UserManager` to hash newly supplied passwords; SQL does not create Identity accounts/password hashes. The tool accepts only Development mode and localhost `berynx_construction_dev`, prompts without echo when credentials are absent, is repeatable, and ordinary provisioning never resets an existing password. A separate explicit `--reset-password <fixture-email>` mode is available only for the documented local fixture accounts; it revokes sessions and audits the reset. The tool does not run migrations.

## Tests and database verification

- Run builds, unit tests, frontend tests and static checks that do not require a live database.
- PostgreSQL integration tests use only a separate disposable localhost database whose name ends in `_test`; tests may create/drop an isolated schema there.
- If no test database is configured, label database-dependent integration coverage **PENDING**. Do not count skipped database tests as passed, and do not attempt to discover credentials by connecting.
- The owner manually inspects and verifies live migration history, schema objects, triggers, seeds and EF synchronization after script execution.

## Safety boundaries

- The owner's development database, if manually used, is `127.0.0.1:5432`, database `berynx_construction_dev`, user `postgres`. The agent must not connect or execute SQL against it.
- Never put credentials in source, committed configuration, scripts, documentation, command output or chat.
- Never include real user identities, passwords, bearer tokens or provider secrets in seed SQL.
- Rollback scripts must be guarded against referenced or posted data; destructive changes require separate owner approval.
- Preserve unrelated changes. The agent does not commit, merge or push; the owner performs all Git finalization.
