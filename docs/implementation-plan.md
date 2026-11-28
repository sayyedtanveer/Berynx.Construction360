# Implementation Plan

**Requirements baseline:** `docs/requirements/BRD_v2.2.md` remains authoritative and unchanged. The additive v2.3 addendum has product-owner approval for DIF-P1-01 through DIF-P1-06 as Phase 1 implementation scope. DIF-P2 through DIF-P5 are approved for planning only and are not implementation scope.

## Gate 0 — First agent task: project scaffolding ONLY
1. Inspect repository, complete BRD and AGENTS instructions, and local versions of dotnet, Node and Angular tooling.
2. Create `backend/` with buildable ASP.NET Core Web API and separate Domain/Application/Infrastructure projects, plus tests.
3. Create `frontend/` with independent Angular workspace and basic working application shell.
4. Configure environment-driven PostgreSQL connection, OpenAPI, health endpoint, errors, DI, safe `.gitignore` and starter test setup.
5. Run builds/tests if toolchain exists, record exact commands/output and blockers honestly.
6. Update README and decisions log. **STOP** and request review before modules.

## Phase delivery sequence
- **Phase 1:** P1-M01 through P1-M19, in the BRD's defined module order; modules completed one at a time.
- **Phase 2:** 10 E2 epics, preserving Phase 1 ledgers/GRNs while introducing perpetual stock and advanced finance.
- **Phase 3:** 7 E3 project-management epics with project-level cost and billing traceability.
- **Phase 4:** 7 E4 quality, safety and collaboration epics.
- **Phase 5:** 5 E5 provider, offline and analytics/AI epics.

## Permanent user-approved module workflow — all modules in Phases 1–5

Follow the workflow in [`AGENTS.md`](../AGENTS.md). Live database synchronization, database-dependent integration tests, and prior-module commits/pushes are **not prerequisites for beginning or implementing another assigned module**. The user manually executes SQL, verifies live databases and performs all Git finalization. Never automatically connect to or modify PostgreSQL, execute SQL, or commit/merge/push.

1. Read the applicable BRD, current source, relevant module guides, dependencies and decisions.
2. Implement the assigned module end-to-end in backend, frontend and business rules, reusing existing architecture and contracts.
3. Generate and review EF Core migrations and model diffs.
4. Generate PostgreSQL SQL for new tables, alterations, indexes, constraints, required seeds and verification; generate separate guarded rollback SQL. Do not execute these scripts.
5. Run available .NET Release build, NUnit unit tests, Angular production build/tests, applicable static checks and `git diff --check`. Fix confirmed non-database failures and rerun affected checks.
6. Mark database-dependent integration tests **PENDING** if the disposable PostgreSQL test database is unavailable; do not claim they passed.
7. Update the module guide and plan with acceptance coverage, migration IDs, exact SQL paths/manual execution order, test results and limitations.
8. Review security, tenant isolation, secrets, generated outputs and unrelated work; preserve all existing changes.
9. Report evidence and stop at the assigned module boundary; do not automatically begin the next module.

Application startup must not call `Database.Migrate`, `EnsureCreated` or execute DDL. Do not use `dotnet ef database update`. Follow [`database/README.md`](../database/README.md) for SQL artifact expectations and owner-run execution order. Report external-provider and pilot/UAT evidence separately; do not claim production approval without evidence.

## Agent implementation completion gate
- Story IDs and business-rule IDs mapped to tests.
- Authorization/tenant/cost leakage negative cases and audit verified.
- Concurrency, idempotency and reversal coverage for posting workflows.
- Appropriate UX empty/error/loading/mobile states and accessibility.
- EF model/migrations and generated deployment/rollback SQL reviewed.
- Available backend/frontend builds, unit tests, static checks and regression tests run; database tests are explicitly PENDING when no disposable DB is configured.
- Module documentation is current.
- SQL paths and manual execution order are provided for the owner.
- No unverified claims of successful builds/testing or provider/UAT/production approval.
- Do not execute SQL or perform Git finalization; stop before the next module.

## P1-M01 implementation review

