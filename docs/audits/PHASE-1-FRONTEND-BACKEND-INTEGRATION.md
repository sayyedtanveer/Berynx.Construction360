# Phase 1 frontend–backend integration audit

Updated: 2026-11-27. Scope: authoritative main checkout and its existing dirty work plus this implementation checkpoint. No Git history operation, SQL execution, EF database update, or development-PostgreSQL access was performed.

## Executive result

**Phase 1 status: PARTIAL / NOT READY FOR UAT.** Latest verification: .NET 10 Release solution build passed; full NUnit solution run had 196 total, 172 passed, 0 failed, and 24 PostgreSQL-dependent tests skipped/PENDING; Angular production build passed with existing style-budget warnings; Vitest passed 73/73 tests; EF reports no pending model changes. Collected coverage is below the 90% line/branch gate for every module (see `docs/audits/coverage/PHASE-1-COVERAGE-BASELINE.md`). No PostgreSQL connection, SQL execution, or migration application occurred. Existing migration/SQL overlap and full golden-path verification remain unresolved.

M17 remains partial. M18 secure evidence storage is present but broader cross-cutting services remain incomplete. M19 now has a partial tenant-scoped API/domain/EF/UI workflow and unapplied migration/seed package; M11 credit-note resolution and several BR-M19 acceptance items remain blocked. No complete M01–M19 workflow was verified against persisted data; the golden path remains **BLOCKED / UNVERIFIED**.

## Screen-to-API and persistence matrix

The shared Angular transport is `ApiClient` (`frontend/src/app/core/api/api-client.service.ts`); module paths below resolve under `/api/v1`. Route and endpoint presence establishes wiring only—not successful persistence or acceptance coverage.

