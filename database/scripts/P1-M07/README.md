# P1-M07 Procurement database delivery status

**Not deployment-ready.** This increment has no generated EF Core migration or model snapshot update. Do not execute `04-seed.sql` until the matching schema, indexes, constraints, migration and complete deployment package are generated and reviewed.

`04-seed.sql` only registers M07 as an optional Platform module and adds the permissions used by the current purchase-order and goods-receipt API. It deliberately does not enable M07 for any subscription or tenant.

Pending before any deployment:

1. Generate the incremental EF Core migration and update the model snapshot.
2. Generate and review `01-schema.sql`, `02-alter-existing.sql`, `03-indexes-constraints.sql`, `05-verification.sql`, and guarded `99-rollback.sql` against the migration and existing database.
3. Add integration tests on a separately configured disposable `_test` database.
4. Review module grants and configure M07 for a test tenant manually through PlatformAdmin.

Never execute SQL or database updates from the application.
