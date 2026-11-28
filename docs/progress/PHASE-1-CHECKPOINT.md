# Phase 1 implementation checkpoint

Updated during the current implementation session.

## Workspace and safety

- Checkout: `C:\Users\Dell\source\repos\Berynx.Construction360`
- Branch: `main` (tracking `origin/main`); working tree contains uncommitted M06 work from the preceding request. Preserve it; no Git history operations have been performed.
- No additional Git worktrees are registered.
- No nested `AGENTS.md` files were found under `backend/`, `frontend/`, `database/`, or `docs/`.
- No database connection or SQL execution was attempted.

## Toolchain

- `dotnet`, `node`, `npm`, `ng`, `psql`, and standard Windows .NET SDK paths: unavailable.
- M06 migration/snapshot generation, all builds, unit tests, Angular tests, and EF consistency checks are blocked until compatible .NET 10 and Angular 22 toolchains are installed.
- PostgreSQL integration tests are **PENDING**; no database was probed.
- `git diff --check` passed; no trailing whitespace was found in newly created files.

## Current module and completed work

**Current module: M16 Reports, selected source-backed report queries. M07–M16 are not end-to-end.**

The existing uncommitted M06 source and SQL package are preserved in `backend/src/Berynx.Api/SalesOrders/`, `backend/src/Berynx.Domain/SalesOrders/`, `backend/src/Berynx.Application/SalesOrders/`, `database/scripts/P1-M06/`, and the related Angular/docs paths. Existing M04/M05/M02 integration changes are also present.

M06 EF migration and `BerynxDbContextModelSnapshot` update are missing. Do not represent M06 as database-ready or apply its SQL package as a substitute for the migration workflow. The current M06 guide records other limitations, including unavailable M03 credit facts, missing M07 purchase-cost facts, HTML instead of a PDF confirmation, and unrun tests.

For M07, added PO/GRN API endpoints, M06 source-line/quantity handoff, M02 approval transitions, M03 supplier/product/tax master use, global cost redaction, tenant-configurable receipt tolerances, an Angular procurement page, API route/side navigation, unit-test source, and an optional-module permission seed. Supplier invoice/payment/accounting/stock lifecycle, complete M08 consumption, migration/snapshot, complete SQL package, and runtime tests remain **pending**. Details: `docs/modules/P1-M07.md`.

For M08, added a partial M06 schedule-to-pour dispatch path, plant capacity warnings, trip planning and state transitions, quantity updates, idempotent trip-action identifiers, receiver/evidence references, an Angular dispatch board, route/navigation, unit-test source and optional-module permission seed. Offline driver retries, OTP and persisted evidence, M07 cost linkage, full approval/exception workflows, generated migration/snapshot and full SQL package remain **pending**. Details: `docs/modules/P1-M08.md`.

For M09, added a transactional challan-creation hook on M08 trip completion, a branch/FY counter using the configured M01 Challan series, trip uniqueness, immutable commercial snapshots, branch-scoped list/detail/history APIs, printable HTML preview, optional module seed, and initial list/detail UI. Challans intentionally stay `Unverified`; a submitted signature/photo reference is not receiver verification. Corrections, M10 invoice lifecycle, PDF/share, configurable ageing/digest, generated migration/snapshot and runtime checks remain **pending**. Details: `docs/modules/P1-M09.md`.

For M10, added a partial draft-only invoice flow: M09 verified-challan eligibility, one branch/customer/site, M06 frozen order-line prices and quantities, decimal tax/round-off preview, unique challan reservation, idempotency, cancellation, invoice details and optional module seed. Posting explicitly fails closed pending M12. Since M08/M09 have no receiver verification, no challan is currently invoiceable. E-invoice/e-way, charges, TCS, due dates, pro-forma, posting and migration/runtime validation remain **pending**. Details: `docs/modules/P1-M10.md`.

For M11, added partial receipt draft/allocation validation, oldest-first allocation rules, posted-only invoice balance reads, promise-to-pay record foundation, route/navigation and optional module seed. M10 has no posted invoices, and M11 posting fails closed pending M12. Cheques, TDS, notes, write-offs, retention, reminders, statements, reconciliation and tests remain **pending**. Details: `docs/modules/P1-M11.md`.

For M12, added a shared double-entry `IM12PostingEngine`, serializable voucher/line/event persistence, decimal balancing and idempotency, setup-ledger validation, manual control-account guard, period-lock posting check, trial balance/voucher/accounts APIs and UI, unit-rule source and optional module seed. M10/M11/M07 source posting, reversal, P&L/BS, year-end, Tally and reconciliation remain **pending**. Details: `docs/modules/P1-M12.md`.

