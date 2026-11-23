# Berynx Construction 360 — Master BRD v2.1 (consolidated, Phase 1 modules verified)

Status of this file - Sections 0–8 and modules P1-M01 to P1-M09: from the uploaded v2.0 file, with the M03, M07 and M08 patches inserted at their module positions. - Modules P1-M10 to P1-M19: from the excerpt file, with the patches inline. - Sections 9, 10, 12, 13 and 14: new in this version. Phase 2–5 user stories are not yet written; they follow in phase chunks.

Berynx Construction 360 — Master BRD v2.0 (Build-Ready Edition) Reviewed and rewritten from “Phase-Wise Master BRD / Roadmap v1” by a construction-domain, product and UX review. First pilot: HAFA INFRA. Target market: Indian construction-material trading, RMC supply and related construction businesses. Item \| Detail Product \| Berynx Construction 360 (multi-tenant SaaS, India-first) Document version \| 2.0 — supersedes v1 roadmap Purpose \| Single source of truth that a delivery team or AI coding agent can follow phase by phase, module by module, without asking for clarification Guiding principle \| ENTER DATA ONCE → USE IT EVERYWHERE Tech baseline (from v1, unchanged) \| ASP.NET Core Web API, PostgreSQL + EF Core, Angular + TypeScript, JWT, modular monolith, DDD/CQRS where useful

0.  How to read and use this document Part 1 (Sections 1–4) explains the build protocol, the gaps found in v1, the domain primer and the Indian compliance baseline. Part 2 (Sections 5–7) defines personas, UX principles, screen patterns and the platform services every module depends on. Part 3 (Section 8) is Phase 1 in full detail: every module with UX flow, screens, fields, business rules, status lifecycle, user stories and acceptance criteria. Part 4 (Sections 9–10) gives the data model, calculations and non-functional requirements. Part 5 (Section 14 and the Phase 2–5 chunks) covers Phases 2–5 with user stories, then the build plan, assumptions, open questions and risks. ID scheme: modules P1-M01; stories US--nn; business rules BR--nn; screens SCR--nn. Every story has acceptance criteria (AC) written as testable statements.

1.  AI / developer build protocol (read before writing any code) These rules exist so the build can run phase by phase without interruption. Scope gate. Build only the phase you are told to build. Within a phase build modules in the order listed. Do not create menu items, screens or tables for later phases. Create extension points only (for example nullable project_id columns, domain events, empty permission codes) where this document names them. Module loop. For each module, in this order: (a) entities + migrations, (b) domain rules and status machine, (c) application commands/queries, (d) REST API + OpenAPI, (e) Angular screens following Section 6 patterns, (f) automated tests for every AC, (g) seed/demo data, (h) update /docs/modules/.md. A module is Done only when the Definition of Done in Section 12 is met. No stopping to ask. When something is ambiguous, apply the default in Section 13 (Assumptions) and record the decision in /docs/decisions.md (date, module, decision, reason). Stop only at a phase gate (Section 12) to demonstrate and get sign-off. Single source of truth. Reports and dashboards read the same transactions as operational screens. No duplicated business logic in the UI; rules live in domain/application layers. Tenant first. Every tenant-owned table carries tenant_id; every query is tenant-filtered by a central mechanism (global query filter + row-level checks), never by per-screen code. No company name, GSTIN, prefix, bank detail, terms or tax rate may be hardcoded. Money and quantity. Money is numeric(18,2); rates numeric(18,4); quantities numeric(18,3). Rounding rules are in Section 10. Never use floating point. No hard deletes of business records. Master data is deactivated; transactions are cancelled/voided/reversed with reason, user and timestamp. Posted financial records are immutable. Every transaction has a lifecycle (status, who changed it, when, why) and shows its linked documents (upstream and downstream) on its detail screen. Everything audited. Create/update/status-change/approval/print/export/login events write to the audit log with before/after values (Section 7.6). UX is a requirement, not polish. Every screen follows the patterns in Section 6, including empty, loading, error and permission-denied states, and is usable on mobile where marked.

2.  Review of v1 — gaps found and how v2 fixes them v1 is a strong roadmap (vision, phases, lifecycle chain, architecture intent). It is not yet a build specification. The gaps below were found by reviewing it against Indian construction-trading practice, SaaS product norms and UX standards. \# \| Gap in v1 \| Why it matters \| Fixed in G01 \| Modules are lists of nouns; no user stories, acceptance criteria, field lists or validations \| A developer or AI must guess behaviour \| Section 8, 11 G02 \| No status machines for quotation, order, delivery, invoice, PO \| Inconsistent states, broken reports \| Section 8 per module, 10.4 G03 \| No UX flows, screen inventory, navigation or design system \| Software becomes feature-heavy but hard to use \| Sections 5–6, 8 G04 \| e-invoice and e-way bill pushed to Phase 5 \| Goods movement and B2B invoicing above thresholds legally need them; trading customers will refuse a product that cannot produce them \| 4, 8 (M10, M13), Phase 1 captures all data and exports portal-ready JSON; API automation stays Phase 5 G05 \| No SaaS commercial layer: super-admin console, plans, subscriptions, tenant provisioning, usage limits \| “SaaS-ready” is claimed but a tenant cannot be created, billed or supported \| 7.1, 8 (M01) G06 \| No onboarding, data migration or opening balances \| A real client cannot go live without opening ledgers, outstanding bills and masters \| 8 (M01, M18) G07 \| No financial year (April–March), period lock or FY-wise numbering \| Invoice numbers must be unique per FY and max 16 characters (GST Rule 46); books need closing \| 7.3, 8 (M12) G08 \| “Accounting” listed without chart of accounts, voucher types, posting rules \| Ledgers and P&L cannot be trusted \| 8 (M12), 10.2 G09 \| No audit-trail compliance \| Accounting software used by companies must keep an edit log that cannot be disabled \| 4, 7.6 G10 \| No multi-GSTIN, branch or plant model; no place-of-supply logic \| CGST/SGST vs IGST depends on it; larger dealers have several GSTINs \| 8 (M01, M10), 10.3 G11 \| TDS/TCS not covered \| Contractors, purchases and receivables are affected \| 4, 8 (M07, M11) G12 \| RMC specifics missing: slump, mix/additives, cube tests, part load, returned/wasted concrete, waiting time, pump type, minimum quantity, time windows, cancellation \| RMC is time-critical; a generic order form fails on the first real day \| 8 (M08) G13 \| Steel/TMT/cement specifics missing: weight-based billing (MT), bundles, weighbridge, theoretical vs actual weight, brand/size/grade variants \| Trading margin hides in weight and unit conversions \| 8 (M03, M06, M08) G14 \| Credit limit and overdue blocking left to Phase 2 \| Cash-flow risk is the biggest risk in construction trading; needed on day one \| 8 (M03, M06) G15 \| Approval workflow “configured” but never defined \| Discounts, credit overrides, PO and expense approvals need rules \| 7.2 G16 \| No pricing model: price lists, customer-specific rates, validity, margin guard \| Quotation accuracy and profitability \| 8 (M03, M05) G17 \| No returns, rejection, cancellation or short-supply handling \| Real deliveries are rejected or partially supplied \| 8 (M08, M09, M11) G18 \| No vehicle, driver and TM masters; no driver/site mobile flow; no proof of delivery \| Dispatch cannot be tracked or disputed without evidence \| 8 (M08) G19 \| Notifications and campaigns lack channel rules, templates, consent and regulatory constraints \| SMS needs DLT registration; WhatsApp needs approved templates; marketing needs consent (DPDP Act) \| 7.5, 8 (M17) G20 \| No permission matrix; no data-level scoping (own records, branch) \| Salespeople should not see everyone’s margins \| 5.3, 7.2 G21 \| Non-functional requirements absent: performance, backup RPO/RTO, security, observability, data residency \| Production readiness \| 10 G22 \| Reports named but not specified (filters, columns, drill-down) \| Different developers produce different reports \| 8 (M16) G23 \| No localisation: INR, lakh/crore formatting, amount in words, Indian date format, language options \| Expected by Indian users \| 6.6 G24 \| No Tally export \| Most Indian SMEs and their accountants still run Tally; without export, adoption stalls \| 8 (M12, M18) G25 \| Advance payments, on-account receipts, cheque/PDC handling, bank reconciliation not covered \| Daily reality of construction collections \| 8 (M11), 11 G26 \| MSME 45-day payable tracking absent \| Late payments to MSME suppliers can lose tax deductions (Sec. 43B(h)) \| 8 (M07, M12) G27 \| Inventory deferred entirely to Phase 2 without a stated rule for Phase 1 \| Steel/cement dealers often hold godown stock \| 13 (A-04) G28 \| No test strategy, seed data, environments, CI/CD, definition of done \| Cannot verify completion \| 12 G29 \| Dashboard KPIs named but not defined \| KPIs must be computed identically everywhere \| 8 (M15), 10.5 G30 \| No in-app help, onboarding tour or guided empty states \| Non-technical construction staff need guidance \| 6.5, 8 (M01) G31 \| Mobile approach undefined \| Drivers, engineers and site staff work on phones \| 6.4 G32 \| Customer/supplier data model has no GST-verification, KYC, contact-person or multiple-address design \| Compliance and CRM quality \| 8 (M03)

3.  Domain primer (for the builder who is not a construction expert) The business in one paragraph. A construction-material trader buys from manufacturers or plants (RMC plants, steel mills/dealers, cement companies, sand/aggregate suppliers) and sells to builders, developers and contractors for specific project sites. The trader’s margin is the gap between purchase rate and selling rate, minus transport, pumping and other costs. Money is tight: customers pay late, suppliers want quick payment, so credit control, outstanding tracking and per-order profitability are the heart of the product. Term \| Meaning RMC (ready-mix concrete) \| Concrete mixed at a batching plant and delivered wet in transit mixers; perishable, so time windows matter Grade (M20, M25, M30…) \| Concrete strength class (M25 = 25 MPa characteristic strength) Slump \| Workability measure in mm (typical 100–150 mm) specified on the order cu.m. (m³) \| Billing unit for concrete TM (transit mixer) \| Truck that carries concrete, typically 6–9 m³ per trip Pump / boom pump / line pump \| Equipment that pushes concrete to height or distance; charged per m³ or per hour/fixed Part load \| A TM carrying less than a full load; usually has extra charges Cube test \| Sample cubes cast at site and tested at 7/28 days; disputes about quality arise here TMT bar (Fe500, Fe500D, Fe550) \| Reinforcement steel by grade and diameter (8, 10, 12, 16, 20, 25, 32 mm); sold by weight (MT) MT \| Metric tonne (1,000 kg) Weighbridge slip \| Weight evidence at loading/unloading, used for steel, sand and aggregates Sand / aggregate \| Sold by brass, cu.m., tonne or truck load; sizes such as 10 mm, 20 mm, 40 mm Delivery challan \| Document accompanying goods that proves what was delivered, where and to whom HSN / SAC \| Tax classification code for goods / services GSTIN \| 15-character GST registration number; first 2 digits = state code e-way bill \| Transport document generated on the GST portal for goods movement above ₹50,000 consignment value, tied to vehicle number e-invoice / IRN \| Invoice registered on the Invoice Registration Portal for businesses above the turnover threshold (currently ₹5 crore aggregate turnover) Site \| The customer’s project location; one customer can have many sites, each with its own address, contact and sometimes its own GSTIN PDC \| Post-dated cheque, common in collections Retention \| Percentage withheld from a contractor’s bill until defects period ends (Phase 3) BOQ, WBS, DPR, MB, RA bill, RFI, ITP, NCR \| Phase 3–4 construction-management terms, defined in Section 14.2 (Phase 3–4 epics)

Typical RMC day: morning enquiries arrive by call/WhatsApp → sales checks the customer’s credit and quotes → customer confirms a pour date and quantity → operations books the plant slot and TMs → dispatcher releases trips through the day → each trip is tracked loading → dispatch → site arrival → unloading → complete, with a receiver signature → one challan per trip → trips roll up into an invoice (per trip, per day or per order, depending on the customer) → payment, often part, often late → follow-ups → profit view. 4. Indian compliance baseline (configurable, never hardcoded) Tax rates, thresholds and rules change. The product stores them as effective-dated master data that a tenant admin can update; the values below are indicative and must be confirmed by the tenant’s CA at onboarding. Area \| Requirement the product must support GST invoice \| Mandatory fields (supplier/recipient GSTIN, place of supply, HSN, taxable value, CGST/SGST/IGST, invoice no. unique per FY, ≤16 characters, date, amount in words); reverse-charge flag; invoice type (tax invoice, bill of supply, credit note, debit note) Place of supply \| Intra-state → CGST+SGST; inter-state → IGST; derived automatically from supplier state and the site/delivery state (not just the billing address) Rates and HSN \| Per-product HSN and GST rate with effective dates (indicative HSN: RMC 3824, cement 2523, TMT bars 7214, sand 2505, aggregates 2517); rate changes never alter past invoices e-invoicing \| Capture IRN, acknowledgement number/date and QR on the invoice; generate portal-compliant JSON for upload in Phase 1; direct API in Phase 5 e-way bill \| Capture transporter, vehicle, distance, validity and EWB number; generate portal-compliant JSON in Phase 1; direct API in Phase 5 GST returns support \| Reports shaped for GSTR-1 (B2B, B2C, credit notes, HSN summary) and GSTR-3B summary in Phase 1; GSTR-2B reconciliation in Phase 2 TDS \| Configurable TDS on supplier/contractor payments (for example Sec. 194C, 194Q), TDS receivable on customer payments; certificate tracking MSME \| Supplier flag with Udyam number; payables report highlighting bills nearing 45 days Audit trail \| Edit log of accounting data that cannot be switched off (Companies (Accounts) Rules audit-trail requirement) Financial year \| April–March with period locking Data protection \| DPDP Act 2023: consent record for marketing contacts, purpose limitation, right to erase/withdraw handled through anonymisation that preserves accounting integrity SMS / WhatsApp \| SMS needs TRAI DLT sender ID and approved templates; WhatsApp needs a Business API provider and pre-approved templates; marketing sends only to opted-in contacts Data residency \| Production data hosted in India region Formats \| INR, lakh/crore grouping, DD-MMM-YYYY dates, amount in words (Indian system)

Disclaimer to keep in product help and legal pages: the software supports compliance workflows; the tenant and its tax advisor remain responsible for filing decisions. 5. Personas, roles and permissions 5.1 Personas Persona \| Goal \| Main pain today \| Device Owner / Director \| See cash, outstanding, profit and risk in 2 minutes \| Numbers arrive late from the accountant; no per-order profit \| Phone + desktop Sales executive \| Respond to enquiries fast, quote correctly, never forget a follow-up \| Quotes in Excel/WhatsApp, lost leads, unknown credit status \| Phone + desktop Operations / dispatcher \| Plan plant slots, TMs and trips; know where every load is \| Phone calls, no live status, disputes over quantity and timing \| Desktop (control room) + phone Purchase manager \| Buy at the best rate, track supplier dues \| Rates in memory, no history, unpaid bills surprise \| Desktop + phone Accountant \| Correct invoices, receipts, GST and books without re-typing \| Double entry in Tally, mismatched quantities, GST rework \| Desktop Driver / TM operator \| Update trip status with 1 tap; show delivery proof \| No simple tool; paper chits \| Phone only (low-end Android) Customer site engineer / receiver \| Confirm receipt quickly \| Paper challan signatures \| Phone via link (no login) Tenant admin \| Configure company, users, rules \| Fear of breaking things \| Desktop Platform super-admin (Berynx) \| Create tenants, manage plans, support \| — \| Desktop

5.2 Roles (system roles; tenant can clone and edit) Tenant Admin, Management, Sales, Operations (Dispatcher), Purchase, Accounts, Plant Coordinator, Driver, Read-only Auditor; Phase 3+: Project Manager, Site Engineer, Contractor Portal user; Phase 4+: QA/QC Engineer, Safety Officer. 5.3 Permission matrix (Phase 1) — C=create, V=view, E=edit, A=approve, X=cancel/void, P=post, —=none Module \| Admin \| Mgmt \| Sales \| Ops \| Purchase \| Accounts \| Auditor Company, users, roles, settings \| CVE \| V \| — \| V Customers, sites \| CVE \| V \| CVE \| V \| CVE \| V Suppliers, plants \| CVE \| V \| CVE \| V Products, price lists \| CVE \| V \| CVE \| V Enquiry / CRM \| V \| CVEX \| V \| — \| V Quotation \| V \| VA \| CVEX \| V \| — \| V Sales order \| V \| VA \| CVEX \| CVE \| V Purchase order / supplier invoice \| V \| VA \| — \| V \| CVEXA \| VP \| V RMC / delivery / trips \| V \| CVEX \| V Challan \| V \| CVEX \| V Sales invoice \| V \| — \| CVEXP \| V Receipts, payments, notes \| V \| — \| V \| CVEXP \| V Accounting vouchers, books \| V \| — \| CVEXP \| V GST, reports \| V \| own \| V Margin / purchase-cost visibility \| Yes \| Off by default \| Off \| Yes Audit log \| V \| — \| V

Data scope per role: All, My branch/plant, Own records. Field-level: purchase rate, margin and profit fields are hidden unless the role has the view_cost permission. Approval rights are separate permissions (quotation.approve, po.approve, credit.override, expense.approve). 6. UX principles, navigation and screen patterns 6.1 Principles Task-first, not table-first. Daily tasks (new enquiry, quote, book delivery, record payment) take ≤3 clicks from the dashboard via Quick Create. Enter once. Selecting a customer auto-fills sites, GST, payment terms, credit status and last rates. Converting a document (Quotation → Order → Delivery → Challan → Invoice) pre-fills everything and links the records. Create in context. If a customer, site or product is missing, a side drawer creates it without leaving the form. Show consequences before posting. Before posting an invoice or receipt, a summary shows the ledger, outstanding and GST effect. Progressive disclosure. Forms show the 8–12 fields users need daily; advanced fields sit under “More details”. Safe by default. Destructive actions need a reason; posted records offer Cancel/Reverse, never Delete; unsaved changes are protected; drafts autosave. Status is always visible as a coloured chip plus text (never colour alone) and the next sensible action is the primary button. Speed. List pages load in \<2 s for 50k rows (server paging); typing in search returns in \<500 ms; keyboard shortcuts for power users. Plain language. Labels use the trade’s words (“Challan”, “TM”, “Site”, “Outstanding”), not database words. 6.2 App shell and navigation Left navigation grouped in lifecycle order: Home · Sell (CRM, Quotations, Orders) · Buy (Purchase Orders, Supplier Bills) · Deliver (Dispatch Board, Trips, Challans) · Bill & Collect (Invoices, Receipts, Notes) · Accounts (Expenses, Books, GST) · Reports · Marketing · Settings. Only modules the user may open appear. Top bar: global search (Ctrl/Cmd+K), + Quick Create menu, notifications bell with unread count, approvals inbox badge, company/branch and financial-year switcher, help (?), user menu. Breadcrumbs on detail pages; recently viewed in search. Mobile: bottom bar with Home, Search, + (Quick Create), Tasks, More. Drivers get a separate minimal shell (Section 8, M08). Home is role-specific (Section 8, M15). 6.3 Standard screen patterns (every module reuses these) Pattern \| Rules List \| Title, primary “+ New” button, search box, filter chips (status, date range, customer, owner), saved views, sortable columns, column chooser, row click opens detail, bulk actions where meaningful, footer totals for money columns, Export (Excel/CSV/PDF), server-side paging, empty state with a call to action Form \| Sectioned card layout, required fields marked, inline validation on blur, sticky bottom action bar (Save Draft · Save & Next · Submit), autosave draft, field help text, line-item grid with add/remove/duplicate row and live totals, keyboard Tab/Enter flow Detail \| Header: document number, status chip, key facts, primary action and a “⋯” menu (Print, PDF, Share, Duplicate, Cancel). Tabs: Overview · Items · Linked documents (upstream/downstream with status) · Payments/Financial effect · Attachments · Activity (audit timeline) Convert action \| Button such as “Create Order” opens the next form pre-filled; user reviews and saves; source shows the link Wizard \| Used for onboarding, imports, campaign creation: numbered steps, back/next, summary step, resumable Approval \| Request → inbox card with summary, amount, requester, history and Approve/Reject (comment mandatory on reject) Board \| Dispatch Board and enquiry pipeline: columns by status, drag-and-drop with confirmation, filters, live refresh Print/PDF \| Tenant-branded template with logo, GSTIN, bank/UPI QR, terms, signature block; A4 and thermal-friendly challan variant Dashboard card \| Metric, trend vs previous period, click-through to the filtered list that explains the number

6.4 Mobile and responsive Breakpoints: phone \<640 px, tablet 640–1024 px, desktop \>1024 px. All Phase 1 modules are usable on tablet; Enquiry, Quotation, Delivery status, Receipt entry, Approvals and Dashboard are usable on phone. Touch targets ≥44 px; camera/photo upload and one-tap phone call/WhatsApp links on contact numbers. Delivered as an installable PWA. The driver and receiver flows use signed, expiring links that work on low-end Android with poor network (retry queue; full offline is Phase 5). 6.5 Help and onboarding Setup checklist on first login (company → GST → products → customers → users → first quotation); guided empty states; optional sample-data toggle in trial tenants; contextual help drawer with short articles and 60-second videos; “What’s new” panel. 6.6 Visual, accessibility and localisation WCAG 2.1 AA: contrast, keyboard navigation, focus states, labels, screen-reader names; never colour-only status. Semantic colour tokens (not raw colours) for status; light theme at launch, dark theme ready via tokens. Indian formatting everywhere: ₹ with lakh/crore grouping, DD-MMM-YYYY, 12-hour time, amount in words. UI strings externalised for i18n. English at launch; Hindi, Marathi and Gujarati packs in Phase 2. 6.7 Standard states and messages Every screen defines: loading (skeleton), empty (explain + action), error (what happened + what to do + retry), no-permission (who to ask), offline banner, validation messages in plain language, confirmation dialogs stating the consequence (“Cancel invoice INV/25-26/0041? Ledger and outstanding will be reversed.”), success toasts with an Undo link where safe. 6.8 Status chip standard Draft = grey · Pending approval = amber · Approved/Confirmed = blue · In progress = indigo · Completed/Paid/Won = green · Overdue/Rejected/Lost = red · Cancelled/Void = dark grey with strike icon · On hold = orange. 7. Platform services (build before or with Phase 1 modules) 7.1 SaaS platform layer (Berynx super-admin) Capability \| Requirement \| Phase Tenant provisioning \| Create tenant, first admin, default roles, chart of accounts template, numbering, sample masters \| 1 Plans and limits \| Plan defines modules enabled, user count, document volume, storage, API/message quotas; limits enforced with friendly upgrade prompts \| 1 Usage metering \| Per-tenant users, documents, storage, messages sent \| 1 Tenant lifecycle \| Trial → Active → Suspended (read-only) → Cancelled → Data export → Purge after retention \| 1 Support access \| Time-boxed impersonation with tenant consent, fully audited \| 1 Feature flags \| Per-tenant and per-plan module toggles \| 1 Subscription billing \| Plans, invoices to tenants, online payment, dunning \| 2 Platform console \| Tenant list, health, usage, announcements, support links \| 1

