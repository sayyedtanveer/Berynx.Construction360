# Phase 1 BRD requirement coverage

Updated: 2026-11-27. BRD v2.2 baseline: **172 M01–M19 user-story identifiers** and **162 M01–M19 business-rule identifiers**. The six v2.3 DIF-P1 requirements are approved scope and tracked separately below; they are not included in the v2.2 identifier counts. The matrix records current implementation disposition, not acceptance evidence; no module is marked COMPLETE without persisted golden-path/UAT verification.

| Module | BRD stories | Rules | Coverage disposition | Evidence / outstanding blocker |
|---|---|---|---|---|
| M01 Tenant & onboarding | US-M01-01..08 | BR-M01-01..05 | PARTIAL | Setup/API code exists; provisioning DB tests skipped. |
| M02 Identity, RBAC, MFA, approvals | US-M02-01..08 | BR-M02-01..05 | PARTIAL | Unit/API code exists; DB-dependent MFA/RBAC/session tests skipped. |
| M03 Masters | US-M03-01..13 | BR-M03-01..10 | PARTIAL | Master UI/API and EF model exist; tenant/branch DB acceptance not run. |
| M04 CRM & enquiries | US-M04-01..08 | BR-M04-01..06 | PARTIAL | Enquiry UI/API exist; persisted enquiry→quotation journey unverified. |
| M05 Quotations | US-M05-01..09 | BR-M05-01..07 | PARTIAL | Quotation UI/API and approval source exist; conversion E2E unverified. |
| M06 Sales orders | US-M06-01..08 | BR-M06-01..07 | PARTIAL | Route/API/entity and migration now present; credit, amendment, fulfillment and quotation conversion not DB-verified. |
| M07 Procurement | US-M07-01..11 | BR-M07-01..12 | PARTIAL | PO/GRN code and schema present; supplier invoice/payment/stock path incomplete. |
| M08 RMC/material delivery | US-M08-01..13 | BR-M08-01..16 | PARTIAL / M18 BLOCKED | Authorized no-OTP evidence review, rejection/resubmission, audit and M09/M10 persisted-fact gates implemented; secure M18 upload/scan/preview provider and several other M08 stories remain incomplete. |
| M09 Delivery challans | US-M09-01..05 | BR-M09-01..05 | PARTIAL | M08 review/resubmission synchronizes status and evidence snapshot transactionally; M10 revalidates matching trip/challan facts. Correction/worklist gaps and PostgreSQL acceptance remain open. |
| M10 Sales invoicing | US-M10-01..13 | BR-M10-01..15 | PARTIAL / M18 BLOCKED | Atomic M12-backed invoice posting and persisted M08/M09 fact revalidation implemented; live new-delivery flow awaits secure M18 evidence provider; charge, TCS, cancellation and DB tie-outs remain open. |
| M11 Receipts & collections | US-M11-01..15 | BR-M11-01..14 | PARTIAL | Cash/bank/UPI receipt allocation and M12 posting implemented with idempotency and remaining-balance checks; cheque, adjustment, refund, notes and DB reconciliation remain open. |
| M12 Accounting | US-M12-01..10 | BR-M12-01..11 | PARTIAL | Effective-dated tenant ledger mappings, account CRUD controls and transaction-aware M10/M11 source posting are implemented; statements/reversals and DB-backed reconciliation remain unverified. |
| M13 GST | US-M13-01..08 | BR-M13-01..10 | PARTIAL | GST APIs and schema present; posted-source and filing/export reconciliation unverified. |
| M14 Expenses & other income | US-M14-01..09 | BR-M14-01..11 | PARTIAL | Expense UI/API and schema present; approval and ledger posting not DB-verified. |
| M15 Dashboard | US-M15-01..06 | BR-M15-01..04 | PARTIAL | Query-backed UI/API exists; KPI tie-out and drill-down acceptance unverified. |
| M16 Reports | US-M16-01..07 | BR-M16-01..06 | PARTIAL | Report/export source and export log exist; coverage and source reconciliation incomplete. |
| M17 Promotion & catalogue | US-M17-01..08 | BR-M17-01..07 | PARTIAL | Catalogue/consent manager, API endpoints and EF tables exist; campaigns, provider templates/wallet, PDF/QR and public quote journey missing. |
| M18 Cross-cutting platform services | US-M18-01..08 | BR-M18-01..06 | PARTIAL | M18 secure document storage/scanning/access source and deployment package exist; import/undo, global search, notification preferences, tenant export, versioned templates, help/settings and full attachment coverage remain open. |
| M19 Quality samples & disputes | US-M19-01..05 | BR-M19-01..05 | PARTIAL | Tenant grade criteria, samples/tests, trip/order/site links, M18 clean report references, dispute workflow/history, M10 posted-invoice holds, role-based in-app manager notice and plant/customer counts exist. Project/site-specific criteria, lot/heat certificates, scheduler-backed reminders, dispute value/drill-down, verified replacement/M11 credit note and PostgreSQL acceptance remain open. |