The module implementation and acceptance matrix are recorded in [P1-M01](modules/P1-M01.md). Its PostgreSQL migration rehearsal and database-backed tests remain owner-run/pending when no test database is configured. This does not prevent implementing the next assigned module.

## P1-M02 — Users, Roles and Approvals

Implement the screen/story inventory in [P1-M02](modules/P1-M02.md) under the agent implementation workflow above. The approved authentication decision uses local ASP.NET Core Identity; the external provider is retained for email delivery only, and mobile/SMS OTP is excluded. M01/M02 SQL scripts are prepared for manual owner execution. Their live application, database integration results and Git finalization remain separately pending and are not gates on an assigned P1-M03 implementation.

## P1-M03 — Masters

The M03 implementation status and acceptance matrix are in [P1-M03](modules/P1-M03.md). Four migrations are prepared (`20261124114145_P1M03Masters`, `20261124115631_P1M03MasterConcurrency`, `20261124121636_P1M03VersionedMasters`, `20261124122738_P1M03PartyOwnership`) with ordered deployment and guarded rollback SQL under `database/scripts/P1-M03/`. The owner executes the scripts and verifies live schema. PostgreSQL-backed acceptance remains pending until run on a separate disposable `_test` database. Do not connect to or modify PostgreSQL, perform Git finalization, or begin M04 automatically.
## BRD v2.3 additive impact assessment and sequence

**Status:** DIF-P1-01 through DIF-P1-06 are approved Phase 1 scope, not implementation-complete requirements. BRD v2.2 remains the unchanged baseline. Phase 2-5 differentiators are approved for planning only; do not implement them. Existing code status, tests, database evidence and UAT remain separately reported.

### Compatibility rules

- Keep v2.2 story/rule IDs and meanings unchanged. Track differentiators only by their new DIF-P* IDs and link them to existing module/epic IDs.
- Prefer read models and existing document references over duplicate aggregates, relationship tables, accounting/pricing/notification/document engines or persisted status summaries.
- Preserve tenant, branch, permission, entitlement, audit, immutability, precision and source-of-truth rules from v2.2. Estimates, unknown values and externally unconfirmed events must remain explicit.
- Illustrative API paths in v2.3 are not approved contracts. Reconcile with existing endpoints and permission filters before any implementation proposal.

### Phase 1 module impact assessment

| Module | Addendum impact | Assessment and dependency |
|---|---|---|
| M01 Tenant/onboarding | DIF-P1-06 | Role-level guided-mode preference/configuration only; reuse existing onboarding, roles and entitlements. |
| M02 Identity/RBAC/approvals | DIF-P1-03, DIF-P1-06 | Briefing tiles and actions must use existing authorization and approval scope; do not add a parallel role system. |
| M03 Masters | DIF-P1-01, DIF-P1-06 | Margin uses existing approved rates and source costs; simple mode reuses master forms and validations. |
| M04 CRM/enquiries | DIF-P1-02, DIF-P1-06 | Link enquiry/quotation references into the order view and guided path; reuse existing IDs and conversion workflow. |
| M05 Quotations | DIF-P1-02, DIF-P1-05, DIF-P1-06 | Order view links quote history; manual share is user-initiated wa.me only; guided forms submit through the same API. |
| M06 Sales orders | DIF-P1-01..04 | Baseline and committed margin, order 360, briefing actions and quantity reconciliation depend on trustworthy order snapshots and lifecycle state. |
| M07 Procurement | DIF-P1-01, DIF-P1-02, DIF-P1-04 | Source procurement/GRN costs and quantities in shared views; deduplicate cost and source links rather than copying them. |
| M08 Delivery | DIF-P1-01..04 | Trip cost, owner briefing and quantity/evidence reconciliation build on actual status transitions and verified quantities. |
| M09 Challans | DIF-P1-02, DIF-P1-04, DIF-P1-05 | Use existing challan/trip IDs and verification evidence; secure sharing must not bypass document access policy. |
| M10 Invoicing | DIF-P1-01..05 | Invoice contribution and eligibility derive from posted billing and accepted delivery; share only secure document links. |
| M11 Receipts/collections | DIF-P1-02..05 | Link allocations and disputes, brief overdue actions and share statements without duplicating receivables calculations. |
| M12 Accounting | DIF-P1-01, DIF-P1-02 | Posted ledger is financial source-of-truth; contribution margin is not accounting net profit; link vouchers, never create a second ledger. |
| M13 GST | DIF-P1-01 | Ex-GST basis must follow v2.2 posting/tax snapshots; no new GST calculation engine. |
| M14 Expenses/other income | DIF-P1-01 | Attribute eligible direct costs once to existing source dimensions; exclude unknown/unlinked amounts from realized claims. |
| M15 Dashboard/home | DIF-P1-01..03 | Reuse KPI definitions and query sources for margin cards, Order 360 entry points and daily briefing; preserve unknown/pending/zero distinctions. |
| M16 Reports | DIF-P1-01..04 | Expose explainable variance/reconciliation through existing report permissions and source drill-down; totals must tie to source records. |
| M17 Promotion/catalogue | DIF-P1-05 | Manual sharing only; no mass messaging, provider delivery, webhook or sent/delivered claim from wa.me launch. |
| M18 Cross-cutting services | DIF-P1-02, DIF-P1-03, DIF-P1-05, DIF-P1-06 | Reuse scoped links, audit, help, preferences and notification infrastructure; secure link expiry/revocation is a prerequisite for private-document sharing. |
| M19 Quality/disputes | DIF-P1-02, DIF-P1-04 | Link dispute/evidence and accepted quantities to authoritative documents; do not create a Phase 4 NCR/quality engine in Phase 1. |