7.2 Identity, access and approvals Login: email/mobile + password, or mobile OTP; optional TOTP MFA (mandatory for Admin and Accounts roles by tenant setting). Password policy: ≥8 chars, breached-password check; lockout after 5 failures for 15 minutes; refresh-token rotation; session idle timeout configurable (default 30 min on desktop); active-sessions list with revoke. RBAC: permissions are module.action codes; roles are bundles; users may have multiple roles and branch/plant scopes. A permission change takes effect on next request. Approval engine: a rule = entity + condition (for example discount \>5%, order value \>₹5 lakh, credit limit exceeded, PO \>₹2 lakh, expense \>₹25,000) + approver role/user + levels + SLA + escalation. Requester cannot approve own item; approver may delegate during absence; every decision is audited; approved snapshots are locked against silent edits (any edit re-triggers approval). 7.3 Document numbering and financial year Pattern tokens {PREFIX}/{BRANCH}/{FY}/{SEQ:4}; sequences reset per financial year and per series; invoice and credit-note series are gap-free and ≤16 characters (enforced when saving the pattern); numbers are assigned at posting, drafts carry a temporary reference; numbering is transaction-safe under concurrency. FY switch and period lock (month/year) block back-dated postings except for roles with period.override. 7.4 Documents and attachments Attach to any record: images, PDF, Excel, Word up to 10 MB each (tenant limit configurable), virus scan, preview, version history, download audit. Branded print/PDF templates per document type with tenant logo, header/footer, terms, signature and UPI QR; template variables managed in Settings; share by link with expiry and optional OTP. 7.5 Notifications and communication Channels: in-app, email, SMS, WhatsApp via a provider abstraction (swap providers without code change). Event catalogue (Phase 1): enquiry assigned, follow-up due/overdue, quotation approved/expiring, order confirmed, delivery scheduled/dispatched/arrived/completed, invoice posted, payment received, payment due/overdue, approval requested/decided, credit limit breached, low-rate/negative-margin alert. Templates with variables and preview; per-user preferences; quiet hours; delivery log with retry and failure reason; consent register per contact and channel (opt-in date, source, opt-out); transactional messages are separate from promotional messages. 7.6 Audit and activity log Immutable append-only log: tenant, user, role, timestamp (UTC + IST display), IP, device, entity, entity id, action, before/after JSON, reason. Covers create/update/status change/approval/print/export/login/permission change. Viewer with filters by user, entity, date; per-record Activity tab; export for auditors. Cannot be disabled or edited by any role including Admin. 7.7 Global search One box searches customers, sites, suppliers, contacts, mobile numbers, GSTIN, enquiries, quotations, orders, POs, deliveries, TM numbers, challans, invoices, receipts, products and notes; results grouped by type, permission-aware, typo-tolerant, ranked by recency and exact match, with keyboard navigation and recent items. 7.8 Import, export and migration Excel/CSV import wizard (download template → upload → validation preview with row-level errors → fix/skip → confirm → summary); supported: customers, sites, suppliers, products, price lists, opening balances, outstanding invoices, contacts. Export from every list (Excel/CSV/PDF). Full tenant data export (zip) for backup and exit. Tally export of vouchers and masters (XML) for accountants who keep Tally. 7.9 Tenant configuration catalogue Company profile and GSTINs, branches/plants, FY start, numbering series, tax/HSN master, units and conversions, payment terms, credit policy (default limit, hard/soft block, overdue-days block), approval rules, quotation validity default, RMC defaults (slump, minimum load, waiting-time free minutes, part-load rule), print templates, notification templates, consent text, rounding mode, bank/UPI accounts, working hours, holidays. 8. PHASE 1 — Client Core ERP (full specification) Phase 1 goal: a real customer runs the daily cycle enquiry → quotation → order → purchase → delivery → challan → invoice → payment → ledger → GST → reports with no duplicate entry, no spreadsheet and no missed follow-up. Module build order: M01 Tenant & onboarding → M02 Users & access → M03 Masters → M04 CRM → M05 Quotation → M06 Sales order → M07 Purchase → M08 RMC & delivery → M09 Challan → M10 Sales invoice → M11 Receipts, payments, notes → M12 Accounting → M13 GST → M14 Expenses → M15 Dashboard → M16 Reports → M17 Promotion & catalogue → M18 Cross-cutting (import, search, notifications, documents). End-to-end flow (with documents and statuses): Contact → Enquiry (New) → Quotation (Draft → Approved → Sent → Accepted) → Sales Order (Confirmed) → Purchase Order (Approved → Sent) → Delivery Schedule → Trips (Loading → Dispatched → At site → Unloading → Completed) → Challan (per trip) → Invoice (Posted) → Receipt → Customer ledger/outstanding; and Supplier Invoice → Supplier ledger → Payment; all feeding Books → P&L → GST. P1-M01 Tenant, company and onboarding Purpose: let a new business sign up, configure itself and go live without developer help. UX flow: Berynx super-admin creates tenant (or self-sign-up trial) → admin receives invite → sets password/MFA → Onboarding wizard (1 Company profile → 2 GSTIN(s) and branches/plants → 3 Financial year and numbering → 4 Bank/UPI/cash accounts → 5 Tax defaults and units → 6 Invite users → 7 Import masters and opening balances → 8 Go-live checklist) → Home with setup checklist. Screens: SCR-M01-01 Platform tenant list · -02 Create tenant · -03 Onboarding wizard · -04 Company profile · -05 Branch/plant list and form · -06 Numbering series · -07 Opening balances · -08 Go-live checklist · -09 Plan and usage. Key fields: legal name, trade name, PAN, GSTIN(s) (state auto-derived), registered/branch addresses, logo, FY start month, currency INR, time zone IST, MSME/Udyam, e-invoice applicable (Y/N), e-way bill applicable, default terms, bank accounts with UPI ID. Business rules BR-M01-01 GSTIN validated by format and checksum; state code and PAN derived; duplicate GSTIN across tenants allowed only if different tenants (isolation), never within one tenant. BR-M01-02 Go-live requires: ≥1 GSTIN, FY, numbering series for quotation/order/PO/challan/invoice/receipt, ≥1 cash or bank account, tax defaults, ≥1 additional user or explicit skip. BR-M01-03 On go-live the opening balances lock; changes afterwards need an adjustment voucher. BR-M01-04 Opening trial balance must balance (debits = credits) before go-live. BR-M01-05 Plan limits (users, storage, volume) are checked on create; at 80% a warning, at 100% a block with upgrade prompt. User stories US-M01-01 As a platform admin, I want to create a tenant with plan, first admin and enabled modules, so that a customer can start within minutes. AC: tenant gets default roles, chart of accounts, numbering series, tax master; invite email sent; tenant data is invisible to other tenants (automated isolation test). US-M01-02 As a tenant admin, I want a guided wizard to set up my company, so that I do not need training. AC: wizard saves progress per step, can be resumed, validates GSTIN, shows % complete; skipping optional steps is allowed. US-M01-03 As an admin, I want multiple branches/plants/GSTINs, so that invoices use the right registration. AC: each document carries a branch; invoice numbering and GSTIN follow the branch; users can be restricted to branches. US-M01-04 As an admin, I want to configure numbering patterns, so that documents follow our conventions. AC: live preview; invoice pattern \>16 chars is rejected; resets per FY; no gaps for invoices. US-M01-05 As an accountant, I want to enter opening balances (cash, bank, bill-wise customer/supplier outstanding, other ledgers), so that books start correct. AC: import via Excel; difference to zero shown live; bill-wise entries create open items usable for receipt allocation. US-M01-06 As an admin, I want a go-live checklist, so that nothing is forgotten. AC: each item links to its screen; Go-Live button disabled until mandatory items are green. US-M01-07 As a platform admin, I want to suspend or cancel a tenant, so that unpaid or closed accounts are handled. AC: suspension makes data read-only with a banner; cancellation allows full export for 30 days before purge. US-M01-08 As an admin, I want to see plan usage, so that I can plan upgrades. AC: shows users, storage, documents this month, messages sent. P1-M02 Users, roles and approvals Purpose: control who can see and do what, safely. UX flow: Settings → Users → Invite (email/mobile, role, branch scope) → user accepts → first login tour. Settings → Roles → clone a system role → tick permissions in a module × action grid → save. Settings → Approvals → add rule (entity, condition, approver, levels, SLA). Screens: SCR-M02-01 Login (password / OTP) · -02 MFA setup · -03 User list/form · -04 Role list/permission grid · -05 Approval rules · -06 My approvals inbox · -07 Profile & sessions · -08 Audit log viewer. Business rules BR-M02-01 At least one active Tenant Admin must always exist. BR-M02-02 System roles cannot be deleted; tenant copies can. BR-M02-03 A user cannot approve their own request; approvals respect amount thresholds and levels in order. BR-M02-04 Deactivating a user keeps history, reassigns open tasks (enquiries, follow-ups, approvals) through a guided dialog. BR-M02-05 Cost/margin fields obey view_cost everywhere (UI, API, exports, PDFs, search results). User stories US-M02-01 As a user, I want to log in with password or mobile OTP, so that I can access quickly on my phone. AC: lockout after 5 failures; OTP expires in 5 min; generic error messages; login events audited. US-M02-02 As an admin, I want to invite users with a role and branch scope, so that they see only what they need. AC: invite link expires in 72 h; resend/revoke; role change applies at next request. US-M02-03 As an admin, I want custom roles with a permission grid, so that I can match my organisation. AC: grid by module × (view/create/edit/approve/cancel/export/print); data scope selector; changes audited. US-M02-04 As an admin, I want approval rules, so that discounts, credit overrides, POs and expenses get proper sign-off. AC: rule conditions on amount, discount %, margin %, credit status; multiple levels; SLA with reminders and escalation. US-M02-05 As an approver, I want a single inbox on phone and desktop, so that I can decide in seconds. AC: card shows document summary, margin (if permitted), history; Approve/Reject; reject needs comment; decision notifies requester. US-M02-06 As an approver, I want to delegate while on leave, so that work does not stall. AC: date-bounded delegation; audited; delegate cannot exceed own limits. US-M02-07 As a user, I want to view and revoke my active sessions and enable MFA, so that my account stays safe. AC: shows device, IP, last seen; revoke logs out immediately. US-M02-08 As an auditor, I want to search the audit log, so that I can trace any change. AC: filter by user/entity/date/action; shows before/after; export CSV; read-only. P1-M03 Masters (customers, sites, suppliers, products, pricing, fleet, tax) Purpose: clean reusable data — entered once, used in every transaction. UX flow: every master has List → Detail → Form; create-in-context drawers inside transactions; Excel import for bulk; duplicate detection on save; deactivate instead of delete. Screens: SCR-M03-01 Customer list/form/360 · -02 Site list/form · -03 Contact list · -04 Supplier/plant list/form · -05 Product list/form · -06 Price list · -07 Vehicle/TM list · -08 Driver list · -09 Pump/equipment list · -10 Tax/HSN master · -11 Units & conversions · -12 Payment terms · -13 Charge types · -14 Bank/cash accounts. Key data Master \| Essential fields Customer \| Type (builder, developer, contractor, individual, institution), legal and trade name, GSTIN, PAN, billing address, multiple contacts (role: purchase manager, site manager, engineer, accounts), payment terms, credit limit and credit days, credit policy (warn/block), default price list, salesperson, opening balance, status (Active/On hold/Blocked), tags, KYC documents Site \| Customer, site/project name, full address with state and geo-pin, site contacts, optional site GSTIN, default plant, RMC access notes (road width, floor/height, pump required, working hours, restrictions), status Supplier / plant \| Type (RMC plant, steel, cement, aggregate/sand, transporter, pump vendor, other), GSTIN, MSME flag + Udyam, bank details, payment terms, TDS category, plant capacity per hour, location, grades offered, contacts, opening balance Product \| Category, name, HSN, GST rate (effective-dated), base unit and alternate units with conversion (for example steel: MT ↔ bundles ↔ pieces; cement: bag ↔ MT; RMC: m³), category attributes (RMC: grade, default slump, aggregate size, cement type, admixture; steel: brand, grade Fe500D, diameter, length, kg/metre; cement: brand, type OPC/PPC, grade 43/53, bag weight; sand/aggregate: type, size) Price list \| Name, applicable customers/groups, rate per product, validity from–to, minimum selling rate/margin guard Vehicle / TM \| Number, type (TM, pump, truck, tipper), capacity m³ or MT, owner (own/hired + transporter), insurance/PUC/fitness expiry, status Driver \| Name, mobile, licence number and expiry, linked vehicle, status Charge types \| Pumping, transport, part-load, waiting/demurrage, night delivery, cancellation, loading/unloading, other; each with default GST and ledger

Business rules BR-M03-01 Duplicate detection on GSTIN, PAN, mobile and (name + city) with a “View existing” link; merge duplicates with audit and relinking of history. BR-M03-02 A customer in On hold/Blocked cannot receive new quotations/orders/deliveries without credit override approval. BR-M03-03 Credit exposure = outstanding invoices + undelivered confirmed orders value (configurable) − unallocated advances; shown on customer and at order time. BR-M03-04 Tax rates are effective-dated; past documents keep the rate used at posting. BR-M03-05 Unit conversions must be consistent in both directions; the base unit cannot change once transactions exist. BR-M03-06 Masters used in any transaction cannot be deleted, only deactivated. BR-M03-07 Price-list lookup order: customer-specific → customer group → default; falling below minimum rate requires approval. BR-M03-08 Vehicle/driver documents nearing expiry (30/15/7 days) raise notifications; expired vehicles cannot be dispatched unless overridden. User stories US-M03-01 As sales, I want to add a customer with GSTIN and see name/address suggestions or validation, so that data is clean. AC: invalid GSTIN blocks save with explanation; valid format auto-fills state/PAN; duplicates flagged. US-M03-02 As sales, I want multiple sites per customer with access notes, so that dispatch knows site constraints. AC: site selectable in all transactions; site notes surface on delivery and driver screens; site state drives tax. US-M03-03 As an admin, I want to set credit limit, days and policy per customer, so that risk is controlled. AC: credit bar (used/limit) on customer, quotation, order forms; hard block requires credit.override approval. US-M03-04 As purchase, I want suppliers and plants with GST, bank, MSME and TDS flags, so that purchasing and payments are compliant. AC: MSME flag drives the 45-day payable alert; TDS category pre-fills payment deduction. US-M03-05 As admin, I want products with category-specific attributes and unit conversions, so that RMC, steel and cement are all handled correctly. AC: selecting category changes form fields; conversion shown live (for example 2.5 MT = 20 bundles). US-M03-06 As sales, I want price lists with validity and minimum rates, so that quotes are consistent and protect margin. AC: quotation line pulls the correct rate; overriding below minimum triggers approval; expired price shows a warning. US-M03-07 As ops, I want vehicles and drivers with document expiry, so that only compliant vehicles run. AC: expiring/expired chips on list; expiry notification to Ops. US-M03-08 As an admin, I want to import customers/sites/products/suppliers from Excel, so that migration is fast. AC: template download; row-level errors; duplicate handling options (skip/update); undo for a batch within 24 h. US-M03-09 As any user, I want to create a customer/site/product from inside a transaction, so that I do not lose context. AC: drawer opens, saves, returns the new record selected. US-M03-10 As an admin, I want to deactivate a master, so that it disappears from pickers but history stays. AC: inactive records hidden in new documents, visible in old ones and reports. US-M03-11 As accounts, I want to maintain charge types and payment terms, so that invoices and quotes pick correct ledgers and due dates. AC: due date = invoice date + terms; ledger mapping visible. US-M03-12 As management, I want a Customer 360 page, so that I see the full relationship. AC: header shows outstanding, overdue, credit used %, last order date, YTD sales and profit (if permitted); tabs: Sites, Enquiries, Quotations, Orders, Deliveries, Invoices, Receipts, Ledger, Documents, Activity. P1-M03 patch — RMC rate matrix, product attributes and category rate override

**Additions** - **Rate card (RMC):** rate key = grade × slump band × admixture × pump type. Pump and admixture surcharges are charge types (M03 Charge types), not separate prices. Price list lookup follows BR-M03-07. - **Category tax override:** a product may carry a GST rate by customer category (for example works contract or government contract) with the rate, effective dates and a mandatory reason. The override is used only where the customer category matches. Overrides are CA-approved (Section 13, OQ-03). - **Traceable flag** on products (TMT, cement, and others) that makes lot/heat no. mandatory on GRN, delivery and challan. - **Quality reference** on products (C8).

**New business rules** - BR-M03-09 Rate-card lookup uses the exact key. A missing key shows *No rate for this combination* and blocks the quote line until a rate is entered and approved. - BR-M03-10 Category tax overrides never change tax on documents already posted (same rule as BR-M03-04).

**New user story** - **US-M03-13** As sales, I want RMC prices to fill in from grade, slump, admixture and pump type, so that quotes are fast and consistent. *AC:* selecting the combination fills the rate and surcharges; changing a combination recalculates the line; missing combinations are flagged.

P1-M04 CRM and enquiries Purpose: never lose a lead; make follow-up a habit. UX flow: Quick capture (≤30 seconds, works on phone): choose or create contact/customer → site → product interest and quantity → required-by date → source → save. System assigns owner (round-robin or rule), sets first follow-up. Owner works from Today’s follow-ups list: call/WhatsApp one-tap → log outcome → set next action date → or “Create Quotation”. Board view moves enquiries across stages. Screens: SCR-M04-01 Enquiry board (Kanban) · -02 Enquiry list · -03 Quick-add (mobile sheet) · -04 Enquiry detail with activity timeline · -05 Follow-up planner (Overdue / Today / Upcoming) · -06 Contact list · -07 Customer 360 (shared with M03). Lifecycle: New → Contacted → Follow-up → Quotation Sent → Negotiation → Won or Lost; also On hold. Won links to an order; Lost needs a reason. Key fields: enquiry no., date/time, source (phone, walk-in, WhatsApp, referral, site visit, website, campaign, repeat), contact, customer (optional until won), site/location, product interest lines (product, quantity, unit), required-by date, expected value, owner, priority, next action and date, notes, attachments (drawings, photos), lost reason (price, competitor, credit, timing, quality, no response, other), consent flags. Business rules BR-M04-01 Every open enquiry must have a next-action date; the system cannot save a non-closed enquiry without one. BR-M04-02 Duplicate warning when the same contact/site and product exist in an open enquiry within 7 days. BR-M04-03 First-response SLA (default 30 min) tracked; breach notifies the manager. BR-M04-04 Marking Won requires linking a sales order (or creating one); a won enquiry auto-promotes the contact to a customer if not already. BR-M04-05 Reassignment keeps history; deactivated owners’ enquiries are reassigned through a bulk dialog. BR-M04-06 Contact consent (WhatsApp/SMS/email) is stored with date, source and channel and is editable only with audit. User stories US-M04-01 As a sales executive, I want to capture an enquiry in under 30 seconds on my phone, so that no lead is lost. AC: minimal form of ≤6 fields; offline-tolerant retry; created record visible instantly on desktop. US-M04-02 As a sales manager, I want enquiries auto-assigned by rule, so that response is fast. AC: rules by source, region or round-robin; reassign with one click; assignee notified. US-M04-03 As a sales executive, I want a daily follow-up list, so that I act on what is due. AC: Overdue/Today/Upcoming tabs; one-tap call/WhatsApp; logging an outcome forces next action date. US-M04-04 As a sales executive, I want to move enquiries on a board, so that I see my pipeline. AC: drag between stages with confirmation; Lost opens reason dialog; totals per column. US-M04-05 As a sales executive, I want to create a quotation from an enquiry, so that I do not retype. AC: customer, site, products and quantities pre-filled; enquiry becomes “Quotation Sent” when the quote is sent. US-M04-06 As a sales manager, I want to see conversion by source, owner and lost reason, so that I can improve. AC: funnel with counts and value; filter by date and owner; click-through to enquiries. US-M04-07 As a sales executive, I want a customer history view before calling, so that I speak with context. AC: last orders, rates, outstanding and last conversation visible on enquiry detail. US-M04-08 As a manager, I want overdue follow-ups escalated, so that leads do not rot. AC: configurable threshold; notification to owner then manager; dashboard count. P1-M05 Quotation Purpose: produce an accurate, professional, margin-safe quote fast, and track it to a decision. UX flow: New Quotation (from enquiry, from customer page, or Quick Create) → pick customer → pick site → add lines (rate auto-fills from price list/last rate) → add charges (pumping, transport, part-load rule) → live credit chip and margin chip (margin only with view_cost) → terms template → Save Draft → (approval if rules trigger) → Send (PDF, WhatsApp, email, or share link) → customer Accepts/Rejects on the link or sales marks it → Create Order. Screens: SCR-M05-01 Quotation list (tabs: Draft, Pending approval, Sent, Accepted, Expired, All) · -02 Quotation form · -03 Quotation detail with revision history · -04 Public quotation view/accept page · -05 Terms template manager. Lifecycle: Draft → Pending approval → Approved → Sent → Accepted \| Rejected \| Expired \| Revised \| Cancelled. Key fields: number, date, valid-until, customer, site, contact, salesperson, price basis (ex-plant / delivered / site), payment terms, delivery terms, lines (product, description, qty, unit, rate, discount %, GST %, taxable, tax, total), charges (pumping per m³ or lump, transport, part-load, waiting terms, other), terms and conditions, internal notes, attachments, revision no. Business rules BR-M05-01 Rate priority: customer price list → group list → default list → last sold rate (suggestion only). BR-M05-02 Discount above threshold or margin below minimum triggers approval; the approver sees margin and credit position. BR-M05-03 Validity default is tenant-configurable (for example 7 days); expired quotes cannot convert without revalidation (new revision). BR-M05-04 Editing a Sent/Approved quote creates a new revision (Rev 2, 3…); earlier revisions become read-only Revised and stay viewable; only one active revision exists. BR-M05-05 One accepted quotation can create multiple orders until its quantity is consumed (supports call-off steel/cement); remaining quantity is tracked. BR-M05-06 GST type (CGST+SGST vs IGST) uses branch state vs site state. BR-M05-07 Blocked/on-hold customers show a red banner and need credit override to send. User stories US-M05-01 As sales, I want a quotation form that pre-fills customer, site and rates, so that I can quote in two minutes. AC: all pre-fills editable; totals recalc live; GST breakup visible; tab order optimised for keyboard. US-M05-02 As sales, I want to see margin and credit status while quoting, so that I do not quote at a loss to a risky customer. AC: margin chip turns amber/red by thresholds; hidden if no view_cost; credit bar shows used/limit. US-M05-03 As a manager, I want discount/low-margin quotes routed to me, so that I control pricing. AC: approval request created automatically with reason; sales sees “Pending approval”; approve/reject notifies sales. US-M05-04 As sales, I want to send the quote by WhatsApp/email/link with a branded PDF, so that the customer gets it instantly. AC: PDF has logo, GSTIN, items, charges, terms, validity; send logs delivery and viewed status; consent checked before WhatsApp. US-M05-05 As a customer, I want to accept or reject the quote from a link, so that I do not need calls. AC: no login; link expires with the quote; accept captures name, mobile OTP, time; sales notified. US-M05-06 As sales, I want to revise a quote, so that negotiation is recorded. AC: new revision copies lines; diff of changes shown; old revision archived. US-M05-07 As sales, I want to convert an accepted quote into an order without retyping, so that data is entered once. AC: order opens pre-filled; quotation shows “Order SO-… created”; consumed/remaining quantity updates. US-M05-08 As a manager, I want expiring-quote alerts and win-rate analytics, so that follow-ups happen. AC: alert 1 day before expiry; report: quotes sent/won/lost by owner, value, margin. US-M05-09 As sales, I want to clone a previous quote, so that repeat customers are quick. AC: clone updates rates to today’s price list with a diff of changed rates. P1-M06 Sales order Purpose: the customer’s commitment; the control point for credit, price, supply and billing. UX flow: Create Order from quotation (or directly) → attach customer PO → credit check result shown → add delivery schedule (one or many pours/deliveries) → Confirm (approval if needed) → order page shows a fulfilment tracker (ordered / scheduled / delivered / invoiced / paid) → actions: Schedule delivery, Create purchase order, Amend, Short-close. Screens: SCR-M06-01 Order list · -02 Order form · -03 Order detail with fulfilment tracker and linked documents · -04 Amendment compare · -05 Order confirmation PDF. Lifecycle: Draft → Pending approval → Confirmed → In fulfilment → Completed \| Short-closed \| On hold \| Cancelled. Key fields: order no., date, customer PO no./date/file, customer, site, quotation ref, order type (RMC / material / mixed), lines (product, ordered qty, delivered qty, invoiced qty, balance, rate, tax), rate validity (price freeze until), delivery schedule rows (date, time window, product, qty), payment terms, credit check snapshot, notes, owner. Business rules BR-M06-01 Confirming runs the credit check (BR-M03-03); failure needs credit.override approval with reason. BR-M06-02 Rates freeze at confirmation until the rate-validity date; later price-list changes never alter the order. BR-M06-03 Delivered quantity may exceed ordered by the configured tolerance (default RMC 2%, steel 1%); beyond that needs approval. BR-M06-04 Cancellation only if nothing has been delivered; otherwise Short-close with reason (balance quantity released). BR-M06-05 Editing a confirmed order’s quantity, rate or site creates an amendment (revision) with approval; unchanged deliveries remain valid. BR-M06-06 Estimated margin is calculated from latest purchase rates and shown (with permission); a negative-margin order needs approval. BR-M06-07 Order drives purchase and delivery: it exposes Create PO and Schedule delivery actions pre-filled; it cannot be invoiced beyond delivered quantity. User stories US-M06-01 As sales, I want to confirm an order from an accepted quote, so that supply can start. AC: no retyping; credit status visible; confirmation PDF shareable. US-M06-02 As an approver, I want to override credit holds with a reason, so that good customers are not blocked unfairly. AC: approval card shows outstanding, overdue days, history; decision audited. US-M06-03 As ops, I want to define multiple delivery dates and quantities on one order, so that phased pours and steel call-offs are planned. AC: schedule rows total ≤ order qty (+tolerance); each row becomes a pour/delivery on the Dispatch Board. US-M06-04 As sales, I want to see order fulfilment at a glance, so that I can answer customers instantly. AC: progress bar by quantity (scheduled/delivered/invoiced/paid) and status of each linked document. US-M06-05 As purchase, I want to raise a PO directly from an order, so that back-to-back buying is accurate. AC: supplier, items, quantities and site pre-filled; order shows linked PO(s); order margin updates when PO rate is saved. US-M06-06 As sales, I want to amend a confirmed order with approval, so that changes are controlled. AC: amendment shows before/after; approved changes update schedule and estimates; history retained. US-M06-07 As ops, I want to short-close an order, so that unfinished balance does not pollute pending lists. AC: reason mandatory; balance quantity zeroed; reversible only by approver. US-M06-08 As management, I want a pending-order report (quantity and value by customer/site/product), so that I plan supply. AC: filter, export, drill-down. P1-M07 Purchase (PO, supplier invoice, supplier payment) Purpose: buy right, track what is owed, and pay on time. UX flow: Create PO (from an order or standalone) → supplier selected → rate suggested from last/agreed supplier rate with variance flag → charges (freight, pumping, loading) → approval by value → Send to supplier (PDF/WhatsApp/email) → supplier confirms → deliveries recorded in M08 mark PO quantity received automatically → enter Supplier Invoice (match against PO and received quantity; variances highlighted) → post → appears in Payables → Pay supplier (allocate to bills, deduct TDS, record mode/reference) → supplier ledger updated. Screens: SCR-M07-01 PO list · -02 PO form · -03 PO detail (received/balance, linked deliveries, bills) · -04 Supplier invoice list/form with match panel · -05 Payables worklist (due, overdue, MSME-45-day) · -06 Supplier payment form · -07 Supplier ledger · -08 Purchase rate history (product × supplier chart/table). Lifecycle: PO: Draft → Pending approval → Approved → Sent → Partially received → Received → Closed \| Cancelled. Supplier invoice: Draft → Matched → Posted → Part-paid → Paid \| Cancelled. Business rules BR-M07-01 Duplicate supplier-invoice check on supplier + invoice number + date. BR-M07-02 Rate variance beyond a configurable % versus the last purchase rate shows a warning and needs approval. BR-M07-03 Invoice quantity above received quantity (beyond tolerance) blocks posting until resolved or approved. BR-M07-04 ITC eligibility flag and reverse-charge flag per invoice line; supplier GSTIN state decides CGST/SGST vs IGST. BR-M07-05 TDS computed from supplier TDS category and thresholds; TDS payable posted automatically with the payment. BR-M07-06 MSME suppliers: payables older than 30 days show amber, 40 days red, 45 days breach; report available. BR-M07-07 Payments can be allocated to specific bills or kept as supplier advance. BR-M07-08 PO closing with unreceived quantity requires a reason. User stories US-M07-01 As purchase, I want to create a PO from a sales order in two clicks, so that nothing is retyped. AC: pre-filled lines, site and expected date; multiple suppliers can split one order’s lines. US-M07-02 As purchase, I want rate history and a rate-variance flag, so that I buy at the best price. AC: last 5 purchase rates per supplier visible inline; variance chip with %. US-M07-03 As an approver, I want POs above a limit routed to me, so that spend is controlled. AC: approval rule by value/supplier/product; PO cannot be sent until approved. US-M07-04 As purchase, I want to send a PO to a supplier by WhatsApp/email, so that confirmation is quick. AC: branded PDF; delivery log; supplier acknowledgement can be marked. US-M07-05 As accounts, I want to enter a supplier invoice matched to PO and deliveries, so that payables are accurate. AC: match panel shows PO qty, received qty, invoice qty with variance colours; posting creates ledger entries and GST input. US-M07-06 As accounts, I want a payables worklist by due date and MSME status, so that I pay in time. AC: filters; select multiple bills to create one payment; shows TDS impact. US-M07-07 As accounts, I want to pay a supplier with TDS and allocate to bills, so that ledgers are correct. AC: net payable = gross − TDS; bank/cash ledger posts; outstanding updates; receipt voucher printable. US-M07-08 As management, I want a supplier ledger and outstanding report, so that I know what I owe. AC: opening, bills, payments, closing; ageing buckets; export. P1-M07 patch — goods receipt (GRN)

