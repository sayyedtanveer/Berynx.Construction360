# Berynx Construction 360 — BRD v2.3 Differentiation Addendum

> Product owner approved all six DIF-P1 items for Phase 1 implementation on 2026-11-26. The BRD v2.2 baseline is unchanged; DIF-P2 through DIF-P5 are approved for planning only and are not authorized for implementation.

# Berynx Construction 360

BRD v2.3 | MVP Differentiation & Cross-Phase Product Alignment Addendum

Date: 26 November 2026    |    Status: Approved for Phase 1 scope    |    Baseline: Master BRD v2.2

Scope: Phase 1 (M01–M19) enhancements plus traceable evolution across Phases 2–5. This addendum does not replace or silently change v2.2 requirements.

## Executive decision

Product thesis: Berynx should be the easiest affordable Indian construction-material and, later, builder ERP that shows money at risk, explains operational exceptions, and enables an owner to act. A feature is not a differentiator by itself; the differentiator is joined-up transaction evidence, role-specific action guidance, and low implementation friction.

| Decision | Rule |
| --- | --- |
| MVP buyer | First target: RMC traders, steel/cement/sand suppliers and material distributors; full builder project operations arrive in Phase 3. |
| Phase 1 delta | Four P0 capabilities: Profit Leakage Control, Order 360, Owner Daily Briefing, Dispute-Proof Delivery. Two lightweight P1 capabilities: manual WhatsApp sharing and Guided Simple Mode. |
| Scope guard | No new parallel accounting, invoicing, dispatch, document, pricing or entitlement engines. Reuse existing modules and approved posting rules. |
| Commercial model | Low-cost modular SaaS is a hypothesis; prices/limits need customer validation and unit-economics approval, not a hardcoded BRD price. |
| Release gate | The product owner has approved DIF-P1-01 through DIF-P1-06 as Phase 1 scope; implementation, verification, database validation and UAT status are tracked separately. Database/UAT remain governed by v2.2 Section 12. |

## Product-owner approval record

Approved 2026-11-26: DIF-P1-01 Profit Leakage Control; DIF-P1-02 One-Click Order 360; DIF-P1-03 Owner Daily Briefing; DIF-P1-04 Dispute-Proof Delivery Reconciliation; DIF-P1-05 WhatsApp Manual Share / Link-First; and DIF-P1-06 Guided Simple Mode. The approved implementation order is documented in docs/implementation-plan.md. Approval is a scope decision only; it does not imply source-code implementation, test passage, database validation, pilot/UAT completion or release approval. Phase 2-5 differentiators are planning-only and must not be implemented in this assignment.

## 1. Governance, compatibility and traceability

- Naming: new requirements use DIF-P1-01 through DIF-P1-06; later enhancements use DIF-P2-*, DIF-P3-*, DIF-P4-* and DIF-P5-*. Original P1-MXX and E2/E3/E4/E5 IDs remain unchanged.

- Classification: DIF-P1-01 through DIF-P1-04 (P0) and DIF-P1-05 through DIF-P1-06 (P1) are product-owner-approved Phase 1 scope. Approval authorizes implementation, not a claim of completion or release readiness. DIF-P2 through DIF-P5 are approved for planning only and remain outside implementation scope.

- Existing code status is separate from requirement status: Proposed / Approved / Implemented / Tested / UAT Passed. Never label a BRD feature implemented merely because an endpoint or screen exists.

- System invariants: tenant/branch scope, module entitlement, RBAC, audit history, soft-delete/void/reversal, decimal precision, posted-document immutability, shared totals and no invented data.

- Cross-branch and client/site visibility always enforced at API and data-access layers; Angular route or UI hiding alone is insufficient.

- Data additions are additive, preserve older records and previously applied migrations; capture source IDs and event timestamps, and avoid rekeying documents across phases.

- Rollout: release features off by default except when the product owner explicitly approves a default; platform admin may provision entitlements, tenant admins only configure entitled features.