For M13, added posted-invoice-only GST workspace query, internal JSON extract (not portal validated), exception/review-note records, all-scope-only period filing record, M03 tax master reuse, basic Angular route/view, test source and optional module seed. M10 posting, M07 ITC, M12 tie-out, GST Offline Tool/Excel schemas, registers, rate-change approvals and filing locks remain **pending**. Details: `docs/modules/P1-M13.md`.

For M14, added category configuration, branch-scoped expense capture/listing, M02 amount-threshold approval submission/status synchronization, and approved expense posting through the shared M12 engine. RCM/ITC/other-income fail closed; attachments, reimbursement, petty cash, recurring templates and tax treatments remain **pending**. Details: `docs/modules/P1-M14.md`.

For M15, added source-query dashboard counts/totals, explicit unavailable states, date filtering, branch/own scope enforcement, cost gating through `global.view_cost`, worklist links and module registration. Role-specific dashboard sets, trends, saved card layout, tie-out, all KPI sources and performance validation remain **pending**. Details: `docs/modules/P1-M15.md`.

For M16, added R-S03/R-D01/R-D04/R-X01 source queries, tenant/branch/own scope, `global.view_cost` restriction for expense analytics, CSV export and export-log persistence, report viewer and optional module seed. Most report catalog items, schedules, saved views, Excel/PDF, drilldowns and tie-out remain **pending**. Details: `docs/modules/P1-M16.md`.

## M07–M19 status

| Module | Status |
|---|---|
| M07 Procurement | In progress — PO/GRN source workflows partially implemented; not deployable or end-to-end |
| M08 RMC & Material Delivery | In progress — schedule/pour/trip source workflows partially implemented; not deployable or end-to-end |
| M09 Delivery Challans | In progress — trip-linked snapshots and list/detail source workflows partially implemented |
| M10 Sales Invoicing | In progress — draft-only invoice source workflow; no invoiceable challans until M08 verification exists |
| M11 Receipts, Payments, Notes & Collections | In progress — draft receipt capture only; no posted invoice/receipt balances |
| M12 Accounting | In progress — core manual posting foundation; no source posting integration or financial reports |
| M13 GST | In progress — posted invoice summary and internal JSON only; source posting, statutory exports, ITC and tie-out pending |
| M14 Expenses & Other Income | In progress — basic category/approval/expense posting; petty cash, reimbursements, tax flows and other income pending |
| M15 Dashboard & Home | In progress — source-backed operational subset; role layouts, trend, full KPIs and tie-out pending |
| M16 Reports | In progress — four reports and audited CSV only; majority of BRD catalogue and schedules pending |
| M17 Business Promotion & Catalogue | Not started |
| M18 Cross-cutting Services | Not started |
| M19 Quality Samples & Disputes | In progress — tenant-scoped sample/test/dispute source and Angular workspace added; full BRD acceptance, M11 credit-note resolution, migration deployment and DB verification remain pending |

## Exact next task

Continue the remaining M17/M18 blockers and finish M19 quality acceptance coverage (project/site criteria, traceable heat/lot certificates, dispute value/drill-down, verified replacement/credit-note linkage) in BRD order. Wire each source to M12/M13 only through shared services; do not report fake financial/operational data. M06–M14 migration/snapshot generation and environment checks remain pending until compatible SDKs are available.

## Recovery and verification update — 2026-11-26

The earlier workspace/toolchain lines above are historical and are superseded by this update. The owner-authorized local main checkout is being used directly; no Delta synchronization, Git history operation, reset, clean, merge, commit, push, SQL execution, or development-database access occurred.

