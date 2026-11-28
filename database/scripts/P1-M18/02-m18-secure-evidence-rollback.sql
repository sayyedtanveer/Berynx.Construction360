-- Guarded manual rollback for P1-M18. This intentionally refuses to delete document metadata or audit history.
BEGIN;
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'm18_document_event')
       AND EXISTS (SELECT 1 FROM m18_document_event) THEN
        RAISE EXCEPTION 'M18 rollback refused: document audit history exists. Preserve records and roll forward instead.';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'm18_document')
       AND EXISTS (SELECT 1 FROM m18_document) THEN
        RAISE EXCEPTION 'M18 rollback refused: document metadata or version history exists. Preserve records and roll forward instead.';
    END IF;
END $$;
DROP TABLE IF EXISTS m18_document_event;
DROP TABLE IF EXISTS m18_document;
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127105316_P1M18SecureEvidenceStorage';
COMMIT;