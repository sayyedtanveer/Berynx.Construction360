# Phase 1 test readiness

Updated: 2026-11-27. Readiness is **PARTIAL**. No database-backed acceptance is claimed.

## Toolchain

| Tool | Verified version/status |
|---|---|
| .NET SDK | 10.0.404 |
| Node.js | v26.13.1 |
| `npm.cmd` | 11.20.0; repository `packageManager`/Angular workspace also reports npm 11.6.2 in the lock/toolchain metadata |
| Angular / CLI | 22.3.0 |
| TypeScript | 6.0.3 |
| Vitest | 5.1.1 |

Use `npm.cmd` in PowerShell because the execution policy blocks `npm.ps1`.

## Phase 1 release-readiness checklist

| Gate | Current status | Evidence / next action |
|---|---|---|
| Product-owner scope approval | PASS - scope only | DIF-P1-01..06 approved 2026-11-26; DIF-P2..P5 planning only. |
| BRD v2.2 blockers (M10/M11/M12/M17/M18/M19 and integrations) | BLOCKED / IN PROGRESS | Reconcile source workflows, migration parity and acceptance evidence; see requirement coverage and module guides. |
| DIF-P1 implementation | NOT VERIFIED | Approval does not establish implementation. Deliver in the approved order and map every AC to tests. |
| .NET / Angular checks | PASS (rerun 2026-11-27) | .NET Release build, full available NUnit suite, Angular production build, and Vitest passed; details below. |
| Coverage gate | FAIL / NOT MET | Per-module backend/frontend line and branch measurements are below 90%; see `coverage/PHASE-1-COVERAGE-BASELINE.md`. |
| EF pending-model check | PASS (rerun 2026-11-27) | No pending model changes; design-time context used a non-routable localhost:1 placeholder and did not connect to PostgreSQL. |
| Disposable PostgreSQL integration / financial tie-outs | PENDING | Requires an explicitly authorized disposable localhost database ending in _test; do not contact development DB. |
| Manual SQL / migration execution | NOT RUN | Owner-run only; no SQL or migration execution. |
| Pilot/UAT and release approval | BLOCKED / NOT READY | Requires database-backed golden-path and differentiator evidence; do not claim UAT readiness. |
| Phase 2-5 implementation | OUT OF SCOPE | Planning only; do not begin later phases. |

## Verification run 2026-11-27

| Check | Result | Notes |
|---|---|---|
| `dotnet build Berynx.Construction360.slnx --configuration Release --no-restore` (from `backend/`) | PASS | All backend projects compile under .NET 10.0.404. |
| `dotnet test Berynx.Construction360.slnx --configuration Release --collect:'XPlat Code Coverage' --settings coverage.runsettings` (from `backend/`) | PASS: 196 total; 172 passed; 0 failed; 24 skipped | 168 unit tests and 4 DB-independent API-host tests passed. The 24 persistence-dependent tests require `BERYNX_TEST_POSTGRES` and skipped because it was unset. No development DB was contacted. |
| `npm.cmd run build` (from `frontend/`) | PASS | Angular production build succeeds. Existing component-style budget warnings remain for M06, M04, header, M02, and login. |
| `npm.cmd test -- --coverage --coverage-include=src/app/features/**/*.ts --coverage-exclude=src/**/*.spec.ts --coverage-reporters=json-summary --coverage-reporters=text-summary` (from `frontend/`) | PASS: 25 test files; 73 passed; 0 failed; 0 skipped | Angular feature coverage: 50.07% line, 28.41% branch overall; per-module results are in the coverage report. |
| `dotnet ef migrations has-pending-model-changes --project src/Berynx.Infrastructure --startup-project src/Berynx.Api --configuration Release --no-build` (from `backend/`) | PASS: no pending model changes | Design-time check used a dummy localhost:1 connection setting; the EF command did not connect to PostgreSQL. |
| SQL execution / migration update | NOT RUN | Explicitly prohibited; no SQL or database update command was executed. |
| `git diff --check` | PASS | Tracked diff whitespace check passed. Untracked documentation files were also checked for trailing whitespace. |

