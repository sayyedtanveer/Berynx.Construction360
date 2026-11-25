# SaaS licensing and tenant activation foundation

## Scope

Adds catalogue-backed base plans, optional M03 licensing, versioned tenant
subscription snapshots, per-module entitlements, plan quotas, manual lifecycle
controls and append-only platform licence audit. It reuses M01 tenant
provisioning and onboarding, M02 Identity/invitation/RBAC, and the existing M03
route/service authorization. No payment gateway, billing ledger, public
company self-registration, support impersonation, or new user credential
system is included.

## Workflows

- Platform Admin tenant workspace: `/setup/platform`
- Platform Admin plans/modules catalogue: `/setup/plans`
- Tenant creation uses the existing `/api/v1/platform/tenants` M01 workflow;
  the first Tenant Admin is invited through existing M02/M01 activation
  infrastructure with the existing 72-hour invitation expiry.
- Existing Tenant Admin activation, password setup, MFA and onboarding remain
  in the existing M02 `/login` and M01 `/setup/*` flows.
- Subscription details, usage, module entitlements and audit are available in
  the Platform Admin tenant detail view.

Platform Admin API routes:

| Capability | API |
|---|---|
| Plan catalogue create/list/update | `GET/POST /api/v1/platform/plans`, `PUT /api/v1/platform/plans/{code}` |
| Module catalogue | `GET /api/v1/platform/modules` |
| Tenant list/create | `GET/POST /api/v1/platform/tenants` |
| Tenant detail and audit | `GET /api/v1/platform/tenants/{tenantId}`, `GET /api/v1/platform/tenants/{tenantId}/audit` |
| Assign plan/add-ons | `PUT /api/v1/platform/tenants/{tenantId}/subscription` |
| Activate/renew/suspend/reactivate/cancel | `POST /api/v1/platform/tenants/{tenantId}/subscription/{action}` |

Every platform route requires a server-issued `PlatformAdmin` role claim.
PlatformAdmin claims are minted only for configured Identity IDs under
`PlatformAdmin:IdentityUserIds`; there is no self-service role assignment.
The bootstrap procedure is documented in
[`database/scripts/SAAS-FOUNDATION/README.md`](../../database/scripts/SAAS-FOUNDATION/README.md).

## Commercial and access rules

- M01 and M02 are required core modules; M03 is the currently available
  optional add-on. The catalogue does not enable unimplemented future modules.
- A subscription snapshots plan version, quotas, price/currency, trial/grace
  terms and selected modules. Plan edits do not silently rewrite existing
  tenant terms.
- Plan/add-on reassignment is version-checked, rejects quotas below current
  usage, records before/after and reason, and takes effect immediately for an
  existing subscription. Future scheduled plan changes are rejected until the
  data model supports concurrent effective terms.
- Lifecycle states are Trial, Active, Grace, Suspended and Cancelled. Expiry
  derives Grace then Suspended from effective dates. Suspended tenants are
  read-only; cancelled tenants retain the existing M01 export path for 30 days.
- Tenant API requests are atomically metered by UTC calendar month. Exhausted
  active-plan quota returns HTTP 429 with a Platform Admin upgrade message.
  Tenant usage reporting and permitted suspended read/export requests remain
  available.
- Existing M02 seat and invitation-message checks remain authoritative.
  M01 document and storage checks are reused. Plan limits are not enforced
  solely by hidden Angular navigation: M01, M02 and M03 backend route/service
  authorization checks lifecycle, module entitlement, permission, tenant and
  branch/plant scope.
- The Angular sidebar consumes the server-authorized `auth/navigation`
  modules/permissions response. It remains a presentation layer; API checks
  are authoritative.

## Database delivery

Migrations:

1. `backend/src/Berynx.Infrastructure/Persistence/Migrations/20261125072458_SaaSLicensingFoundation.cs`
2. `backend/src/Berynx.Infrastructure/Persistence/Migrations/20261125073502_SaaSUsageQuota.cs`

Manual PostgreSQL scripts, in order:

1. `database/scripts/SAAS-FOUNDATION/01-schema.sql`
2. `database/scripts/SAAS-FOUNDATION/02-alter-existing.sql`
3. `database/scripts/SAAS-FOUNDATION/03-indexes-constraints.sql`
4. `database/scripts/SAAS-FOUNDATION/04-seed.sql`
5. `database/scripts/SAAS-FOUNDATION/05-verification.sql`

Separate guarded rollback:
`database/scripts/SAAS-FOUNDATION/99-rollback.sql`.

All M01–M03 schema prerequisites must be deployed first. SQL scripts are for
manual owner execution only. No database connection, SQL execution, or
`dotnet ef database update` was performed for this implementation.

## Verification and limitations

- .NET 10 Release solution build: **passed**.
- Backend NUnit unit tests: **55 passed**.
- Angular production build: **passed**, with component-style budget warnings in the existing login, header and M02 styles.
- Angular unit tests: **36 passed** across 10 files, including Platform workspace API wiring, plan quota submission and server-authorized sidebar navigation.
- EF `has-pending-model-changes`: **passed**; no model drift from the reviewed migrations.
- `git diff --check`: **passed**.
- PostgreSQL-backed integration tests: **PENDING** unless the owner explicitly
  configures the isolated disposable localhost database described by the test
  fixture (`BERYNX_TEST_POSTGRES`, database ending in `_test`).
- Browser E2E, live invitation email/MFA provider delivery, and manual SQL
  deployment verification were not performed.
- M02 currently supports TOTP enrollment and login challenges for accounts
  that have enabled MFA, but it does not yet expose the BRD's tenant policy
  that makes MFA mandatory for Tenant Admin/Accountant roles. The SaaS
  provisioning flow does not silently force a new policy; add that existing
  M02 policy before claiming mandatory MFA for first-admin activation.
- Phase 2 payment gateway, invoices, dunning, proration and payment-driven
  activation remain external dependencies. Manual Platform Admin activation
  and renewal are available now.