## 2. Phase 1 — detailed MVP requirements

### DIF-P1-01 — Profit Leakage Control

Priority: P0 | Traceability: M03, M05, M06, M07, M08, M10, M12, M15, M16

Business outcome: Show expected, committed and realized order/trip contribution margin with explainable variance causes: purchase price, freight, pump, waiting, rejection, short quantity, discount and unbilled charges.

- BR-01.1 Expected margin snapshots sales net of tax minus approved/known direct costs; missing cost is UNKNOWN, never zero.

- BR-01.2 At confirmation, preserve quote/order estimate. Realized uses actual eligible procurement, trip expense and posted billing data; avoid prematurely presenting estimate as booked profit.

- BR-01.3 Configurable threshold compares variance to baseline; alerting never silently blocks an approved workflow unless an existing guard already requires approval.

- BR-01.4 Each variance must include source document, timestamp, currency, quantity unit, owner and explainability; estimates clearly labeled.

- BR-01.5 Do not double count expenses posted by both purchasing and trip cost services. Rounding and source-of-truth follow BRD v2.2 financial rules.

User stories: US-01 Owner views expected vs latest estimated vs realized contribution margin on an order and filters critical variance reasons. US-02 Operations opens a loss-risk alert, reviews source PO/trip/challan and assigns a corrective action.

Acceptance criteria: AC-01 Costs absent on a test order show Unknown and exclude realized-margin claim. AC-02 Same freight charge linked twice is counted once; cross-tenant requests return no data. AC-03 Example revised freight of ₹2,000 reduces current estimated margin by ₹2,000 (subject to GST-exclusive basis).

### DIF-P1-02 — One-Click Order 360

Priority: P0 | Traceability: M04–M12, M15, M16, M18

Business outcome: A single read-only command-center view of an order: enquiry/quotation, approvals, sales order, PO/GRN, schedules/trips, challans, invoices/credits, payments, disputes and margin.

- BR-02.1 Use document references and source IDs; no manual duplicate relationship tables unless required by data-model review.

- BR-02.2 A missing downstream document displays Not started, not Done.

- BR-02.3 Permissions mask financial margins, contacts and sensitive records per role and branch.

- BR-02.4 Status is computed from authoritative domain state and never manually persisted in a conflicting aggregate.

- BR-02.5 Navigation supports back-to-order with existing filters preserved.

User stories: US-03 Owner reviews an order timeline and opens every evidence document. US-04 Sales/ops spots blockers and follows permitted actions.

Acceptance criteria: AC-04 A partial order with one delivered trip shows partial delivery and invoice eligibility only for proven quantities. AC-05 A salesperson without margin permission never receives underlying cost values from the API. AC-06 Links point to actual documents and handle cancelled or void documents transparently.

### DIF-P1-03 — Owner Daily Briefing

Priority: P0 | Traceability: M02, M06–M08, M10–M12, M15, M16, M18

Business outcome: Role-aware daily action queue summarizing overdue receivables, credit-risk orders, loss-risk orders, unverified deliveries, unbilled challans and approvals.

- BR-03.1 Every KPI is computed from the same transactional queries as operations and reports; timezone/branch/date boundaries explicit.

- BR-03.2 Every tile drills down to filtered authoritative source records.

- BR-03.3 Unknown, pending and zero values remain distinct; do not display false green states.

- BR-03.4 No AI-generated recommendation, prediction or external messaging is required in Phase 1.

- BR-03.5 Action ordering may use fixed, explainable severity rules; no opaque scoring without user agreement.

User stories: US-05 Owner sees an actionable daily snapshot in under two minutes. US-06 Manager opens an exception and assigns an accountable owner when supported.

Acceptance criteria: AC-07 Overdue count matches receivables report for same tenant/branch/date filters. AC-08 User without approval scope sees no approval-action button. AC-09 Source transactions changing trigger correct briefing recalculation.

### DIF-P1-04 — Dispute-Proof Delivery Reconciliation

