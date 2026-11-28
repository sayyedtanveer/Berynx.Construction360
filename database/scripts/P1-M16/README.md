# P1-M16 Reports

No generated EF migration/model snapshot exists; this manual package is draft-only.

Implemented R-S03 Pending Orders, R-D01 Delivery Summary, R-D04 Unbilled Challans, and R-X01 Expense Analytics (requires `global.view_cost`). CSV export carries the same report filters/columns and writes an export audit row. JSON viewer totals are source-derived. Other reports, Excel/PDF, saved views, schedules, print, export history UI, detailed drill-down and nightly reconciliation are pending.

Manual order after all source migrations: migration parity review → `01-schema.sql` → `02-alter-existing.sql` → `03-indexes-constraints.sql` → `04-seed.sql` → `05-verification.sql`. Do not deploy before EF migration review. Rollback refuses export history. No DB operation occurred.
