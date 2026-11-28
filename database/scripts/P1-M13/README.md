# P1-M13 GST — internal reporting checkpoint

No generated EF migration or model snapshot is available; this package is a draft and is **not deployable**.

Current reports query only M10 `Posted` invoice data. Since no invoices can post, current periods contain no outward supplies. Supplier ITC is explicitly unavailable until M07 invoice posting and M12 accounting are integrated. The JSON export is an internal extract, not a portal-certified GSTR-1 payload. No GST portal calls or filing are performed.

Manual order after migration parity review: apply migration → `01-schema.sql` → `02-alter-existing.sql` → `03-indexes-constraints.sql` → `04-seed.sql` → `05-verification.sql`. Rollback refuses filed period history. No DB/SQL operation occurred.
