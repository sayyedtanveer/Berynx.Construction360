-- Read-only verification for P1-M18. Run after the deployment SQL.
DO $$
BEGIN
    IF to_regclass('m18_document') IS NULL OR to_regclass('m18_document_event') IS NULL THEN
        RAISE EXCEPTION 'M18 document tables are missing.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_m18_document_TenantId_TripId_Purpose_VersionNumber') THEN
        RAISE EXCEPTION 'M18 document version uniqueness index is missing.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_m18_document_TenantId_TripId_Purpose_IsCurrent') THEN
        RAISE EXCEPTION 'M18 single-current-version index is missing.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127105316_P1M18SecureEvidenceStorage') THEN
        RAISE EXCEPTION 'M18 migration history entry is missing.';
    END IF;
END $$;
SELECT 'm18_document' AS table_name, count(*) AS records FROM m18_document
UNION ALL
SELECT 'm18_document_event', count(*) FROM m18_document_event;