- Verified backup created outside the repository at `C:\Users\Dell\source\repos\Berynx.Construction360-recovery-20261126-141812`; verification reported SHA-256 matches for every copied file, including untracked files and `.git` metadata.
- Recovered the four NUL-corrupted M17 source files from the readable source available to the authorized main checkout recovery, retained `M17Entities.cs`, and repaired the M17 rule test. All five requested M17 files were checked for NUL bytes after recovery.
- Fixed build blockers in M09 raw string/CSS, M06 M03 credit-evaluation injection, and M11/M16 LINQ/status query code. `dotnet build backend/Berynx.Construction360.slnx --configuration Release` passed.
- `dotnet test backend/tests/Berynx.UnitTests/Berynx.UnitTests.csproj --configuration Release --no-build`: **132 passed, 0 failed, 0 skipped**.
- Full solution tests were attempted without the development DB; API test hosts nevertheless start M02 DB-polling workers. They attempted a TCP connection to the deliberately non-listening test placeholder `127.0.0.1:1` (connection refused); no PostgreSQL server was reached. The aggregate result is not counted as integration verification. Fix the TestServer hosted-worker setup before another DB-independent integration run.
- `dotnet ef migrations has-pending-model-changes --project src/Berynx.Infrastructure --startup-project src/Berynx.Api` reports pending model changes. No migration was generated or applied. M06–M19 migration/SQL reconciliation remains incomplete.
- Angular is not runnable: Node/npm/ng are unavailable and dependencies are absent. Required versions are Node `^22.22.3 || ^24.15.0 || >=26.0.0`, npm `11.6.2`, Angular CLI `^22.3.0`.
- Integration matrix created at `docs/audits/PHASE-1-FRONTEND-BACKEND-INTEGRATION.md`. It records M01–M16 partial status, M17 endpoint unregistered/no UI, and missing M18/M19 source. Golden-path readiness remains BLOCKED/UNVERIFIED; PostgreSQL-backed workflows remain PENDING.
- `git diff --check` passed for tracked changes at the time checked; untracked files were not included.

Next action: repair test-host background-service isolation, then install the required frontend toolchain and continue the documented backend migrations and cross-module integration against an explicitly authorized disposable `_test` database only.
## Test-host isolation follow-up — 2026-11-26

- Added `TestApplicationFactory` for WebApplicationFactory tests, set the host to `Testing`, and configured the database-independent HTTP test fixtures to use an isolated Data Protection key ring under the test output directory.
- `Program.cs` now omits the M02 database-polling background hosted services only for the `Testing` environment; production/development registrations are unchanged.
- Rebuilt Release successfully after this change. Full solution tests now report **159 total: 136 passed, 0 failed, 23 skipped** (132 unit tests and 4 API host tests passed; 23 disposable-PostgreSQL tests skipped because `BERYNX_TEST_POSTGRES` was unset). The final run made no DB connection. The earlier pre-fix run's refused connection to the `127.0.0.1:1` test placeholder did not reach a PostgreSQL service.
- Integration audit updated to reflect final test counts and test-host repair. DB persistence coverage remains PENDING; EF reports model changes since the last migration; Angular remains blocked by missing Node/npm.
## Repair and integration update — 2026-11-26

- Authoritative checkout remains local `main` at `cbe16ce`; existing tracked and untracked work is preserved. A pre-edit recovery archive was created and SHA-256 verified at `C:\Users\Dell\source\repos\Berynx.Construction360-recovery-20261126-141812` (including `.git`, tracked changes and untracked source; ignored build output excluded). No reset, clean, checkout, commit, merge, push or worktree creation was done.
- Fixed the requested Angular compile blockers: M07 uses bounded component pagination and reloads API results, M10 inline template syntax parses, and M12 displays API-reported debit/credit totals. Added targeted unit tests.
- M17 API registration was completed; added a tenant/module/permission guarded catalogue/consent manager route and sidebar entry, optional module/permission seed, and frontend tests. M17 remains partial: campaign/provider workflows and public request-quote are not implemented.
- Added EF migration `20261126104710_P1M06M17OperationalFoundation` (52 new tables, 124 indexes) for the model delta covering M06–M14, M16 and M17. `has-pending-model-changes` now reports no pending model changes. Generated apply, verification and data-guarded rollback SQL is in `database/scripts/P1-M06-M17-FOUNDATION/`; do not execute overlapping legacy per-module schema scripts until they are reconciled. M18/M19 models and migrations remain missing.
- Final local checks: .NET Release build PASS; NUnit 159 total / 136 passed / 0 failed / 23 skipped; Angular production build PASS with existing style-budget warnings; Angular 24 spec files / 59 passed / 0 failed / 0 skipped; EF model check reports no pending changes. The 23 DB-dependent tests remain PENDING because `BERYNX_TEST_POSTGRES` was unset. No PostgreSQL connection, SQL execution or database update was performed.
- Phase 1 remains **PARTIAL / BLOCKED / UNVERIFIED**, not UAT-ready. The M01–M19 integration matrix, gap register, BRD coverage, test readiness and deployment order were refreshed under `docs/audits/` and `database/scripts/PHASE-1-DEPLOYMENT-ORDER.md`. The immediate outstanding blockers are M18, M19, incomplete M17 and unverified persisted golden paths.
## Current implementation checkpoint — M19, 2026-11-27