Priority: P0 | Traceability: M06, M07, M08, M09, M10, M11, M19

Business outcome: Reconcile ordered, planned, loaded, dispatched, arrived, accepted, rejected, returned, challaned and invoiced quantities with evidence and explanations.

- BR-04.1 Each quantity has an explicit UOM and conversion basis; support steel weight and RMC volume per existing BRD.

- BR-04.2 Delivered/accepted quantities are sourced from verified delivery evidence and legal business transitions; exceptions remain visible.

- BR-04.3 Prevent duplicate trip-to-challan mapping and invoice quantity in excess of eligible accepted quantity.

- BR-04.4 Corrections use existing reversal/adjustment authorization; never silently edit a posted challan/invoice.

- BR-04.5 Exceptions require owner, reason, evidence reference and timestamps; missing evidence is Unverified.

- BR-04.6 M19 dispute record links back to quantities and documents; no advanced Phase 4 NCR engine in MVP.

User stories: US-07 Dispatcher identifies load-vs-acceptance discrepancies. US-08 Accounts sees quantity eligible for invoice; unresolved dispute is flagged.

Acceptance criteria: AC-10 For 100 MT ordered, 96 MT dispatched and 94.5 MT accepted, view separately reports 4 MT not dispatched and 1.5 MT dispatched-not-accepted; do not label all 5.5 MT as physically lost. AC-11 A repeated delivery completion event cannot double-count accepted quantity. AC-12 Unverified acceptance cannot be represented as verified proof.

### DIF-P1-05 — WhatsApp Manual Share / Link-First

Priority: P1 | Traceability: M05, M09, M10, M11, M17, M18

Business outcome: Offer Share via WhatsApp actions that open a wa.me URL with text plus a secure link, with user-controlled sending and no dependency on Meta Business Manager.

- BR-05.1 Manual share uses wa.me/click-to-chat; ERP can log Share opened, not Sent or Delivered.

- BR-05.2 Customer phone validated/normalized; prefilled text escaped and optional; do not expose secrets or private PDFs in public URLs.

- BR-05.3 Share links are time-limited/revocable or authenticated, using existing document security design.

- BR-05.4 Marketing mass messages, scheduled reminders, delivery receipts and automated sending are OUT of scope without an approved official API provider.

- BR-05.5 Consent/legal messaging constraints from v2.2 remain; opt-out honored for marketing.

User stories: US-09 Sales opens a customer-addressed WhatsApp quotation share. US-10 Accounts opens an invoice/statement share without claiming it was delivered.

Acceptance criteria: AC-13 No Meta token/account is required for manual share. AC-14 Audit shows opened only; no false delivered tick. AC-15 Expired document link blocks unauthorized download.

### DIF-P1-06 — Guided Simple Mode

Priority: P1 | Traceability: M01, M03–M11, M18

Business outcome: A role-aware guided experience for nontechnical staff, using existing forms/steps while retaining Advanced Mode for authorized users.

- BR-06.1 Simple Mode is an alternate UI, not a separate business workflow or hidden bypass of validations.

- BR-06.2 Show required next step, available action, missing prerequisite and contextual help.

- BR-06.3 No shortcut bypasses credit approval, receipt verification, GST or financial posting controls.

- BR-06.4 Preserve user progress and accessibility; mobile-first for field roles.

User stories: US-11 New staff completes enquiry→quotation with contextual guidance. US-12 Admin chooses guided/default mode per role.

Acceptance criteria: AC-16 Guided form and advanced form create equivalent backend documents. AC-17 Validation errors identify missing information without claiming save succeeded.

## 3. Shared model and API contract strategy

