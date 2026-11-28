# M19 Quality & disputes database package

## Contents and order

1. `01-m19-migration.sql` — generated idempotent PostgreSQL script for EF migration `20261127134458_P1M19QualityDisputes`; it creates the seven M19 tables, constraints, tenant-scoped foreign keys and indexes and records the migration ID.
2. `04-seed.sql` — registers M19 and its implemented permissions. It does not add M19 to plans or grant tenant subscription entitlements. Configure a plan/tenant entitlement through the normal SaaS administration workflow before enabling the module.
3. `05-verification.sql` — read-only object and permission checks.
4. `99-rollback.sql` — guarded destructive rollback. It refuses to run while any M19 table contains records. Use only with explicit owner approval and after taking a verified backup.

Apply after the existing M18 secure-evidence schema/deployment package and before enabling M19 application traffic. The migration SQL is generated from EF and is the sole schema source; do not also apply handwritten table DDL or run `dotnet ef database update`. No SQL or migration was executed during implementation.

The default project acceptance values are not seeded. Quality owners must configure grade-specific criteria, effective dates and references. M11 credit-note posting remains a blocking dependency for credit-note dispute resolution.