**UX flow:** a purchase order that is Approved or Sent shows **Receive material**. The store user (or the plant coordinator for RMC) enters the receipt: lines matched to the PO, quantity by weighbridge or count, lot/heat no., and a photo or MTC. Shortages and damages are entered with a reason. Saving updates the PO’s received and balance quantities. The supplier invoice then matches against the GRN.

**Screens:** SCR-M07-09 GRN list · SCR-M07-10 GRN form (mobile and desktop) · SCR-M07-11 GRN detail.

**GRN lifecycle:** Draft → Posted \| Cancelled (reversed with reason).

**GRN fields:** GRN no. (series per branch and FY), date and time, PO ref, supplier, delivery site or godown, vehicle no., driver, weighbridge slip no., gross, tare and net weight (where weighed), received quantity by unit, lot/heat no. (per line), manufacturer or mill name, MTC or test certificate attachment, damaged or short quantity with reason code, received by, remarks.

**Business rules** - BR-M07-09 GRN posts no accounting entry. Purchase accounting is still posted from the supplier invoice (periodic method, A-04). The GRN changes quantities only. - BR-M07-10 Received quantity above PO quantity plus tolerance (default steel 1%, cement 2%) blocks posting until approved. - BR-M07-11 For products with the *traceable* attribute (TMT, cement, and any product the tenant marks), lot/heat no. is mandatory on each line. The lot no. carries to challans and to the stock report. - BR-M07-12 A GRN without a supplier invoice shows as *Received, not billed* in the payables and purchase reports (accrual visibility, not a ledger entry).

**User stories** - **US-M07-09** As a store in-charge, I want to record received steel and cement against the PO with weighbridge and lot numbers, so that the supplier bill can be matched and disputes have evidence. *AC:* PO balance updates on posting; over-receipt blocked above tolerance; lot no. required for traceable products; GRN printable. - **US-M07-10** As accounts, I want the supplier invoice match panel to show PO quantity, GRN quantity and invoice quantity, so that short or excess billing is caught before posting. *AC:* three-way variance colours; posting blocked beyond tolerance unless approved. - **US-M07-11** As a purchase manager, I want to see goods received but not yet billed, so that I know my accrued liability. *AC:* report by supplier with ageing from GRN date.

P1-M08 RMC and material delivery (the operational core) Purpose: plan, dispatch and prove every delivery; capture the facts that drive billing, disputes and profit. Key concepts: a Pour (or delivery schedule) is a planned supply of a product to a site on a date; a Trip is one vehicle load against a pour; each completed trip creates one Challan (M09). RMC pours are split into trips by TM capacity. Non-RMC materials (steel, cement, aggregates) use the same board with simpler trip steps. UX flow — Operations: open Dispatch Board → Unscheduled panel lists confirmed order schedule rows → select a pour → set date, time window, grade, quantity, slump, pump needs, plant (capacity warning) → Confirm pour (customer notified) → on the day, open pour → Plan trips (system suggests split, for example 24 m³ → 3 × 8 m³) → assign TM and driver (conflict detection) → plant marks Loaded with batch-slip number → Dispatched → At site → Unloading → Completed with receiver confirmation → challan auto-generated. The board shows lanes by plant/TM and cards coloured by delay. UX flow — Driver (mobile, minimal): receives assigned trip by link/app notification → card shows site, contact, map link, quantity, site notes → four large buttons in order: Loaded → Left plant → Reached site → Unloading started → Done → on Done: receiver name, signature on screen, photo of signed challan, optional OTP sent to receiver → submit. Works with weak network via retry queue. UX flow — Customer receiver: SMS/WhatsApp link per trip (no login) → shows ETA, then “Confirm quantity received” with OTP. Screens: SCR-M08-01 Dispatch Board (timeline + status columns) · -02 Pour form · -03 Pour detail (ordered/supplied/balance, trips, charges) · -04 Trip planner dialog · -05 Trip detail with timeline and evidence · -06 Driver mobile screens · -07 Receiver confirmation page · -08 Weighbridge entry (non-RMC) · -09 Delivery exceptions list · -10 Delivery KPIs. Pour fields: order/line, customer, site, date, time window, product/grade, quantity, slump, placement (pump/boom/crane/direct), pump type and quantity, plant, customer-side engineer, our coordinator, special instructions, purchase rate, selling rate, charges, ordered/supplied/balance. Trip fields: trip no., pour, vehicle, driver, plant, batch-slip no., load quantity, loading time, dispatch time, arrival time, unloading start/end, delivered qty, returned qty, receiver (name, mobile, signature, photo, OTP), delay minutes, waiting charge, remarks, status, exception type. Non-RMC fields: vehicle, driver/transporter, gross weight, tare weight, net weight (auto), bundles/bags/pieces, e-way bill reference, delivery receipt photo. Lifecycle (trip): Planned → Loading → Dispatched → At site → Unloading → Completed \| Rejected \| Returned \| Cancelled \| Breakdown (replaced). Pour: Scheduled → In progress → Completed \| Postponed \| Cancelled. Business rules BR-M08-01 Credit check re-runs at scheduling; blocked customers cannot be dispatched without override. BR-M08-02 Delivered quantity per order line cannot exceed ordered + tolerance (BR-M06-03). BR-M08-03 A vehicle or driver cannot be assigned to overlapping trips; vehicles with expired documents are blocked unless override. BR-M08-04 Trip timestamps must be in sequence; corrections after completion need trip.correct permission and a reason. BR-M08-05 Part-load charge applies automatically when load \< tenant minimum (default 4 m³), using the charge-type rate. BR-M08-06 Waiting charge applies when unloading start − arrival exceeds free minutes (default 30) at a per-hour or per-minute rate; waiver needs a reason. BR-M08-07 Rejected load: quantity excluded from billing, flagged for cost review (plant may still charge us); returned/wasted concrete recorded with reason and cost impact. BR-M08-08 Cancellation of a confirmed pour within the configured notice window (for example 4 hours) adds a cancellation charge proposal for approval. BR-M08-09 Balance quantity of the pour and order recalculates live after every completed trip. BR-M08-10 Purchase cost of a trip defaults from the plant’s agreed rate and PO; override requires approval; this cost feeds trip/order profitability. BR-M08-11 Plant allocation warns when scheduled quantity exceeds plant hourly capacity for the slot. BR-M08-12 Every completed trip produces exactly one challan; a trip without evidence (signature or photo or OTP) is flagged Unverified and cannot auto-invoice. BR-M08-13 Drivers see only their own trips and no commercial values. User stories US-M08-01 As ops, I want a Dispatch Board of the day’s pours and trips, so that I control the day from one screen. AC: timeline by plant and TM; colour by delay; filters; auto-refresh every 30 s; totals scheduled/dispatched/pending m³. US-M08-02 As ops, I want to schedule a pour from an order, so that supply matches the customer commitment. AC: pre-filled; plant capacity warning; customer notification sent on confirm. US-M08-03 As ops, I want the system to suggest trip splits and assign TM/driver, so that planning is fast. AC: suggestion editable; conflicts and expired documents flagged before saving. US-M08-04 As a plant coordinator, I want to mark loading with batch-slip number and quantity, so that traceability exists. AC: required fields enforced; time captured automatically. US-M08-05 As a driver, I want a simple phone screen with big buttons, so that I can update status without training. AC: works on low-end Android; one action per tap; retries when offline; Hindi/Marathi labels available in Phase 2. US-M08-06 As a customer receiver, I want to confirm quantity via OTP, so that disputes are avoided. AC: link expires after 24 h; OTP verified; confirmation timestamp saved to the trip. US-M08-07 As ops, I want to record rejection, return, breakdown or wastage, so that costs and billing are right. AC: reason list; quantity excluded from challan if rejected; breakdown allows replacement TM linked to the same pour. US-M08-08 As accounts, I want waiting and part-load charges calculated automatically, so that we do not lose money. AC: computed from timestamps and rules; shown on trip; editable with reason. US-M08-09 As ops, I want to postpone or cancel a pour with reason, so that the plan stays accurate. AC: customer notified; cancellation charge proposed if inside notice window; order balance unchanged on postpone. US-M08-10 As sales, I want to see live delivery status and ETA on the order, so that I can inform the customer. AC: order detail shows trip statuses and times; “Share status” button sends a tracking link. US-M08-11 As a dispatcher of steel/cement, I want weighbridge entry and net weight calculation, so that billing uses actual weight. AC: net = gross − tare; variance against theoretical weight shown; challan quantity uses net. US-M08-12 As management, I want delivery KPIs, so that service quality is measured. AC: on-time %, average turnaround (loading→completion), average waiting time, rejections %, m³ per TM per day, by plant/customer/date. P1-M08 patch — driver and receiver rules (Phase 1)

- **Driver shell in Hindi and Marathi** in Phase 1 (screens and buttons only; the rest of the app stays English). Strings are externalised (6.6).
- **Action queue (Phase 1):** every driver or receiver action is stored locally with a device timestamp and a unique action ID, then sent when the network returns. The server accepts each action ID once, so retries never duplicate a status. Full offline (offline planning and offline reporting) remains Phase 5.
- BR-M08-14 Lot/heat no. and MTC reference on non-RMC lines carry from the GRN or the pour into the challan.
- BR-M08-15 Each driver action has a defined failure state: *Saved on phone, not yet sent* (visible badge), *Sent* (tick), *Rejected by server* (with reason and an action to fix). Drivers are never asked to re-enter a sent action.
- BR-M08-16 Receiver OTP is re-sendable up to three times per trip, and the link stays valid for 24 hours (M08 US-M08-06).

**New user story** - **US-M08-13** As a driver in a low-network area, I want my status steps saved and sent later, so that no trip update is lost. *AC:* actions queued with an unsent badge; retry automatic; no duplicate statuses after retry; queue visible on the driver home.

P1-M09 Delivery challan Purpose: proof of delivery that drives billing and settles disputes. UX flow: challan is created automatically when a trip completes; ops/accounts open Challans → filter Unbilled → select one or many → Create Invoice. A challan can be printed (A4, 3-copy, thermal), shared as PDF/link, or corrected before billing. Screens: SCR-M09-01 Challan list (tabs: Unbilled, Billed, Cancelled, Unverified) · -02 Challan detail · -03 Challan correction request · -04 Print templates. Lifecycle: Generated → Delivered (receiver-verified) → Invoiced \| Cancelled. Key fields: challan no. (series per branch/FY), date and time, customer, site, order, PO ref, product/grade, quantity, unit, vehicle, driver, plant, batch-slip no., receiver and evidence, e-way bill no. (if applicable), linked trip, linked invoice, remarks. Business rules BR-M09-01 One challan per completed trip; auto-numbered at completion. BR-M09-02 After invoicing, the challan is locked; correction requires invoice cancellation/credit note. BR-M09-03 Quantity corrections before invoicing need approval and keep the original in history. BR-M09-04 Unbilled-challan ageing is a monitored KPI; challans unbilled beyond N days raise an alert (revenue-leakage control). BR-M09-05 Tenant billing mode per customer: per challan, daily consolidated, per order, monthly; invoice creation honours it. User stories US-M09-01 As ops, I want challans generated automatically, so that nobody types them. AC: completed trip → challan within 5 s; numbering gap-free per series. US-M09-02 As accounts, I want an Unbilled challans worklist grouped by customer and site, so that no delivery is forgotten. AC: shows quantity, value, age; select multiple → invoice. US-M09-03 As ops, I want to print or share a challan, so that drivers carry and customers receive proof. AC: branded A4 and compact format; share by WhatsApp/link. US-M09-04 As accounts, I want to request a correction with a reason, so that errors are fixed with control. AC: approval needed; original and corrected values stored; invoice draft updates if not posted. US-M09-05 As a manager, I want an alert on unverified or long-unbilled challans, so that cash is not leaked. AC: dashboard widget and daily digest.

P1-M10 Sales invoice

Purpose: turn verified deliveries into a correct, GST-compliant invoice with one click, and make the money owed visible immediately.

UX flow: Challans → Unbilled worklist → tick challans of one customer/site → Create Invoice → review screen (lines grouped by product and rate, charges shown as separate lines, GST type chip “CGST+SGST” or “IGST”, due date) → Consequence preview (ledger, outstanding after posting, credit used %, GST effect) → Post → number assigned → Share / Print / Generate e-invoice / Generate e-way bill / Record payment. Other entry points: from an order (services or non-delivery items), pro-forma (advance), or the Bulk invoice run for customers on daily/monthly billing.

Screens: SCR-M10-01 Invoice list (tabs: Draft, Pending approval, Posted, Overdue, Paid, Cancelled) · -02 Invoice form with challan picker · -03 Invoice detail · -04 Consequence preview dialog · -05 e-invoice / e-way bill panel · -06 Pro-forma list and form · -07 Print templates (tax invoice, bill of supply, pro-forma) · -08 Bulk invoice run (preview and exceptions) · -09 Public invoice view page (PDF download, UPI QR).

Lifecycle: Draft → Pending approval (only if a rule triggers) → Posted → Part-paid → Paid \| Cancelled (reversed). Overdue is a derived flag (due date passed and balance \> 0), not a status. Pro-forma: Draft → Sent → Converted \| Expired \| Cancelled.

Key fields: invoice no. (assigned at posting), date, due date, type (tax invoice / bill of supply), branch and supplier GSTIN, customer, billing and delivery site (state drives tax), place of supply, reverse-charge flag, order ref, customer PO ref, linked challans, lines (product, HSN/SAC, qty, unit, rate, discount, taxable, CGST/SGST/IGST, total), charge lines, TCS (if configured), round-off, grand total, amount in words, payment terms, bank/UPI, notes and terms, e-invoice block (IRN, ack no./date, signed QR, status), e-way block (EWB no., date, validity, vehicle, distance, transporter), salesperson, dispute-hold flag.

Business rules - BR-M10-01 Only Delivered (receiver-verified) challans can be invoiced; Unverified ones need the invoice.unverified approval with a reason. - BR-M10-02 The customer’s billing mode (BR-M09-05: per challan, daily, per order, monthly) decides grouping; a user may override with a reason. - BR-M10-03 Line rates come from the order line (frozen, BR-M06-02); charge lines come from trip calculations (BR-M08-05/06). Changing a rate at invoicing needs approval and a reason. Invoiced quantity can never exceed delivered quantity (BR-M06-07). - BR-M10-04 Place of supply = delivery-site state versus branch GSTIN state (Section 10.3). Override needs a reason and is audited. Tax type is fixed at posting. - BR-M10-05 Number is assigned at posting: gap-free per branch/FY series, ≤16 characters (Section 7.3). Posting date must be in an unlocked period; back-dating needs period.override. - BR-M10-06 A posted invoice is immutable. Cancel is allowed only if no receipt is allocated (unallocate first) and either no IRN exists or the portal cancellation window (tenant setting, default 24 h) is still open; otherwise issue a credit note. Cancel reverses ledger, GST and outstanding, and returns the challans to Unbilled. - BR-M10-07 Due date = invoice date + payment terms (customer override allowed). Overdue chips and ageing use due date. - BR-M10-08 Invoice type follows the branch registration: tax invoice for regular, bill of supply for composition/exempt. Reverse-charge flag changes the tax treatment and print text. - BR-M10-09 Round-off to the nearest rupee (tenant setting), posted to the Round-off ledger, never more than ₹1. - BR-M10-10 e-invoice: when the branch is e-invoice applicable and the customer has a GSTIN (B2B), the system prepares portal-compliant JSON. Setting Enforce blocks final print until IRN details are captured; Warn only warns. Where enforced, the IRN is generated before the final print; the print step follows the portal response, not the other way round. Phase 1 = JSON export + response import; direct API is Phase 5. - BR-M10-11 e-way bill: generated for goods movements above the configured value threshold; JSON export + capture of EWB number, validity and Part-B vehicle updates; expiry alert 4 h before validity ends. Whether RMC movement by transit mixer needs an e-way bill is an open question (OQ-05). - BR-M10-12 Pro-forma carries no accounting effect and no GST liability; converting it to a tax invoice follows the tenant’s advance-and-supply policy (A-10). - BR-M10-13 An invoice flagged Dispute hold (M19) stays visible but is excluded from reminder cadences until the hold is lifted. - BR-M10-14 TCS (Sec. 206C(1H)) is calculated on the invoice value above the cumulative annual threshold, only for customers in a tenant turnover class that the CA has configured as applicable. The TCS line is shown separately, posts to the TCS ledger, and is excluded from the taxable value. - BR-M10-15 TCS is not applied where the customer has submitted a valid exemption or the tenant setting is off. Each override is audited.

User stories - US-M10-01 As accounts, I want to create an invoice from selected unbilled challans, so that I never retype quantities. AC: only one customer/site/GSTIN per invoice; lines grouped by product and rate with challan references; on posting the challans turn Invoiced and become read-only. - US-M10-02 As accounts, I want pumping, part-load, waiting and transport charges to appear as separate lines automatically, so that no charge is forgotten. AC: each charge has its own HSN/SAC, GST rate and ledger; editable only with reason; waived charges show zero with the waiver reason. - US-M10-03 As accounts, I want the GST type decided automatically from the delivery site, so that tax is right. AC: automated tests cover intra-state, inter-state, SEZ/exempt flags; override requires reason; chip shown before posting. - US-M10-04 As accounts, I want a consequence preview before posting, so that I see the effect first. AC: shows debit/credit lines, new outstanding, credit used %, GST payable effect; Post and Back buttons. - US-M10-05 As accounts, I want a branded PDF with number, GSTIN, items, charges, bank, UPI QR, terms and amount in words, so that customers can pay and accept it. AC: A4 layout; share by WhatsApp/email/link with delivery log; consent checked for WhatsApp. - US-M10-06 As accounts, I want a bulk invoice run for daily/monthly customers, so that billing takes minutes. AC: pick billing mode and date range; preview shows invoices to be created, exceptions (unverified, credit hold, rate missing); generate as drafts or post; summary report. - US-M10-07 As accounts, I want e-invoice JSON for selected invoices and an import for the portal response, so that IRN and QR are stored and printed. AC: validation errors listed per invoice before export; import matches by invoice number; invoice shows IRN/ack and QR; failed imports listed with reason. - US-M10-08 As ops or accounts, I want e-way bill JSON and a place to store EWB details, so that goods move legally. AC: mandatory fields validated; vehicle change logged; expiring bills listed on Home. - US-M10-09 As accounts, I want to cancel an invoice with a reason, so that mistakes are corrected without deleting records. AC: blocked with an explanation when receipts or an expired IRN window exist; reversal voucher linked; challans released. - US-M10-10 As sales, I want a pro-forma invoice for advance payment, so that customers can pay before supply. AC: numbered in its own series; converts to tax invoice; expiry date. - US-M10-11 As a manager, I want an overdue invoice view with one-tap reminders, so that collections start early. AC: chips by days overdue; reminder uses approved template; log kept. - US-M10-12 As a customer, I want to open an invoice link and download the PDF, so that I can process it. AC: no login; link expires per tenant setting; downloads audited. - US-M10-13 As accounts, I want TCS applied automatically above the threshold, so that collections are compliant. AC: cumulative sales per customer per FY shown on the invoice preview; TCS line visible; ledger posted.

P1-M11 Receipts, payments, notes and collections

Purpose: record every rupee in or out once, allocate it to the right bills, and make collection a daily routine instead of a surprise.

UX flow — receipt: Quick Create → Record Receipt → pick customer (credit and outstanding shown) → bills appear oldest first with Allocate fields and an Auto-allocate (oldest first) button → choose mode (cash, bank transfer/NEFT/RTGS/IMPS, UPI, cheque, post-dated cheque, adjustment) → reference no., date, deposit account → optional TDS deducted by customer and bank charges → leftover goes to Keep as advance (against customer or order) → consequence preview → Post → receipt number → share receipt. Supplier payments reuse the same engine (M07, US-M07-07).

UX flow — collections: Collections worklist (buckets 0–30, 31–60, 61–90, 90+ days) → one-tap call/WhatsApp → log promise to pay (date, amount) → system reminds on that date and escalates if broken.

Screens: SCR-M11-01 Receipt list · -02 Receipt form with allocation grid · -03 Receipt detail and print · -04 Advances and unallocated list · -05 Cheque/PDC register · -06 Credit/debit note list and form · -07 Collections worklist · -08 Customer statement · -09 Bank reconciliation workspace · -10 Write-off / adjustment form · -11 Reminder rules and templates.

Lifecycles: Receipt: Draft → Posted → Cancelled (reversed). Cheque: Received → Deposited → Cleared \| Bounced (PDC: Held → Due → Deposited …). Credit/debit note: Draft → Pending approval → Posted → Cancelled. Promise to pay: Open → Kept \| Broken \| Cancelled.

Business rules - BR-M11-01 Allocated total ≤ receipt amount; the excess becomes an advance that stays visible on the customer and reduces credit exposure (BR-M03-03). - BR-M11-02 Cash receipts above the configured limit (indicative ₹2 lakh per customer per day/transaction; tenant’s CA confirms) show a warning or block per tenant setting. - BR-M11-03 Cheque lifecycle: allocation of a PDC happens only when it is deposited and cleared (tenant setting: allocate on receipt vs on clearance). A bounce reverses allocation, restores outstanding, notifies the owner and can raise a bounce-charge debit note. - BR-M11-04 TDS deducted by the customer posts to TDS Receivable; certificate tracking: Pending → Received; mismatch report versus certificates. - BR-M11-05 Credit note must reference an invoice (standalone allowed with reason and approval). Reason taxonomy: sales return/rejected goods, rate difference, short supply, discount/rebate, billing error, other. It reduces outstanding and output GST in the same period; separate gap-free series; warning when beyond the statutory time limit for GST credit notes. - BR-M11-06 Debit note to customer covers additional charges (waiting, rate increase, bounce charge) and increases outstanding and output GST. - BR-M11-07 Supplier-side debit/credit notes (purchase returns, rate differences) adjust the supplier ledger and ITC. - BR-M11-08 Write-off/adjustment needs approval and posts to Bad debts / Discount allowed; GST treatment follows tenant CA setting. - BR-M11-09 Posted receipts are immutable; cancellation reverses allocations and ledger with a reason. - BR-M11-10 Reminder cadences (default: 3 days before due, on due date, +3, +7, +15 days) use approved transactional templates, respect quiet hours and stop automatically when the bill is paid or put on dispute hold. Sending uses transactional consent rules (7.5), not promotional opt-in. - BR-M11-11 Bank reconciliation: bank statement CSV/Excel import; suggested matches by amount, date window and reference; unmatched lines can be turned into a receipt, payment or expense in place; reconciled lines are locked; closing book balance versus statement balance difference shown. - BR-M11-12 Refund of an unallocated advance creates a payment voucher to the customer and reduces advance. - BR-M11-13 Retention deducted by the customer posts to a Retention Receivable ledger, not to Bank. Retention is tracked per customer, per invoice, with a release date. Release is a receipt type. - BR-M11-14 Where the tenant CA has configured advance-tax-on-receipt, an advance receipt creates the GST liability for that receipt. When the advance is allocated to an invoice, the liability is adjusted against the invoice tax. Off by default until configured (OQ-04).

User stories - US-M11-01 As accounts, I want to record a receipt and allocate it to bills in one screen, so that outstanding is always current. AC: oldest-first auto-allocation; manual override; live remaining amount; posting updates ledger and invoice status (Part-paid/Paid). - US-M11-02 As accounts, I want to book an advance against a customer or order, so that early payments are not lost. AC: advance listed in SCR-M11-04; one click to apply to an invoice; shown in credit exposure. - US-M11-03 As accounts, I want a cheque register with due and bounce handling, so that cheques are tracked. AC: status chips; due-today list; bounce action reverses and notifies; optional bounce charge. - US-M11-04 As accounts, I want to track TDS deducted by customers, so that I can claim credit. AC: amount, section, certificate status; report by customer and quarter. - US-M11-05 As accounts, I want to issue a credit note against an invoice, so that returns and rate differences are corrected. AC: reason mandatory; GST reversed proportionally; approval above threshold; linked on invoice detail. - US-M11-06 As accounts, I want to issue a debit note for additional charges, so that extra billing is compliant. AC: own series; GST computed; appears in customer ledger. - US-M11-07 As purchase/accounts, I want debit/credit notes for supplier returns and rate differences, so that payables are right. AC: linked to supplier invoice; ITC adjusted; ledger updated. - US-M11-08 As a collector, I want a ranked collections worklist with promise-to-pay, so that I focus on the largest overdue bills. AC: sort by amount × days overdue; one-tap call/WhatsApp; outcome required; broken promises flagged. - US-M11-09 As a manager, I want automatic payment reminders, so that nobody chases manually. AC: cadence editable; consent and quiet hours respected; stops when paid; log visible on customer. - US-M11-10 As accounts, I want a customer statement of account, so that balances can be confirmed. AC: period, opening, invoices, receipts, notes, closing, ageing; PDF/WhatsApp share; customer can reply “Confirm” via link. - US-M11-11 As accounts, I want to reconcile my bank statement, so that books match the bank. AC: import template; match suggestions with confidence; create-in-place for unmatched; difference must reach zero to mark month reconciled. - US-M11-12 As an approver, I want to approve write-offs and adjustments, so that losses are controlled. AC: card shows invoice age, history; decision audited. - US-M11-13 As accounts, I want to refund an advance, so that customer credit can be returned. AC: payment voucher created; advance balance drops; bank ledger posts. - US-M11-14 As accounts, I want retention tracked as a receivable with its release date, so that outstanding is not overstated. AC: retention appears in ageing separately; release creates a receipt; report by contract. - US-M11-15 As accounts, I want GST on advances handled per the CA’s configuration, so that output tax is right. AC: advance GST shown on the receipt; adjusted on invoice; report of advance tax liability.

P1-M12 Accounting (books, ledgers, period control)

Purpose: books that are correct by construction because every posting comes from a business document — no re-typing into Tally or Excel.

UX flow: Accounts menu → Vouchers (auto-generated list; manual Journal and Contra only) → Books (Day book, Cash book, Bank book, any Ledger) → Statements (Trial balance, Profit & Loss, Balance sheet) → Period control (lock months, year-end wizard) → Tally export wizard. Every figure drills down: statement → ledger → voucher → source document.

Screens: SCR-M12-01 Chart of accounts tree · -02 Voucher list and detail · -03 Manual journal/contra form · -04 Day / cash / bank book · -05 Ledger view · -06 Trial balance · -07 Profit & Loss · -08 Balance sheet · -09 Period lock · -10 Year-end close wizard · -11 Tally export wizard · -12 Ledger mapping (for Tally) · -13 Audit trail viewer (shared with M02).