**Status: PARTIAL; not a Phase 1 completion or UAT claim.** M19 now has tenant-scoped entities/rules, registered ASP.NET API, permission-gated Angular workspace, generated additive EF migration, an unapplied PostgreSQL package, and M19 domain/UI tests. Failed 28-day test results open disputes and invoice holds using persisted M10/M18/M02 references. Credit-note resolution refuses to proceed without a real M11 record.

**Changed files for this checkpoint:**
- Backend: `backend/src/Berynx.Domain/Quality/M19Entities.cs`, `M19Rules.cs`; `backend/src/Berynx.Application/Quality/M19Contracts.cs`; `backend/src/Berynx.Api/Quality/M19Endpoints.cs`, `Program.cs`; `backend/src/Berynx.Infrastructure/Persistence/BerynxDbContext.cs`; M19 migration, designer and snapshot; M19 domain tests.
- Frontend: `frontend/src/app/features/quality/m19.page.ts`, `.spec.ts`, `frontend/src/app/app.routes.ts`, sidebar navigation.
- Database package: `database/scripts/P1-M19/01-m19-migration.sql`, `04-seed.sql`, `05-verification.sql`, `99-rollback.sql`, `README.md`.
- Documentation: `docs/modules/P1-M19.md`, this checkpoint, implementation plan, decisions, Phase 1 requirement/gap/integration/test-readiness audits.

**Verification run:** .NET 10 Release solution build passed. `dotnet test backend/Berynx.Construction360.slnx --configuration Release --no-build` passed with 192 total, 168 passed, 0 failed, 24 skipped; 24 database-dependent tests remain PENDING. Angular production build passed with existing component stylesheet budget warnings; `npm.cmd test` passed 69/69 tests across 25 files, including 3 M19 page tests. `dotnet ef migrations has-pending-model-changes` reports no changes. No PostgreSQL connection, SQL execution or migration application occurred.

**Remaining M19 blockers:** project/site-specific criteria; TMT/cement heat/lot certificate traceability; scheduled reminders beyond explicit in-app refresh; dispute value/drill-down; verified replacement and M11 credit-note linkage; M19 API/persistence and cross-tenant acceptance tests on authorized disposable PostgreSQL. Details: `docs/modules/P1-M19.md`.

**Next task:** complete remaining core M10–M12/M17/M18 blockers and M19 acceptance gaps before implementing the six approved differentiators in their approved order. Keep database-backed tests pending until an authorized disposable `_test` database is supplied.

## Current P0 wiring checkpoint — M10–M12, 2026-11-27

**Status: PARTIAL; no Phase 1 completion or UAT claim.** This pass repaired three concrete cross-module wiring gaps without schema changes:

- **M10:** invoice list filters now derive Paid/Overdue/Open from posted M11 receipt allocations and due dates; removed the unsupported “Pending approval” filter and draft-save claim that posting is available. Server-side collection-state rules and Angular tests added.
- **M11:** receipt draft customer and branch selectors now load active customer-role parties and authorized branches from `/api/v1/m11/receipt-options`; selected branch is sent to open-invoice lookup. Allocation rules now materialize one-shot inputs once.
- **M12:** exposed the existing `TcsPayable` mapping role; manual voucher ledger selection now uses active chart-of-accounts records instead of free-text codes. This does not implement M10 TCS calculation or threshold policy.

**Changed source/tests:** `backend/src/Berynx.Api/Invoicing/M10Endpoints.cs`, `backend/src/Berynx.Api/Receivables/M11Endpoints.cs`, `backend/src/Berynx.Domain/Invoicing/M10Rules.cs`, `backend/src/Berynx.Domain/Receivables/M11Rules.cs`, `backend/src/Berynx.Domain/Accounting/M12LedgerMapping.cs`, `frontend/src/app/features/invoicing/m10.page.ts` and `.spec.ts`, `frontend/src/app/features/receivables/m11.page.ts` and `.spec.ts`, `frontend/src/app/features/accounting/m12.page.ts` and `.spec.ts`, `backend/tests/Berynx.UnitTests/Invoicing/M10RulesTests.cs`, `backend/tests/Berynx.UnitTests/Receivables/M11RulesTests.cs`, and `backend/tests/Berynx.UnitTests/Accounting/M12RulesTests.cs`. Module notes and `docs/audits/coverage/` were refreshed.

