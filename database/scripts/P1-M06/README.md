# P1-M06 manual database deployment

## Preconditions

- Apply the existing M01–M05 and SaaS foundation database packages first.
- Confirm the current EF migration history; do not run this package against a development or production database from the application.
- This package is authored for PostgreSQL. The corresponding EF Core migration and model snapshot are pending generation/review.
- No database connection, SQL execution, or `dotnet ef database update` was performed by the implementation agent.

## Manual execution order

1. `01-schema.sql`
2. `02-alter-existing.sql` (no earlier-module columns are changed; M04's order reference already exists)
3. `03-indexes-constraints.sql` (constraints/indexes and EF migration history)
4. `04-seed.sql` (M06 optional module catalog and permission catalog; no plans or tenant entitlements are changed)
5. `05-verification.sql`

The owner should execute each file in a PostgreSQL-aware SQL client, review transaction results, and stop if any step errors. `99-rollback.sql` is a separate guarded recovery script; it is not part of normal deployment. Do not stamp `__EFMigrationsHistory` with a guessed ID.

## Optional tenant activation

M06 is registered as a non-core module. Existing tenants receive no M06 entitlement from these scripts. A PlatformAdmin may activate it for a test tenant using the existing `PUT /api/v1/platform/tenants/{tenantId}/subscription` endpoint:

1. Read the tenant's current subscription/version and the chosen active plan.
2. Re-submit that plan's `planCode`, current subscription `version`, effective `startsAt`, future `endsAt`, and current `status` (`Trial` or `Active`) using the existing contract.
3. Set `addOnModules` to include `M06` and include a reason identifying the test tenant and authorization.
4. Verify `/api/v1/auth/navigation` reports the M06 route and that a user has the required M06 permissions. Do not grant M06 to existing tenants as a deployment side effect.

This replaces the tenant's effective module entitlements according to the plan plus add-ons; PlatformAdmin must preserve any existing add-ons in the request.

## Rollback

`99-rollback.sql` refuses to proceed if orders, tenant entitlement history, or role assignments exist. A refusal is intentional and requires a separately reviewed retirement plan. Never manually cascade-delete posted business records.
