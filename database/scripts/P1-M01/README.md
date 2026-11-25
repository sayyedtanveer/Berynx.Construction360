# P1-M01 SQL deployment

This bundle covers EF Core migrations `20261124000100_Onboarding` and
`20261124000200_OnboardingDelivery`. `01-schema.sql` contains the actual
idempotent migration SQL, including tables, delivery alterations, constraints,
functions and triggers. The legacy flat generated script remains available
separately.

## Prerequisites and execution

M01 is the base schema dependency for M02 and M03. On a fresh database, execute
the files in this order:

1. `01-schema.sql` — creates the M01 tables and applies migration-defined
   alterations, indexes, constraints, functions and triggers. It records both
   migration IDs.
2. `02-alter-existing.sql` — no separate alteration step; the alterations are
   included in the generated migration SQL in step 1.
3. `03-indexes-constraints.sql` — no separate constraint step; indexes and
   constraints are included in step 1.
4. `04-seed.sql` — no global seed is required for M01.
5. `05-verification.sql` — retain its results with the deployment record.

The files contain direct SQL and can be run individually in a PostgreSQL SQL
client, or with `psql` using `ON_ERROR_STOP`:

```sh
psql -v ON_ERROR_STOP=1 -f database/scripts/P1-M01/01-schema.sql
```

On an existing database, first confirm the target database, inspect its actual
schema and `__EFMigrationsHistory`, and take an owner-managed backup. Do not
infer database state from source files.

`99-rollback.sql` is a separate destructive owner action, not part of routine
deployment. It refuses to proceed when either migration is missing or any M01
tenant/setup data exists. A failed preflight must be resolved by an approved
backup/restore plan, not by removing the guard.