| Module | Angular screen / API request | Backend endpoint / business source | EF persistence / migration | Tests and limitations | Status |
|---|---|---|---|---|---|
| M01 Tenant & onboarding | `setup/*`; `ApiClient` onboarding/platform tenant paths | Onboarding, tenant provisioning and subscription endpoints | Tenant/onboarding records in existing foundation | API host authorization tests pass; provisioning persistence is among DB-dependent tests | PARTIAL |
| M02 Identity, RBAC, MFA, approvals | Login, MFA, users/roles/approvals; `AuthService` and `ApiClient` `/m02/*` | Registered M02 auth, identity, permission, approval and session endpoints | Identity plus M02 records; existing M02 migrations | Unit suite passes; DB tests for MFA/RBAC/security lifecycle skipped | PARTIAL |
| M03 Masters | `m03/*`; `/m03/*` products, parties, contacts, sites, pricing, fleet and tax | Registered M03 endpoints and master service | M03 master entities and prior migrations | No DB-backed CRUD/tenant-scope verification in this run | PARTIAL |
| M04 CRM & enquiries | `m04/enquiries`; `/m03/*` lookups and `/m04/*` enquiry/follow-up | Registered M03/M04 endpoints | Enquiry, assignment, activity and follow-up records | No persisted enquiry-to-quotation journey run | PARTIAL |
| M05 Quotations | `m05/quotations`; `/m05/*` quotation, approval, revision and conversion requests | Registered M05 endpoints | Quotation, lines, approval/revision entities in existing foundation | No DB-backed approval/conversion journey run | PARTIAL |
| M06 Sales orders | `m06/orders`; `/m06/*` list/detail/create/confirm/amend/schedule | Registered M06 endpoints, quotation handoff, credit service and approval integration | `m06_*` tables in migration `20261126104710_P1M06M17OperationalFoundation` | Backend build/tests pass globally; no M06 DB E2E; prior module SQL not reconciled | PARTIAL |
| M07 Procurement | `m07/procurement`; `/m07/*` PO/GRN and supplier workflows | Registered M07 endpoints; supplier invoice/payment/stock lifecycle remains incomplete | `m07_*` tables in combined migration | M07 pagination repair covered by Angular build; no persistence or purchase-to-GRN E2E | PARTIAL |
| M08 RMC/material delivery | `m08/dispatch`; `/m08/*` schedules, pours, trips and events | Registered M08 endpoints | `m08_*` tables in combined migration | No DB-backed scheduling/trip verification; receiver evidence is not proof of persisted delivery | PARTIAL |
| M09 Delivery challans | `m09/challans`; `/m09/*` list/detail/create/revision | Registered M09 endpoints | `m09_*` tables in combined migration | Challan/trip eligibility and numbering remain DB-unverified | PARTIAL |
| M10 Sales invoicing | `m10/invoices`; `/m10/*` drafts/details/actions | Registered M10 endpoints; list derives collection state from posted M11 allocations | `m10_*` tables in combined migration | Transaction-aware M12 post path exists; M18 verified-evidence prerequisite and database posting/tie-out remain pending | PARTIAL |
| M11 Receipts & collections | `m11/receipts`; `/m11/*` receipts, allocations, promises-to-pay, receipt options | Registered M11 endpoints; customer/branch choices use tenant-scoped sources | `m11_*` tables in combined migration | Cash/bank/UPI posting path uses M12; database scope, posting and balance reconciliation remain unverified | PARTIAL |
| M12 Accounting | `m12/accounting`; `/m12/*` accounts, vouchers, trial balance and mappings | Registered M12 endpoints and shared ledger source | `m12_*` tables in combined migration | Manual lines use active ledger choices; `TcsPayable` mapping role is available; reversals/statements/reconciliation remain incomplete or unverified | PARTIAL |
| M13 GST | `m13/gst`; `/m13/*` workspace, review and export | Registered M13 endpoints | `m13_*` tables in combined migration | GST source depends on posted invoices; filing/portal workflow not verified | PARTIAL |
| M14 Expenses & other income | `m14/expenses`; `/m14/*` categories, expenses and posting | Registered M14 endpoints | `m14_*` tables in combined migration | Expense approval/posting/reconciliation not DB-verified | PARTIAL |
| M15 Dashboard | `m15/dashboard` and home dashboard; `/m15/home` | Registered query-backed M15 endpoint using source transactions | No separate M15 transaction tables | No persisted KPI tie-out or drill-down E2E | PARTIAL |
| M16 Reports | `m16/reports`; `/m16/*` report/export requests | Registered M16 report endpoints | Source transactions and `m16_export_log` in combined migration | Report coverage, exports and source reconciliation remain incomplete/unverified | PARTIAL |
| M17 Promotion & catalogue | `m17/promotion`; `/m17/catalogue`, `/m17/consents`; M03 product/contact lookups | `MapM17` registered; M17 requires tenant entitlement and server-side permissions; public catalogue endpoint is rate-limited | `m17_catalogue_item`, `m17_consent_record`, `m17_promotion_event` in combined migration | 3 Angular page tests pass; no M17 API/DB test. Campaigns, approved templates, wallet, PDF/QR and public request-quote flow are missing | PARTIAL |
| M18 Cross-cutting platform services | Secure evidence service exists; broader M18 screens/services remain unverified | Secure evidence endpoints and existing M02 notification APIs | M18 secure-evidence migration/package exists | Import/search/notification preferences/export/templates/help/settings and DB access are incomplete | PARTIAL |
| M19 Quality samples & disputes | `m19/quality`; sample/test, criteria, dispute and report UI through `ApiClient` | `MapM19` at `/api/v1/m19`; module/permission/tenant/branch checks; M08/M09/M10/M18/M02 references | M19 criteria/sample/test/dispute/evidence/invoice/event tables in `20261127134458_P1M19QualityDisputes` | 5 M19 domain-rule tests and 3 Angular page tests; no DB-backed/API integration run. Credit-note, project criteria, traceability and dispute-value items remain incomplete | PARTIAL |

## Golden-path and integration findings