| Capability | Recommended contract / design |
| --- | --- |
| Profit insights | Read model keyed by tenantId, orderId, asOf; amountBasis ex-GST; cost completeness flag; sourceReferences; known/unknown breakdown; immutable confirmed baseline. |
| Order 360 | GET /api/sales-orders/{id}/overview (illustrative; reuse existing route conventions); links only where permitted; customer/site and status from existing aggregates. |
| Daily briefing | GET /api/management/daily-briefing?date=&branchId= (illustrative); server-computed counts, severity, drilldown references and lastRefresh timestamp. |
| Delivery reconciliation | Per-order/trip quantity read model built on existing schedule, trip, challan and invoice IDs; explicit UOM, verification source and discrepancy classifications. |
| WhatsApp | Client composes encoded wa.me URL from server-issued secure document link; no server-side send or status webhook in Phase 1. |
| Simple mode | User preference/role setting referencing existing screens and API contracts; no secondary master data stores. |

API paths above are proposed contracts, not claims about implemented endpoints. Final endpoint naming must follow existing server conventions after code audit.

## 4. Phase 2 — Operational and commercial depth

Preserve original E2-01 through E2-10. The following additions extend those epics without changing their baseline meaning.

| New ID | Existing epic | Approved-direction enhancement / measurable outcome |
| --- | --- | --- |
| DIF-P2-01 | E2-01 Inventory and stores | Material variance ledger: reconciles GRN, stock movements, site issues, consumption and physical counts; alerts on unexplained leakage. |
| DIF-P2-02 | E2-02 Procurement depth | Supplier reliability score based on on-time, accepted quantity, documented quality outcomes and price variance; explainable, no black box. |
| DIF-P2-03 | E2-03 Commercial controls | Customer rate-change and margin impact simulation using approved cost/price data; safeguards against outdated rates. |
| DIF-P2-04 | E2-04 Financial control / E2-05 Analytics | Collections risk worklist from overdue age, promised payment and observed receipts; rule-based first, probability only after governance. |
| DIF-P2-05 | E2-09 Subscription billing | Module-based plans, tenant usage and self-service upgrade/downgrade rules; no hidden entitlement bypass. |
| DIF-P2-06 | E2-10 Languages | Hindi/Marathi/Gujarati labels and validation messages for guided field workflows; currency and document records unchanged. |

Maintain E2-06 GST 2B, E2-07 fixed assets and E2-08 TDS credit reconciliation unchanged; preserve their source-ledger ties.

## 5. Phase 3 — Builder Projects & Profitability Cockpit

This is where Berynx becomes compelling for a builder managing construction projects. The existing E3-01 to E3-07 project, BOQ, WBS, execution, contractors, measurement/RA billing and variation epics remain authoritative.

| New ID | Existing epic | Builder-visible value |
| --- | --- | --- |
| DIF-P3-01 | E3-01 Projects & sites | Project 360: project customer/site, BOQ, budget, work packages, committed POs, deliveries, bills, receivables/payables, evidence and risks in one project workspace. |
| DIF-P3-02 | E3-02 Estimation & BOQ / E3-03 WBS | Budget-versus-actual-versus-committed cockpit per cost code, BOQ item and project; approved baseline, forecast-at-completion and variance reasons. |
| DIF-P3-03 | E3-04 Site execution | Site daily pulse: DPR progress, delivered/consumed materials, workforce and blocked activities, with mobile evidence and accountability. |
| DIF-P3-04 | E3-05 Contractors & work orders / E3-06 RA billing | Contractor certification vs issued quantities, retention, measurement evidence, pending RA bill and cost-to-complete. |
| DIF-P3-05 | E3-07 Variations | Change-impact preview before approval: cost, time, impacted work packages and downstream billing commitments. |

Critical integration rule: a Phase 1 customer/site/order may link to a Phase 3 project, but a project never overwrites its original tenant, branch or transaction owner. Cost-code mappings must be versioned and auditable.

## 6. Phase 4 — Evidence, quality and project risk

