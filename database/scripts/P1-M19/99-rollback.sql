-- Guarded M19 rollback. Stop if any M19 business data exists; do not use CASCADE.
BEGIN;
DO $$
DECLARE table_name text; has_rows boolean;
BEGIN
  FOREACH table_name IN ARRAY ARRAY['m19_dispute_evidence','m19_dispute_invoice','m19_quality_test','m19_quality_event','m19_dispute','m19_quality_sample','m19_quality_criteria'] LOOP
    IF to_regclass(format('%I.%I',current_schema(),table_name)) IS NOT NULL THEN
      EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)',current_schema(),table_name) INTO has_rows;
      IF has_rows THEN RAISE EXCEPTION 'Refusing M19 rollback: %.% contains data',current_schema(),table_name; END IF;
    END IF;
  END LOOP;
END $$;
DROP TABLE IF EXISTS m19_dispute_evidence;
DROP TABLE IF EXISTS m19_dispute_invoice;
DROP TABLE IF EXISTS m19_quality_test;
DROP TABLE IF EXISTS m19_quality_event;
DROP TABLE IF EXISTS m19_dispute;
DROP TABLE IF EXISTS m19_quality_sample;
DROP TABLE IF EXISTS m19_quality_criteria;
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId"='20261127134458_P1M19QualityDisputes';
DELETE FROM m02_role_permission WHERE "PermissionCode" LIKE 'm19.%';
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm19.%';
DELETE FROM platform_module WHERE "ModuleCode"='M19'
  AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M19');
COMMIT;