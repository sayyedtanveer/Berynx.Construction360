# Decisions and Open Validations

Proposed decisions do not equal customer approval. Amend this log during implementation.

| ID | Topic | Proposal | Status |
|---|---|---|---|
| ADR-001 | Repository | One root Git repo; separate `backend/` and `frontend/` projects | Proposed |
| ADR-002 | Backend | .NET 10 LTS if SDK/dependencies support it | Proposed |
| ADR-003 | Angular | Select compatible current version after checking environment | Proposed |
| ADR-004 | Scope | Gate 0 scaffold only, then Phase 1 modules in BRD order | Proposed |
| ADR-005 | DB | PostgreSQL + EF Core migrations owned by backend; manual SQL deployment from P1-M02 onward | Superseded for deployment by ADR-009 |
| ADR-006 | Backend target | .NET 10 LTS (`net10.0`) | Selected; .NET 10.0.404 restore/build and NUnit unit tests verified for P1-M02 |
| ADR-007 | Frontend | Angular 22.3.0 + Vitest on Node 24.19.0 | Selected |
| ADR-008 | Tenant identity | Resolve tenant only from a validated JWT `tenant_id` claim | Enforced; locally issued JWTs are accepted only after active membership/session validation |
| ADR-009 | Schema deployment | Keep EF Core migrations as schema source; deploy reviewed PostgreSQL scripts manually, never migrate at startup | Required for P1-M02 onward |
| ADR-010 | M01/M02 authentication ownership | ASP.NET Core Identity is the sole local credential and token authority; one account per normalized email, tenant-scoped memberships, provider retained only for email delivery; no mobile/SMS OTP | Approved by product owner; implementation and database verification in progress |
| ADR-011 | PlatformAdmin bootstrap | Mint the global PlatformAdmin claim only for Identity IDs explicitly listed in server configuration; no tenant-facing role-grant API | Selected for SaaS foundation; operator-managed bootstrap |

## Gate 0 technical notes

- These are the original Gate 0 environment findings. Current SDK/build and
  module verification status is recorded in ADR-006 and
  [P1-M02](modules/P1-M02.md).
- The backend targets .NET 10 as requested. This machine has only .NET SDK
  8.0.200, so restore, build, and test verification remain blocked until the
  .NET 10 SDK is installed. The target was not silently downgraded.
- Angular CLI 22.3.0 is pinned in the workspace. Node 24.19.0 satisfies its
  published Node engine range; the globally installed Angular CLI 19.2.14 was
  not used to scaffold the workspace.
- PostgreSQL 16 and 17 services were found running with port 5432 listening.
  No database credentials were supplied, so the app connection was not tested
  and no schema or migration was created.
- JWT validation is configured to fail closed. There is no token issuer,
  login/session API, tenant onboarding, or tenant-owned business entity in
  Gate 0. Tenant resolution reads only the authenticated principal's
  `tenant_id` claim; no request header, query, or route value selects a tenant.
- Authorization middleware uses authenticated principals and a role-claim
  mapping; tenant roles and permission policies belong to P1-M02. Immutable
  business-audit persistence and multi-step financial posting transactions
  were not added without their module contracts or data model. EF Core
  `SaveChanges` provides the persistence boundary; explicit posting
  transactions will be introduced with the first applicable module.

## Business/legal validations still open
- GST, e-invoice, e-way bill, TDS/TCS and MSME rules: verify current applicability and get CA/owner approval.
- Phase 1 GRN and Phase 2 inventory valuation cutover: approve migration and reconciliation plan.
- Subscription billing provider and timing; tenant pricing/commercial policy.
- Cloud region, RPO/RTO, retention and operational approvals.
- Pilot-specific credit/approval thresholds, RMC pricing and invoicing variants.

## Decision template
### ADR-XXX: [title]
- Date / module / story IDs:
- Problem and alternatives:
- Decision and rationale:
- Status: Proposed / Approved / Superseded
- DB/API/UI/testing impact:
- Approver:

## 2026-11-24 · P1-M01 implementation decisions

| Decision | Rationale |
|---|---|
| Store tightly coupled setup fields in one tenant-owned JSONB aggregate with a concurrency token. Keep opening money and posting allocations relational. | A step update and go-live share one consistency boundary. Bill-wise items have stable identities after lock. |
| Provision a local ASP.NET Core Identity account linked to the tenant-scoped first-admin membership; send a protected, expiring activation link through the configured HTTPS email-delivery service. Tenant and permission checks use locally issued signed claims and active local role assignments. | Keeps authentication in one system while retaining the existing provider solely for outbound email. |
| Reserve a user seat and a message allowance when an invite is queued. Display delivered messages separately. | Pending invitations cannot oversubscribe the plan. |
| Use administrator-supplied plan limits and tax defaults. Offer neutral fiscal-year and sequence tokens for initial series, editable per branch. | Commercial terms, registration details and tax rates remain configurable. |
| Master imports stage named references for later master adoption. Number assignment requires the posting caller’s database transaction. | M01 can prepare downstream data without exposing posting or voucher operations. |
| Retain cancelled data beyond the 30-day export window pending authorized disposition. | The BRD does not supply an approved physical purge schedule or legal-hold clearance. |

## 2026-11-24 · P1-M02 implementation decisions