### Approved implementation order

1. **Close v2.2 release blockers first.** Finish baseline M01-M19 requirements and verify source data, tenant/branch isolation, financial posting, M17-M19 dependencies and database migration alignment. The differentiators rely on these records and do not replace baseline acceptance.
2. **DIF-P1-02 One-Click Order 360.** Build a permission-aware, read-only projection using existing source IDs and authoritative module states. Show missing downstream documents as Not started; mask cost/contact data by permission.
3. **DIF-P1-04 Dispute-Proof Delivery Reconciliation.** Join order, schedule, trip, delivery evidence, challan, invoice and dispute references; preserve UOM and distinguish not-dispatched from dispatched-not-accepted. Reuse existing transitions, idempotency and reversal controls.
4. **DIF-P1-01 Profit Leakage Control.** After source costs and billing are reliable, derive expected/committed/estimated/realized contribution on an ex-GST basis. Count source costs once; unknown cost stays UNKNOWN; alerts remain advisory absent an existing approval rule.
5. **DIF-P1-03 Owner Daily Briefing.** Reuse operational/report query definitions, explicit branch/time-zone/date boundaries and filtered source drill-downs. Keep zero, unknown and pending distinct; no AI/predictions in Phase 1.
6. **DIF-P1-05 WhatsApp Manual Share / Link-First.** Implement after approved secure link expiry/revocation and audit behavior are available. Record only Share opened; do not claim delivery. No official API dependency or automated sending.
7. **DIF-P1-06 Guided Simple Mode.** Add last, as a UI preference over existing forms and APIs; prove identical server-side validation and document outcomes between guided and advanced flows.
8. **Run approved DIF acceptance and existing v2.2 Phase 1 gate.** Do not enable the next phase based on builds alone; follow v2.2 Section 12 and the addendum's data-validation and pilot gates.

### Future-phase additive crosswalk (deferred)

