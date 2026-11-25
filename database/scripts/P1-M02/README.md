# P1-M02 SQL deployment

This bundle covers the M02 migrations:

- `20261124000300_UsersRolesApprovals`
- `20261124092204_P1M02IdentityAuthentication`
- `20261124094727_P1M02SessionSecurityStamp`

The bundle files contain the actual SQL for the M02 tables, indexes,
constraints, functions, triggers, Identity changes and reference seeds. The
legacy flat EF-generated scripts remain available separately.

## Prerequisites and execution order

M01 must be deployed first. For a fresh database, execute in this exact order
from the `database/scripts/P1-M02` directory. These files contain direct SQL
and can be run individually in a PostgreSQL SQL client or with `psql`
(`ON_ERROR_STOP` enabled):

1. `01-schema.sql` — M02 user, role, approval, audit and authorization tables,
   indexes, constraints, functions and triggers from the EF-generated
   migration. This creates `m02_permission_catalog`, required by M03 seeds.
2. `02-alter-existing.sql` — Identity authentication/session/challenge
   migrations, including the membership link and session security-stamp
   alteration.
3. `03-indexes-constraints.sql` — indexes and constraints are already in the
   migration SQL in steps 1 and 2; this file records that no extra DDL is
   needed.
4. `04-seed.sql` — permission catalog and system-role templates, then
   idempotent role provisioning for existing tenants.
5. `05-verification.sql` — retain the migration, object, trigger and seed
   counts with the deployment record.

Example:

```sh
psql -v ON_ERROR_STOP=1 -f database/scripts/P1-M02/01-schema.sql
```

Run each file as a separate ordered invocation. The owner must verify the
target database, inspect its actual schema and `__EFMigrationsHistory`, and
take an approved backup before deployment to an existing database. Do not
infer applied migrations from these files.

`99-rollback.sql` is a separate destructive owner action. It requires all
three migrations and aborts if user, role, approval, identity, session or audit
records exist. It removes M02 system templates as well as schema, and must not
be run as a routine deployment step. If the guard fires, restore from an
approved backup rather than weakening it.