**Verification:** .NET 10 Release solution build passed. Full NUnit solution run: 196 total, 172 passed, 0 failed, 24 skipped; the 24 PostgreSQL-backed tests remain PENDING because `BERYNX_TEST_POSTGRES` is unset. Angular production build passed with existing stylesheet-budget warnings; full Vitest suite passed 73/73 tests across 25 files. Angular feature coverage is 50.07% line / 28.41% branch. Per-module backend/frontend line and branch results are in `docs/audits/coverage/PHASE-1-COVERAGE-BASELINE.md` and its CSV; no module meets the 90%/90% gate. EF pending-model check reports no changes.

No migration was generated because this pass changes no EF model. Existing M10–M12/M19 migrations remain unapplied. No SQL ran and no PostgreSQL connection was made.

**Outstanding P0 work:** PostgreSQL authorization/tenant-scope and transaction verification for M10/M11/M12; invoice TCS/charge/billing-mode and reversal gaps; M11 credit/debit notes, cheque/refund/reconciliation flows; M12 reversals and financial statements. Continue with M08/M09/M18 evidence dependencies, then M17 and remaining modules. Keep the six DIF-P1 differentiators separate and unimplemented/unverified.

## UI/API contract checkpoint — M08–M12, 2026-11-27

**Status: PARTIAL; source wiring and PostgreSQL verification are tracked separately.** Added real Angular `HttpClient`-backed contract tests for the completed-trip M08 action and the M09 challan detail. The M08 test asserts the exact `POST /api/v1/m08/trips/{tripId}/actions` payload (action ID, optimistic version, delivered/returned quantities, null receiver evidence references), then verifies that the billing eligibility message and refreshed dispatch state are rendered. The M09 test verifies the initial `GET /api/v1/m09/challans` filter contract, detail request path, and rendered persisted detail response. ASP.NET authorization integration cases now include M08 dispatch/evidence-review and M09 challan routes; unauthenticated calls return 401 before handler execution.

**Current source/contract evidence:** M08 remains source-wired for dispatch, trip actions, evidence upload/review, and fail-closed scan state; M09 remains source-wired for filtered list/detail. The new HTTP tests validate client paths, method, payload/response handling; they do not prove authorized handler execution or persisted state. M10–M12 have prior Angular request/payload tests, with M11 receipt options and M12 active account options exercised through their current component contracts. Route guards and module access behavior are covered by the existing auth/entitlement guard suite.

**Current coverage (2026-11-27 run):** Full Angular/Vitest suite passed 83/83 tests in 27 files. Feature-code coverage is 51.27% line (2,086/4,068) and 28.73% branch (822/2,861). M08 frontend is 36.66% / 23.43%; M09 is 91.20% / 63.88%; M10 is 68.99% / 47.72%; M11 is 85.57% / 41.37%; M12 is 60.00% / 21.78%. None meets the combined 90% line/branch gate. Full .NET Release coverage run passed 208 total: 184 passed, 0 failed, 24 skipped. All 24 skips require `BERYNX_TEST_POSTGRES`; PostgreSQL is not installed here. Current backend module-source results: M10 8.87% / 26.90%, M11 13.41% / 17.86%, M12 11.13% / 11.90%, with shared EF persistence excluded from module attribution. Reports are under `artifacts/coverage/phase1-current/dotnet-ui-contract-pass/` and `frontend/coverage/berynx-construction360/coverage-summary.json`.

**Verification dimensions:** Angular UI contract tests: verified for the named M08/M09 actions, not every action in every module. API routing/authentication: unauthenticated route behavior verified; authorized business handlers and tenant/branch database scope remain unverified. Domain tests: existing and current unit suite passed, but functional completeness is partial. EF/LINQ translation: the M11 posted-allocation query has offline Npgsql `ToQueryString()` regression coverage; SQL translation is not query execution. PostgreSQL persistence, constraints, transactions, and migrations: PENDING, not verified. M18 scanner and private-storage source enforce quarantine/clean-only preview; ClamAV/provider availability and secure-storage operations still require configured deployment/provider verification. M18 has no dedicated Angular feature page; its delivery upload/review UI is M08.

**Next actionable task:** continue P0 M10/M11/M12 source gaps—TCS/charges/billing modes and authorized cancellation/reversal; receipt notes, cheque/PDC, refunds/reconciliation; accounting reversals/statements—while adding meaningful negative/state-transition tests. Then close the M08/M09/M18 evidence workflow around M18 configuration, authorized handler behavior, and scan-clean persistence. Do not claim UAT readiness or complete modules while these requirements and PostgreSQL evidence remain pending.