| Workflow | Evidence | Result |
|---|---|---|
| Tenant setup → employee login/MFA → permissions/licence | Routes, endpoints, guards and domain code exist; DB-dependent security/provisioning tests skipped | PARTIAL / UNVERIFIED |
| Customer/site → enquiry → quotation → approval → sales order | M03–M06 client/API source and source references exist; no persisted golden-path run | PARTIAL / UNVERIFIED |
| Sales order → PO → GRN → supplier invoice | M06/M07 source exists; downstream supplier invoice/payment/stock path is incomplete and DB-unverified | BLOCKED |
| Schedule → vehicle/driver → trip → challan | M08/M09 sources and contracts exist; no persisted eligibility, quantity or receiver-event verification | PARTIAL / UNVERIFIED |
| Delivered quantity → invoice → receipt allocation | M10/M11 source paths use verified challans/posted invoices and M12 posting; secure M18 evidence and database tests remain pending | PARTIAL / DB PENDING |
| Invoice/receipt/expense → journal → ledger → GST/reports | M10/M11 source posting uses shared M12 engine; no database-backed cross-module posting/reversal/reconciliation proof, and M13 depends on posted invoices | PARTIAL / DB PENDING |
| Actual transactions → dashboard/report drill-down | M15/M16 query code exists; no KPI tie-out or database drill-down run | PARTIAL / UNVERIFIED |
| Delivery/sample → test → dispute/credit-note resolution | M19 source now creates samples/results and failed-28-day disputes/holds; M11 credit-note records are not available and database path is unverified | PARTIAL / BLOCKED |

## Repairs and migration work in this pass

- Fixed M07 pagination by removing `Math` from template expressions and adding bounded page navigation/reload behavior; fixed M10 inline-template parsing; made M12 totals use backend-reported debit and credit values. The 2026-11-27 M10–M12 slice added allocation-derived invoice collection filters, tenant-scoped M11 receipt master-data choices, and active-account selection plus the missing TCS mapping role.
- Added M07 pagination/M12 totals tests and corrected stale frontend expectations for M02 context and M11 draft posting.
- Registered M17 in `Program.cs`; added a tenant/module/permission guarded catalogue and consent UI route and sidebar link; added tests for API loading, save/refresh, and API failure feedback.
- Added the M17 optional module and permission-catalogue seed without granting M17 entitlements to tenants.
- Added migration `20261126104710_P1M06M17OperationalFoundation` and generated a combined manual SQL package. It contains 52 new tables and 124 indexes; its Up has no alter/drop operations. Per-module schema scripts overlap this combined artifact and are explicitly not safe to layer until reconciled.
- M18/M19 have not been implemented. M17 does not meet the complete BRD campaign or public request-quote scope.

## Checks and limitations

- `dotnet build backend/Berynx.Construction360.slnx --configuration Release`: **passed**.
- `dotnet test backend/Berynx.Construction360.slnx --configuration Release --no-build`: **159 total; 136 passed; 0 failed; 23 skipped**. This is 132 unit tests and 4 DB-independent API-host tests passed. All 23 persistence-dependent tests were skipped because `BERYNX_TEST_POSTGRES` was unset. A dummy localhost:1 connection string was used where a startup/design-time setting was required; test configuration left `BERYNX_TEST_POSTGRES` unset, and no database connection was made.
- `npm.cmd run build`: **passed**. Angular reports component-style budget warnings in existing M06/M04/header/M02/login components; no compilation errors remain.
- `npm.cmd test`: **24 test files; 59 passed; 0 failed; 0 skipped**.
- `dotnet ef migrations has-pending-model-changes --project src/Berynx.Infrastructure --startup-project src/Berynx.Api`: **no pending model changes** after the new migration. The command was design-time only; no database was contacted.
- New migration inventory: `20261126104710_P1M06M17OperationalFoundation`; prior latest migration is `20261125092036_P1M04M05Foundation`. M18/M19 migrations are absent.
- SQL files were generated and statically reviewed only. PostgreSQL syntax, deployment, constraints against live data, and rollback behavior remain unverified because SQL was not run.
- Tenant isolation, RBAC, entitlements and branch/data-scope enforcement are present in parts of the codebase, but this pass does not claim full database/API proof for every screen.

## Deployment artifacts

- Combined schema package: `database/scripts/P1-M06-M17-FOUNDATION/README.md`, `01-apply.sql`, `05-verification.sql`, `99-rollback.sql`.
- M17 optional module seed and checks: `database/scripts/P1-M17/README.md`, `04-seed.sql`, `05-verification.sql`.
- Manual order and duplicate-script warning: `database/scripts/PHASE-1-DEPLOYMENT-ORDER.md`.

The database owner must reconcile any previously executed scripts, confirm the exact migration history, and manually authorize all SQL execution. No deployment or UAT readiness is claimed.