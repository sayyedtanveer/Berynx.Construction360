# M01–M05 Implementation Audit

**Audit basis:** repository implementation inspected on 25-Nov-2026, BRD v2.2 (binding precedence noted in §15.3), current checked-in M01–M05 code, migrations, SQL artifacts and tests. This report distinguishes deployed evidence from repository evidence. It does not infer that a requirement is operational merely because it is specified.

## Executive summary

- **M01–M03:** owner reports M01, M02, M03 and SaaS Foundation SQL deployed. The repository contains onboarding, Identity/RBAC/MFA, M03 masters and SaaS entitlement/licensing paths, but no database inspection was performed. Deployment state is therefore **owner-reported, not independently verified**.
- **M04 CRM:** APIs and Angular flows exist for enquiry capture/list/detail/status, assignment, activity, follow-ups, analytics, SLA refresh and lost-enquiry reopening. M04 is **partially implemented**: API supports multiple product lines but the UI still captures one; Kanban drag/drop, one-tap contact actions, attachment upload, manager configuration screens, escalated notification delivery, offline retry and full analytics interactions are absent or incomplete. Lost enquiry reopening and a follow-up completion action are now surfaced. “Won” is correctly blocked until a genuine M06 order exists.
- **M05 Quotations:** server supports multi-line/charge documents, rate/tax snapshots, approval, revision/clone APIs, issue/public link, OTP-gated customer response, cancellation and print. Angular now supports multiple product lines with per-line RMC rate-card lookup, typed M03 charge rows, revision creation and quote cloning/repricing. Revision/clone change-diff/history UX, browser testing and complete print branding remain partial. OTP, email/WhatsApp/PDF delivery, customer alerts and M06 conversion are not evidenced as provider-backed.
- **Shared M01–M05:** migration `20261125092036_P1M04M05Foundation` and manual deployment SQL fragments exist. SQL verification and guarded rollback packages remain absent, so manual deployment should stay held pending review. No DB access, SQL execution or migration application occurred in this work.

## BRD v2.2 precedence and audit method

Section 15.3 makes v2.2 conflict resolutions and v2.2 epic specs take priority over retained v2.1 material; the retained Phase 1 M04/M05 specs remain applicable when not superseded. Section 16.1 requires tenant-scoped references, idempotency, audit, permissions, and explicit no-fabrication integration behavior. Section 16.2 includes global tenant-isolation, idempotency, revocation and user-flow gates. Section 24 release readiness and Section 12 Definition of Done require evidence beyond a compiling API/UI.

Status labels: **Implemented** means code path exists; **Partial** means meaningful code exists but the acceptance criterion is incomplete; **Blocked** means another module/provider/owner action is needed; **Not evidenced** means no verified implementation or execution evidence in this repository review. None of these labels represents production acceptance.

## M01–M05 status

| Module | Actual repository status | Deployment evidence | Major caveats |
|---|---|---|---|
| M01 Tenant/onboarding | Implemented code footprint present | Owner-reported deployed; not independently verified | Go-live wizard completeness, demo data and all acceptance evidence require separate audit/UAT |
| M02 Users/RBAC/approvals/MFA | Implemented code footprint includes tenant MFA policy/enforcement changes and tests | Owner-reported deployed; not independently verified | Provider delivery, audit/access negative tests and production security evidence remain gates |
| M03 Masters | Implemented code footprint includes customer/site/product/pricing/RMC/tax/charge masters | Owner-reported deployed; not independently verified | v2.2 tax/legal decision-table approval and completeness of all M03 ACs are not established by this M04/M05 audit |
| M04 CRM | Partial | M04 shares an unapplied local migration with M05 | Single-line UI, missing attachments/drag-drop/contact actions, scheduler dependency; no real linked M06 order |
| M05 Quotations | Partial | M05 shares an unapplied local migration with M04 | No provider delivery, incomplete revision/clone diff and branded PDF, no M06 order consumer, SQL deployment artifacts incomplete |

## M04 CRM traceability

| Requirement | Evidence/status |
|---|---|
| Quick capture with source/customer/contact/site/product/quantity/required-by/expected value/priority/next action | `M04Endpoints.Create`, `M04Page`; core present. UI requires product and follow-up; form is not the BRD ≤6-field/mobile/offline workflow. It sends one line only. |
| Open enquiry must have next action; decimal currency precision | Server validates `NextActionAt`, expected value non-negative and up to 2 decimals; schema check enforces open follow-up. |
| Idempotency and duplicate warning | Server create is idempotent and duplicate detection exists for complete keys; coverage depends on contact/site/product. |
| Assignment/rules/history/inactive reassignment | Server endpoints, audit and notification records exist. Rules/owner assignment maintenance and bulk dialog are not fully surfaced in UI. |
| Follow-up planner and outcome-required next action | Server list/completion/activity handling and UI buckets exist; UI now exposes a completion action and schedules a default next-day call. User-controlled next-action selection and one-tap calls/WhatsApp and notification cadence/delivery remain incomplete. |
| Pipeline and status lifecycle | Status state machine and list/board rendering exist; no drag/drop confirmation; UI excludes Won (by design) and now wires lost-record reopening with a future next action. |
| First-response SLA | Timestamps and idempotent in-app alert refresh endpoint exist; it requires an external scheduler invocation. No external message delivery evidence. |
| Analytics by source/owner/lost reason | Server grouping endpoint exists. UI does not provide full funnel totals, filters, click-through or robust analytics panel. |
| Won links to order and promotes contact | Won is explicitly rejected in API until M06 creates and links a real order. Promotion is not implemented and must remain blocked pending M06. |
| Consent and channel activity | M03 consent is checked before communications activity logging. M04 does not send messages. |
| Attachments/customer history/contact actions | Not complete: no M04 attachment workflow, one-tap contact action, full Customer 360 panel or offline retry queue demonstrated. |

