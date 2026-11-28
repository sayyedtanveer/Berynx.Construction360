# M01–M06 integration audit

## Result

The M06 source changes establish a quotation-to-sales-order handoff and an M06 lifecycle boundary. This is a source-level implementation only: no Release build, tests, EF migration generation, database deployment, pilot, or UAT was verified. The selected checkout's shell has no `dotnet`, `node`, or `npm` executable. There is no claim of production approval.

## Integration findings

| Boundary | Evidence | Result |
|---|---|---|
| M01 tenant and branch context | M06 uses the existing tenant context and tenant-filtered EF DbContext; branch is copied from the accepted quote or resolved from M03 site/branch. | Source-level; cross-tenant integration tests pending. |
| M02 RBAC / approval | Each API checks M06 entitlement and permission; credit override, pricing, over-tolerance scheduling, amendment and short-close reversal request the existing approval engine. `/m02/context` supplies enabled modules and permissions across assigned scopes for frontend navigation. | Source-level; test tenant requires explicit add-on activation and role grants. |
| M03 customer/site/product/price/tax/credit | Direct orders use M03 customer, site, product, payment term, charge, price, RMC rate-card and tax resolution. Confirmation invokes credit evaluation. | Credit facts currently report unavailable; margin purchase-cost data is not present. |
| M04 enquiry | Accepted-quote conversion from an enquiry marks that enquiry Won and links its M06 order within the conversion transaction. Manual Won requires a live linked M06 order. | Source-level; old quote/enquiry cases and concurrent calls require testing. |
| M05 quotation | Accepted and valid quote conversion copies stored line/price/tax snapshots, allocates consumed quantity and apportions charges. M05 detail displays remaining quantity and created orders. | Source-level; idempotency/concurrency and rounding need tests. |
| SaaS entitlement | Optional `M06` catalog seed; route guard and API module guard. | Seed grants no tenant entitlement. API activation is a PlatformAdmin plan/add-on assignment. |
| Tenant tolerance configuration | RMC and steel tolerances are editable through M06 settings with optimistic version checks and M02 audit events. | No migration/test runtime validation yet. |
| M07/M08/M10/M11 | Stable data contracts and persisted linked-document references; UI presents unavailable states. | Producers not implemented; no purchase, dispatch, invoice or receipt is fabricated. |

## Acceptance evidence and gaps

- `US-M06-01` through `US-M06-08` and `BR-M06-01` through `BR-M06-07` are mapped in `docs/modules/P1-M06.md`.
- Added unit and Angular component specs, but neither test runner is available here.
- Database integration tests are **PENDING**; no database connectivity probe was performed.
- Manual SQL package exists, but the EF migration source and model snapshot remain **PENDING**. Do not apply package SQL until that gap is reviewed.
- M06 confirmation currently cannot evaluate negative margin because M07 purchase-cost data is absent. A margin-based production approval claim would be false.
- The order confirmation endpoint returns printable HTML, not a PDF.
- Fulfilment delivered/invoiced/paid metrics remain unavailable until M08/M10/M11 persist those events.
- Provider integration, pilot, UAT and production readiness are not verified.

## Recommended next steps

1. Generate the EF Core migration and model snapshot with the supported SDK; compare every table, FK, index, check and precision against `database/scripts/P1-M06/`.
2. Configure only a disposable PostgreSQL database whose name ends in `_test`, then execute M06 integration tests for conversion, concurrency, scope, credit and amendment behavior.
3. Supply transaction-backed M03 credit facts and authorized M07 purchase-cost facts; add required negative-margin approval before production activation.
4. Validate PlatformAdmin entitlement assignment and RBAC grants with a test tenant without changing other tenant entitlements.
5. Complete real PDF, M07/M08/M10/M11 integrations and pilot/UAT evidence before marking the module accepted.
