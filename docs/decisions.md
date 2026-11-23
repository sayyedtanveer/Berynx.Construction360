# Decisions and Open Validations

Proposed decisions do not equal customer approval. Amend this log during implementation.

| ID | Topic | Proposal | Status |
|---|---|---|---|
| ADR-001 | Repository | One root Git repo; separate `backend/` and `frontend/` projects | Proposed |
| ADR-002 | Backend | .NET 10 LTS if SDK/dependencies support it | Proposed |
| ADR-003 | Angular | Select compatible current version after checking environment | Proposed |
| ADR-004 | Scope | Gate 0 scaffold only, then Phase 1 modules in BRD order | Proposed |
| ADR-005 | DB | PostgreSQL + EF Core migrations owned by backend | BRD baseline |

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
