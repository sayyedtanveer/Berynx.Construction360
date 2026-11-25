# P1-M05 Quotations SQL deployment

Manual deployment package for migration `20261125092036_P1M04M05Foundation`.
This package shares the migration with M04; apply M04 and M05 together in the
exact order documented in `docs/modules/P1-M04-M05.md` after verifying the
M01–M03 and SaaS Foundation prerequisites and current migration history.

Scripts in this folder:

- `01-schema.sql`, `02-alter-existing.sql`, `03-indexes-constraints.sql`
- `04-verify.sql` (read-only; run only after all six M04/M05 deployment fragments)

The shared guarded rollback is `database/scripts/P1-M04/99-guarded-rollback.sql`.
It refuses rollback if feature tables contain rows, later EF migrations exist,
or the M03 `PriceBasis` columns contain non-default values. It has not been
executed against PostgreSQL. The deployment fragments are not restart-safe after
a partial multi-file apply; reconcile actual schema/history before any recovery.

Owner manual execution requires a verified backup, maintenance window, exact
schema/history review, prerequisite verification and a separate disposable
`_test` database for integration testing. No DB connection, SQL execution or
migration application was performed. Do not use `dotnet ef database update` or
apply against the development database.