## Pending / unverified

- All 24 database-backed NUnit integration tests are **PENDING**, not passed. Required configuration: `BERYNX_TEST_POSTGRES` must point only to an explicitly authorized, disposable localhost PostgreSQL database whose name ends in `_test`. It was unset in this run. Do not use the development database.
- Full tenant onboarding, licensing, quotation→order, procurement, dispatch/challan, invoicing, receipt allocation, accounting, GST, dashboard/report and M19 quality/dispute golden paths remain unverified against persisted data.
- PostgreSQL-generated SQL has only been generated and statically reviewed. Manual execution, migration-history reconciliation, index/constraint behavior, rollback, isolation policies and live data checks are pending owner approval.
- Angular lint/type-check beyond the production build is not claimed. The build emitted only component style-budget warnings noted above.
- Integration-test result does not establish provider-backed email/SMS/WhatsApp delivery, UAT, or production approval.

## Migration inventory boundary

Latest pre-existing migration: `20261125092036_P1M04M05Foundation`.

New additive migration: `20261126104710_P1M06M17OperationalFoundation` (52 tables, 124 indexes; current model snapshot synchronized). M18 storage migration `20261127105316_P1M18SecureEvidenceStorage` and schema are now generated but unapplied; M19 migration/schema remain outstanding. See `database/scripts/P1-M06-M17-FOUNDATION/README.md` and `database/scripts/PHASE-1-DEPLOYMENT-ORDER.md` for the manual package boundary and overlap warning.

## Critical-chain implementation verification refresh — 2026-11-26

This refresh supersedes the older historical counts above. Current checks on the implementation work:

| Check | Result | Notes |
|---|---|---|
| `dotnet build backend/Berynx.Construction360.slnx --configuration Release` | PASS | All backend projects built with no warnings. |
| `dotnet test backend/Berynx.Construction360.slnx --configuration Release --no-build` | PASS: 171 total; 148 passed; 0 failed; 23 skipped | 144 unit tests and 27 integration test cases discovered; all 23 requiring `BERYNX_TEST_POSTGRES` are **PENDING** because no disposable `_test` database was configured. Compared with the prior 159-case baseline, 12 cases were added. |
| `npm.cmd run build` (from `frontend/`) | PASS | Angular production build; existing unrelated stylesheet-budget warnings remain. |
| `npm.cmd test` | PASS: 24 files; 60 passed; 0 failed | Includes M12 chart-account API wiring and M11 receipt-post action coverage. |
| EF pending-model check | PASS | `dotnet ef migrations has-pending-model-changes` reports no model changes after migration `20261126151858_P1M12TenantLedgerMappingsAndPostingLinks`; design-time only, no DB connection. |
| `git diff --check` | PASS | No whitespace errors; Git emitted only existing line-ending normalization warnings. |
| SQL deployment / PostgreSQL tie-outs | NOT RUN / PENDING | `database/scripts/P1-M12/05-ledger-mapping-posting-deploy.sql` is generated only. The owner must review and execute manually after prerequisites; no database was contacted. |

**Release status remains BLOCKED / NOT READY.** M08 has no secure evidence review/OTP endpoint to transition a trip to persisted `Verified`; the M09/M10 chain therefore correctly fails closed. Passing builds and unit/UI tests are not evidence of financial posting against PostgreSQL, tenant-row-policy behavior, or UAT.

### Critical-chain migration inventory update

Latest additive migration is `20261127081534_P1M08ReceiverEvidenceReview` (M08 persisted decision reason, reviewer identity and tenant-bound reviewer FK). The preceding M12 migration `20261126151858_P1M12TenantLedgerMappingsAndPostingLinks` supplies mapping/audit tables, M09 verification snapshot, and M10/M11 voucher links. EF model snapshot is synchronized (`has-pending-model-changes`: none). M08 SQL is `database/scripts/P1-M08/05-receiver-evidence-review-deploy.sql`; guarded rollback is `database/scripts/P1-M08/06-receiver-evidence-review-rollback.sql`. Apply only after the required M06/M17 foundation and M12 package; owner-run scripts only, neither was executed.

