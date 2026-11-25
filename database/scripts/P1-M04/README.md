# P1-M04 CRM SQL deployment

Manual deployment package for migration `20261125092036_P1M04M05Foundation`.
This package shares the migration with M05; apply M04 and M05 together in the
exact order documented in `docs/modules/P1-M04-M05.md` after verifying the
M01–M03 and SaaS Foundation prerequisites and current migration history.

Scripts in this folder:

- `01-schema.sql`, `02-alter-existing.sql`, `03-indexes-constraints.sql`
- `04-verify.sql` (read-only; run only after all six M04/M05 deployment fragments)
- `99-guarded-rollback.sql` (shared rollback for the complete M04/M05 migration)

`99-guarded-rollback.sql` requires the migration history row, refuses later
EF migrations and non-empty M04/M05 tables, and refuses non-default M03
`PriceBasis` values. It uses `DROP TABLE` without `CASCADE`. This is still a
destructive manual artifact: use only after a verified backup, maintenance
window, exact schema/history review and confirmation that no dependent objects
or later deployments require the migration. It has not been executed against
PostgreSQL. The deployment fragments are not restart-safe after a partial
multi-file apply; stop and reconcile actual schema/history with the owner rather
than blindly rerunning them.

No database connection, SQL execution or migration application was performed.
Do not use `dotnet ef database update` or apply against the development database.