## M05 Quotations traceability

| Requirement | Evidence/status |
|---|---|
| Server-resolved effective M03 price/RMC rate and tax snapshots | Implemented in `M05Endpoints.Build`; calculator uses decimal and separates CGST/SGST/IGST. Missing exact rates are rejected by M03 resolution. |
| Multi-line and charge quoting | API accepts up to 100 lines/charges; server calculator/tax snapshot persists them. Angular now supports multiple product rows, exact per-line RMC card lookup, M03 charge type selection and amounts; frontend tests cover multi-line plus charge submission. Live totals still come from server response/detail, not a client pricing calculator. |
| Approval for discounts/below-minimum pricing | M02 approval engine submission and issue gate exist. Cost status is honestly unavailable; do not claim margin guard based on actual cost facts. |
| Issue/customer public view/decision | Random public token hash and public view routes exist; actual OTP provider is unconfigured and correctly blocks recording customer decisions. Email/WhatsApp delivery is not implemented. |
| Validity/expiry | Issue rejects and marks a stale approved quote expired. Expiry automation/alerts are not evidenced. |
| Revision/clone | UI now calls revision flow with version and re-resolves current M03 prices; clone hydrates all source lines/charges and saves through server price resolution. Rate-change diff, archived revision navigation and explicit old/new line comparison remain incomplete. |
| Print/PDF | Authorized HTML print now includes tax split, charges, place of supply/GSTIN/site snapshots and writes print activity/audit records. Tenant-branded assets, generated PDF service and external send log are not proven. |
| Accepted quantity -> multiple orders | **Blocked by M06.** No M06 conversion or accepted quantity consumption is implemented here. |
| Credit hold/approval | M03 credit status snapshot is shown; transaction exposure/credit override behavior is not fully evidenced. |

## Shared safeguards

- M04/M05 routes use authenticated tenant groups, module entitlement checks, M02 permissions, branch/own scopes, tenant-owned entities and server-side validations.
- M05 public routes use token hashes, expiry/revocation, explicit public entitlement checks and OTP gating; provider success is not fabricated.
- M04 and M05 use audit/event records; M05 print now writes activity and audit records. Report/export audit coverage and a full negative cross-tenant API suite are not evidenced.
- Cost visibility is server response-filtered in M05 detail using `global.view_cost`; this change requires confirmation against export/print/background paths before claiming full BR-M02-05 / v2.2 GAT-04.
- Carbon & Copper styling remains in Angular page-local colors/layout; no broad visual redesign was made.
- Identity/RBAC and SaaS license architecture were preserved; no SaaS module catalogue or entitlement policy changes were made as part of M04/M05 continuation.

## Migration and manual SQL

Shared EF migration: `backend/src/Berynx.Infrastructure/Persistence/Migrations/20261125092036_P1M04M05Foundation.cs` and designer/snapshot. This migration includes M04 and M05 plus M03 price-basis/index changes. Deployment SQL fragments:

1. `database/scripts/P1-M04/01-schema.sql`
2. `database/scripts/P1-M04/02-alter-existing.sql`
3. `database/scripts/P1-M04/03-indexes-constraints.sql`
4. `database/scripts/P1-M05/01-schema.sql`
5. `database/scripts/P1-M05/02-alter-existing.sql`
6. `database/scripts/P1-M05/03-indexes-constraints.sql`

Both modules share one migration id; the fragments’ migration-history guards mean the owner must review execution and history behavior carefully as one deployment package. As of this audit, there is no dedicated read-only verification SQL or guarded rollback SQL in either folder. **Do not apply the package until those are generated/reviewed and prerequisites/history are checked.** The owner reports earlier M01–M03/SaaS SQL deployed; actual schema and migration history were not inspected. No connection, SQL execution or `dotnet ef database update` was run.

## Test/build status

The evidence of commands/results is recorded in `docs/audits/M01-M05-TEST-RESULTS.md`. Source tests are not exhaustive against every BR/AC and no browser/UAT, tenant-isolation integration run, provider delivery or migration-on-empty-database run is claimed.

## Release blockers

1. Generate and review idempotent read-only verification scripts and separately guarded rollback scripts for shared migration; reconcile a manual apply/history plan.
2. Expand M04 UI to multi-line enquiries, drag/drop confirmation, user-selected follow-up rescheduling, one-tap contact actions, attachments, assignment maintenance and analytics drill-down.
3. Complete M05 revision/clone diff and archived history navigation, expiry workflow, branded PDF and customer delivery/decision visibility; add regression tests for those actions.
4. Configure and verify OTP and communication providers before claiming customer acceptance or quote delivery; keep explicit unavailable states until then.
5. Implement M06 before enabling M04 Won/contact promotion and M05 accepted quantity conversion.
6. Run exhaustive unit/API/tenant-isolation/permission tests and real browser accessibility/mobile/UAT and migration verification on a separate disposable `_test` database after owner config.
7. Validate tax/place-of-supply decision tables with tenant CA as required by v2.2; no production/legal compliance approval is inferred.