Business rules - BR-M12-01 Every voucher is double-entry and balances (debits = credits) inside one database transaction; unbalanced postings are impossible. - BR-M12-02 Vouchers are generated by source documents using the rules in Section 10.2. Manual vouchers are limited to Journal and Contra and cannot post to control accounts (Debtors, Creditors, GST, TDS) except through a party line. - BR-M12-03 Posted vouchers are immutable; a correction is a reversal voucher linked to the original plus a new voucher. - BR-M12-04 Period lock (month/year) blocks posting; unlocking needs Admin/Accounts permission with a reason and is audited. - BR-M12-05 Stock treatment (A-04): Phase 1 uses the periodic method — Purchases are debited on supplier invoice; at period end an optional manual Closing stock journal adjusts stock. Default closing stock is zero for RMC and direct-delivery businesses. Order-level margin uses trip purchase cost (operational) and a margin reconciliation report explains the difference to book gross profit. - BR-M12-06 Revenue is recognised at invoice posting. Delivered-but-unbilled value is reported separately (BR-M09-04), never booked silently. - BR-M12-07 P&L structure: Revenue → Direct costs (purchases, freight-in, pumping hire, loading) → Gross profit → Indirect expenses → Other income → Depreciation → Profit before tax. Balance sheet uses simplified Schedule III-style grouping; chart-of-accounts template per tenant, editable, system accounts locked. - BR-M12-08 Year-end close checks: no draft documents in the year, bank reconciled, closing stock journal posted (if used), trial balance balanced; closing carries balances forward; prior year remains viewable and lockable. - BR-M12-09 Every voucher line carries branch and optional customer site and order dimensions so profitability can be sliced without a second ledger. Trip dimension is added where a voucher line is linked to a trip. - BR-M12-10 Tally export: masters (ledgers with groups) and vouchers (sales, purchase, receipt, payment, journal, contra, credit/debit note) as XML for a date range; ledger-name mapping screen; exported vouchers are flagged and re-export asks for confirmation. - BR-M12-11 Fixed assets in Phase 1 are recorded by purchase voucher to an asset ledger and depreciation by journal; the full register arrives in Phase 2 (E2-07).

User stories - US-M12-01 As accounts, I want a ready chart of accounts for construction trading, so that I can start without accounting design. AC: template loaded at tenant creation; add/rename/deactivate non-system accounts; groups map to P&L and BS. - US-M12-02 As accounts, I want vouchers to appear automatically from invoices, receipts, payments, expenses and notes, so that nothing is double-entered. AC: each voucher shows its source link; mismatch between a document and its voucher is impossible (reconciliation test). - US-M12-03 As accounts, I want manual journal and contra vouchers, so that adjustments and cash-to-bank transfers are possible. AC: balance check; narration; attachment; cannot touch control accounts directly. - US-M12-04 As accounts, I want Day, Cash and Bank books, so that I can verify daily activity. AC: date range; opening/closing; running balance; export; drill-down. - US-M12-05 As an owner, I want Trial balance, P&L and Balance sheet at any date, so that I see my position. AC: comparative period; branch filter; drill to ledger and voucher; print/PDF/Excel. - US-M12-06 As accounts, I want to lock periods, so that closed months cannot change. AC: month lock; override permission; blocked actions show a clear message. - US-M12-07 As accounts, I want a year-end wizard, so that the financial year closes safely. AC: checklist of blocking checks; carry-forward preview; reversible only before next-year postings. - US-M12-08 As an accountant using Tally, I want to export masters and vouchers, so that my CA can import them. AC: mapping screen; date range; XML file; export log; re-export warning. - US-M12-09 As an auditor, I want an unalterable audit trail for accounting data, so that compliance is met. AC: log cannot be disabled or edited by any role; filters; CSV export. - US-M12-10 As an owner, I want margin reconciliation between order-level margin and book gross profit, so that I trust both. AC: report lists differences by cause (closing stock, unbilled deliveries, unposted bills).

P1-M13 GST

Purpose: make month-end GST a review-and-export job, not a rework job.

UX flow: Accounts → GST opens the GST workspace: month selector, readiness score (checks passed), tiles for GSTR-1 (outward), GSTR-3B workings, ITC register, e-invoice register, e-way register. Fix the Exceptions list → tick Mark as reviewed → Export JSON/Excel → file on the portal (outside the product) → Mark as filed (locks the GST period).

Screens: SCR-M13-01 GST workspace · -02 GSTR-1 tables (B2B, B2CL, B2CS, credit/debit notes registered and unregistered, HSN summary, document series summary) · -03 GSTR-3B workings (outward tax, ITC, reverse charge, net payable) · -04 ITC register · -05 Reverse-charge register · -06 e-invoice register · -07 e-way register · -08 GST exceptions · -09 HSN and tax-rate master · -10 Period filing log.

Business rules - BR-M13-01 Reports read the same posted invoices, notes and supplier bills as the books; no separate GST entry. - BR-M13-02 Pre-export checks: missing or invalid GSTIN on B2B, missing HSN, place of supply mismatch, invoice number \>16 characters or out of series, e-invoice applicable but IRN missing, credit note without reference. Errors block export; warnings do not. - BR-M13-03 Rate and HSN come from the effective-dated master (BR-M03-04); rate changes never alter past documents; a rate-change log is kept. - BR-M13-04 ITC eligibility per supplier invoice line: eligible / ineligible / blocked (for example blocked credit categories) / reverse charge; ITC for a bill unpaid beyond the statutory 180-day window is flagged for reversal review. - BR-M13-05 GSTR-1 export follows the portal offline-tool JSON structure; Excel export mirrors the portal tables. - BR-M13-06 Marking a period Filed locks GST-relevant documents of that month; later changes go through amendment/credit-note flows and appear in the next period. - BR-M13-07 The product does not file returns or compute interest/late fees in Phase 1; GSTR-2B reconciliation is Phase 2 (E2-06). - BR-M13-08 Rates, thresholds and rules are tenant-editable master data; help text states that the tenant and its tax advisor remain responsible for filing decisions. - BR-M13-09 HSN digits in GSTR-1 and invoices follow the tenant’s turnover slab, as set in the effective-dated configuration (CA to confirm the current slab). The setting drives both the invoice line and the HSN summary. - BR-M13-10 Rate changes and category overrides (M03) are recorded in the rate-change log with the effective date and the approver.

User stories - US-M13-01 As accounts, I want a GSTR-1 workspace with portal-shaped tables, so that I can check outward supplies quickly. AC: totals tie to sales register; drill-down to invoices; export JSON and Excel. - US-M13-02 As accounts, I want a GST exceptions list, so that errors are fixed before filing. AC: each exception links to the document; count shown as readiness %; re-check on demand. - US-M13-03 As accounts, I want GSTR-3B workings, so that I know tax payable. AC: output tax, ITC (eligible/blocked), reverse charge, net payable; tie-out to ledger balances. - US-M13-04 As accounts, I want an ITC register by supplier invoice, so that I claim only what is valid. AC: eligibility flag editable with reason; 180-day alert; export. - US-M13-05 As accounts, I want HSN summary and document-series summary, so that mandatory tables are ready. AC: HSN digits per tenant setting; series gaps listed. - US-M13-06 As accounts, I want e-invoice and e-way registers, so that I can find pending IRN or expiring EWB. AC: filters by status; bulk export; links to documents. - US-M13-07 As an admin, I want to maintain HSN and GST rates with effective dates, so that rate changes are safe. AC: new rate applies from date; past documents unchanged; change log. - US-M13-08 As accounts, I want to mark a month as filed, so that data is locked. AC: unlock needs permission and reason; audited.

P1-M14 Expenses and other income

Purpose: capture every cost — especially the ones tied to a trip, order or site — in seconds, from a phone, with approval where needed.

UX flow: Quick Create → Expense (≤5 fields on phone: date, category, amount, paid from, photo of bill) → More details (vendor, GST, TDS, order/site/trip link, notes) → Save → approval if amount above rule → posted. Petty cash: assign a float to a user; their expenses draw from it; top-up and settle screens.

Screens: SCR-M14-01 Expense list · -02 Expense form (mobile and desktop) · -03 Expense detail · -04 Petty cash / imprest · -05 Reimbursements · -06 Recurring expense templates · -07 Other income and capital entries · -08 Trip and order direct costs · -09 Expense categories.

Lifecycle: Draft → Pending approval → Approved → Posted → Paid \| Rejected \| Cancelled.

Business rules - BR-M14-01 Categories are tenant-defined and typed Direct (purchases-related: freight, hired TM, diesel, driver allowance, pump hire, loading, plant charges), Indirect (rent, salaries via journal, utilities, marketing), Capital (asset purchase) or Other income/Capital introduced. Type drives P&L placement. - BR-M14-02 A direct expense can be linked to order, pour, trip or site; linked costs flow to order and trip profitability (Section 10.5). Hired-vehicle freight entered on a supplier bill can be allocated across trips. - BR-M14-03 Approval rule by amount (default ₹25,000) and category; requester cannot approve own expense. - BR-M14-04 Expenses with GST capture supplier GSTIN, invoice no. and ITC flag; duplicate check on supplier + bill no. + amount. - BR-M14-05 TDS-bearing payments (rent, professional fees, contractors) use the same TDS engine as supplier payments (BR-M07-05). - BR-M14-06 Petty-cash balance cannot go negative without an override; unsettled advances older than N days are flagged. - BR-M14-07 Bill photo or PDF is mandatory above a configurable amount; employee-paid expenses create a reimbursement payable. - BR-M14-08 Salaries and statutory payroll are out of Phase 1 scope (A-12); they enter books by journal or Excel journal import. - BR-M14-09 Freight from a goods transport agency (GTA) is captured with transporter GSTIN and the reverse-charge flag. Where RCM applies, the system computes GST under RCM and posts the payable and the ITC on the same voucher. - BR-M14-10 TDS under Sec. 194C is applied to transporter payments per the tenant’s TDS category (BR-M07-05). - BR-M14-11 Freight lines carry the trip or order link so that they flow to profitability (BR-M14-02).

User stories - US-M14-01 As any employee, I want to log an expense with a photo from my phone in 20 seconds, so that costs are captured at source. AC: five-field form; camera upload; saved offline-tolerant; visible to Accounts instantly. - US-M14-02 As a manager, I want to approve expenses from my phone, so that spending is controlled. AC: approval card with bill image; approve/reject with comment. - US-M14-03 As ops, I want to link diesel, freight or pump-hire costs to a trip or order, so that profit is true. AC: link picker; order detail shows direct costs; margin updates. - US-M14-04 As accounts, I want petty cash floats per person, so that cash is accountable. AC: top-up, spend, settle; negative balance blocked; statement per person. - US-M14-05 As accounts, I want recurring expense templates, so that rent and similar costs post on time. AC: schedule; generates drafts for review; skip/stop. - US-M14-06 As accounts, I want to record other income and capital introduced, so that books are complete. AC: ledger mapping; separate from sales; appears in P&L/BS properly. - US-M14-07 As owner, I want expense analytics by category, branch, site and month, so that I find leaks. AC: trend, top categories, drill to entries. - US-M14-08 As accounts, I want to reimburse employee-paid expenses in bulk, so that settlement is simple. AC: select expenses → one payment voucher; employee ledger cleared. - US-M14-09 As accounts, I want GTA freight to carry the correct reverse-charge and TDS treatment automatically, so that transport costs are compliant. AC: RCM flag from transporter GSTIN and category; TDS 194C applied per transporter category; ITC shown on the line.

P1-M15 Dashboard and home

Purpose: each role sees what needs attention now, in two minutes, with every number clickable.

UX flow: Login → Home (role-specific) → cards show metric, trend arrow vs previous period and a short explanation; click opens the filtered list behind the number. A Needs your attention strip lists approvals, overdue follow-ups, expiring quotes, cheques due, unbilled challans, expiring vehicle documents and pending e-way bills. Period filter (Today / Week / Month / FY / custom) and branch filter in the header; the setup checklist shows until go-live is complete.

Screens: SCR-M15-01 Owner home · -02 Sales home · -03 Operations home (control-room view) · -04 Purchase home · -05 Accounts home · -06 Driver home (minimal) · -07 Card configuration (show/hide, reorder).

Role cards: - Owner / Management: Sales, collections, expenses, gross profit, cash and bank balance, receivable and payable, overdue, credit-risk customers (top 5), today’s m³ and deliveries, unbilled challans value, pending approvals. - Sales: My enquiries by stage, follow-ups due/overdue, quotes expiring, win rate, my pipeline value, my customers’ overdue. - Operations: Today’s pours and trips by status, delays, plant utilisation, TMs available, exceptions (rejections, breakdowns), tomorrow’s schedule. - Purchase: POs awaiting approval, pending receipts (GRN), payables due, MSME bills nearing 45 days, rate variance alerts. - Accounts: Unbilled challans, invoices pending post, receipts to allocate, cheques due, bank unreconciled, GST readiness, period lock status.

Business rules - BR-M15-01 Dashboard numbers come from read models updated by domain events (target lag ≤60 s) and must equal the corresponding report totals (automated tie-out test). - BR-M15-02 Cost, margin and profit cards are hidden without view_cost; data scope (branch/own) applies to every card. - BR-M15-03 Every card has a definition tooltip linking to the KPI definition (Section 10.5). - BR-M15-04 Home loads in ≤3 s on a typical 4G phone for the Owner view; heavy cards load lazily with skeletons.

User stories - US-M15-01 As an owner, I want one screen with cash, receivable, payable, sales and profit, so that I can decide quickly. AC: cards with trend; click-through; period and branch filters. - US-M15-02 As a sales executive, I want my day on one screen, so that I never miss a follow-up. AC: overdue and today lists; one-tap call; quick-add enquiry button. - US-M15-03 As a dispatcher, I want a live operations view, so that I run the day. AC: shows trips by status; auto-refresh 30 s; delay colouring; link to Dispatch Board. - US-M15-04 As accounts, I want an action-oriented home, so that I clear pending work. AC: counts link to worklists; zero states show “All clear”. - US-M15-05 As a user, I want to hide or reorder cards, so that Home fits my work. AC: saved per user; reset to default. - US-M15-06 As a manager, I want an attention strip, so that exceptions find me. AC: items ranked by urgency; each opens the exact record.

P1-M16 Reports

Purpose: consistent, filterable, exportable reports built on the same transactions as operations.

Common behaviour (all reports): filters bar with saved views; as-on date or period; branch and customer/site/product filters; column chooser; grand totals; drill-down to documents; export Excel/CSV/PDF; print; schedule by email (daily/weekly/monthly); cost columns hidden without view_cost; data scope enforced; “last refreshed” stamp.

Reports: - R-S01 Sales register (daily/monthly) \| Filters: period, customer, site, product, salesperson \| Columns: invoice, date, customer, site, taxable, GST, total \| Drill-down: invoice - R-S02 Sales by customer / site / product \| period, grouping \| qty, value, share %, trend \| invoices - R-S03 Pending orders \| customer, site, product \| ordered, delivered, balance qty and value \| order - R-S04 Quotation analytics \| owner, period \| sent, won, lost, value, win % \| quotation - R-C01 Enquiry funnel and lost reasons \| source, owner \| stage counts, value, conversion % \| enquiry - R-P01 Purchase register \| period, supplier, product \| bill, taxable, GST, total, ITC flag \| bill - R-P02 Purchase rate history and variance \| product, supplier \| rate by date, min/avg/max, variance % \| PO/bill - R-P03 Goods received not billed (GRN) \| supplier, product, age \| GRN qty, value, age \| GRN - R-D01 RMC delivery summary \| grade, site, plant, TM, date \| m³, trips, on-time %, turnaround \| trip - R-D02 TM and driver utilisation \| vehicle, driver, date \| trips, m³, idle time \| trip - R-D03 Delivery exceptions \| type, plant \| rejections, returns, wastage, breakdowns, cost \| trip - R-D04 Unbilled challans ageing \| customer, age \| count, qty, value, days \| challan - R-A01 Receivable ageing \| as-on, customer, owner \| 0–30/31–60/61–90/90+ \| invoice - R-A02 Payable ageing incl. MSME \| as-on, supplier \| buckets, MSME days, TDS, interest exposure (MSMED s.16, indicative, see OQ-07) \| bill - R-A03 Customer and supplier ledger / statement \| party, period \| opening, entries, closing \| voucher - R-A04 Collections performance \| period, owner \| due, collected, % collected, promises kept \| receipt - R-A05 Retention register \| customer, contract \| retained, released, balance, release date \| invoice - R-F01 Profitability by order / customer / site / product / invoice / month \| period, grouping \| revenue, purchase cost, direct costs, margin ₹ and % \| order/trip - R-F02 Margin reconciliation \| period \| order-level vs book gross profit \| voucher - R-F03 Trial balance, P&L, Balance sheet, Cash/Bank/Day book \| date range, branch \| standard layouts \| ledger - R-G01 GST packs (M13) \| month \| portal-shaped tables \| invoice - R-X01 Expense analytics \| category, branch, site \| amount, trend \| expense - R-X02 Approvals and SLA \| type, approver \| pending, age, breaches \| request - R-U01 Audit and login report \| user, entity, date \| action, before/after \| record

UX flow: Reports (left menu) → pick a report from the catalogue → filters bar (as-on date or period, branch, party, product) → results with grand totals → click any row to drill to the source document → Export (Excel, CSV, PDF), Print, Save view, or Schedule by email.

Screens: SCR-M16-01 Report catalogue (grouped by Sales, Purchase, RMC, Profit, Outstanding, Finance, GST, Expenses, Audit) · -02 Report viewer (shared by all reports) · -03 Saved views manager · -04 Scheduled reports (recipients, format, time in IST, delivery log) · -05 Export history.

Business rules - BR-M16-01 Every report reads the same posted transactions as the operational screens; no report keeps its own copy of the data. - BR-M16-02 Cost, margin and profit columns are hidden, and their values excluded from exports and scheduled emails, unless the viewer has view_cost. - BR-M16-03 An export or scheduled report carries the same totals and filters as the screen it came from, and each export is written to the audit log. - BR-M16-04 A scheduled report is sent only to recipients whose own permissions and data scope cover the report; otherwise that recipient’s copy is withheld and logged. - BR-M16-05 Every report shows a last-refreshed time. Figures built from read models are tied out to source transactions nightly (BR-M15-01). - BR-M16-06 Interest and other computed exposures (for example R-A02 MSME interest) are labelled indicative and show the rate and method used.

User stories - US-M16-01 As an owner, I want profitability by customer, site, product and order, so that I know where I earn. AC: formulas per Section 10.5; hidden without view_cost; drill to trips; ties to dashboard. - US-M16-02 As any user, I want to save a filtered view of a report, so that I reopen it in one click. AC: named, private or shared; default view per user. - US-M16-03 As a manager, I want reports emailed on a schedule, so that I get numbers without logging in. AC: recipients, format, time (IST); delivery log; respects the recipient’s permissions. - US-M16-04 As accounts, I want any report exported to Excel/CSV/PDF with the same totals as on screen, so that sharing is easy. AC: export matches filters; export audited. - US-M16-05 As an owner, I want ageing reports with drill-down to invoices, so that I can chase specific bills. AC: bucket click opens filtered list; export. - US-M16-06 As a manager, I want the margin reconciliation report, so that order margin and books can be trusted together. AC: differences categorised; totals tie. - US-M16-07 As accounts, I want to see MSME interest exposure per supplier, so that I can prioritise payments. AC: column visible with the rate basis shown in the report footer; export carries the same numbers; figures marked indicative until the CA confirms the method.

P1-M17 Business promotion and catalogue

Purpose: use the CRM and sales history to promote products responsibly, and let prospects see and request products from a shareable catalogue.

UX flow — campaign wizard: Marketing → New campaign → 1 Audience (segment builder: customer type, products bought, last order older than N days, city/area, tags, owner; only contacts with valid consent for the chosen channel) → 2 Channel (WhatsApp, SMS, email) → 3 Template (approved templates only; variables preview) → 4 Schedule (now/later; quiet hours respected) → 5 Review (recipient count, excluded count with reasons, estimated cost) → Send → Results (sent, delivered, read, replies, enquiries generated).

UX flow — catalogue: Marketing → Catalogue → choose products to publish (image, description, key specs, unit, optional indicative price) → preview → Share as PDF, public web page or QR → visitors tap Request quote → form creates an Enquiry (source = catalogue/campaign) and notifies the owner.

Screens: SCR-M17-01 Campaign list · -02 Campaign wizard · -03 Campaign results · -04 Segments · -05 Template registry (DLT/WhatsApp approval status) · -06 Consent register · -07 Catalogue manager · -08 Public catalogue page and Request-quote form · -09 Message wallet and usage.

Business rules - BR-M17-01 Promotional messages go only to contacts with recorded consent for that channel (date, source, wording); opt-out (for example reply STOP, link click, manual) is honoured immediately across channels and logged. - BR-M17-02 Templates must be registered/approved (DLT for SMS, provider-approved for WhatsApp) before use; unapproved templates cannot be selected. - BR-M17-03 Frequency cap per contact (default 2 promotional messages per week) and quiet hours (default 9 pm–9 am) are enforced centrally. - BR-M17-04 Transactional messages (quotes, challans, invoices, reminders) are separate from promotional and never consume promotional consent rules; both are logged. - BR-M17-05 Message cost is estimated before send and metered per tenant (7.1); insufficient wallet or plan quota blocks the send with an upgrade prompt. - BR-M17-06 The public catalogue shows only products marked Published; prices show only if the tenant enables them; no customer-specific data is ever exposed; page works without login and is rate-limited with a CAPTCHA on the form. - BR-M17-07 Enquiries created from the public form include consent checkbox text and timestamp (DPDP Act).

User stories - US-M17-01 As a sales manager, I want to build an audience from CRM and sales data, so that offers reach the right people. AC: segment preview count; excluded contacts shown with reason (no consent, opted out, frequency cap). - US-M17-02 As a sales manager, I want to send a WhatsApp/SMS/email campaign using approved templates, so that promotion is compliant. AC: wizard with review step; send/schedule; delivery log with failures and retries. - US-M17-03 As a manager, I want campaign results including enquiries generated, so that I measure return. AC: sent/delivered/read/replied; enquiries linked by source; cost per enquiry. - US-M17-04 As an admin, I want a consent register, so that I can prove permission. AC: per contact and channel: opt-in date, source, opt-out; export; edits audited. - US-M17-05 As sales, I want to publish a product catalogue as PDF and web link, so that I can share it from my phone. AC: branded; updates reflected without re-sending the link; QR available. - US-M17-06 As a prospect, I want to request a quote from the catalogue page, so that I get a reply fast. AC: form fields (name, mobile, product, quantity, site city, consent); creates Enquiry; confirmation shown; owner notified within 1 minute. - US-M17-07 As an admin, I want a template registry showing approval status, so that only valid templates are used. AC: status chips; variables listed; preview. - US-M17-08 As an admin, I want to see message usage and balance, so that costs are controlled. AC: per channel counts and cost; low-balance alert.

P1-M18 Cross-cutting services (import, search, notifications, documents, help)

Purpose: the platform services of Section 7, delivered as verifiable user-facing features. Build them as shared components first; every module then reuses them.

Screens: SCR-M18-01 Import wizard · -02 Import history and undo · -03 Global search overlay · -04 Notification centre and preferences · -05 Template manager (notification and print) · -06 Attachment viewer · -07 Help drawer and What’s new · -08 Tenant data export · -09 Settings catalogue (Section 7.9).

UX flow: the platform services are reached from the top bar and the Settings area, not from a separate module. Import: Settings → Import → choose entity → download template → upload → validation preview with row-level errors → fix or skip each row → confirm → summary report (undo available for 24 h). Search: Ctrl/Cmd+K from anywhere. Notifications: bell in the top bar → centre → preferences per event and channel. Attachments: any record’s Attachments tab. Help: ? in the top bar opens the help drawer.

Business rules - BR-M18-01 An import never commits partially without confirmation. Every imported batch is recorded as one import batch that can be undone within 24 h, and the undo is audited. - BR-M18-02 Search returns only records the user may see (permission and data scope), and results never reveal the existence of a record the user cannot open. - BR-M18-03 Notifications respect consent and quiet hours (7.5). Transactional messages are sent even outside promotional rules, but each send is logged with channel and template version. - BR-M18-04 Attachments are virus-scanned before download is allowed, and downloads are audited. Files over the tenant limit are rejected at upload with a plain-language message. - BR-M18-05 A tenant data export contains only that tenant’s data; the download link expires, and the request and download are logged. - BR-M18-06 Templates (notification and print) are versioned. A published document keeps the template version it was printed with.

User stories - US-M18-01 As an admin, I want a guided import for customers, sites, suppliers, products, price lists, opening balances and outstanding invoices, so that migration is quick. AC: template download; row-level validation preview; fix/skip; duplicate handling options; undo within 24 h; summary report. - US-M18-02 As any user, I want one search box (Ctrl/Cmd+K), so that I find anything. AC: searches the entities in 7.7 (including GRN, retention and lot/heat no.); grouped, permission-aware, typo-tolerant; results in \<500 ms; recent items shown first. - US-M18-03 As a user, I want a notification centre and channel preferences, so that I receive what matters. AC: in-app list with read/unread; email/SMS/WhatsApp toggles per event; quiet hours; delivery log for admins. - US-M18-04 As an admin, I want to edit notification and print templates with variables and preview, so that messages and documents match our brand. AC: variable picker; preview with sample data; versioned; approved-template status shown for SMS/WhatsApp. - US-M18-05 As any user, I want to attach, preview and download files on any record, so that evidence stays with the transaction. AC: size and type limits (7.4); virus scan status; download audited; version history. - US-M18-06 As an owner, I want a full tenant data export, so that I can back up or leave. AC: zip with CSV/Excel per entity and attachments; request logged; link expires. - US-M18-07 As a new user, I want guided empty states, a setup checklist and a help drawer, so that I can learn inside the product. AC: every empty list explains and offers the primary action; checklist tracks progress; help articles are searchable. - US-M18-08 As an admin, I want one Settings area listing every configuration item of 7.9, so that I can find and change them. AC: search within settings; each setting has help text and shows who changed it last.

P1-M19 Quality samples and disputes

Purpose: record concrete test samples and customer disputes so payment is not silently withheld and quality evidence exists. (Full QA/QC, ITP and NCR remain Phase 4, E4-04.)

