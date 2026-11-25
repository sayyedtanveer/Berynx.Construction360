# SaaS licensing foundation SQL

These are reviewed, owner-executed PostgreSQL scripts for the SaaS licensing and
tenant activation foundation. The application does not run migrations or DDL at
startup. Do not execute these scripts from an agent or connect them to the
development database.

## Prerequisites

1. Apply and verify the existing P1-M01 Onboarding schema.
2. Apply and verify the existing P1-M02 Identity, tenant membership, role and
   permission schema.
3. Apply and verify the existing P1-M03 Masters schema. The SaaS catalogue
   exposes M03 as an optional add-on only because those routes are already
   implemented.
4. Ensure the existing EF migration history is current through
   `20261124122738_P1M03PartyOwnership`.

The tenant, M02 Identity and role tables are reused. No payment gateway, tenant
credential table, or duplicate RBAC model is introduced.

## Manual execution order

Run each script once, in this exact order, against the intended deployment
database:

1. `01-schema.sql` — create the SaaS plan, module, subscription, entitlement,
   usage and audit tables.
2. `02-alter-existing.sql` — add the current-subscription pointer to
   `onboarding_tenant` and the API request quota to `platform_plan`.
3. `03-indexes-constraints.sql` — install foreign keys, lifecycle/quota
   constraints, indexes, immutable audit trigger, and the two EF migration
   history rows.
4. `04-seed.sql` — seed M01/M02 core module records and the M03 add-on record.
5. `05-verification.sql` — run read-only checks and review every result.

The scripts are idempotent for the tables, columns, indexes, module seed and
constraints where applicable. Stop if any DDL statement fails; do not continue
with later scripts or hand-edit the schema. `99-rollback.sql` is a separate,
destructive rollback and is refused once plan, subscription, entitlement, API
usage or audit data exists.

If an earlier revision of script 03 reported that
`onboarding_tenant_status_check` did not exist, run the corrected
`03-indexes-constraints.sql` again, then rerun `05-verification.sql`. Script 03
now normalizes either case variant of the legacy status constraint before
installing the single lifecycle constraint.

After schema deployment, create at least one active plan through the Platform
Admin Plans screen before provisioning a tenant. No sample price or quota is
seeded as business policy.

## EF Core migrations

The reviewed EF migration source is:

- `backend/src/Berynx.Infrastructure/Persistence/Migrations/20261125072458_SaaSLicensingFoundation.cs`
- `backend/src/Berynx.Infrastructure/Persistence/Migrations/20261125073502_SaaSUsageQuota.cs`

The manual scripts correspond to those migrations. They were generated from the
EF model and reviewed; neither migrations nor SQL were executed against a
database for this change.

## PlatformAdmin bootstrap

There is no public or tenant-admin API for granting PlatformAdmin. Securely
provision the Identity account through the approved operator workflow, then
allowlist its Identity user ID in server-side environment configuration:

```text
PlatformAdmin__IdentityUserIds__0=<IdentityUserId>
```

For additional operators, use subsequent numeric keys. The value is not a
password or API secret, but it is privileged configuration and must be managed
by the deployment owner. The account must also have an active tenant membership
because local M02 authentication issues tenant-scoped sessions. Sign out and
sign in again after allowlist changes so the server issues a fresh role claim.
Never accept this ID from a client request or tenant-controlled setting.

## Rollback guard

`99-rollback.sql` requires the SaaS plan catalogue, tenant subscriptions,
entitlements, API usage and licence audit history to be empty. It also refuses
rollback if any tenant is in `Grace`, because the older M01 lifecycle check did
not support that status. Export and review any retained records before
considering rollback; the normal product flow is to retain cancelled tenant
data for disposition rather than drop it.