## Verification boundary

- Frontend/API contracts were code-audited; only the Angular production build and unit suite are verified in this pass.
- Backend Release build and in-memory/unit tests pass as recorded in `PHASE-1-TEST-READINESS.md`.
- EF model is synchronized to the new migration, but PostgreSQL deployment and schema/constraint behavior remain unverified.
- The combined migration supersedes no existing migration and is not executed. Existing individual module SQL schemas overlap it; follow `database/scripts/PHASE-1-DEPLOYMENT-ORDER.md` before any owner-run deployment.
- The 23 persistence-dependent NUnit tests remain PENDING until an explicitly configured disposable PostgreSQL database ending in `_test` is authorized.

**Overall acceptance: PARTIAL / NOT UAT-READY.**

## Automated coverage gate

Collected .NET line/branch and Angular feature line/branch results are recorded per module in [`coverage/PHASE-1-COVERAGE-BASELINE.md`](coverage/PHASE-1-COVERAGE-BASELINE.md) and [`coverage/M01-M19-COVERAGE-BASELINE.csv`](coverage/M01-M19-COVERAGE-BASELINE.csv). No module meets the requested 90% line-and-branch threshold. Coverage is test-execution evidence only; it does not replace BRD acceptance or the pending PostgreSQL-backed tenant, persistence, posting, and cross-module checks.
## Approved v2.3 differentiator traceability (implementation status tracked separately)

The v2.2 module counts and statuses above remain the baseline. The product owner approved all six additive DIF-P1 requirements on 2026-11-26; their local story labels are disambiguated below by DIF ID. Approval is not implementation, test, database-validation or UAT evidence. Phase 2-5 differentiators remain planning-only.

| Addendum requirement | Priority | Proposed stories / acceptance criteria | Existing v2.2 traceability | New behavior to verify (without duplicating baseline) | Status |
|---|---|---|---|---|---|
| DIF-P1-01 Profit Leakage Control | P0 | US-01..02; AC-01..03; BR-01.1..1.5 | M03, M05-M08, M10, M12, M15-M16 | Explain expected/committed/estimated/realized contribution from existing approved costs and posted billing; unknown is not zero; source costs counted once; advisory variance alerts. | APPROVED / IMPLEMENTATION NOT VERIFIED |
| DIF-P1-02 One-Click Order 360 | P0 | US-03..04; AC-04..06; BR-02.1..2.5 | M04-M12, M15-M16, M18 | Read-only order projection with real document links, computed statuses, scope-aware redaction, and Not started state for absent documents. | APPROVED / IMPLEMENTATION NOT VERIFIED |
| DIF-P1-03 Owner Daily Briefing | P0 | US-05..06; AC-07..09; BR-03.1..3.5 | M02, M06-M08, M10-M12, M15-M16, M18 | Shared KPI sources, explicit branch/time boundaries, source drill-down, permission-aware actions and separate zero/unknown/pending states; no AI requirement. | APPROVED / IMPLEMENTATION NOT VERIFIED |
| DIF-P1-04 Dispute-Proof Delivery Reconciliation | P0 | US-07..08; AC-10..12; BR-04.1..4.6 | M06-M11, M19 | Reconcile planned/dispatched/accepted/rejected/returned/challaned/invoiced quantities with UOM, verified evidence and duplicate-event protection; keep exceptions visible. | APPROVED / IMPLEMENTATION NOT VERIFIED |
| DIF-P1-05 WhatsApp Manual Share / Link-First | P1 | US-09..10; AC-13..15; BR-05.1..5.5 | M05, M09-M11, M17-M18 | User-launched wa.me text + secure expiring/revocable document link; audit Share opened only. No provider token, automatic send, scheduled marketing, or delivery claim. | APPROVED / IMPLEMENTATION NOT VERIFIED |
| DIF-P1-06 Guided Simple Mode | P1 | US-11..12; AC-16..17; BR-06.1..6.4 | M01, M03-M11, M18 | Role-aware guided UI over the same forms/API/rules, preserves progress and accessibility, and cannot bypass validations or approvals. | APPROVED / IMPLEMENTATION NOT VERIFIED |

## v2.3 future-scope crosswalk (not Phase 1 acceptance)

Original E2/E3/E4/E5 epic meanings and IDs remain those in BRD v2.2. The roadmap is approved for planning only; no Phase 2-5 implementation is authorized by this approval.