UX flow: on a completed RMC trip the dispatcher or site engineer taps Add cube sample (set of 3 or 6 cubes, cast date) → system schedules 7-day and 28-day test reminders → lab result entered (MPa, report file) → pass/fail versus the grade’s characteristic strength → a failed result opens a Dispute automatically. Any user can raise a dispute from a customer, order, trip or invoice: type (quality, quantity, delay, billing, other) → owner → status → resolution (credit note, replacement, rejected claim).

Screens: SCR-M19-01 Sample register · -02 Sample form and result entry · -03 Dispute list and board · -04 Dispute detail with timeline and evidence.

Lifecycle: Sample: Cast → 7-day due → 28-day due → Tested (Pass \| Fail). Dispute: Open → Investigating → Resolved \| Escalated \| Rejected.

Business rules - BR-M19-01 Sample is linked to trip, pour, grade and customer site; test due dates computed from cast date; reminders to the nominated owner. - BR-M19-02 Pass/fail uses the tenant’s acceptance rule per grade (configurable); result edits need a reason and are audited. - BR-M19-03 A failed 28-day result opens a dispute automatically and notifies the manager; the linked invoices may be flagged Dispute hold (BR-M10-13). - BR-M19-04 Resolution by credit note must link to the credit note (M11); every status change is audited. - BR-M19-05 Acceptance references are shown on the sample form as the basis for pass or fail: RMC per IS 456 and IS 516 (sampling frequency per project specification), TMT per IS 1786 with a mill test certificate per heat, cement per IS 269 or IS 1489 with a batch test certificate per lot. The tenant can change the threshold for a project, but the change needs a reason and is audited.

User stories - US-M19-01 As a site engineer, I want to register cube samples against a trip in under a minute, so that evidence exists. AC: mobile form; sample IDs auto-generated; due dates calculated. - US-M19-02 As a plant coordinator, I want test reminders and result entry, so that no test is missed. AC: due list; overdue alert; report upload; automatic pass/fail. - US-M19-03 As a manager, I want disputes tracked with owner and ageing, so that they close. AC: board by status; ageing chips; reminder to owner. - US-M19-04 As accounts, I want disputed invoices excluded from reminders and clearly marked, so that we do not annoy customers or lose track. AC: hold flag visible on invoice and ledger; removed on resolution. - US-M19-05 As an owner, I want a quality and dispute summary by plant and customer, so that I can act on recurring problems. AC: counts, pass %, dispute value; drill-down.

End of Phase 1 module specification

Phase 1 modules M01–M19 are complete when all user stories pass their acceptance criteria, the Phase-1 gate in Section 12.6 is signed off, and the golden-path UAT script (Section 12.5) runs end to end without manual database work.

# BERYNX CONSTRUCTION 360 — MASTER BRD v2.1 ADDENDUM

Merge and completion of v2.0. This addendum (a) records what the review of v2.0 found, (b) patches Phase 1 modules, (c) adds the sections v2.0 referred to but never delivered (9, 10, 12, 13, 14), and (d) sets out the plan for Phases 2–5 user stories.

## A. Status of the uploaded v2.0 file (verified)

| Check                                      | Result                                                                                                                                                                                                     |
|--------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Content present in the uploaded v2.0 .docx | Sections 0–8 and modules P1-M01 to P1-M09 only. The file ends after US-M09-05.                                                                                                                             |
| P1-M10 to P1-M19                           | Not in the uploaded file. Present only in the two excerpt documents supplied separately.                                                                                                                   |
| Sections 9–14                              | Not present in any uploaded file.                                                                                                                                                                          |
| v2.0 Section 2 claims                      | Section 2 says gaps G02, G08, G09, G21, G22, G28 and G29 are fixed in Sections 10, 11, 12 and 8. Sections 10–14 do not exist, so those fixes were never delivered.                                         |
| Broken cross-references                    | 10.2 (posting rules), 10.3 (place of supply), 10.4 (status machines), 10.5 (KPI definitions), 11.3–11.4 (construction terms), 12 (Definition of Done, Phase gate 12.7), 13 (assumptions A-04, A-10, A-12). |
| Phase 2–5                                  | Roadmap v1 gives feature lists only. There are no user stories or acceptance criteria, which the build protocol requires.                                                                                  |

**Merge rule:** the final master document = v2.0 Sections 0–8 + modules P1-M10 to P1-M19 (from the excerpt file, with patches applied inline; see the change record in Section C) + this addendum (Sections 9, 10, 12, 13, 14 and the Phase 2–5 plan). Roadmap v1 is retired and kept only as a historical reference.

## B. Gap review (domain expert, UX and build)

### B1. Domain gaps (Indian construction trading)

| ID  | Gap                                                                                                                                                    | Why it matters                                                                                                          | Fix in v2.1                                                                     |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|
| D01 | No goods receipt (GRN). M07 match needs “received quantity”, but nothing in Phase 1 records what arrived at a godown or site for steel and cement.     | Supplier invoices cannot be matched, and shortages cannot be claimed.                                                   | New screens and rules in P1-M07 (Section C1).                                   |
| D02 | No mill test certificate (MTC), heat/lot number, or cement batch/manufacturer test certificate for TMT and cement.                                     | Structural steel and cement disputes need traceability to the mill or cement batch. Contractors ask for MTCs routinely. | Lot/heat fields in GRN and challan; MTC attachment (C1, C7).                    |
| D03 | Retention money deducted by contractor customers is not modelled.                                                                                      | Common on contractor and government-linked sales. Without it, outstanding balances are overstated.                      | Retention receivable type in P1-M11 (C4).                                       |
| D04 | Goods transport agency (GTA) freight: reverse-charge flag and TDS under 194C are not specified.                                                        | Freight is often the largest direct cost, and the tax treatment differs from ordinary purchases.                        | Transporter RCM and TDS rules in P1-M14 (C5).                                   |
| D05 | TCS on sales (Sec. 206C(1H)) and TDS on purchases above the Sec. 194Q threshold are listed as configurable, but no thresholds or conditions are given. | Wrong deductions or missed collections on large accounts.                                                               | Configurable rules with turnover and value thresholds (C2, C3).                 |
| D06 | RMC pricing is a single rate per product. Real RMC rate cards vary by grade, slump, admixture and pump type.                                           | Quotes become manual and error-prone.                                                                                   | Rate matrix in P1-M03 (C2).                                                     |
| D07 | MSME delay cost is absent. Beyond 45 days the Sec. 16 MSMED Act interest (at three times the RBI bank rate, compounded monthly) applies.               | Hidden cost and a compliance exposure.                                                                                  | Computed interest exposure in the payables report (C6).                         |
| D08 | Quality standards are not referenced: RMC sampling and testing (IS 456, IS 516), TMT (IS 1786), cement (IS 269 / IS 8112).                             | Acceptance rules and disputes need a basis.                                                                             | Default acceptance references per product (C8).                                 |
| D09 | Whether RMC movement by transit mixer needs an e-way bill is not settled in the spec.                                                                  | Compliance risk at checkpoints.                                                                                         | Per-product EWB rule, confirmed by the tenant’s CA (Section 13, OQ-05).         |
| D10 | HSN digit requirement depends on turnover slab and changes with notifications.                                                                         | Invalid GSTR-1 tables.                                                                                                  | HSN digit setting driven by effective-dated config (C9).                        |
| D11 | GST classification of RMC and works contracts varies (supply of goods versus works contract; government contracts).                                    | Wrong GST rate on invoices.                                                                                             | Product rate with per-customer-category override and mandatory reason (C2, C9). |
| D12 | GST on advances: tax may become payable when an advance is received, and this is not reflected in M11.                                                 | Under-declared output GST.                                                                                              | Advance-tax rule, configured by the CA (C4).                                    |
| D13 | Drivers and receivers work on poor networks, but the spec defers all offline working to Phase 5.                                                       | Trip status and proof of delivery get lost at site.                                                                     | Phase 1 action queue with idempotency. Full offline stays Phase 5 (C5, C7).     |
| D14 | Driver screens are English-only in Phase 1. Many drivers read Hindi or Marathi more easily.                                                            | Driver adoption and data quality.                                                                                       | Driver shell only, in Hindi and Marathi, in Phase 1 (C7).                       |
| D15 | Form 26AS / TRACES reconciliation for TDS credits is not specified.                                                                                    | Lost TDS credits.                                                                                                       | Specified as a Phase 2 item (Section 14.2, E2-08).                              |

### B2. UX gaps

| ID  | Gap                                                                                                                | Fix                                                                  |
|-----|--------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------|
| U01 | No error and retry behaviour defined for driver and receiver flows.                                                | Each step in C7 defines the failure state and the retry.             |
| U02 | No accessibility rules for low-literacy and low-end-device users.                                                  | Icon + text labels, large touch targets, one action per screen (C7). |
| U03 | Party data is split into separate customer and supplier masters, but many dealers buy and sell with the same firm. | Unified Party with roles (Section 9 decision D-01).                  |
| U04 | Search and quick-create do not cover GRN and retention.                                                            | Added to the global search list and the Quick Create menu (C1, C4).  |

### B3. Build gaps

| ID  | Gap                                                                                                | Fix                               |
|-----|----------------------------------------------------------------------------------------------------|-----------------------------------|
| B01 | Definition of Done is referenced but missing.                                                      | Section 12.                       |
| B02 | Idempotency for offline submissions and for document numbering under concurrency is not specified. | NFR-07 and NFR-08 (Section 10.6). |
| B03 | No domain-event list, so dashboard read-model lag is untestable.                                   | Event catalogue in Section 9.3.   |
| B04 | Assumptions A-04, A-10 and A-12 are cited but never stated.                                        | Section 13.1.                     |
| B05 | No environments, CI/CD or test-data policy.                                                        | Section 12.2 to 12.4.             |

## C. Change record — where each patch now lives

Every patch below is already applied inline in the module text of this document. Nothing here needs to be copied again.

| Patch                                                                                | Where applied in the merged document             |
|--------------------------------------------------------------------------------------|--------------------------------------------------|
| C1 GRN (new screens, fields, BR-M07-09 to 12, US-M07-09 to 11)                       | P1-M07 patch block, after P1-M07                 |
| C2 RMC rate matrix, category tax override, traceable flag (BR-M03-09, 10, US-M03-13) | P1-M03 patch block, after P1-M03                 |
| C3 TCS (BR-M10-14, 15, US-M10-13)                                                    | P1-M10 business rules and user stories           |
| C4 Retention and advance GST (BR-M11-13, 14, US-M11-14, 15)                          | P1-M11 business rules and user stories           |
| C5 GTA freight, RCM, TDS 194C (BR-M14-09 to 11, US-M14-09)                           | P1-M14 business rules and user stories           |
| C6 MSME interest exposure (R-A02 column, US-M16-07)                                  | P1-M16 reports and user stories                  |
| C7 Driver shell, action queue, failure states (BR-M08-14 to 16, US-M08-13)           | P1-M08 patch block, after P1-M08                 |
| C8 Quality acceptance references (BR-M19-05)                                         | P1-M19 business rules                            |
| C9 HSN digits and rate-change log (BR-M13-09, 10)                                    | P1-M13 business rules                            |
| C10 Cross-module rules (BR-X-01 to 03)                                               | Applied as a rule set; see Section 6.3 standards |

## D. Section 9 — Data model (summary for the developer)

### 9.1 Conventions

- Every tenant-owned table carries `tenant_id`, and branch-scoped tables carry `branch_id`. Global query filters enforce both (Section 1).
- Every transaction has `status`, `status_changed_by`, `status_changed_at`, `status_reason`, `created_by`, `created_at`, `updated_at` and `row_version` (optimistic concurrency).
- Posted records are immutable. Corrections use reversal or adjustment rows that link to the original.
- Money `numeric(18,2)`, rates `numeric(18,4)`, quantities `numeric(18,3)`, percentages `numeric(7,4)`. No floating-point types anywhere.
- Primary keys are UUIDs. Human-readable numbers come from the numbering service (7.3) and are unique per tenant, series and FY.

### 9.2 Decision D-01 — unified Party

A single **Party** entity carries roles (customer, supplier, transporter, contractor, contact-only). A firm that buys and sells keeps one ledger identity with two sub-ledgers (receivable and payable). This avoids duplicate GST and bank records and fixes gap U03.

### 9.3 Core entities

| Group                   | Entities                                                                                                                                                                                                        |
|-------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Tenant and access       | Tenant, Company, Branch (GSTIN, state, plant or godown), FinancialYear, Period (month lock), NumberSeries, User, Role, Permission, UserBranchScope, ApprovalRule, ApprovalRequest, ApprovalDecision, Delegation |
| Parties                 | Party, PartyRole, Contact, Site, BankAccount, ConsentRecord, KycDocument, CreditPolicy                                                                                                                          |
| Products and tax        | Product, ProductCategory, ProductAttribute, UnitOfMeasure, UnitConversion, HsnCode, TaxRate (effective-dated), TaxRateChangeLog, PriceList, PriceListLine, RmcRateCard, ChargeType                              |
| Sales                   | Enquiry, EnquiryLine, FollowUp, Quotation, QuotationRevision, QuotationLine, QuotationCharge, SalesOrder, OrderLine, OrderAmendment, DeliverySchedule (pour rows)                                               |
| Operations              | Plant, Pour, PourCharge, Trip, TripEvent (status timeline), TripEvidence, Vehicle, VehicleDocument, Driver, DeliveryException, WeighbridgeEntry, Challan, ChallanLine                                           |
| Purchase                | PurchaseOrder, PurchaseOrderLine, GoodsReceipt (GRN), GrnLine, SupplierInvoice, SupplierInvoiceLine, SupplierInvoiceTax (ITC flag), SupplierPayment, SupplierPaymentAllocation                                  |
| Billing and collections | SalesInvoice, SalesInvoiceLine, SalesInvoiceTax, CreditNote, DebitNote, Receipt, ReceiptAllocation, Advance, Cheque, PromiseToPay, Dispute, ReminderSchedule, RetentionReceivable                               |
| Finance                 | Account (chart of accounts), Voucher (typed), VoucherLine (with branch, site, order and trip dimensions), BankStatementLine, Reconciliation, RecurringExpenseTemplate, Expense, ExpenseCategory, PettyCashFloat |
| Quality                 | CubeSample, SampleResult, Dispute link                                                                                                                                                                          |
| Platform                | AuditLogEntry (append-only), Attachment (versioned), NotificationTemplate, NotificationLog, MessageWallet, ImportBatch, ImportRowError, Plan, Subscription, UsageCounter, FeatureFlag                           |
| Read models             | DashboardCard snapshot, OutstandingAgeing, ProfitabilityByOrder, GstSummary (rebuilt from events)                                                                                                               |

### 9.4 Domain events (drive notifications, dashboards and read models)

Quotation.Sent · Quotation.Accepted · Order.Confirmed · Order.ShortClosed · Pour.Scheduled · Trip.StatusChanged · Trip.Completed · Challan.Generated · Challan.Unverified · Invoice.Posted · Invoice.Cancelled · Receipt.Posted · Receipt.BounceRecorded · Advance.Created · Retention.Released · GRN.Posted · SupplierInvoice.Posted · SupplierPayment.Posted · Expense.Posted · Approval.Requested · Approval.Decided · Period.Locked · Dispute.Opened · Sample.ResultRecorded · Credit.Breached · Rate.Changed

Read models update within the target lag in BR-M15-01 (60 s). Each read model has a nightly rebuild job that must match the source transactions (automated tie-out test).

## E. Section 10 — Calculations, posting rules, KPIs and non-functional requirements

### 10.1 Rounding and precision

- Line taxable value is rounded to 2 decimals per line. Tax per line is rounded to 2 decimals. Invoice totals are the sum of rounded lines.
- Round-off to the nearest rupee (tenant setting) is posted to the Round-off ledger and never exceeds ₹1 (BR-M10-09).
- Quantity is rounded to 3 decimals; weight-to-unit conversions round at the final line, not at intermediate steps.
- Rates are kept at 4 decimals internally and displayed at 2.

### 10.2 Posting rules (document → accounting entries)

| Source document                       | Debit                                                                  | Credit                                                                                        | Notes                                                |
|---------------------------------------|------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|------------------------------------------------------|
| Sales invoice (intra-state)           | Party receivable (sub-ledger)                                          | Sales (per product group), Output CGST, Output SGST, TCS receivable (if any)                  | Net of round-off; round-off on its own line          |
| Sales invoice (inter-state)           | Party receivable                                                       | Sales, Output IGST                                                                            | Place of supply per 10.3                             |
| Receipt (cash or bank)                | Bank or Cash                                                           | Party receivable (allocated); Advance (unallocated part); Retention receivable (if retention) | TDS deducted by customer: Dr TDS receivable          |
| Advance receipt with GST configured   | Bank                                                                   | Advance from customer; Output GST on advance                                                  | Reverses on allocation (BR-M11-14)                   |
| Credit note (sales)                   | Sales, Output GST                                                      | Party receivable                                                                              | Reverses taxable value and tax proportionally        |
| Debit note (sales)                    | Party receivable                                                       | Other income or charge revenue, Output GST                                                    |                                                      |
| Supplier invoice                      | Purchase (or direct cost), Input CGST/SGST/IGST (only if ITC eligible) | Party payable (supplier)                                                                      | Ineligible or blocked ITC goes to purchase cost      |
| Reverse-charge purchase (GTA freight) | Freight expense, Input GST                                             | Party payable (transporter), Output GST under RCM                                             | Net effect nil on cash; ITC if eligible              |
| Supplier payment                      | Party payable                                                          | Bank or Cash, TDS payable                                                                     | Net payable = gross − TDS                            |
| Expense (direct or indirect)          | Expense ledger (per category), Input GST if eligible                   | Cash, Bank, Petty cash or Party payable                                                       | Trip or order link stored on the line                |
| Recurring expense generation          | As above                                                               | As above                                                                                      | Creates drafts; posts only after review              |
| Petty-cash top-up                     | Petty cash float                                                       | Bank or Cash                                                                                  |                                                      |
| Petty-cash settlement                 | Expense ledgers                                                        | Petty cash float                                                                              |                                                      |
| Write-off (approved)                  | Bad debts                                                              | Party receivable                                                                              | GST treatment per CA setting (BR-M11-08)             |
| Cheque bounce                         | Party receivable                                                       | Bank                                                                                          | Bounce charge = debit note (BR-M11-03)               |
| Closing stock journal (period end)    | Closing stock (asset)                                                  | Purchases (cost of goods)                                                                     | Manual, reviewed (A-04)                              |
| Manual journal and contra             | As entered                                                             | As entered                                                                                    | Cannot post to control accounts directly (BR-M12-02) |

Control accounts (Party receivable, Party payable, GST accounts, TDS accounts, Retention receivable, Advance from customer) are system-locked.

### 10.3 Place of supply and tax determination

1.  Supplier state = state of the branch GSTIN that issues the document.
2.  Place of supply for goods = delivery site state (the state where goods are delivered). Billing address is used only if no delivery site exists.
3.  If supplier state = place of supply: CGST + SGST (each half of the rate). Otherwise: IGST.
4.  Reverse charge flag, SEZ, export and exempt categories override the above and need the category set on the customer or the invoice with a reason.
5.  Tax type is fixed at posting. Changing the delivery site after posting requires a credit note and a new invoice.
6.  Overrides of place of supply need a reason and are audited (BR-M10-04).

### 10.4 Status machines

The status diagram for each transaction is defined in its module (Section 8 and the module patches). This section sets the common rules: - Every transition writes an audit entry with actor, time, from-status, to-status and reason where required. - A transition that changes money or stock runs in one database transaction with the posting. - A status can be reverted only by the workflow named in the module (cancel, reverse, re-open with approval). No silent edits to posted status.

### 10.5 KPI definitions (one definition, used everywhere)

| KPI                          | Definition                                                                                                  | Source documents                         |
|------------------------------|-------------------------------------------------------------------------------------------------------------|------------------------------------------|
| Sales (period)               | Sum of taxable value of posted sales invoices in period, net of posted credit notes                         | Sales invoice, credit note               |
| Collections (period)         | Sum of posted receipts allocated to invoices in period (excludes advances not yet allocated)                | Receipt, allocation                      |
| Receivable outstanding       | Open invoices + open debit notes − open credit notes − unallocated advances − retention is shown separately | Ledger                                   |
| Overdue                      | Balance where due date \< as-on date and balance \> 0; excludes Dispute hold                                | Invoice                                  |
| Payable outstanding          | Open supplier bills − unallocated supplier advances                                                         | Supplier invoice, payment                |
| Gross margin ₹               | Taxable sales − (purchase cost of goods sold + direct costs linked to the order/trip)                       | Invoice, supplier invoice, expense, trip |
| Gross margin %               | Gross margin ₹ ÷ taxable sales                                                                              | As above                                 |
| Unbilled challans value      | Sum of Delivered challan values not yet invoiced                                                            | Challan                                  |
| On-time delivery %           | Trips completed within the pour time window ÷ trips completed                                               | Trip                                     |
| m³ per TM per day            | Delivered m³ ÷ (TM × days in service)                                                                       | Trip, vehicle                            |
| Pour fulfilment %            | Delivered quantity ÷ scheduled quantity for pours in period                                                 | Pour, trip                               |
| Collection efficiency        | Collections ÷ (opening receivable + sales), period                                                          | Receipt, invoice                         |
| DSO (days sales outstanding) | Receivable outstanding ÷ (sales in period ÷ days in period)                                                 | Ledger, invoice                          |
| Enquiry conversion %         | Won ÷ (Won + Lost) by source and owner, period                                                              | Enquiry                                  |
| Quote win %                  | Accepted ÷ (Accepted + Rejected + Expired)                                                                  | Quotation                                |

### 10.6 Non-functional requirements

| ID     | Requirement                       | Target                                                                                                                             |
|--------|-----------------------------------|------------------------------------------------------------------------------------------------------------------------------------|
| NFR-01 | Availability (business hours IST) | 99.5% monthly                                                                                                                      |
| NFR-02 | Recovery point objective          | 15 minutes (point-in-time recovery)                                                                                                |
| NFR-03 | Recovery time objective           | 4 hours                                                                                                                            |
| NFR-04 | Backups                           | Daily full, continuous log; restore test quarterly                                                                                 |
| NFR-05 | Data residency                    | India region for all production and backup data                                                                                    |
| NFR-06 | Security                          | OWASP ASVS Level 2; TLS 1.2+; encryption at rest; secrets in a vault; PII masked in logs                                           |
| NFR-07 | Idempotency                       | Every create and status action from mobile carries a client-generated action ID; server accepts each once                          |
| NFR-08 | Concurrency                       | Numbering and balance updates are transaction-safe; optimistic concurrency (row_version) on edits                                  |
| NFR-09 | Performance                       | List pages \<2 s for 50k rows (server paging); search \<500 ms; Owner home ≤3 s on 4G                                              |
| NFR-10 | Scale for Phase 1 sizing          | 200 tenants, 50 concurrent users per tenant, 10 million transaction lines in total (to be validated in load testing, Section 12.3) |
| NFR-11 | Observability                     | Structured logs, metrics per endpoint, tracing across API and jobs, alerting on error rate and queue lag                           |
| NFR-12 | Tenant isolation                  | Automated cross-tenant test suite runs on every build                                                                              |
| NFR-13 | Browser and device support        | Latest two versions of Chrome, Edge, Safari, Firefox; Android 9+ and iOS 15+ for the PWA                                           |
| NFR-14 | Accessibility                     | WCAG 2.1 AA (6.6)                                                                                                                  |
| NFR-15 | Retention                         | Financial records kept for at least 8 years (CA to confirm the period); audit logs for the same period                             |
| NFR-16 | Message delivery                  | Transactional messages sent within 60 s of the event under normal load                                                             |

## F. Section 12 — Definition of Done, environments, phase gates and UAT

### 12.1 Definition of Done (per module)

A module is Done only when every item below is true: 1. Entities, migrations and seed data exist; migrations run on an empty database. 2. Domain rules and status machine are implemented in the domain/application layer, not in UI code. 3. API endpoints are documented in OpenAPI, with tenant and permission checks. 4. Angular screens follow Section 6 patterns, with empty, loading, error, no-permission and offline-badge states where applicable. 5. Automated tests cover every acceptance criterion (AC) of every user story in the module, and every business rule (BR) has at least one test. 6. Tenant-isolation and permission tests pass. 7. Audit entries are written for every create, edit, status change, approval, print and export. 8. Reports and dashboards tie out to the source transactions (automated tie-out test). 9. Accessibility checks pass on the module’s screens. 10. `/docs/modules/<module>.md` is updated; open decisions are in `/docs/decisions.md`. 11. Demo data exists to walk the module’s golden path.

### 12.2 Environments

Local (developer machine with seeded data) → Dev (shared, reset nightly) → Staging (production-like, anonymised data, used for UAT) → Production (India region). Feature branches merge to Dev after the pipeline passes.

### 12.3 Testing strategy

| Level        | Scope                                                                                         | Tooling and rule                 |
|--------------|-----------------------------------------------------------------------------------------------|----------------------------------|
| Unit         | Domain rules, calculations (10.1 to 10.3), status machines                                    | Every BR has a unit test         |
| Integration  | Database posting (one transaction per document), numbering concurrency, tenant filters        | Real PostgreSQL in containers    |
| API contract | Every endpoint in OpenAPI                                                                     | Contract tests on each build     |
| End-to-end   | Golden path (12.5) and each module’s happy path and main failure path                         | Browser automation on Staging    |
| Tie-out      | Dashboard = report = ledger for the same period                                               | Runs nightly on Staging          |
| Performance  | NFR-09 and NFR-10                                                                             | Load test before each phase gate |
| Security     | Dependency scan on every build; penetration test before first paying tenant                   |                                  |
| Usability    | Five real users per persona (Section 5.1) run the golden path; task times and errors recorded | Before each phase gate           |

### 12.4 CI/CD

Every commit runs build, unit and integration tests, lint and dependency scan. Merge to Dev deploys automatically. Staging deploys need a passing end-to-end run. Production deploys are manual with approval, with a rollback plan and database migrations that are backward-compatible for one release.

### 12.5 Golden-path UAT (Phase 1 gate)

Run on Staging with the HAFA INFRA pilot data set, end to end, with no database work: 1. Onboard a tenant, set GSTIN, FY, numbering and bank accounts; import masters and opening balances; trial balance balances. 2. Create a customer with a site; create a sales enquiry on a phone; follow it up; create a quotation with an RMC rate-card line; approve a discounted line; send the quotation and accept it through the link. 3. Confirm the order (credit check passes); create a purchase order to a plant; record a GRN for a steel delivery with lot no. 4. Schedule a pour; plan trips; assign TM and driver; run one trip on the driver shell with a dropped network; receiver confirms by OTP. 5. Confirm the challan is generated and unverified trips are flagged; create an invoice from unbilled challans; review the consequence preview; post; print with QR. 6. Record a part payment with TDS deducted by the customer; record a retention deduction; allocate an advance. 7. Enter the supplier invoice matched to the GRN; pay the supplier with TDS. 8. Run the GST pack; fix one exception; mark the period filed; confirm the period is locked. 9. Check the dashboard values against Profit & Loss and the ledger; export the GSTR-1 JSON and the Tally XML.

