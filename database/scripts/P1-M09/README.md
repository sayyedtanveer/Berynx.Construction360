# P1-M09 Delivery Challans — manual SQL package status

**Do not deploy until an EF Core migration and model-snapshot update are generated and reviewed.**

## Dependencies

- P1-M01 tenant setup, branch and `Challan` series configured per branch.
- P1-M02 tenant authorization and permissions.
- P1-M03 customers, sites, products, plants, vehicles and drivers.
- P1-M06 sales orders, lines and delivery schedules.
- P1-M08 pours and trips.
- A supported .NET 10/EF toolchain is required to generate the corresponding incremental migration. Migration identifier: **not generated**.

## Manual order after the migration is reviewed

1. Apply the generated EF migration through the owner's approved deployment process; do not use these scripts as a substitute for migration review.
2. Review and execute `01-schema.sql` only if it matches the generated migration (the file is currently a draft).
3. Execute `02-alter-existing.sql`, then `03-indexes-constraints.sql`.
4. Execute `04-seed.sql` to register M09 as optional; this does not enable any plan/tenant.
5. Execute `05-verification.sql`.
6. Keep `99-rollback.sql` as a guarded manual recovery reference; it refuses rollback when challans exist.

No database connection or SQL execution has been performed. The current implementation exposes printable HTML, not generated PDF or share links; corrections, M10 invoice linkage and daily alert delivery remain unimplemented.