## Feature implementation checkpoint — M10 posted invoice cancellation, 2026-11-27

**Status: source implementation added; authorized handler and PostgreSQL persistence are not verified.** This increment implements the BR-M10-06 / US-M10-09 case where a posted invoice has no receipt allocations and no IRN. It is not an audit-only increment. Invoice posting/cancellation is still not a complete BRD workflow.

- **Requirement slice:** BR-M10-06 (posted-invoice immutability and controlled reversal), US-M10-09 (cancel with reason, reverse voucher, release challans). M12 shared reversal-link support was added to enable this M10 flow.
- **Production files:** `backend/src/Berynx.Api/Invoicing/M10Endpoints.cs`; `backend/src/Berynx.Domain/Invoicing/M10Rules.cs`, `M10Entities.cs`; `backend/src/Berynx.Domain/Accounting/M12Rules.cs`; `backend/src/Berynx.Application/Accounting/M12Contracts.cs`; `backend/src/Berynx.Infrastructure/Persistence/BerynxDbContext.cs`, `M12PostingEngine.cs`; `frontend/src/app/features/invoicing/m10.page.ts`.
- **Trace:** invoice detail “Cancel invoice and reverse voucher” → `POST /api/v1/m10/invoices/{invoiceId}/cancel` → `M10Endpoints.CancelPostedInvoice` (requires `m10.invoices.post`, branch scope, version and reason) → M10 cancellation guards and exact mirror lines from original M12 voucher → existing `IM12PostingEngine` in the same serializable transaction → reversal link, cancellation audit, challan reservation release, challan invoice-link clear and M06 invoiced-quantity restoration.
- **Fail-closed cases:** receipt allocations must first be unallocated; that workflow is not implemented. IRN-bearing invoices are blocked because portal cancellation acknowledgement is not implemented. Missing source voucher/lines or inconsistent challan/order quantity facts also block. No status, voucher or challan is fabricated as successful.
- **Persistence:** generated additive migration `20261127181300_P1M10PostedInvoiceReversal` adds `ReleasedAt`, an active-only unique challan reservation index, a unique one-reversal-per-voucher index and same-tenant self-FK. Manual SQL and guarded rollback are in `database/scripts/P1-M10/07-posted-invoice-reversal.sql` and `08-posted-invoice-reversal-rollback.sql`. Migration remains unapplied; no SQL was executed.
- **Tests run:** focused backend `M10RulesTests` + `M12RulesTests`: 24 passed, 0 failed. Full backend unit tests: 174/174 passed. Full solution test run: 212 total, 188 passed, 0 failed, 24 PostgreSQL-dependent tests skipped because `BERYNX_TEST_POSTGRES` is unset. Focused Angular page run: 6/6 passed; full Angular suite: 84/84 passed. Financial endpoint auth tests: 10/10 passed; the new cancellation route’s 401 case verifies only anonymous rejection, not authorized handler behavior. Angular contract test asserts POST path/body and simulates the success response/list/detail refresh.
- **Coverage measured after implementation:** M10 backend (unit-test source filter) 46/703 lines = **6.54%**, 93/336 branch outcomes = **27.68%**; M12 backend 59/664 = **8.89%**, 50/398 = **12.56%**. M10 frontend 101/147 lines = **68.70%**, 25/55 branches = **45.45%**; M12 frontend 93/155 = **60.00%**, 22/101 = **21.78%**. These full-module denominators are intentionally not narrowed to only the changed code; the 90% targets remain unmet.
- **Build/schema checks:** .NET Release solution build passed; Angular production build passed with pre-existing stylesheet-budget warnings; EF reports no pending model changes after the new migration. The migration SQL script was generated and reviewed against the manual deployment script. `git diff --check` passed after final documentation edits (with existing LF/CRLF normalization warnings). Initial concurrent focused builds timed out; the sequential rerun and final cached Release build passed.
- **Not verified:** authorized TestServer handler execution; PostgreSQL atomicity, serialization/concurrency, filtered index/FK behavior, tenant isolation, reversal-to-source totals and re-invoice after cancellation. These require a disposable PostgreSQL `_test` database, which is not installed/configured here.

**Next exact implementation step:** finish M10 invoice charge/billing-mode policy and TCS threshold calculation without inventing tax policy; first identify existing customer billing-mode and CA-configured tenant tax settings. Then implement the M11 receipt unallocation prerequisite and M10 IRN portal-cancellation acknowledgement boundary. Do not describe this cancellation slice as database-complete.