### 12.6 Phase gates

| Phase | Gate                                                                                                                                                                        | Sign-off                                 |
|-------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------|
| 1     | Golden path (12.5) passes; all Phase 1 module DoD items met; load test passes NFR-10; pilot customer signs off the golden path and the GST pack reviewed by the tenant’s CA | Product owner, pilot customer, tenant CA |
| 2     | Phase 2 golden path (written with the Phase 2 chunk) passes; no P1 defects open                                                                                             | Product owner, pilot customer            |
| 3     | A construction project planned, budgeted, executed, measured, billed, with cost control (written with the Phase 3 chunk)                                                    | Product owner, pilot customer            |
| 4     | Collaboration, quality, safety and closeout golden path                                                                                                                     | Product owner, pilot customer            |
| 5     | Each integration or AI feature validated by at least two customers before general release                                                                                   | Product owner                            |

## G. Section 13 — Assumptions, open questions and risks

### 13.1 Assumptions register (the build uses these defaults until the client decides otherwise)

| ID   | Assumption                                                                                                              | Default used by the build                                                                             |
|------|-------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| A-01 | Pilot is a single legal entity with one or more GSTINs in its state                                                     | Multi-GSTIN supported; cross-state pilot tested with two branches                                     |
| A-02 | Currency INR only                                                                                                       | INR; no multi-currency in Phase 1–2                                                                   |
| A-03 | Customers are mostly B2B; B2C retail is limited                                                                         | B2C invoices use the B2CS summary, no e-invoice                                                       |
| A-04 | Inventory valuation uses the periodic method in Phase 1; stock is not valued perpetually                                | Purchases expensed on supplier invoice; closing stock journal at period end; GRN tracks quantity only |
| A-05 | RMC is supplied from the pilot’s own plant or a third-party plant under purchase contract                               | Both supported; own plant uses plant cost from the agreed rate card                                   |
| A-06 | Delivery to site is by the company’s transit mixers or hired TMs                                                        | Both supported; hired TMs carry transporter and freight lines                                         |
| A-07 | Financial year April–March                                                                                              | As stated; configurable start month                                                                   |
| A-08 | Tax rates and HSN codes in v2.0 are indicative                                                                          | Effective-dated; CA confirms before go-live (BR-M01-02)                                               |
| A-09 | Payment gateway and bank API integrations are not in Phase 1                                                            | Bank statement import and UPI QR are used                                                             |
| A-10 | Pro-forma invoices carry no accounting or GST effect; conversion to a tax invoice follows the advance-and-supply policy | Policy set by the tenant; default is conversion on receipt of advance                                 |
| A-11 | Customer and supplier portals are not in Phase 1                                                                        | Public quotation, invoice and challan links only                                                      |
| A-12 | Payroll and statutory payroll are out of scope (Phases 1–2)                                                             | Salaries enter books by journal or Excel import (M14 BR-M14-08)                                       |
| A-13 | Hindi and Marathi are needed on the driver shell in Phase 1                                                             | Driver shell only; rest of app English until Phase 2                                                  |
| A-14 | Retention and TDS deducted by customers are common on contractor accounts                                               | Retention receivable and TDS receivable are both in Phase 1                                           |
| A-15 | Trip volumes are at most 300 trips per day per tenant in Phase 1                                                        | Sizing in NFR-10                                                                                      |

### 13.2 Open questions for the client and the tenant’s CA

| ID    | Question                                                                                        | Needed by                           |
|-------|-------------------------------------------------------------------------------------------------|-------------------------------------|
| OQ-01 | GST rate and HSN for RMC, for TMT, cement, sand and aggregate (the v2.0 codes are indicative)   | Before Phase 1 golden path          |
| OQ-02 | Which customer categories are on works-contract or government rates, and which rate applies     | Before quotation module build (M05) |
| OQ-03 | Whether category tax overrides (C2) are needed in Phase 1 or can wait                           | Before M03 build                    |
| OQ-04 | Does the pilot expect advance-tax-on-receipt, and from which date                               | Before M11 build                    |
| OQ-05 | Does RMC movement by transit mixer need an e-way bill in the pilot’s states, and at which value | Before M10 build                    |
| OQ-06 | TCS (206C(1H)) and TDS 194Q applicability for the pilot, and the turnover basis                 | Before M10 and M07 build            |
| OQ-07 | Pilot’s MSME interest policy (pay on time, or compute and report only)                          | Before M16 build                    |
| OQ-08 | Which mill and cement brands must have MTC or batch certificates attached                       | Before M07 build                    |
| OQ-09 | Number of plants, godowns and branches in the pilot; their GSTINs                               | Before M01 build                    |
| OQ-10 | Retention terms per major contractor (percentage, release on defect period)                     | Before M11 build                    |
| OQ-11 | Driver language preferences (Hindi, Marathi, others)                                            | Before M08 build                    |
| OQ-12 | Record retention period required by the CA (NFR-15)                                             | Before M12 build                    |

### 13.3 Risks

| ID   | Risk                                                  | Likelihood | Impact | Mitigation                                                                                               |
|------|-------------------------------------------------------|------------|--------|----------------------------------------------------------------------------------------------------------|
| R-01 | Tax rules change during build                         | High       | High   | Effective-dated master data; CA sign-off per release; rule changes tested by tie-out                     |
| R-02 | Drivers reject the app (too complex, low-end devices) | Medium     | High   | Driver shell in Hindi and Marathi; usability tests with real drivers; paper fallback for first two weeks |
| R-03 | Poor network at sites loses status updates            | High       | Medium | Action queue (C7), idempotency (NFR-07)                                                                  |
| R-04 | Pilot’s data quality is poor at migration             | High       | High   | Import validation preview; opening-balance reconciliation before go-live                                 |
| R-05 | Scope creep from later phases into Phase 1            | High       | High   | Scope gate (Section 1); Phase gates (12.6)                                                               |
| R-06 | Accountant insists on Tally as the master             | Medium     | Medium | Tally export (M12); clear statement of which system is master                                            |
| R-07 | Customer disputes over RMC quality                    | Medium     | High   | Cube-sample register and disputes (M19)                                                                  |
| R-08 | Incorrect GST on invoices                             | Medium     | High   | Consequence preview; exception list; CA review of the GST pack; disclaimer (Section 4)                   |

## H. Section 14 — Build plan, Phase 2–5 scope and merge map

### 14.1 Phase 1 build sequence (module order, each to Definition of Done)

M01 Tenant and onboarding → M02 Users and access → M03 Masters (with C2 patches) → M04 CRM → M05 Quotation → M06 Sales order → M07 Purchase (with GRN, C1) → M08 RMC and delivery (with C7) → M09 Challan → M10 Sales invoice (with C3) → M11 Receipts, retention and advances (C4) → M12 Accounting → M13 GST (C9) → M14 Expenses (C5) → M15 Dashboard → M16 Reports (C6) → M17 Promotion and catalogue → M18 Platform services → M19 Quality samples and disputes (C8).

Platform services (M18 and Section 7) are built as shared components before M03, because every module depends on numbering, documents, audit and search.

### 14.2 Phase 2–5 scope (epics; user stories are written in the next chunk per phase)

| Phase | Epic                                     | Scope                                                                                                      | Stories    |
|-------|------------------------------------------|------------------------------------------------------------------------------------------------------------|------------|
| 2     | E2-01 Inventory and stores               | Warehouse stock ledger, transfers, material issue, returns, reorder levels, reconciliation (builds on GRN) | Next chunk |
| 2     | E2-02 Procurement depth                  | Indent, RFQ, quotation comparison, supplier evaluation                                                     | Next chunk |
| 2     | E2-03 Commercial controls                | Rate agreements, price revisions, credit-limit policy changes                                              | Next chunk |
| 2     | E2-04 Financial control                  | Payment allocation rules, cash and bank controls, reversal workflows                                       | Next chunk |
| 2     | E2-05 Management analytics               | Sales and purchase trend, supplier performance, KPI pack                                                   | Next chunk |
| 2     | E2-06 GSTR-2B reconciliation             | Reconciling purchase ITC to the portal                                                                     | Next chunk |
| 2     | E2-07 Fixed assets register              | Asset master, depreciation schedule                                                                        | Next chunk |
| 2     | E2-08 TDS credit reconciliation          | Form 26AS and TRACES reconciliation with customer TDS                                                      | Next chunk |
| 2     | E2-09 Subscription billing               | Plans, tenant invoices, online payment, dunning (7.1)                                                      | Next chunk |
| 2     | E2-10 Languages                          | Hindi, Marathi and Gujarati packs                                                                          | Next chunk |
| 3     | E3-01 Projects and sites                 | Project master, team, dates, documents                                                                     | Next chunk |
| 3     | E3-02 Estimation and BOQ                 | Estimate versions, BOQ hierarchy, revisions and approvals                                                  | Next chunk |
| 3     | E3-03 WBS, schedule and budget           | Work packages, dependencies, baseline, cost codes, forecast                                                | Next chunk |
| 3     | E3-04 Site execution                     | DPR, manpower, material consumption, site photos                                                           | Next chunk |
| 3     | E3-05 Contractors and work orders        | Contractor master, work orders, scope and rates                                                            | Next chunk |
| 3     | E3-06 Measurement and RA billing         | Measurement book, certification, retention, deductions                                                     | Next chunk |
| 3     | E3-07 Variations                         | Change requests with cost and schedule impact                                                              | Next chunk |
| 4     | E4-01 Drawings and documents             | Register, revisions, transmittals, markups                                                                 | Next chunk |
| 4     | E4-02 RFI and submittals                 | Request, response, approval loop                                                                           | Next chunk |
| 4     | E4-03 Issues, tasks and meetings         | Issue register, minutes, action items                                                                      | Next chunk |
| 4     | E4-04 Quality                            | Quality plan, ITP, inspection, NCR, corrective action                                                      | Next chunk |
| 4     | E4-05 Safety                             | Plan, checklist, observation, incident, closure                                                            | Next chunk |
| 4     | E4-06 Equipment                          | Allocation, usage, maintenance, breakdown, cost                                                            | Next chunk |
| 4     | E4-07 Punch list and closeout            | Snags, evidence, handover, as-built, archive                                                               | Next chunk |
| 5     | E5-01 RMC batching and plant integration | Only when a customer runs a batching plant (mix design, batching, production scheduling)                   | Next chunk |
| 5     | E5-02 Fleet and GPS                      | Route and live position                                                                                    | Next chunk |
| 5     | E5-03 Integrations                       | Direct e-invoice and e-way bill APIs, bank and payment APIs, website-to-CRM                                | Next chunk |
| 5     | E5-04 Full offline mode                  | Offline planning and reporting for field users                                                             | Next chunk |
| 5     | E5-05 Analytics and AI                   | Forecasting, payment-risk analysis, document search, management summaries                                  | Next chunk |

### 14.3 How Phase 2–5 stories will be written

Each phase is written as its own chunk with the same format as Phase 1: UX flow, screens, lifecycle, business rules (BR), user stories with acceptance criteria (AC), and the phase golden path. A phase chunk is not started until the previous phase’s gate in 12.6 is met, so the build can continue without interruption.

### 14.4 Merge map (final master document order)

1.  Front matter and change log (v2.1 header with this addendum’s status).
2.  Sections 0–8 from v2.0 (as uploaded), including the M01–M09 module text.
3.  P1-M10 to P1-M19 (done in this file; patches applied inline in each module).
4.  This addendum: Sections A–C (status, gap review, change record), D (Section 9), E (Section 10), F (Section 12), G (Section 13), H (Section 14).
5.  Phase 2–5 chunks, one per phase, appended here as they are written.

Roadmap v1 is kept as an appendix for history only.

## I. Corrections made in the consolidation pass

- Section 11 did not exist. Every reference to it now points to Section 14.2 (Phase 2–5 epics) or Section 14.3 (how the chunks will be written).
- The Phase 1 Definition of Done is in Section 12.1. The build protocol in Section 1 refers to it by name.
- BR-M10-10 is clarified: when e-invoicing is enforced, the IRN is generated before the final print.
- Roadmap v1 is an appendix for history only and is not a source of rules.

# V2.2 COMPLETION — Binding expansion of v2.1

Document control \| Version 2.2 \| Date 23-Nov-2026 \| Product: Berynx Construction 360 \| Pilot: HAFA INFRA \| Status: Complete functional requirements baseline; regulatory, commercial and pilot choices marked as validation gates, not assumed approvals.

Read precedence: (1) statutory law and approved effective-dated tenant tax configuration; (2) v2.2 conflict-resolution rules and v2.2 epic specifications; (3) retained v2.1 Phase 1 module specification; (4) previous roadmap for historical context only. This appendix does not authorize developing later-phase UI or tables during an earlier phase. Original v2.1 paragraphs announcing unfinished chunks are historic and are superseded by the sections below.

# 15. Closure audit and decisions

| Finding                                           | Resolution in v2.2                                                                                                 | Verification                           |
|---------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|----------------------------------------|
| 29 Phase 2–5 epics had no developer-ready stories | Sections 17–20 provide flows, screens, fields, lifecycle, rules, stories and ACs for all 29                        | Traceability and phase UAT             |
| Phase 1 periodic stock versus Phase 2 inventory   | GRN quantity-only in Phase 1; Phase 2 begins dated perpetual movement and opening valuation cutover                | Stock/GL cutover tie-out               |
| Tax/document timing ambiguity                     | Tax liability and legal validity independent of print; immutable posting and explicitly configured IRN/EWB gates   | CA-approved sample documents           |
| Numbering versus legally void documents           | Allocate number at posting; never reuse; cancellation creates audit-visible void, not physically gap-free sequence | Concurrency/void test                  |
| Site delivery versus GST place-of-supply nuance   | Require tax-determination decision table by supply type; never assume every site state decides place of supply     | CA sign-off                            |
| Subscriptions and tenant suspension               | Platform billing Phase 2; tenant subscription ledgers isolated from each tenant ERP books                          | Platform/tenant isolation test         |
| Data migration and reporting backfill             | Explicit mapping, cutover date, reconciliation and read-model rebuild before go-live                               | Migration UAT                          |
| Phase 3–4 accounting duplication risk             | One posting engine; site/project cost allocations annotate existing source transactions, not duplicate them        | Trial balance and cost reconciliations |

# 16. Common contracts applying to all 29 epics

Each entity has id(UUID), tenant_id, applicable branch_id/project_id, created/updated fields, row_version, soft-delete/archival policy and immutable audit events; cross-tenant references are prohibited by service and DB boundary checks.

Every document exposes source_document_id, downstream linked IDs, status, owner, effective date, attachments, approvals and activity; posted records use dedicated reversal/correction flows, never direct overwrites.

All financial amounts use decimal, explicit currency INR, rate/quantity precision and configured per-line tax rounding; journal legs must balance before commit; reconciliation exceptions are visible and actionable.

Outbox + idempotent consumers deliver side effects; user action idempotency keys are scoped to tenant/action; optimistic concurrency failures return a conflict with current version and safe retry guidance.

Approvals require permission, delegation audit, separation of duties, reason on rejection, immutable approved snapshot and re-approval on material changes.

All screens support search, server filters/pagination, loading/empty/error/no-access/offline, meaningful keyboard and mobile support, saved views, consistent date/currency formatting and accessible validation.

Every module publishes documented OpenAPI operations and permission codes, seed/demo data, negative-case tests, activity audit, and printable/exportable outputs with filters and provenance.

External integrations are optional and failure-isolated; if required for legal validity, the posting workflow must make that dependency explicit and block issuance as configured, never fabricate an IRN, EWB, signature or payment success.

The system provides configurable retention, encrypted backup, least-privilege access, secure signed links and operational telemetry without logging OTPs, secrets or unnecessary personal data.

Phase implementation scope is strictly gated; later-phase fields may be reserved only if an explicit earlier-phase contract requires them. Existing valid Phase 1 workflows and API contracts remain backward compatible.

## 16.1 Cross-module invariants and boundaries

| Boundary                       | Binding rule                                                                                                                                                                                              |
|--------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Order → Purchase → GRN → Stock | PO receipt quantity and invoice matching are independent from accounting valuation; each movement references its actual document and cannot exceed allowed remaining quantity without approved tolerance. |
| Dispatch → Challan → Invoice   | Each billed challan quantity is allocated to exactly one active invoice line at a time; partial invoice allocation records balance; corrections release quantities only through approved reversal.        |
| Inventory → Finance            | Phase 2 stock valuation vouchers are generated exactly once per posting; periodic Phase 1 closing stock journals are retired after explicit opening inventory cutover.                                    |
| Projects → Accounting          | Projects, WBS and cost codes annotate already-posted transaction lines; direct project cost journals permitted only for genuinely distinct costs, never for the same PO/GRN/expense.                      |
| Retention / TDS / Advance      | All balances remain separate account types with allocation/release references; receipt cash and TDS withholding reconcile to invoice settlement without double counting.                                  |
| Tax / IRN / EWB                | Legal applicability is effective-dated by state, transaction, customer, nature of supply and notified limits; CA-approved decision tables are mandatory before production.                                |
| Portal uploads / integrations  | Portal acknowledgement or success must be recorded from actual provider results; JSON export alone does not imply statutory filing.                                                                       |
| Access                         | Financial and cost visibility is enforced server-side on API responses, exports, saved views, search suggestions and background jobs.                                                                     |
| Notification privacy           | Transactional and promotional consent/purpose separated with opt-outs, template approvals, retry budgets and audit.                                                                                       |
| Tenancy                        | Platform super-admin billing/support data in platform schema/database cannot be read by tenant ERP users; support access requires explicit tenant permission and expiry.                                  |

## 16.2 Global acceptance and test matrix

GAT-01: Create matching codes in two tenants and prove all list, detail, export, search, read model and attachment paths are isolated.

GAT-02: Submit two identical requests concurrently and verify one document/ledger posting, one sequence allocation, and replay returns the same result.

GAT-03: Reverse a posted invoice/payment/stock adjustment and prove original audit retained, linked correction created, balances tie out.

GAT-04: Revoke view_cost and branch scope while session remains open: next request blocks restricted cost and cross-branch data.

GAT-05: Rebuild dashboard/report read models and compare against source journal/transactions to configured rounding tolerance.

GAT-06: Simulate lost network, delayed callbacks, provider errors and signed-link expiry: retriable actions remain safe and users see next action.

GAT-07: Run migration with 10% invalid rows, resume corrected import, prove no duplicate masters and opening trial balance matches.

GAT-08: Verify a legally relevant document with boundary dates for rule effective date, state registration, taxable category and fiscal year.

GAT-09: Run full keyboard, screen reader labels, phone/tablet screenshots and 50,000-row pagination performance checks.

GAT-10: Restore backup to clean environment and verify access, data integrity, attachment links and recovery objectives.

# 17. PHASE 2 — Operational depth, inventory and finance

Prerequisite: Phase 1 gate passed. Phase 2 acceptance: inventory cutover with stock and GL reconciled, purchase through supplier settlement, ITC/TDS reconciliation, asset postings and SaaS subscription billing independently verified.

## E2-01 — Inventory and stores

Business outcome. Authoritative branch/warehouse/bin stock by item, lot, UOM and value.

Primary user journey. Receive via Phase 1 GRN → quality hold/release → put-away → transfer or issue → return/adjust → physical count → valuation close.

Screens (responsive, permission-scoped). SCR-E2-01-01 List/board with saved filters and totals · SCR-E2-01-02 Create/edit wizard with contextual masters · SCR-E2-01-03 Detail with linked documents, audit and actions · SCR-E2-01-04 Review/approval or reconciliation workspace · SCR-E2-01-05 Exceptions and exports

Required core fields / entities. Warehouse, bin, SKU, base UOM, batch/lot/heat, cost-layer id, movement type, source ref, qty, unit cost, reserved qty, available qty, reason, approval and count variance.

Lifecycle / transitions. Draft → Pending approval (where applicable) → Posted → Reversed; count Draft → Counted → Reviewed → Posted. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-01-01. No negative available stock except under an explicitly approved product+warehouse override with warning and audit.

BR-E2-01-02. Use weighted-average valuation as default; configuration frozen for the fiscal year; landed-cost allocations remain traceable; serial/lot controls follow item settings.

BR-E2-01-03. Opening stock at the Phase 2 cutover is imported once; reconcile stock value to the GL opening entry and deactivate Phase 1 periodic closing stock journals.

BR-E2-01-04. Concurrent stock movement locks the same SKU/location balances; reversal never destroys original movement.

US-E2-01-01 — Receive and put away. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: A posted Phase 1 GRN creates exactly one receipt movement after cutover.

AC 1.2: A quarantined lot cannot be issued or sold until a permitted release action.

US-E2-01-02 — Transfer and issue. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Each transfer creates paired in-transit and receipt movements with matched quantities.

AC 2.2: Stock issue to a project records cost code and appears once in project actual cost.

US-E2-01-03 — Count and reconcile. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Cycle count shows system versus counted quantity and value before approval.

AC 3.2: Posting a variance creates stock adjustment and balanced GL entry and retains before/after evidence.

Dependencies / posting / downstream consumers. GRN, challan, POs, materials, project issues, fixed accounting posting engine and reorder alerts.

Failure, permission and audit tests. Reject duplicate receipt, unknown lot, insufficient stock, unsupported UOM conversion, closed period and wrong branch. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-02 — Procurement depth

Business outcome. Control request-to-contract purchasing and transparent bid comparisons.

Primary user journey. Requisition/indent → approve → invite suppliers → receive bids → compare → negotiate → award → create PO.

Screens (responsive, permission-scoped). SCR-E2-02-01 List/board with saved filters and totals · SCR-E2-02-02 Create/edit wizard with contextual masters · SCR-E2-02-03 Detail with linked documents, audit and actions · SCR-E2-02-04 Review/approval or reconciliation workspace · SCR-E2-02-05 Exceptions and exports

Required core fields / entities. Indent items, required-by date, project, delivery site, vendor RFQ, quoted rates, freight, taxes, validity, lead time, compliance, bid attachment, selection reason.

Lifecycle / transitions. Indent Draft → Submitted → Approved/Rejected → Sourced → Closed; RFQ Open → Quoted → Evaluated → Awarded/Expired. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-02-01. PO must not exceed approved requisition amounts without revised approval.

BR-E2-02-02. Bid comparisons normalize UOM and separate base rate, freight, taxes, payment terms and lead-time weighting.

BR-E2-02-03. Supplier bids are visibility-restricted until comparison is authorized; award requires a documented decision.

BR-E2-02-04. POs retain reference to bid snapshot and revisions.

US-E2-02-01 — Create requisition. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Approved indent can be converted to RFQ without re-entering items.

AC 1.2: Requester cannot authorize own approval where separation of duties applies.

US-E2-02-02 — Compare suppliers. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Comparison displays landed cost and flags missing certificates or expired validity.

AC 2.2: Awarded bid becomes immutable source for PO with revision history.

US-E2-02-03 — Track sourcing. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Users can distinguish unquoted, overdue and awarded RFQs with filters.

AC 3.2: Rejected bid cannot be converted to a PO without override.

Dependencies / posting / downstream consumers. P1 purchase PO/GRN, pricing, suppliers, approval engine and project cost budgets.

Failure, permission and audit tests. Bid submission after deadline, duplicate vendor quotation, wrong UOM and cancellation with downstream PO are blocked or handled via explicit revision. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-03 — Commercial controls

Business outcome. Apply negotiated rates and controlled commercial terms to sales and purchasing.

Primary user journey. Define agreements → approve rates and effective dates → assign customer/vendor/site → price transaction → request override → audit rate use.

Screens (responsive, permission-scoped). SCR-E2-03-01 List/board with saved filters and totals · SCR-E2-03-02 Create/edit wizard with contextual masters · SCR-E2-03-03 Detail with linked documents, audit and actions · SCR-E2-03-04 Review/approval or reconciliation workspace · SCR-E2-03-05 Exceptions and exports

Required core fields / entities. Agreement, counterparty, item/grade, site/branch, price basis/UOM, validity, tier breaks, revision, tax treatment, margin floor, credit override.

Lifecycle / transitions. Draft → Pending approval → Active → Superseded/Expired → Cancelled. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-03-01. Price precedence: explicit approved transaction override → active party-site agreement → party price list → default list → catalog base, with source label.

BR-E2-03-02. No simultaneous active conflicting agreements for the same party, product, location, tier and effective dates.

BR-E2-03-03. Rate updates create new versions; posted lines preserve applied rate snapshot and approval ID.

BR-E2-03-04. Credit overrides have expiry, limit, exposure basis and cannot bypass mandatory hard stops without authorised level.

US-E2-03-01 — Approve agreement. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Agreement approval checks overlapping effective periods and discount authority.

AC 1.2: A quote created on the boundary date selects only the effective valid version.

US-E2-03-02 — Use prices. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Quotation and PO show rate source and normalized unit price.

AC 2.2: Change after confirmation does not reprice posted transactions.

US-E2-03-03 — Review credit. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Order blocked for overdue/limit breach offers an authorized approval route.

AC 3.2: All override reasons appear in audit and exposure reports.

Dependencies / posting / downstream consumers. P1 price matrices, quotation, sales order, purchase order, branch/party credit and cost visibility.

Failure, permission and audit tests. Missing price returns actionable error; overlap, negative margin and stale approval trigger explicit review. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-04 — Financial control

Business outcome. Provide bank, cash, allocation and reversal controls with reconciled books.

Primary user journey. Import bank statement → auto-match candidates → confirm → resolve exceptions → close bank period.

Screens (responsive, permission-scoped). SCR-E2-04-01 List/board with saved filters and totals · SCR-E2-04-02 Create/edit wizard with contextual masters · SCR-E2-04-03 Detail with linked documents, audit and actions · SCR-E2-04-04 Review/approval or reconciliation workspace · SCR-E2-04-05 Exceptions and exports

Required core fields / entities. Bank account, statement hash, transaction reference/value date, narration, amount, matched voucher, match score, allocation, cheque/PDC status, reconciliation batch.

Lifecycle / transitions. Imported → Suggested → Matched/Unmatched → Reconciled → Reopened by approval. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-04-01. Bank imports de-duplicate by bank account and transaction identity; unmatched entries never auto-post without explicit rule.

BR-E2-04-02. On-account, advance, retention, TDS and invoice allocations remain separate and sum to actual received amount.

