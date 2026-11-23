# Decisions and Open Validations

Proposed decisions do not equal customer approval. Amend this log during implementation.

| ID | Topic | Proposal | Status |
|---|---|---|---|
| ADR-001 | Repository | One root Git repo; separate `backend/` and `frontend/` projects | Proposed |
| ADR-002 | Backend | .NET 10 LTS if SDK/dependencies support it | Proposed |
| ADR-003 | Angular | Select compatible current version after checking environment | Proposed |
| ADR-004 | Scope | Gate 0 scaffold only, then Phase 1 modules in BRD order | Proposed |
| ADR-005 | DB | PostgreSQL + EF Core migrations owned by backend | BRD baseline |
| ADR-006 | Backend target | .NET 10 LTS (`net10.0`); install the matching SDK to verify | Selected; local SDK unavailable |
| ADR-007 | Frontend | Angular 22.3.0 + Vitest on Node 24.19.0 | Selected |
| ADR-008 | Tenant identity | Resolve tenant only from a validated JWT `tenant_id` claim | Foundation only; no auth flow yet |

## Gate 0 technical notes

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