## M08 no-OTP evidence review implementation verification — 2026-11-27

| Check | Result | Notes |
|---|---|---|
| .NET Release build | PASS | `dotnet build backend/Berynx.Construction360.slnx --configuration Release`; all solution projects compiled. |
| Backend NUnit | PASS with pending DB tests | 180 total; 156 passed; 0 failed; 24 skipped. 152 unit tests passed. Four database-independent integration/API-host tests passed; 24 PostgreSQL integration tests are **PENDING** because `BERYNX_TEST_POSTGRES` was not configured. |
| Angular production build | PASS | `npm.cmd run build`; existing component-style budget warnings remain. |
| Angular unit tests | PASS | 24 test files; 62 passed; 0 failed. Includes M08 missing-evidence non-billable completion and review UI tests, plus M10 eligible-challan refresh. |
| EF pending-model check | PASS | `dotnet ef migrations has-pending-model-changes`; no model changes since `20261127081534_P1M08ReceiverEvidenceReview`. Design-time used a dummy localhost:1 connection setting; no PostgreSQL connection was made. |
| `git diff --check` | PASS | No whitespace errors. Git emitted existing LF/CRLF normalization warnings. |
| SQL/migration execution | NOT RUN | Migration and SQL packages were generated only; owner execution is not authorized in this task. |

At this prior readiness checkpoint, M08 had the no-OTP review lifecycle but M18 secure storage was not yet implemented. The following M18 refresh supersedes that provider blocker: source integration is now present, while migration application, live ClamAV, PostgreSQL isolation/concurrency, end-to-end billing tie-outs, and UAT remain unverified. Do not claim UAT readiness.
## M18 secure evidence implementation refresh — 2026-11-27

This refresh supersedes earlier M08/M18 blocker wording. M18 source now provides private filesystem storage, ClamAV `clamd` scanning, quarantined retryable uploads, authenticated/audited preview and download, version history with out-of-order scans prevented from replacing a newer current version, and M08 review-provider integration. M09/M10 remain fail-closed until persisted clean evidence is supervisor-reviewed. **Source implementation is not deployment or database-backed acceptance.**

| Check | Result | Notes |
|---|---|---|
| `dotnet build backend/Berynx.Construction360.slnx --configuration Release` | PASS | All backend projects compiled. |
| `dotnet test backend/Berynx.Construction360.slnx --configuration Release` | PASS: 186 total; 162 passed; 0 failed; 24 skipped | 158 unit tests and 4 database-independent integration/API-host tests passed. The 24 PostgreSQL-backed tests were skipped because `BERYNX_TEST_POSTGRES` was removed for this safe run. New tests cover file policy/signatures, scope binding, private filesystem bounds, ClamAV clean/infected protocol, and scanner outage. |
| `npm.cmd run build` (from `frontend/`) | PASS | Angular production build; existing component stylesheet-budget warnings remain. |
| `npm.cmd test` | PASS: 24 files; 66 passed; 0 failed | Includes M08 file validation/upload and M10 eligibility UI tests. |
| `dotnet ef migrations has-pending-model-changes --project src/Berynx.Infrastructure --startup-project src/Berynx.Api --context BerynxDbContext` | PASS: no pending model changes | Design-time check used an unreachable localhost:1 `_test` connection string; it made no database connection. |
| `git diff --check` | PASS | No whitespace errors in the final source/documentation diff. |
| Migration/SQL application | NOT RUN | New M18 migration and manual SQL were generated only; development PostgreSQL was not contacted. |

### M18 deployment and acceptance gates