BR-E2-04-03. Posted financial corrections use reversal vouchers and an approval trail.

BR-E2-04-04. Reconciliation cannot close with an unexplained difference; period override is audited.

US-E2-04-01 — Reconcile a statement. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Matched totals equal bank statement debits and credits and unmatched records remain visible.

AC 1.2: One bank transaction cannot settle two vouchers beyond its amount.

US-E2-04-02 — Allocate payments. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Partial and overpayment allocations are explicitly represented.

AC 2.2: Reversal restores open invoice amount and prevents duplicate settlement.

US-E2-04-03 — Manage PDC. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Bounce changes cheque status and reverses collection only once.

AC 3.2: Bank close blocks unauthorized modifications.

Dependencies / posting / downstream consumers. P1 receipts, payables, cashbook, posting engine, FY locks, balances and audit.

Failure, permission and audit tests. Duplicate bank row, statement import retry, mismatched currency, incorrect voucher date and already reversed entry handled without silent posting. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-05 — Management analytics

Business outcome. Provide governed sales, margin, liquidity and vendor performance analytics.

Primary user journey. Choose metric and permitted scope → select fiscal/date filters → view trends → drill to source documents → export reconciled snapshot.

Screens (responsive, permission-scoped). SCR-E2-05-01 List/board with saved filters and totals · SCR-E2-05-02 Create/edit wizard with contextual masters · SCR-E2-05-03 Detail with linked documents, audit and actions · SCR-E2-05-04 Review/approval or reconciliation workspace · SCR-E2-05-05 Exceptions and exports

Required core fields / entities. Metric catalogue, dimension, fact date, tenant/branch/project, cost permission, refresh watermark, value, comparison period and filter set.

Lifecycle / transitions. Scheduled → Materialized → Validated → Published; failure → Stale. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-05-01. Every KPI has one documented formula and source document definition, including returns/cancellations and date basis.

BR-E2-05-02. Profit calculation distinguishes estimated versus actual landed/transport/project cost and shows incomplete-cost warnings.

BR-E2-05-03. Snapshots include as-of date, data-lag indicator and data quality exception count.

BR-E2-05-04. Exports enforce the same row/field permissions as onscreen reports.

US-E2-05-01 — Drill into metrics. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Displayed sales total ties to posted sales invoices net of issued credits for the chosen period.

AC 1.2: Selecting a KPI opens exactly the documents included in its numerator.

US-E2-05-02 — Compare profitability. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Users can compare customer, product and site margins with cost permission.

AC 2.2: Missing actual cost produces an explicit estimated or unavailable label.

US-E2-05-03 — Trust refresh. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Failed refresh leaves last-known-good time visible.

AC 3.2: Nightly rebuild ties out to accounting and operational source totals.

Dependencies / posting / downstream consumers. P1 dashboard/report contracts, GL, project cost centers, procurement, inventory and commercial rates.

Failure, permission and audit tests. Handle no data, backdated corrections, stale read model, access changes and currency precision. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-06 — GSTR-2B reconciliation

Business outcome. Reconcile supplier tax documents and eligible input tax credit against returns.

Primary user journey. Import official 2B/portal download → normalize → match supplier invoices → investigate mismatch → approve adjustments → export working papers.

Screens (responsive, permission-scoped). SCR-E2-06-01 List/board with saved filters and totals · SCR-E2-06-02 Create/edit wizard with contextual masters · SCR-E2-06-03 Detail with linked documents, audit and actions · SCR-E2-06-04 Review/approval or reconciliation workspace · SCR-E2-06-05 Exceptions and exports

Required core fields / entities. GSTIN, supplier GSTIN, invoice type/number/date, taxable and tax values, ITC eligibility, filing period, matching status, reason, attachment.

Lifecycle / transitions. Imported → Matched/Variance/Missing → Reviewed → Locked. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-06-01. Use exact identifier and configurable normalized match, never suppress missing/duplicate documents.

BR-E2-06-02. ITC eligibility and reversal are effective-dated CA-controlled rules and recorded independently from matching confidence.

BR-E2-06-03. Locked periods remain immutable; later corrections follow next eligible adjustment period.

BR-E2-06-04. Manual match requires reason, approver and original portal row retention.

US-E2-06-01 — Import and match. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Repeated upload of the same 2B file does not duplicate portal entries.

AC 1.2: Matching shows matched, missing in books, missing in portal and amount variance separately.

US-E2-06-02 — Review ITC. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Ineligible and blocked credits excluded from claim totals with audit reason.

AC 2.2: Approved reconciliation sums tie to tax control ledger to the defined tolerance.

US-E2-06-03 — Export evidence. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Download includes source version, invoice mapping and reviewer sign-off.

AC 3.2: A locked period cannot be silently reclassified.

Dependencies / posting / downstream consumers. Supplier invoice, GST pack, tax ledger and accounting lock.

Failure, permission and audit tests. Conflicting invoice numbers, amended 2B, portal re-upload, bad tax IDs and period mismatch captured as actionable exception. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-07 — Fixed assets register

Business outcome. Track capitalisation, depreciation and disposal with books reconciliation.

Primary user journey. Acquire asset → tag/category → place in service → periodic depreciation → transfer/impairment → disposal.

Screens (responsive, permission-scoped). SCR-E2-07-01 List/board with saved filters and totals · SCR-E2-07-02 Create/edit wizard with contextual masters · SCR-E2-07-03 Detail with linked documents, audit and actions · SCR-E2-07-04 Review/approval or reconciliation workspace · SCR-E2-07-05 Exceptions and exports

Required core fields / entities. Asset ID, category, acquisition cost, supplier bill, capitalisation date, useful life, depreciation method/rate, location, custodian, residual value, disposal reference.

Lifecycle / transitions. Planned → Capitalized → In service → Impaired/Disposed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-07-01. Asset cost must reconcile to booked purchase/capital journal, excluding recoverable tax where configured.

BR-E2-07-02. Depreciation schedule is deterministic and posts one balanced journal per asset per period using idempotency.

BR-E2-07-03. Transfers preserve accumulated depreciation, disposal calculates gain/loss and posts reversal-safe vouchers.

BR-E2-07-04. Book method and tax method kept separately; tenant CA approves applicable depreciation policies.

US-E2-07-01 — Register asset. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Cannot capitalise an asset twice from the same identified supplier line.

AC 1.2: In-service date controls first period depreciation schedule.

US-E2-07-02 — Run depreciation. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Re-run of a locked month does not duplicate depreciation.

AC 2.2: Fixed asset register net book value reconciles to control accounts.

US-E2-07-03 — Dispose asset. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Disposal computes gain/loss from book value and proceeds.

AC 3.2: Disposed asset is excluded from subsequent depreciation runs.

Dependencies / posting / downstream consumers. P1 AP/GL, bank and project/equipment; E4-06 equipment references asset ID where capitalized.

Failure, permission and audit tests. Invalid asset life, backdated disposal, closed period and double posting rejected. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-08 — TDS credit reconciliation

Business outcome. Track customer tax withholdings against certificates and portal evidence.

Primary user journey. Record TDS receivable on receipt → import 26AS/TDS certificate → match deductor and period → flag short credit → resolve with customer/CA.

Screens (responsive, permission-scoped). SCR-E2-08-01 List/board with saved filters and totals · SCR-E2-08-02 Create/edit wizard with contextual masters · SCR-E2-08-03 Detail with linked documents, audit and actions · SCR-E2-08-04 Review/approval or reconciliation workspace · SCR-E2-08-05 Exceptions and exports

Required core fields / entities. Deductor TAN/PAN, customer, FY/quarter, certificate number, section, challan, amount deducted, amount reported, credit claim date, status.

Lifecycle / transitions. Expected → Evidence received → Matched/Disputed → Reconciled → Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-08-01. TDS receivable arises from customer deduction once, distinct from collected cash.

BR-E2-08-02. One certificate credit cannot be allocated twice; partial match is permitted with remaining balance.

BR-E2-08-03. Mismatch reports retain original certificate and normalized statement.

BR-E2-08-04. Tax claim/return treatment requires CA-approved configuration, not automatic assumptions.

US-E2-08-01 — Match withheld tax. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Customer receipt creates expected TDS receivable linked to invoice/receipt.

AC 1.2: Portal credit may partially clear multiple expectations without exceeding certificate amount.

US-E2-08-02 — Handle discrepancy. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Missing TAN, wrong FY and short-deduction remain separate exceptions.

AC 2.2: Dispute notes and follow-up owner are stored.

US-E2-08-03 — Reconcile report. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Outstanding credits tie to receivable ledger as of period end.

AC 3.2: Only permitted Accounts roles can close a reconciliation.

Dependencies / posting / downstream consumers. P1 M11 retention/TDS, receivables, general ledger and reports.

Failure, permission and audit tests. Conflicting certificate, duplicate import, unmatched customer and reversal after tax filing require controlled resolution. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-09 — Subscription billing

Business outcome. Monetise SaaS platform tenants independently of their business accounting.

Primary user journey. Choose plan → trial/activate → meter usage → issue platform bill → collect → retry/dun → suspend/read-only → recover/export.

Screens (responsive, permission-scoped). SCR-E2-09-01 List/board with saved filters and totals · SCR-E2-09-02 Create/edit wizard with contextual masters · SCR-E2-09-03 Detail with linked documents, audit and actions · SCR-E2-09-04 Review/approval or reconciliation workspace · SCR-E2-09-05 Exceptions and exports

Required core fields / entities. Platform tenant ID, plan version, entitlement snapshot, billing cycle, taxable supply details, invoice, payment attempt, gateway ref, retry state, grace period.

Lifecycle / transitions. Trial → Active → Past due → Grace → Suspended → Cancelled; restore Active with validated payment. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-09-01. Platform billing runs in Berynx-controlled ledger/schema, not HAFA or other tenants’ accounting books.

BR-E2-09-02. Entitlements and usage are enforced server-side with quota warnings before blocks; read-only tenants may view/export permitted data.

BR-E2-09-03. Webhook verification uses signatures and idempotent provider event IDs; payment status never inferred from redirect alone.

BR-E2-09-04. Proration, refunds, taxes, billing period and suspension policy are versioned plan terms.

US-E2-09-01 — Purchase a subscription. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Only actual verified payment marks invoice paid.

AC 1.2: Tenant plan entitlement updates atomically after authorized activation.

US-E2-09-02 — Handle renewal failure. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Dunning schedule and grace expiry create one notification per configured attempt.

AC 2.2: Suspension blocks new posting but preserves lawful read/export access.

US-E2-09-03 — Manage billing. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Super-admin can review platform invoice and metering with full audit.

AC 3.2: A tenant cannot access another tenant’s billing or change system plan limits.

Dependencies / posting / downstream consumers. P1 SaaS provisioning, quotas, identity, notifications and separate platform finance.

Failure, permission and audit tests. Failed webhook, duplicated provider event, disputed refund and suspended active sessions handled safely. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E2-10 — Languages and localization

Business outcome. Ship tested Hindi, Marathi and Gujarati UI packs for permitted user flows.

Primary user journey. Choose user language → localize menus/forms/alerts/print where supported → validate translation fallback and numerical entry.

Screens (responsive, permission-scoped). SCR-E2-10-01 List/board with saved filters and totals · SCR-E2-10-02 Create/edit wizard with contextual masters · SCR-E2-10-03 Detail with linked documents, audit and actions · SCR-E2-10-04 Review/approval or reconciliation workspace · SCR-E2-10-05 Exceptions and exports

Required core fields / entities. Translation key, locale, string, version, approval, help content, number/date formatter, language preference.

Lifecycle / transitions. Draft translation → Reviewed → Released → Deprecated. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E2-10-01. Business keys, statuses, API payloads and stored descriptions are locale-neutral; only display strings translate.

BR-E2-10-02. Amounts, quantities, GST codes and English legal numbering are never altered by localization.

BR-E2-10-03. Missing translation falls back to English with logging and no blank action buttons.

BR-E2-10-04. Print/document bilingual content follows signed-off tenant template policies.

US-E2-10-01 — Switch language. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: User can change language without losing unsaved form data.

AC 1.2: Critical confirm/reject/error messages have reviewed equivalents.

US-E2-10-02 — Enter forms. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Decimal and date parsing remains correct with locale-specific display.

AC 2.2: Screen readers announce localized labels and error summaries.

US-E2-10-03 — Maintain catalogue. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Translation deployment has version and rollback ability.

AC 3.2: No backend schema key changes merely to support labels.

Dependencies / posting / downstream consumers. P1 shell, driver strings, print templates, notifications and help.

Failure, permission and audit tests. Unknown locale, missing labels, truncation, 390px viewport and right-to-left assumptions tested. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

# 18. PHASE 3 — Construction project execution and commercial control

Phase 3 adds project management on top of the trading ERP. Projects reference existing Party/Site, stock, invoices and GL; project financials must reconcile to GL and support commitments versus actual versus forecast.

## E3-01 — Projects and sites

Business outcome. Create governed construction project identity and workspaces.

Primary user journey. Register project under party → define sites and branches → assign team/roles → set dates and contract → activate → archive at closeout.

Screens (responsive, permission-scoped). SCR-E3-01-01 List/board with saved filters and totals · SCR-E3-01-02 Create/edit wizard with contextual masters · SCR-E3-01-03 Detail with linked documents, audit and actions · SCR-E3-01-04 Review/approval or reconciliation workspace · SCR-E3-01-05 Exceptions and exports

Required core fields / entities. Project code, party/owner, site location, GST branch context, contract dates/value, project manager, currency, document folder, project status.

Lifecycle / transitions. Draft → Pending approval → Active → On hold → Completed → Closed/Archived. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-01-01. Project codes unique per tenant; site links may be shared across sales but project financial ownership is explicit.

BR-E3-01-02. Project/branch roles restrict documents and financials across all APIs and exports.

BR-E3-01-03. Cannot close with open unapproved commitments, disputed measurements or unresolved closeout blockers without exception approval.

US-E3-01-01 — Create project. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: New project reuses existing Party/Site without cloning the master.

AC 1.2: Assigned manager has only authorized project scope.

US-E3-01-02 — Control lifecycle. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: On-hold project blocks new commitments unless override.

AC 2.2: Closure shows unbilled work, payables, retentions and unresolved issues.

US-E3-01-03 — View workspace. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Project shows linked orders, budgets, schedule, site reports and audit.

AC 3.2: Archived project is searchable read-only for permitted users.

Dependencies / posting / downstream consumers. P1 Party/Site, invoice/GL, users, attachments; all Phase 3 project modules.

Failure, permission and audit tests. Prevent duplicate code, expired assignment, scope escalation and closure with active obligations. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-02 — Estimation and BOQ

Business outcome. Provide auditable quantity and rate estimates through approved BOQ baselines.

Primary user journey. Import/create BOQ hierarchy → add resources/rates → calculate → review → approve baseline → revise with comparison.

Screens (responsive, permission-scoped). SCR-E3-02-01 List/board with saved filters and totals · SCR-E3-02-02 Create/edit wizard with contextual masters · SCR-E3-02-03 Detail with linked documents, audit and actions · SCR-E3-02-04 Review/approval or reconciliation workspace · SCR-E3-02-05 Exceptions and exports

Required core fields / entities. BOQ item, WBS/cost code, description, unit, quantity, rate breakdown, escalation, tax exclusion basis, contingency, rate source, revision.

Lifecycle / transitions. Draft → Submitted → Approved/Baselined → Superseded; variation via approved revision. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-02-01. BOQ estimated cost separates material, labour, plant, subcontract and overhead; contingency is not actual expenditure.

BR-E3-02-02. Unit conversion and quantities are deterministic, with source rate effective date.

BR-E3-02-03. Baseline cannot be overwritten; revision stores delta quantity, cost, reason and approver.

US-E3-02-01 — Build estimate. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Spreadsheet import has row errors and import preview.

AC 1.2: BOQ roll-up equals sum of approved child quantities/costs based on specified calculation basis.

US-E3-02-02 — Approve baseline. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Submitted estimate records approval snapshots and budget impact.

AC 2.2: Prior baseline remains readable and used by historic comparisons.

US-E3-02-03 — Track revisions. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Change summary displays original, proposed and delta per BOQ line.

AC 3.2: Unauthorized edits cannot modify approved baseline.

Dependencies / posting / downstream consumers. Project master, item/rate/price catalogues, E3-03 budget, E3-07 changes and RA bill BOQ linkage.

Failure, permission and audit tests. Unknown unit, duplicate BOQ code, circular hierarchy and negative values require explicit validation. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-03 — WBS schedule and budget

Business outcome. Plan deliverables, dependencies, baselines and cost control.

Primary user journey. Define WBS and tasks → assign predecessors/calendars → allocate budget → approve baseline → update progress → forecast completion.

Screens (responsive, permission-scoped). SCR-E3-03-01 List/board with saved filters and totals · SCR-E3-03-02 Create/edit wizard with contextual masters · SCR-E3-03-03 Detail with linked documents, audit and actions · SCR-E3-03-04 Review/approval or reconciliation workspace · SCR-E3-03-05 Exceptions and exports

Required core fields / entities. WBS code, cost code, activity, duration, dependency, calendar, baseline dates, budget lines, commitment, actual, estimate-to-complete, earned-progress basis.

Lifecycle / transitions. Draft → Baseline pending → Baseline approved → In execution → Reforecast → Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-03-01. Prevent dependency cycles and date invalidity; calculate critical path consistently.

BR-E3-03-02. Budget actuals derive from posted source transactions, never hand-maintained duplicate values.

BR-E3-03-03. Commitments from open POs/work orders are separate from booked actual cost and forecast; currency basis disclosed.

BR-E3-03-04. Budget change requires approved variation/baseline revision with full history.

US-E3-03-01 — Plan WBS. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Schedule rejects circular predecessor chains and shows calculated late tasks.

AC 1.2: Each cost code has one recognized active baseline per project revision.

US-E3-03-02 — Monitor project. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Committed, actual and forecast values drill to underlying sources.

AC 2.2: Zero-progress projects show no invented earned value.

US-E3-03-03 — Reforecast. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Forecast displays reason, date and delta versus prior baseline.

AC 3.2: Locked prior periods retain posted actuals.

Dependencies / posting / downstream consumers. E3-02 BOQ, E3-04 site reports, E3-05 work orders, P1 purchase/GL and E3-07 variations.

Failure, permission and audit tests. Invalid calendar, cross-project cost code, duplicate source spending and missing progress baseline flagged. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-04 — Site execution and daily progress

Business outcome. Capture reliable daily work, manpower, materials and evidence at site.

Primary user journey. Open DPR for date/site → record weather/work/manpower/equipment → link material issues and photos → supervisor review → submit daily record.

Screens (responsive, permission-scoped). SCR-E3-04-01 List/board with saved filters and totals · SCR-E3-04-02 Create/edit wizard with contextual masters · SCR-E3-04-03 Detail with linked documents, audit and actions · SCR-E3-04-04 Review/approval or reconciliation workspace · SCR-E3-04-05 Exceptions and exports

Required core fields / entities. Project, date, shift, task/WBS, manpower category/hours, material issue refs, completed qty, constraints, photos, GPS capture consent, reviewer.

Lifecycle / transitions. Draft → Submitted → Verified → Locked; corrected via revision. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-04-01. One approved DPR per site/shift/date/version, with revision chain instead of overwriting.

BR-E3-04-02. Material consumption links posted stock issue; free-text quantities are operational observations, not second stock depletion.

BR-E3-04-03. DPR progress cannot exceed allowed measurement tolerance without reviewer exception and evidence.

BR-E3-04-04. Offline submissions use client UUID, eventual sync and conflict visibility.

US-E3-04-01 — Record a DPR. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Engineer can capture photos and save draft on phone.

AC 1.2: Material issues appear linked from stock and do not create duplicate consumption.

US-E3-04-02 — Verify work. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Supervisor can approve or return with reason and versioned edits.

AC 2.2: Verified DPR updates progress source with recorded effective date.

US-E3-04-03 — Report delay. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Constraints and missed milestones appear on project exception board.

AC 3.2: DPR correction preserves original entries and attachments.

Dependencies / posting / downstream consumers. WBS, inventory material issues, equipment, attendance-like counts (not payroll), site issue register.

Failure, permission and audit tests. Offline duplicate, stale quantity, forbidden GPS capture and invalid work code handled with recoverable errors. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-05 — Contractors and work orders

Business outcome. Manage subcontract scope, rates, commitments and certification.

Primary user journey. Select contractor Party → invite/agree rates → issue work order → mobilise → track executed scope → certify → close.

Screens (responsive, permission-scoped). SCR-E3-05-01 List/board with saved filters and totals · SCR-E3-05-02 Create/edit wizard with contextual masters · SCR-E3-05-03 Detail with linked documents, audit and actions · SCR-E3-05-04 Review/approval or reconciliation workspace · SCR-E3-05-05 Exceptions and exports

Required core fields / entities. Contractor Party, contract/work order, scope, BOQ item, units/rates, mobilisation, retention, tax/TDS, variation, security deposit, milestone.

Lifecycle / transitions. Draft → Pending approval → Released → In progress → Completed → Closed/Terminated. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-05-01. Committed work value sourced from approved work-order baseline and approved variations.

BR-E3-05-02. Contractor performance and measurement certification are independent from invoice/payment authorization.

BR-E3-05-03. TDS, retention, GST and supplier-bill accounting use the existing AP engine, not bespoke project books.

BR-E3-05-04. Order revisions after issue require approval and retain prior version.

US-E3-05-01 — Issue work order. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Approved scope and rates map to BOQ with signed document.

AC 1.2: Work order produces commitment without booking expense before actual supply/certification policy.

US-E3-05-02 — Certify contractor work. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Certified measurements limit billable quantities within contract plus variations.

AC 2.2: Same quantity cannot be certified in two active RA bills.

US-E3-05-03 — Close contract. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Final close checks pending retention, advances, claims and performance docs.

AC 3.2: Termination preserves historical certificates and liabilities.

Dependencies / posting / downstream consumers. Unified Party, P1 AP/TDS/ledger, E3-02 BOQ, E3-06 measurement and E3-07 variations.

Failure, permission and audit tests. Duplicate work order, rate variance, unapproved variation, closed project and repeated bill line rejected. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-06 — Measurement book and RA billing

Business outcome. Certify measured work and generate legally reviewable running account bills.

Primary user journey. Create MB measurement → attach evidence → check quantities → certify → generate RA bill → apply retention/advances/taxes → post invoice/payable.

Screens (responsive, permission-scoped). SCR-E3-06-01 List/board with saved filters and totals · SCR-E3-06-02 Create/edit wizard with contextual masters · SCR-E3-06-03 Detail with linked documents, audit and actions · SCR-E3-06-04 Review/approval or reconciliation workspace · SCR-E3-06-05 Exceptions and exports

Required core fields / entities. MB no, item/WBS, location, dimension formula, measured qty, cumulative prior qty, certified qty, rate snapshot, deductions, retention, signer, tax profile.

Lifecycle / transitions. MB Draft → Submitted → Checked → Certified/Returned; RA Draft → Approved → Posted → Credited/Final. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-06-01. Cumulative certified qty cannot exceed approved BOQ+variation beyond agreed tolerance.

BR-E3-06-02. RA billing deducts prior certified quantities; one MB item can be billed only once per certification amount.

BR-E3-06-03. Retention, advances, TDS and other recoveries post to distinct control accounts; negative payable/receivable requires explicit handling.

BR-E3-06-04. Final bill closes only after prior RA bills, claims and release status reconciled.

US-E3-06-01 — Measure work. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Dimension-based measurement displays quantity formula and evidence.

AC 1.2: Certification locks measurement snapshot and signer.

US-E3-06-02 — Generate RA. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: RA bill calculates current payable as certified cumulative less previously billed quantities and approved deductions.

AC 2.2: Posting creates one linked invoice/AP voucher and balances GL.

US-E3-06-03 — Prepare final bill. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: All prior deductions and retentions are shown and traceable.

AC 3.2: Reversal restores certifiable unbilled quantity through controlled correction.

Dependencies / posting / downstream consumers. BOQ, WBS, contractor work order, P1 invoices, receipts, accounting and retention.

Failure, permission and audit tests. Over-certification, double billing, closed FY, invalid tax basis and mismatched signer blocked. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E3-07 — Variations and change management

Business outcome. Govern scope, schedule, budget and commercial impacts of changes.

Primary user journey. Raise change request → estimate cost/time → route approval → negotiate → approve change order → revise baseline/contract.

Screens (responsive, permission-scoped). SCR-E3-07-01 List/board with saved filters and totals · SCR-E3-07-02 Create/edit wizard with contextual masters · SCR-E3-07-03 Detail with linked documents, audit and actions · SCR-E3-07-04 Review/approval or reconciliation workspace · SCR-E3-07-05 Exceptions and exports

Required core fields / entities. Change ID, origin, reason, impacted WBS/BOQ, quantities, cost estimate, days impact, customer instruction, contractor change, approval evidence.

Lifecycle / transitions. Draft → Submitted → Evaluated → Approved/Rejected → Implemented → Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E3-07-01. No contract or baseline changes until an approved change order exists.

BR-E3-07-02. Approvals compare aggregate previous change orders and threshold policies, not only individual request value.

BR-E3-07-03. Change impact flows once to budget and relevant commercial commitment, maintaining original baseline history.

US-E3-07-01 — Submit variation. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Request captures before/after cost and critical-path impact.

AC 1.2: Reviewer can reject with reason without overwriting estimate.

US-E3-07-02 — Approve commercial change. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Approved variation creates versioned change order and budget revision reference.

AC 2.2: Unapproved change excluded from certified RA quantity.

US-E3-07-03 — Trace implementation. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Project dashboard distinguishes proposed, approved and pending value/time impacts.

AC 3.2: Every change links instructions, approvals and executed document.

Dependencies / posting / downstream consumers. Project WBS, BOQ, work order, schedules, client billing and purchase commitments.

Failure, permission and audit tests. Duplicate change IDs, unapproved work, circular dependency and conflicting baseline versions blocked. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

# 19. PHASE 4 — Quality, safety and collaborative delivery

Quality and safety records are compliance evidence, not substitutes for external certification; document control and field collaboration inherit project roles, immutable evidence and expiry policies.

## E4-01 — Drawings and controlled documents

Business outcome. Ensure teams work from the current approved document revision.

Primary user journey. Upload drawing → classify/version → internal check → approve → issue transmittal → acknowledge → supersede/close.

Screens (responsive, permission-scoped). SCR-E4-01-01 List/board with saved filters and totals · SCR-E4-01-02 Create/edit wizard with contextual masters · SCR-E4-01-03 Detail with linked documents, audit and actions · SCR-E4-01-04 Review/approval or reconciliation workspace · SCR-E4-01-05 Exceptions and exports

