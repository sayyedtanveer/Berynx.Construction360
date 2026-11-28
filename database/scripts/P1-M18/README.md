# P1-M18 manual database package

This package adds only M18 document metadata and immutable document audit tables. The migration and SQL are generated but **not applied**.

1. Reconcile the currently applied migration history and owner-approved M08/M09/M10 prerequisites. M08 evidence review migration `20261127081534_P1M08ReceiverEvidenceReview` must be applied first, along with the existing M08 trip schema and M02 user tables.
2. Review and manually execute `01-m18-secure-evidence-deploy.sql`, generated for migration `20261127105316_P1M18SecureEvidenceStorage`.
3. Run read-only checks in `03-m18-secure-evidence-verification.sql`.
4. `02-m18-secure-evidence-rollback.sql` is provided only for an empty M18 schema. It aborts if any document or audit record exists; do not modify that guard to force a rollback.

Do not execute these files as part of normal application startup. No development database was contacted during generation. See `docs/modules/P1-M18.md` for scanner and private-storage configuration.