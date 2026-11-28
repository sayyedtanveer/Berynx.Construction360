# Phase 1 gap register

Updated: 2026-11-27. Source: `docs/requirements/BRD_v2.2.md` and current authoritative local checkout. The application is **PARTIAL / NOT UAT-READY**. This register does not treat build success as acceptance completion.

| ID | Priority | Gap | Status / evidence | Required next action |
|---|---|---|---|---|
| P1-ANG-001 | P0 | M07/M10/M12 frontend compilation defects | RESOLVED; production build passes. M07 page navigation, M10 inline template, M12 backend-reported totals repaired. | Keep pagination and financial-total tests in CI. |
| P1-TEST-001 | P1 | Full persistence and M01–M19 golden path | UNVERIFIED; 23 database-dependent tests skipped; no authorized disposable `_test` database configured. | Configure a disposable test DB and run persistence/API golden-path tests; never point tests to development DB. |
| P1-DB-001 | P1 | M06–M17 EF/schema deployment alignment | PARTIAL; additive `20261126104710_P1M06M17OperationalFoundation` adds 52 tables/124 indexes and model snapshot is current. Existing per-module SQL schema fragments overlap it. | Reconcile and approve one schema source; compare migration history and SQL against owner-controlled target before execution. |
| P1-DB-002 | P1 | M18/M19 database model and scripts | MISSING; no EF entities/migrations or module script packages in checkout. | Implement according to BRD before creating migrations and deployment SQL. |
| P1-M17-001 | P1 | Campaign/audience/template/message-wallet workflows | MISSING/PARTIAL; current M17 covers catalogue and consent management only. | Implement consent-safe audience selection, approved templates, quiet hours/frequency caps, metering, delivery logs and failure/retry behavior; provider-backed delivery remains separately verified. |
| P1-M17-002 | P1 | Public catalogue distribution and request-quote | PARTIAL; backend public catalogue read is rate-limited; no Angular public page, PDF/QR, CAPTCHA quote form, enquiry creation or owner notification flow. | Implement the BRD public page/form with tenant entitlement, privacy, CAPTCHA and M04 source/consent linkage. |
| P1-M18-001 | P1 | Cross-cutting import/search/notifications/documents/help/settings | PARTIAL; secure M18 evidence storage/scanning endpoints, persistence and deployment package exist. | Implement remaining BR-M18/US-M18 platform services: import/undo, scoped global search, notification preferences/channels, tenant export, versioned templates, help and settings catalogue; verify attachments on disposable PostgreSQL. |
| P1-M19-001 | P1 | Quality samples/results/disputes | PARTIAL; M19 APIs/entities/UI, idempotent sample/dispute create, configured-grade pass/fail, M18 clean evidence links, M10 invoice holds, audit and M19 migration exist. | Complete site/project criteria, heat/lot certificate traceability, scheduled reminders, dispute value/drill-down, actual replacement/M11 credit-note linkage; run disposable-PostgreSQL acceptance. |
| P1-FLOW-001 | P1 | M10/M11 financial posting dependency | BLOCKED; invoice/receipt flows are fail-closed without verified eligible source quantities and accounting posting. | Verify M08/M09/M10/M11/M12 transaction boundaries, eligibility, idempotency and reconciliation on disposable PostgreSQL. |
| P1-FLOW-002 | P1 | M07 supplier invoice/payment/stock lifecycle | PARTIAL; current M07 source lacks the full BRD procurement lifecycle. | Complete only the missing M07 contracts and persisted workflows before claiming PO→GRN→supplier invoice. |
| P1-AUDIT-001 | P1 | Module-by-module acceptance evidence | PARTIAL; implementation and route presence do not prove BRD acceptance. | Maintain `PHASE-1-REQUIREMENT-COVERAGE.md`; add API/service and DB-backed acceptance tests per story. |
| P2-UI-001 | P2 | Existing component style budgets | Warning only; build passes with style budget warnings in M06, M04, header, M02 and login components. | Reduce component styles or adjust approved budgets in a separate reviewed change; do not hide compile issues. |

## Module disposition

| Modules | Current disposition |
|---|---|
| M01–M05 | PARTIAL; source and UI/API surfaces exist, but persistence-backed onboarding/security/master/CRM/quotation acceptance is not verified in this run. |
| M06–M09 | PARTIAL; current code and new combined schema exist, but business workflows and persistence are unverified. |
| M10–M11 | BLOCKED for end-to-end financial posting; DB-dependent execution is pending. |
| M12–M16 | PARTIAL; reports/ledger/GST/dashboard/expense paths are not reconciled end to end against posted transaction data. |
| M17 | PARTIAL; endpoint, tenant manager UI and schema are present; most campaign and public quote acceptance is missing. |
| M18–M19 | MISSING. |

No live database access or SQL execution is authorized by this register. No Phase 2–5 feature is started.