| New ID | Existing epic | Differentiation |
| --- | --- | --- |
| DIF-P4-01 | E4-01 Drawings / E4-02 RFIs | Decision and evidence chain: link revision, RFI response, material impact, variation and project task with permission-aware history. |
| DIF-P4-02 | E4-03 Issues / E4-04 Quality | Project exception cockpit linking NCR, delivery dispute, material lot/test evidence and closure status without duplicate incident records. |
| DIF-P4-03 | E4-05 Safety / E4-06 Equipment | Cost/schedule impact of incidents, unavailable equipment and downtime with sourced evidence; not a predictive claim. |
| DIF-P4-04 | E4-07 Punch list & closeout | Handover completeness index: outstanding snags, as-built docs, approvals, warranties and evidence trail. |

## 7. Phase 5 — AI, field automation and integrations

| New ID | Existing epic | Scope and guardrails |
| --- | --- | --- |
| DIF-P5-01 | E5-05 Analytics and AI | Hindi/Hinglish query assistant for authorized financial, delivery and project status; read-only first, cite source documents and date, return unknown when data missing. |
| DIF-P5-02 | E5-05 Analytics and AI | Explainable margin/payment risk forecasting with training/evaluation, tenant isolation, confidence and opt-out; never imply certainty. |
| DIF-P5-03 | E5-02 Fleet GPS / E5-01 RMC batching | Trip ETA, plant actual-versus-plan and quality deviations tied to existing order/dispatch evidence. |
| DIF-P5-04 | E5-03 Integrations | Official WhatsApp Cloud API as OPTIONAL tenant-connected provider (templates, webhooks, opt-in, quotas, charges, credentials), plus existing planned GST/payment integrations. |
| DIF-P5-05 | E5-04 Full offline | Resilient field workflows with idempotency, conflict resolution, device security and explicit sync status; no fabricated live state. |

The WhatsApp official API could be prioritized earlier commercially via separate approval, but it is not an implicit Phase 1 dependency; wa.me/manual sharing remains sufficient for the initial pilot.

## 8. Commercial packaging and pricing hypotheses

| Plan concept | Target | Included modules / principles |
| --- | --- | --- |
| Starter | Small supplier | CRM, quotation, sales order and basic billing; strict usage and support limits. |
| Business | Growing trader | Purchase, delivery, collections, accounting/reporting as authorized; add-on capacity for extra users/locations. |
| RMC Pro | RMC trader | Scheduling, trips, challans, margin exceptions, evidence reconciliation. |
| Builder 360 | Builder from Phase 3 | Projects, BOQ, WBS, site execution, contractor management, RA billing and project cockpit. |

Illustrative earlier price ideas (₹699 / ₹1,499 / ₹2,999 / ₹4,999+ per month) are experiments, NOT approved tariffs. Validate cost-to-serve, onboarding effort, messaging/provider fees, support, gross margin, GST and plan conversion before publishing prices. Prefer feature limits and fair usage over deliberately crippled safety or accounting.

## 9. Cross-phase synchronization rules

- Single master ID chain: tenant/branch → customer/site → enquiry/quotation/order → procurement → trip/challan → invoice/payment/journal/GST; Phase 3 adds project/work package/BOQ/cost code references, not a duplicate flow.

- Keep phase-specific read models derived from actual transactions. A material transaction may have both supplier-side and project/site-side classifications, but cannot post twice.

- Pricing/cost definitions: expected margin, committed cost, realized contribution, project budget variance and cash collection are distinct metrics; the UI labels every metric with formula, time scope and completeness.

- Data provenance: attach originating document IDs, version, timestamp, actor, tenant and evidence references; do not modify posted financial documents.

- All features behind explicit entitlements and role/branch permissions, with server-side authorization and module availability independent from navigation.

- Tenant data/WhatsApp provider credentials strictly isolated. External integrations report Pending/Failed/Delivered only when confirmed by the relevant system.

- Use effective-dated configuration for taxes, threshold values and fee assumptions; do not hardcode Indian compliance values into BRD enhancement logic.

- Backward-compatible additive DB migrations; do not run combined generated migration SQL plus overlapping per-module schema scripts against one database.

- Preserve Angular 22, .NET 10, PostgreSQL and Carbon & Copper compact UX; do not redesign Phase 1 just to add a cockpit.