| Phase | Addendum IDs | Existing v2.2 scope to extend, not replace | Recommended dependency/order |
|---|---|---|---|
| 2 | DIF-P2-01..06 | E2-01 inventory; E2-02 procurement; E2-03 commercial controls; E2-04 financial control/E2-05 analytics; E2-09 subscription billing; E2-10 languages. E2-06 GST 2B, E2-07 fixed assets and E2-08 TDS reconciliation remain unchanged. | Preserve E2-01..10 contracts and ledgers. Add stock variance with inventory; supplier scoring with procurement; rate simulation after cost/price history; collections worklist after financial controls; packaging/languages in their existing epics. |
| 3 | DIF-P3-01..05 | E3-01 projects/sites; E3-02 estimation/BOQ; E3-03 WBS/schedule/budget; E3-04 execution; E3-05 contractors/work orders; E3-06 measurement/RA billing; E3-07 variations. | Build project 360 on project/site links, then cost-code budget/actual/committed, daily pulse, contractor certification and variation impact. Preserve original Phase 1 tenant/branch/transaction ownership; version cost-code mappings. |
| 4 | DIF-P4-01..04 | E4-01 drawings; E4-02 RFI/submittals; E4-03 issues/tasks/meetings; E4-04 quality/NCR; E4-05 safety; E4-06 equipment; E4-07 punch list/closeout. | Reuse shared evidence/document history and existing delivery disputes; deliver linked decision/evidence chain, exception cockpit, sourced incident/equipment impacts and closeout completeness. No duplicate incident records. |
| 5 | DIF-P5-01..05 | E5-01 batching integration; E5-02 fleet/GPS; E5-03 integrations; E5-04 full offline; E5-05 analytics/AI. | Integrate only behind tenant opt-in and provider/security gates. Start with read-only cited assistant and governed forecasting; then ETA/plant signals, optional official WhatsApp and conflict-safe offline. Validate accuracy, permissions, consent, cost and failure modes before release. |


### Proposed v2.3 release evidence gates

| Gate | Minimum evidence before sign-off |
|---|---|
| Phase 1 code acceptance | Every product-owner-approved DIF-P1 acceptance criterion is tested; .NET and Angular builds pass; no known P0 differentiator remains unimplemented; v2.2 M01-M19 acceptance is reconciled. |
| Phase 1 data validation | Additive EF/manual SQL paths reviewed; disposable PostgreSQL tests and financial tie-outs pass; tenant isolation is verified; any unavailable prior persistence tests remain explicitly pending. |
| Phase 1 pilot | v2.2 golden path plus Order 360, margin and delivery discrepancy scenarios pass; owner can use the briefing; no false invoice, proof, cost or delivery state is shown. |
| Phase 2 | Reconcile stock-to-GL and procurement/credit reporting; safely activate subscription billing per v2.2 gate. |
| Phase 3 | Reconcile project BOQ budget, consumption, progress and contractor RA billing by cost code, site and company ledger. |
| Phase 4 | Trace quality/safety/document issues to closure and source evidence without breaking audit lineage. |
| Phase 5 | Pass accuracy, permission, consent, cost metering, offline conflict and failure-mode tests; validate integrations/AI with customers as specified. |

The product owner approved all six DIF-P1 items and their sequence for implementation. Open product decisions OD-02..OD-08 remain design/configuration questions where the addendum does not grant a specific policy; resolve them without inventing tenant-wide business rules. Approval does not authorize Phase 2-5 implementation.
Commercial package names and indicative prices in v2.3 are hypotheses, not approved rates or implementation requirements.

## Critical transaction-chain implementation status — 2026-11-26

M08/M09 authorized no-OTP evidence review and persisted challan gates, M12 effective-dated tenant mappings/chart configuration, M10 invoice posting, and M11 receipt allocation/posting have partial source implementations. The backend and Angular flows compile and automated tests pass; live M08 verification is fail-closed until the approved M18 secure storage/scanning/preview provider is available. OTP/SMS is excluded from Phase 1. M10 billing charges/TCS and M11 cheque/adjustment/refund/note/reconciliation workflows remain incomplete. Additive schemas include `20261126151858_P1M12TenantLedgerMappingsAndPostingLinks` and `20261127081534_P1M08ReceiverEvidenceReview`; manual M08 deploy/guarded rollback SQL are `database/scripts/P1-M08/05-receiver-evidence-review-deploy.sql` and `06-receiver-evidence-review-rollback.sql`. This is code progress only: no SQL was executed and no UAT readiness is claimed.

## Current implementation evidence — 2026-11-27

M19 has a new **PARTIAL** tenant-scoped source implementation, UI, additive migration and SQL package. The detailed acceptance disposition and blockers are in `docs/modules/P1-M19.md` and `docs/progress/PHASE-1-CHECKPOINT.md`. This does not close the M10–M12, M17 or M18 release blockers and does not implement any of DIF-P1-01..06. Phase 1 remains NOT READY FOR UAT; database-dependent tests are PENDING.