| Phase | Addendum IDs | Existing epic links | Additive requirement focus | Status |
|---|---|---|---|---|
| 2 | DIF-P2-01 | E2-01 Inventory and stores | Reconcile GRN, stock movements, site issues, consumption and counts; explain leakage. | FUTURE / NOT IMPLEMENTED |
| 2 | DIF-P2-02 | E2-02 Procurement depth | Explainable supplier reliability from on-time, accepted quantity, quality outcomes and price variance. | FUTURE / NOT IMPLEMENTED |
| 2 | DIF-P2-03 | E2-03 Commercial controls | Simulate customer rate changes and margin impact from approved cost/price data. | FUTURE / NOT IMPLEMENTED |
| 2 | DIF-P2-04 | E2-04 Financial control; E2-05 Management analytics | Rule-based collections risk worklist; probability only after governance. | FUTURE / NOT IMPLEMENTED |
| 2 | DIF-P2-05 | E2-09 Subscription billing | Module plans, usage and safe self-service plan changes; no entitlement bypass. | FUTURE / NOT IMPLEMENTED |
| 2 | DIF-P2-06 | E2-10 Languages and localization | Hindi/Marathi/Gujarati guided workflow labels and validation messages; preserve currency/document records. | FUTURE / NOT IMPLEMENTED |
| 2 | No new DIF ID | E2-06 GSTR-2B; E2-07 Fixed assets; E2-08 TDS reconciliation | Existing v2.2 scope is retained unchanged. | BASELINE FUTURE SCOPE |
| 3 | DIF-P3-01 | E3-01 Projects and sites | Project 360 over existing transaction/project references. | FUTURE / NOT IMPLEMENTED |
| 3 | DIF-P3-02 | E3-02 Estimation/BOQ; E3-03 WBS/schedule/budget | Budget vs actual vs committed by cost code, approved baseline, forecast and variance reasons. | FUTURE / NOT IMPLEMENTED |
| 3 | DIF-P3-03 | E3-04 Site execution | Site daily pulse with progress, materials, workforce, blockers and evidence. | FUTURE / NOT IMPLEMENTED |
| 3 | DIF-P3-04 | E3-05 Contractors/work orders; E3-06 Measurement/RA billing | Contractor certification, retention, measurement evidence, pending RA bills and cost-to-complete. | FUTURE / NOT IMPLEMENTED |
| 3 | DIF-P3-05 | E3-07 Variations | Pre-approval cost, time, work-package and downstream billing impact. | FUTURE / NOT IMPLEMENTED |
| 4 | DIF-P4-01 | E4-01 Drawings; E4-02 RFI/submittals | Permission-aware decision and evidence chain. | FUTURE / NOT IMPLEMENTED |
| 4 | DIF-P4-02 | E4-03 Issues; E4-04 Quality | Exception cockpit links NCR, delivery disputes, lot/test evidence and closure without duplicate incidents. | FUTURE / NOT IMPLEMENTED |
| 4 | DIF-P4-03 | E4-05 Safety; E4-06 Equipment | Sourced cost/schedule impacts for incidents, downtime and equipment availability. | FUTURE / NOT IMPLEMENTED |
| 4 | DIF-P4-04 | E4-07 Punch list/closeout | Handover completeness across snags, as-builts, approvals, warranties and evidence. | FUTURE / NOT IMPLEMENTED |
| 5 | DIF-P5-01, DIF-P5-02 | E5-05 Analytics and AI | Authorized, read-only cited assistant; governed, explainable forecasting with uncertainty and opt-out. | FUTURE / NOT IMPLEMENTED |
| 5 | DIF-P5-03 | E5-01 Batching; E5-02 Fleet/GPS | ETA, plant actual-vs-plan and quality deviations grounded in source evidence. | FUTURE / NOT IMPLEMENTED |
| 5 | DIF-P5-04 | E5-03 Integrations | Optional official WhatsApp Cloud API and existing GST/payment integrations; tenant opt-in, consent, provider and quota safeguards. | FUTURE / NOT IMPLEMENTED |
| 5 | DIF-P5-05 | E5-04 Full offline field operations | Idempotent sync, conflict resolution, device security and explicit sync state. | FUTURE / NOT IMPLEMENTED |

**Coverage boundary:** The DIF-P1 approval is a product scope decision only. This crosswalk is requirements traceability only. It does not assert source-code coverage, test coverage, UAT or release approval. The v2.3 Phase 2–5 additions are outcome-level enhancements, not replacement epic specifications or complete new acceptance-criterion sets; v2.2 epic stories and gates remain authoritative, and further decomposition requires product-owner approval. Map each approved DIF ID to executable acceptance tests during implementation; retain v2.2 phase gates and additive migration rules.

## Workflow implementation status refresh — 2026-11-26

The rows above reflect code status, not approval or release readiness. The authorized no-OTP supervisor review and persisted M09/M10 verification gates now exist in source. The workflow remains **blocked on the M18 secure attachment provider**: verification refuses to proceed without tenant-bound, clean-scan evidence and a short-lived preview link; no arbitrary URL/file reference is accepted. See `docs/modules/P1-M08.md` through `P1-M12.md` for implementation boundaries, SQL paths, checks, and remaining requirements. Database-backed tests remain pending; UAT is not claimed.