- No auto-deploy, database mutation, Git commit/push/clean/reset during agent implementation without explicit authorization.

## 10. Testing and release gates

| Gate | Evidence required |
| --- | --- |
| Phase 1 code acceptance | Every approved DIF-P1 AC tested; backend build and Angular build pass; no known P0 unimplemented stories; existing M01–M19 AC coverage reconciled. |
| Phase 1 data validation | EF migrations and SQL path reviewed; disposable PostgreSQL database tests pass; financial tie-outs and tenant isolation verified; 23 previous skipped persistence tests resolved or explicitly documented. |
| Phase 1 pilot UAT | Realistic RMC/material trader golden path plus new Order 360, margin and delivery discrepancy scenarios; owner completes daily briefing; no false invoice/proof records. |
| Phase 2 gate | Stock-to-GL cutover and procurement/credit reports reconciled; subscription billing safely activated. |
| Phase 3 gate | Project BOQ budget, consumption, progress and contractor RA bill reconcile by cost code, site and company ledger. |
| Phase 4 gate | Every quality/safety/document issue traces to closure and source evidence without loss of audit lineage. |
| Phase 5 gate | API/AI accuracy, permissions, consent, cost metering, offline conflict and failure-mode tests pass. |

## 11. Implementation sequence and change-control

- Step 0: Product owner approved all six DIF-P1 requirements on 2026-11-26; retain this scope and do not silently expand the Phase 1 boundary.

- Step 1: Finish baseline M01–M19 BRD v2.2 blockers, especially M17–M19, M10/M11 posting, migrations, integration testing.

- Step 2: Implement approved P0 DIF-P1-02 Order 360 + DIF-P1-04 delivery reconciliation as foundational read models; reuse true source IDs.

- Step 3: Add DIF-P1-01 margin variance and DIF-P1-03 owner daily briefing on top of trusted data; do not do predictions in P1.

- Step 4: Add optional P1 DIF-P1-05 manual share and DIF-P1-06 guided mode when no release-critical blocker remains.

- Step 5: Verify all ACs and run phase gate. Only then enable an approved next phase.

- Agent deliverables: file changes, endpoint/UI matrix, M01–M19 and DIF-P1 coverage, tests counts, migrations, SQL deployment order, unresolved gaps and no unsupported COMPLETE labels.

## 12. Open product decisions requiring sign-off

| ID | Decision | Recommended default |
| --- | --- | --- |
| OD-01 | Are the four P0 differentiators mandatory before MVP launch? | Treat as proposed; select feature freeze date before committing. |
| OD-02 | What constitutes realized profit? | Net-of-GST contribution after attributable actual direct costs, NOT net accounting profit. |
| OD-03 | Should loss alerts block orders? | No; advisory unless covered by an existing approval rule. |
| OD-04 | Are unverified deliveries billable? | Follow v2.2 eligibility; never auto-approve missing receiver proof. |
| OD-05 | WhatsApp sharing link TTL and viewer authorization? | Short-lived and revocable, per document security policy. |
| OD-06 | Starter plan exact seats, branches, storage and API limits? | Pilot-test and approve before tenant billing. |
| OD-07 | Data thresholds and field definitions for briefing? | Tenant-configurable and documented. |
| OD-08 | When is Builder 360 commercially offered? | Only after Phase 3 project-side golden-path gate. |

## Source and status notes

Source baseline: Berynx_Construction_360_Master_BRD_v2_2_Complete.docx, particularly Section 1 build protocol, Sections 4–8 Phase 1 domain/platform/modules, Section 12 Definition of Done and gates, and Section 14 / E2–E5 phase scope. Original section and module requirements are retained rather than restated in full.

DIF-P1-01 through DIF-P1-06 are product-owner-approved Phase 1 requirements; they are additive to, and do not change, BRD v2.2. Their approval is not a claim that the current code implements them. DIF-P2 through DIF-P5 are approved for planning only. Existing M01–M19 implementation status remains governed by the project audits.