| Decision | Rationale |
|---|---|
| **Superseded by ADR-010.** Authentication, password hashing, JWT issuance, refresh rotation/revocation, password reset, TOTP, recovery codes, lockout and session checks are owned by ASP.NET Core Identity plus local application services. The existing provider adapter is restricted to invitation/password-reset email delivery. SMS/mobile OTP is excluded. | Product owner approved the unified local authentication model. External provider login/token capabilities are no longer required; actual email delivery remains configuration-dependent. |
| Use one Identity account per normalized email with many tenant memberships linked to the existing tenant user and role records. Login selects the only active authorized tenant automatically or returns a protected tenant-selection challenge when several are available. | Avoid duplicate credentials while preserving tenant-scoped ownership and permissions. Client-supplied tenant IDs are accepted only after server-side membership validation. |
| Use tenant-owned role assignments with permission unions across assigned roles. Data scope is the union of `All`, `Branch`, `Plant` and `Own` grants for the requested permission; role permissions do not inherit from other roles. | This is an explicit, testable evaluation rule; it avoids a hidden role hierarchy. Tenant Admin remains the immutable system role key and is subject to database last-admin protection. |
| An approval rule's configured predicates are combined with AND. If exactly one enabled rule matches, that workflow is created; no match means no approval, and overlapping rules fail closed for administrator correction. A changed pending source snapshot cancels the old request with an immutable decision and submits a new version; approved history is retained. | The BRD specifies configurable thresholds and multiple levels but does not define OR groups or conflict resolution. Silently choosing an overlapping rule would be unsafe. |
| Keep future-module approval calls behind `IM02ApprovalEngine`; M02 stores generic source type/ID and an immutable request snapshot without implementing business modules. | Reusable integration point without pre-creating M03 or later modules. |
| Generate PostgreSQL deployment scripts from the EF migration history and execute them manually. Application startup never migrates or creates schema. | Explicit deployment safety requirement; preserves EF Core as the ORM and migration source of truth. |

## Approved authentication decision details

- Passwords use the ASP.NET Core Identity password hasher and configured lockout. JWT access tokens are tenant-scoped and short-lived. Refresh secrets are stored as hashes, rotated transactionally, and revoked on reuse, logout, password reset or session revocation.
- Password reset and invitation activation links use ASP.NET Core Data Protection. Production must configure a durable, shared, access-controlled key ring. The email provider receives messages only; it must never mint access tokens or authenticate a user.
- TOTP MFA and Identity recovery codes are local. Mobile/SMS OTP is explicitly excluded from P1-M02.
- PlatformAdmin entitlement is granted only by `PlatformAdmin:IdentityUserIds` server configuration. The account must still authenticate through an active tenant membership; there is no public or tenant-admin role-grant endpoint. Configure the allowlist after securely provisioning the Identity account, then reauthenticate to receive the claim.

## 2026-11-24 · P1-M03 implementation decisions

| Decision | Rationale |
|---|---|
| Represent customers, suppliers, transporters and other trading entities as one tenant-scoped party with role records. Keep sites, contacts, supplier plants and protected party bank accounts in separate relational records. | Supports multi-role counterparties and controlled duplicate merge without duplicating legal identity. |
| Reuse the M01 onboarding JSON aggregate for tenant bank/cash accounts; retain supplier bank account numbers under tenant-scoped Data Protection and return masked values only. | Preserves the existing source of truth while protecting sensitive counterparty banking fields. |
| Keep tax and RMC rate records effective-dated and fail closed when an applicable approved rate is missing. Do not compute or rewrite posted-document snapshots in M03. | M03 owns reusable configuration; transaction modules must capture immutable rate/tax snapshots at posting. |
| Treat `CreditLimit = 0` as a zero credit cap, not as an unlimited credit account. HardBlock and SoftBlock apply their configured block/approval behavior; Warn surfaces a warning. Credit facts are usable only when invoice outstanding, confirmed-undelivered order value, unallocated advances and overdue facts are all explicitly supplied. | The BRD defines a credit limit and exposure formula but does not define zero as unlimited. Fail closed rather than silently granting unlimited credit. A tenant wanting open credit must configure a positive limit. |
| Return explicit pending/null values for transaction-derived Customer 360 metrics and define M04+ consumer contracts for transaction facts and immutable tax snapshots. | Avoids fabricating balances or implementing later transaction features prematurely. Create-in-context drawers are implemented as reusable M03 components; M04 wires them into quotation/order workflows. |

## 2026-11-25 · SaaS licensing foundation decisions

| Decision | Rationale |
|---|---|
| Snapshot plan version, quotas, price/currency, grace terms and selected modules on each tenant subscription. Plan edits do not silently rewrite existing tenant terms; an authorized reassignment creates an audited change. | Preserves commercial history and gives Phase 2 billing a stable integration contract without implementing payment processing. |
| M01 and M02 are required core modules; M03 is the currently available optional add-on. The catalogue contains only implemented modules, and later modules must be added when their routes/contracts exist. | Prevents licensing nonexistent features or creating unused navigation/API surface. |
| Meter tenant API requests atomically by UTC calendar month. At quota, tenant API calls receive HTTP 429 with an upgrade contact message; usage visibility and permitted suspended read/export access remain available. | Enforces concurrency-safe usage without blocking recovery/export paths. |
| A plan change on an existing subscription takes effect immediately; future scheduled plan changes are rejected until the model supports multiple effective subscription terms. | Avoids replacing an active entitlement snapshot before a scheduled effective date. |
| First PlatformAdmin assignment remains operator-controlled through `PlatformAdmin:IdentityUserIds`; tenant creation and employee invitation never grant this role. | Prevents self-escalation and keeps initial global privilege outside tenant-controlled APIs. |