- Configure a real `clamd` endpoint and private storage path. A local ClamAV daemon was not installed or verified; scanner protocol unit tests used a local fake daemon and do not constitute malware-engine validation.
- Review and manually apply `database/scripts/P1-M18/01-m18-secure-evidence-deploy.sql` only after reconciling prerequisites. Run its verification script under owner-controlled database procedure.
- PostgreSQL FK/index/tenant isolation, concurrent replacement, transactional audit behavior, and complete M08 upload → clean scan → supervisor decision → M09 challan → M10 invoice eligibility remain **PENDING** until an explicitly authorized disposable `_test` database is used.
- No UAT, production storage/key-protection readiness, or end-to-end database verification is claimed. No OTP/SMS dependency was introduced.
## Verification run 2026-11-27

| Check | Result | Notes |
|---|---|---|
| `dotnet build backend/Berynx.Construction360.slnx --configuration Release` | PASS | All backend projects built successfully. |
| `dotnet test backend/Berynx.Construction360.slnx --configuration Release --no-build` | PASS: 192 total; 168 passed; 0 failed; 24 skipped | The skipped integration tests require `BERYNX_TEST_POSTGRES` pointing at an explicitly authorized disposable local database ending in `_test`; they remain PENDING. No database was contacted. |
| `npm.cmd run build` (from `frontend/`) | PASS | Angular production bundle generated; existing M06/header/M04/M02/login style budget warnings remain. |
| `npm.cmd test` | PASS: 25 test files; 69 passed; 0 failed | Includes 3 M19 UI tests. |
| `dotnet ef migrations has-pending-model-changes --project src/Berynx.Infrastructure --startup-project src/Berynx.Api` | PASS: no pending model changes | Design-time placeholder used; no PostgreSQL connection. |
| SQL/migration execution and PostgreSQL end-to-end | NOT RUN / PENDING | Explicitly prohibited in this task; SQL package is unapplied. |

## UI/API contract and coverage refresh — 2026-11-27

This refresh records the latest no-PostgreSQL source/contract verification. It does not replace the historical entries above.

| Check | Result | Notes |
|---|---|---|
| `dotnet build Berynx.Construction360.slnx --configuration Release --no-restore` | PASS | All backend projects compile under .NET 10. |
| Full .NET Release solution tests with coverage | PASS: 208 total; 184 passed; 0 failed; 24 skipped | The 24 tests requiring `BERYNX_TEST_POSTGRES` remain PENDING; PostgreSQL is not installed locally. Coverage outputs: `artifacts/coverage/phase1-current/dotnet-ui-contract-pass/`. |
| `npm.cmd run build` | PASS | Angular production bundle generated. Existing component-style budget warnings remain for header, M06, M04, M02, and login. |
| Full Angular/Vitest suite | PASS: 27 files; 83 passed; 0 failed | Includes real Angular `HttpClient` request/response tests for M08 action/upload and M09 challan detail. |
| Angular feature coverage | PASS: instrumentation completed; gate NOT MET | 51.27% line (2,086/4,068), 28.73% branch (822/2,861); per-module updates are in `coverage/PHASE-1-UI-CONTRACT-PASS-2026-11-27.md`. |
| M08/M09 ASP.NET route authentication tests | PASS: 9 cases in focused class | Unauthenticated requests return 401 before endpoint handlers. No authorized persistence handler was exercised. |
| M11 Npgsql query translation/model-shape tests | PASS: 3 tests | `ToQueryString()` checks joins, grouping, tenant/customer/branch filters; checks EF indexes/relationships. Does not execute SQL. |
| EF pending-model check | PASS: no pending model changes | Design-time only with a non-routable localhost:1 connection setting; no PostgreSQL connection. |
| PostgreSQL integration and financial persistence verification | PENDING | Not available locally; no development DB was contacted, no SQL executed, and no migration applied. |

These checks prove only the stated source, route, request/response, rendering, and offline translation contracts. They do not establish full module completeness, PostgreSQL runtime behavior, scanner/provider readiness, or UAT. All six DIF-P1 features remain tracked separately and are not declared complete.