Required core fields / entities. Drawing number, discipline, revision, status, project, file hash, author, approver, transmittal recipient, issue purpose, issue date.

Lifecycle / transitions. Work in progress → For review → Approved → Issued → Superseded/Archived. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-01-01. One current revision per drawing number/discipline/project; revisions never overwrite issued files.

BR-E4-01-02. Superseded versions remain visible with warning but cannot be selected as current without override.

BR-E4-01-03. Transmittals preserve exact file revision and recipients; delivery is not presumed acknowledgement.

US-E4-01-01 — Register drawing. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Duplicate drawing number and revision rejected per project.

AC 1.2: Latest approved revision shown by default with historical trace.

US-E4-01-02 — Transmit. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Recipient gets version-specific signed link with expiration and access logging.

AC 2.2: Acknowledgements appear separately from message delivery.

US-E4-01-03 — Revise. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: New revision flags affected RFIs/ITPs and site activities for review.

AC 3.2: Existing historic evidence remains bound to original version.

Dependencies / posting / downstream consumers. Projects, E4-02 RFIs, quality inspections, portal/signed links.

Failure, permission and audit tests. Malware, missing file, revoked link, outdated revision and cross-project access tested. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-02 — RFI and submittals

Business outcome. Control technical questions and material/method approvals.

Primary user journey. Draft RFI/submittal → submit with drawings → assign reviewer/SLA → respond/revise → approve/close and notify affected work.

Screens (responsive, permission-scoped). SCR-E4-02-01 List/board with saved filters and totals · SCR-E4-02-02 Create/edit wizard with contextual masters · SCR-E4-02-03 Detail with linked documents, audit and actions · SCR-E4-02-04 Review/approval or reconciliation workspace · SCR-E4-02-05 Exceptions and exports

Required core fields / entities. RFI no, package, drawing refs, question, proposed solution, priority, due date, reviewer, response, material submittal spec and evidence.

Lifecycle / transitions. Draft → Submitted → Under review → Responded/Revise → Approved/Closed/Rejected. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-02-01. Approval responses bind to exact referenced drawing and submittal revisions.

BR-E4-02-02. Overdue SLA and hand-off ownership are visible in queues; delegated decisions audited.

BR-E4-02-03. Only approved submittal status may satisfy applicable work-start quality gates.

US-E4-02-01 — Request clarification. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: RFI includes drawing number and issue version plus site/WBS location.

AC 1.2: Response history and decision owner are immutable after closure.

US-E4-02-02 — Approve submittal. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Review can approve, approve with conditions or reject with required comments.

AC 2.2: Condition list must be closed or accepted before related work release.

US-E4-02-03 — Manage queue. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: SLA reminders avoid duplicate delivery and support escalations.

AC 3.2: Closed RFI remains linked to dependent site tasks.

Dependencies / posting / downstream consumers. Drawing register, project schedule, quality inspection test plan and notifications.

Failure, permission and audit tests. Expired reviewers, missing attachments, revision supersession and unanswered response handled. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-03 — Issues tasks and meetings

Business outcome. Manage cross-team blockers and accountable follow-through.

Primary user journey. Capture issue/meeting → assign owner and due date → discuss/actions → verify evidence → close and publish minutes.

Screens (responsive, permission-scoped). SCR-E4-03-01 List/board with saved filters and totals · SCR-E4-03-02 Create/edit wizard with contextual masters · SCR-E4-03-03 Detail with linked documents, audit and actions · SCR-E4-03-04 Review/approval or reconciliation workspace · SCR-E4-03-05 Exceptions and exports

Required core fields / entities. Issue number, category, severity, project/WBS, owner, reporter, due date, action checklist, attendees, minutes version, evidence.

Lifecycle / transitions. Open → Assigned → In progress → Blocked → Resolved → Verified/Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-03-01. Critical issue closure requires reviewer verification independent from action owner as configured.

BR-E4-03-02. Meeting minutes are versioned, circulated to participants and acknowledged where required.

BR-E4-03-03. Duplicate issue links are explicit; deleting a meeting never deletes posted work or evidence.

US-E4-03-01 — Track issue. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Issue aging and due-date escalations display on project dashboard.

AC 1.2: Every status change carries actor/time/reason.

US-E4-03-02 — Record meeting. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Action items become trackable tasks linked to minutes and owner.

AC 2.2: Revised minutes preserve original distribution record.

US-E4-03-03 — Close tasks. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Required evidence and verification enforced before critical issue closes.

AC 3.2: Project closeout displays outstanding issues.

Dependencies / posting / downstream consumers. Project DPR, safety observations, RFIs, NCRs, closeout and alerts.

Failure, permission and audit tests. Unknown owner, due date in past, duplicate action and permissions enforced. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-04 — Quality inspections and NCR

Business outcome. Track ITP checkpoints, tests, defects and corrective/preventive action.

Primary user journey. Configure quality plan/ITP → request inspection → record test → pass/hold/fail → NCR → corrective action → reinspection → close.

Screens (responsive, permission-scoped). SCR-E4-04-01 List/board with saved filters and totals · SCR-E4-04-02 Create/edit wizard with contextual masters · SCR-E4-04-03 Detail with linked documents, audit and actions · SCR-E4-04-04 Review/approval or reconciliation workspace · SCR-E4-04-05 Exceptions and exports

Required core fields / entities. ITP ID, hold/witness point, acceptance standard/version, sample/lab, result, instrument, inspector, NCR severity, root cause, CAPA, evidence.

Lifecycle / transitions. ITP Planned → Requested → Inspected → Accepted/Rejected; NCR Open → Actioned → Reverified → Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-04-01. Critical hold points block downstream work release until authorized evidence is accepted.

BR-E4-04-02. Reference standards are effective-dated; result includes sample method and lab credentials where required.

BR-E4-04-03. NCR cannot be closed by originator alone for critical categories; all nonconforming material disposition traceable.

US-E4-04-01 — Request inspection. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Inspector receives scheduled ITP check with required tests and criteria.

AC 1.2: Acceptance records measurable values and reference standard version.

US-E4-04-02 — Issue NCR. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Failed result creates NCR linked to work location, material lot and drawing.

AC 2.2: CAPA includes root cause, owner, due date and verification.

US-E4-04-03 — Release hold. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Authorized reviewer confirms corrective evidence before hold release.

AC 3.2: Released decision and prior failed tests remain auditable.

Dependencies / posting / downstream consumers. P1 quality samples, GRN batch/heat, Phase 3 DPR and E4 drawings.

Failure, permission and audit tests. Failed lab test, missed hold, invalid standard version and duplicate NCR closure are handled as exceptions. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-05 — Safety observations and incidents

Business outcome. Standardize hazard prevention, inspections and incident response.

Primary user journey. Define safety plan → checklist / toolbox talk → observe hazard → assign corrective action → investigate incident → review → close.

Screens (responsive, permission-scoped). SCR-E4-05-01 List/board with saved filters and totals · SCR-E4-05-02 Create/edit wizard with contextual masters · SCR-E4-05-03 Detail with linked documents, audit and actions · SCR-E4-05-04 Review/approval or reconciliation workspace · SCR-E4-05-05 Exceptions and exports

Required core fields / entities. Project, observation, hazard class, severity, personnel role, incident time, injuries (restricted), PPE, evidence, statutory reporting flag, corrective action.

Lifecycle / transitions. Observed → Assigned → Contained → Investigated → Corrected → Verified/Closed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-05-01. Serious incident escalation and reporting reminders follow tenant legal/safety policy; no system claim of filing without evidence.

BR-E4-05-02. Sensitive incident/personnel data has strict field-level access and retention.

BR-E4-05-03. Unsafe work hold can be placed by permitted safety officer and released only after independent approval.

US-E4-05-01 — Report hazard. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Phone workflow works with offline retry and supports photo evidence.

AC 1.2: High-severity observations notify configured safety chain without exposing sensitive information.

US-E4-05-02 — Investigate incident. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Investigation captures causes, actions and reporting decisions with timestamp.

AC 2.2: Required approvals prevent premature closure.

US-E4-05-03 — Review trends. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Near misses, open corrective actions and overdue incidents can be filtered by project.

AC 3.2: Safety dashboards do not disclose restricted personal details to general roles.

Dependencies / posting / downstream consumers. Project site access, tasks, incident notification, quality hold and closeout.

Failure, permission and audit tests. Late offline upload, duplicate event, missing emergency notification delivery and permission leakage tested. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-06 — Equipment utilization and maintenance

Business outcome. Control machinery deployment, operating time, maintenance and project cost.

Primary user journey. Register asset/equipment → allocate project → log daily meter/fuel → schedule maintenance → record breakdown/service → return/reassign.

Screens (responsive, permission-scoped). SCR-E4-06-01 List/board with saved filters and totals · SCR-E4-06-02 Create/edit wizard with contextual masters · SCR-E4-06-03 Detail with linked documents, audit and actions · SCR-E4-06-04 Review/approval or reconciliation workspace · SCR-E4-06-05 Exceptions and exports

Required core fields / entities. Equipment ID, owned/hired, linked fixed asset, meter, location, operator, rental rate, fuel, repair order, downtime, preventive schedule.

Lifecycle / transitions. Available → Allocated → Operating → Maintenance/Breakdown → Available → Retired. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-06-01. Asset accounting belongs to E2-07; operational equipment logs must not duplicate depreciation in GL.

BR-E4-06-02. Meter hours and maintenance intervals validated; downtime and work hours cannot overlap improperly.

BR-E4-06-03. Equipment actual charge-to-project uses effective rate with cost source and approval; one log billed once.

US-E4-06-01 — Allocate equipment. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Equipment cannot be allocated to overlapping active locations without approved exception.

AC 1.2: Daily log updates utilization but not a duplicate asset journal.

US-E4-06-02 — Maintain. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Service schedule creates reminder and maintenance job with cost source.

AC 2.2: Unsafe equipment cannot be dispatched while maintenance block is active.

US-E4-06-03 — Analyse cost. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Project shows hired charges, fuel and allocated operating costs with drill-down.

AC 3.2: Invoiced charges and operational estimates clearly distinguished.

Dependencies / posting / downstream consumers. E2 assets, E3 cost codes, P1 expenses, site DPR and safety.

Failure, permission and audit tests. Negative meter, maintenance overlap, missing fuel rate and double charging prevented. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E4-07 — Punch list and project closeout

Business outcome. Complete snag remediation, handover documents and final financial sign-off.

Primary user journey. Create snag → assign → fix → inspect → accept → compile as-built/manuals/certificates → settle claims → approve handover → archive.

Screens (responsive, permission-scoped). SCR-E4-07-01 List/board with saved filters and totals · SCR-E4-07-02 Create/edit wizard with contextual masters · SCR-E4-07-03 Detail with linked documents, audit and actions · SCR-E4-07-04 Review/approval or reconciliation workspace · SCR-E4-07-05 Exceptions and exports

Required core fields / entities. Snag, exact location, trade, photo, severity, owner, due date, evidence, acceptance, handover package, warranty dates, final account checklist.

Lifecycle / transitions. Open → Assigned → Rectified → Verified → Closed; project handover Draft → Ready → Approved → Archived. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E4-07-01. Critical open snags and outstanding quality/safety holds block normal handover.

BR-E4-07-02. Handover package pins exact revisions and includes acceptance acknowledgements.

BR-E4-07-03. Final commercial closure checks retention, advances, claims, outstanding bills and open commitments separately.

US-E4-07-01 — Resolve punch. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Snag before/after evidence and independent acceptance required for defined severity.

AC 1.2: Reopened snag retains earlier closure history.

US-E4-07-02 — Prepare handover. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Checklist evaluates drawings, ITP reports, manuals and warranties with missing items.

AC 2.2: Generated package has index and exact document revision hashes.

US-E4-07-03 — Archive. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Approved project becomes read-only except approved contractual amendments.

AC 3.2: Data and signed links follow retention policy.

Dependencies / posting / downstream consumers. Projects, E4 document control/quality/safety, E3 final RA, retention and GL.

Failure, permission and audit tests. Missing sign-off, expired warranty, open NCR and unresolved financial balances shown before gate. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

# 20. PHASE 5 — Optional integrations, offline operations and intelligence

All Phase 5 capabilities are optional per plan and per tenant. Each integration requires separate conformance testing, operational runbooks and customer-specific activation.

## E5-01 — Batching plant production integration

Business outcome. Integrate actual RMC manufacture without confusing plant output with traded purchase.

Primary user journey. Approve mix design → schedule batch → send batching request → receive batch data/weights → quality gate → reconcile load and dispatch.

Screens (responsive, permission-scoped). SCR-E5-01-01 List/board with saved filters and totals · SCR-E5-01-02 Create/edit wizard with contextual masters · SCR-E5-01-03 Detail with linked documents, audit and actions · SCR-E5-01-04 Review/approval or reconciliation workspace · SCR-E5-01-05 Exceptions and exports

Required core fields / entities. Mix design/version, materials, target/actual weights, machine run, batch no, moisture/admixture, output m3, quality readings, rejection/wastage.

Lifecycle / transitions. Planned → Released → Batching → Produced → QC cleared → Dispatched/Rejected. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E5-01-01. Mix-design changes require versioned approval; actual batch payload retained unaltered with provider identity.

BR-E5-01-02. One plant batch links to one or more approved trip-load allocations subject to quantity controls.

BR-E5-01-03. Material consumed and produced stock/COGS postings depend on explicitly activated manufacturing inventory accounting; never duplicate trading PO cost.

BR-E5-01-04. Provider downtime falls back to documented manual batch capture with later reconciliation.

US-E5-01-01 — Plan production. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Batch request uses active approved mix and expected pour schedule.

AC 1.2: Device retry does not create duplicate batch IDs.

US-E5-01-02 — Verify quality. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: QC failed batch is blocked from dispatch with reason and supporting readings.

AC 2.2: Actual versus theoretical consumption variance appears in report.

US-E5-01-03 — Reconcile. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Dispatched cubic metres tie to batch output less recorded returns/waste.

AC 3.2: Production and finance reconcile to stock/COGS only once.

Dependencies / posting / downstream consumers. RMC orders, P1 trips, E2 inventory, E4 quality and optional device adaptors.

Failure, permission and audit tests. Broken device connectivity, unexpected batch revision, overweight dosage and unknown sensor units logged. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E5-02 — Fleet GPS and routing

Business outcome. Track consented transport routes and ETA while preserving proof of delivery.

Primary user journey. Assign approved driver/vehicle → begin trip → collect location with permission → ETA/route deviations → site arrival → POD.

Screens (responsive, permission-scoped). SCR-E5-02-01 List/board with saved filters and totals · SCR-E5-02-02 Create/edit wizard with contextual masters · SCR-E5-02-03 Detail with linked documents, audit and actions · SCR-E5-02-04 Review/approval or reconciliation workspace · SCR-E5-02-05 Exceptions and exports

Required core fields / entities. Trip id, vehicle, driver, location accuracy, timestamp, device permission, geofence, ETA source, route plan, alert, telemetry retention.

Lifecycle / transitions. Scheduled → Active → Arrived → Completed/Cancelled; telemetry availability On/Off/Degraded. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E5-02-01. GPS does not substitute receiver proof-of-delivery or legal challan.

BR-E5-02-02. Driver location collected only for active work purposes under tenant policy; permission denial must support manual status.

BR-E5-02-03. Out-of-order telemetry is event-timestamp ordered; stale location visibly labelled with last-seen time.

BR-E5-02-04. Routing provider failure does not block safe delivery posting.

US-E5-02-01 — Track trip. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Dispatcher sees trip last-known location/accuracy and update age.

AC 1.2: Arrival can be confirmed without GPS when authorized.

US-E5-02-02 — Alert deviation. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Configurable route/geofence rules dedupe repetitive alerts.

AC 2.2: Telemetry retention expiry is enforced.

US-E5-02-03 — Reconcile POD. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Trip completion requires existing Phase 1 POD criteria irrespective of tracking.

AC 3.2: Off-duty position is not exposed.

Dependencies / posting / downstream consumers. P1 fleet/driver/challan, consent, provider API, alerts and dispatch board.

Failure, permission and audit tests. GPS denied, low signal, clock skew, provider rate-limit and privacy requests tested. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E5-03 — Government, bank and CRM integrations

Business outcome. Integrate regulated or external systems with verifiable outcomes.

Primary user journey. Configure provider → validate credentials → create export request → submit → poll/webhook → reconcile response → retry/correct via supported portal flow.

Screens (responsive, permission-scoped). SCR-E5-03-01 List/board with saved filters and totals · SCR-E5-03-02 Create/edit wizard with contextual masters · SCR-E5-03-03 Detail with linked documents, audit and actions · SCR-E5-03-04 Review/approval or reconciliation workspace · SCR-E5-03-05 Exceptions and exports

Required core fields / entities. Integration account, scope, certificate/secret ref, provider request id, response receipt, IRN/EWB/payment reference, state, error category, retry timestamp.

Lifecycle / transitions. Configured → Verified → Active → Degraded/Disabled; request Queued → Sent → Accepted/Rejected → Reconciled. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E5-03-01. E-invoice and e-way applicability is per CA-approved effective-dated rule; do not claim compliance solely from integration availability.

BR-E5-03-02. External submission idempotency and reference uniqueness prevent duplicate statutory or banking requests.

BR-E5-03-03. Webhook events are authenticated and matched to originated requests; secrets in vault only.

BR-E5-03-04. Provider rejections are immutable evidence, require correction workflow and may block final issuance per legal policy.

US-E5-03-01 — Submit e-invoice. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: When IRN required, invoice issuance respects mandatory IRN and provider success gates.

AC 1.2: System persists actual IRN, acknowledgement, signed QR and error trace.

US-E5-03-02 — Generate e-way bill. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: User sees required fields, provider acknowledgement and EWB validity; edits use permitted update/cancel rules.

AC 2.2: No invented EWB number when provider unavailable.

US-E5-03-03 — Reconcile bank and lead. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Bank callback reconciles once to existing voucher, web lead is deduplicated and consent-tagged.

AC 3.2: Integration can be disabled without corrupting core books.

Dependencies / posting / downstream consumers. P1 invoice/GST/EWB data, E2 financial reconciliation, CRM, secure vault and observability.

Failure, permission and audit tests. Bad credentials, duplicate callbacks, outage and statutory timing failure surfaced to accountant. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E5-04 — Full offline field operations

Business outcome. Support planned offline work with version-safe synchronization.

Primary user journey. Download permitted assignment snapshot → capture field data and evidence offline → queue commands → regain connection → sync with conflict review.

Screens (responsive, permission-scoped). SCR-E5-04-01 List/board with saved filters and totals · SCR-E5-04-02 Create/edit wizard with contextual masters · SCR-E5-04-03 Detail with linked documents, audit and actions · SCR-E5-04-04 Review/approval or reconciliation workspace · SCR-E5-04-05 Exceptions and exports

Required core fields / entities. Device identity, local encrypted queue item, client UUID, version token, capture timestamp, payload hash, attachment upload state, sync result.

Lifecycle / transitions. Available offline → Draft locally → Queued → Syncing → Applied/Conflict/Rejected. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E5-04-01. Only explicitly listed commands may be offline; financial posting, approvals and legal submissions require online server confirmation.

BR-E5-04-02. Local data encrypted, tenant-account bound, purged on logout/revocation/expiry and never silently transferred between accounts.

BR-E5-04-03. Server validates permissions and current state at sync; stale offline approvals are rejected, not forced.

BR-E5-04-04. Conflict screen supports compare/refresh/recreate without losing original capture evidence.

US-E5-04-01 — Capture offline. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Driver and engineer can save assigned work and attachments with no connectivity.

AC 1.2: Network retry yields exactly one accepted server command per client UUID.

US-E5-04-02 — Resolve conflict. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Stale quantity or closed work returns explicit conflict and valid remediation steps.

AC 2.2: User sees applied versus pending versus rejected queue counts.

US-E5-04-03 — Secure device. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Revoked user cannot sync or expose cached restricted projects.

AC 3.2: Client data is removed at configured logout/expiry boundaries.

Dependencies / posting / downstream consumers. P1 driver action queue, project DPR, quality/safety inspections, secure PWA and synchronization API.

Failure, permission and audit tests. Lost phone, revoked role, multi-device editing, quota exhaustion and clock mismatch have safe behavior. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

## E5-05 — Analytics and AI assistants

Business outcome. Provide explainable predictive insights over authorised, reconciled business data.

Primary user journey. Define opted-in use case → prepare de-identified governed data → score/answer → cite documents and model version → review/act manually.

Screens (responsive, permission-scoped). SCR-E5-05-01 List/board with saved filters and totals · SCR-E5-05-02 Create/edit wizard with contextual masters · SCR-E5-05-03 Detail with linked documents, audit and actions · SCR-E5-05-04 Review/approval or reconciliation workspace · SCR-E5-05-05 Exceptions and exports

Required core fields / entities. Model version, feature snapshot, confidence, prediction horizon, source refs, permission scope, reviewer feedback, run audit and retention.

Lifecycle / transitions. Configured → Validated → Active → Suspended/Retired; insight Generated → Reviewed → Acted/Dismissed. All transitions check tenant, permission, expected row_version and source prerequisites; terminal financial effects use append-only reversals.

BR-E5-05-01. AI outputs are advisory; no autonomous financial posting, statutory filing, legal decision or irreversible approval.

BR-E5-05-02. Every insight links to permitted source documents and has confidence/uncertainty or missing-data warning.

BR-E5-05-03. Prompt/document retrieval enforces tenant and field permissions before and after ranking.

BR-E5-05-04. Opt-in, data processing terms, model versioning, drift checks and human escalation are required.

US-E5-05-01 — Forecast payment risk. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 1.1: Owner sees risk band with underlying signals, period and model version.

AC 1.2: User can open supporting permitted invoices and dismiss the signal.

US-E5-05-02 — Search documents. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 2.1: Answers cite exact source documents and never reveal other tenants’ data.

AC 2.2: No supporting evidence produces a clearly labelled insufficient-information answer.

US-E5-05-03 — Review governance. As an authorised user, I can perform this task without re-keying verified upstream data.

AC 3.1: Admin can disable a model and inspect prediction audit.

AC 3.2: Drift and erroneous prediction reports inform rollback and retraining.

Dependencies / posting / downstream consumers. Approved dashboards and source read models, retrieval access service, consent and model monitoring.

Failure, permission and audit tests. Adversarial prompt injection, unsupported answer, sparse data, stale embeddings and restricted data are tested. Re-run with wrong tenant, revoked permission, duplicate idempotency key, stale row_version, and retry after timeout: no cross-tenant leak, duplicate posting, silent overwrite or mismatched balance. Capture actor, reason and event for every approved change.

# 21. Cross-phase delivery plan, testing and traceability

| Phase | Outcome                               | UAT golden path                                                                                                                                                                  | Exit evidence                                                                        |
|-------|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| 2     | Stock/finance operational depth       | Receive GRN → warehouse put-away → move stock → issue to project → count adjustment → AP reconciliation → bank match → GST2B/TDS reconcile → depreciation → subscription renewal | Inventory vs GL cutover tie-out; bank and tax controls; SaaS payment webhook audit   |
| 3     | Project profitability and contracting | Create project → BOQ version → WBS baseline → indent/PO → site DPR/material issue → work order → certified MB/RA → variation → forecast                                          | Baseline/revision history; certified quantity math; project costs match GL           |
| 4     | Controlled delivery and handover      | Issue drawing → RFI → inspect hold point → NCR/CAPA → safety report → equipment downtime → punch list → full handover                                                            | All critical hold/issue gates cleared; document revision trace; safety privacy tests |
| 5     | Integration without core regression   | Batch/dispatch → GPS → approved IRN/EWB provider flow → offline DPR sync → explainable forecast                                                                                  | Provider conformance receipts; duplicate callback/offline tests; AI permission eval  |

Traceability: Every BR-{epic}-nn and US-{epic}-nn above is a baseline requirement. The build backlog must reference module IDs, screen IDs, API operation IDs, migrations, test case IDs and milestone; a module is complete only when each acceptance criterion is demonstrated with automated tests and signed pilot UAT. No “stories next chunk” remains for the 29 listed epics.

# 22. Required architecture contracts

Bounded modules: Identity & Tenancy, Party & Commercial, CRM & Sales, Procurement, Fulfilment & RMC, Inventory, Finance & Tax, Projects, Collaboration & Quality, Equipment, Platform Billing, Integrations & Analytics.

Backend ASP.NET Core modular monolith; PostgreSQL + EF Core; Angular TypeScript. Enforce Domain → Application → Infrastructure/API boundaries, one transaction boundary per monetary/stock state transition and transactional outbox for all downstream messages.

Use dedicated command/query contracts and central authorization; every tenant DB operation has tenant filter plus trusted tenant boundary verification, including migration scripts, SQL reporting and stored assets.

Exposed API resources implement consistent paging, filtering, RFC7807-style problem details, trace IDs, idempotency and ETag/row-version behavior; server computes totals and approvals.

Each posted financial source maps to a deterministic balanced journal batch; read-model and BI tables are derived and rebuildable, never a parallel financial source of truth.

Document service supports immutable blob versions, AV scanning, server-side signed URLs, release-specific revision manifest and GDPR/DPDP-purposeful retention policies subject to law.

Operational dashboard and integration runbooks cover queue backlog, rejected provider events, read-model lag, ledger imbalance, stock imbalance, failed reconciliation and SLA breaches.

# 23. Open validations — explicit, not omitted requirements

The BRD specifies configurable behavior but cannot itself certify external tax law or obtain client approvals. Prior to relevant production activation, the tenant CA/legal advisor confirms: GST/HSN and rate determination by supply category and effective date; e-invoice and e-way applicability and issuer timing; TDS/TCS applicability; MSME obligations; construction contract/retention tax handling; invoice numbering and adjustment legality; record retention and DPDP notices. The customer confirms opening stock valuation, landed-cost basis, project billing terms, subscription prices, safety/quality standards and approval matrices. Defaults are documented and changes are effective-dated, tenant-owned and audited.

# 24. Release readiness checklist

All 19 original Phase 1 modules retained with no removed stories; 29 Phase 2–5 epics now detailed in v2.2.

Each API and entity from the implementation backlog traced to screen, rule and acceptance test.

Phase 1 sample company golden path rerun after Phase 2 cutover to prove no regression.

Negative tenant/cost permissions, concurrency, idempotency, reversal, data migration and fiscal close tests pass.

Books, stock, project actuals and tax reports reconcile on same as-of date.

Staging backup restore, observability, support runbooks, approvals and security gates signed.

CA and pilot sign-offs attached as external evidence, not presumed.
