-- Guarded, destructive P1-M03 rollback. Never run as part of normal deployment.
-- Before execution: take a verified backup and explicitly execute:
--   SET berynx.allow_p1_m03_rollback = 'on';
-- The guard refuses rollback unless M03 tables contain no rows (including seeds)
-- and all four P1-M03 migrations are recorded in the current database. Additive
-- M02 permission catalog/role grants are intentionally retained for review.

BEGIN;

DO $rollback_guard$
DECLARE
  table_row record;
  has_rows boolean;
BEGIN
  IF current_setting('berynx.allow_p1_m03_rollback', true) IS DISTINCT FROM 'on' THEN
    RAISE EXCEPTION 'Rollback refused: explicitly set berynx.allow_p1_m03_rollback=on after a verified backup';
  END IF;

  IF current_database() <> 'berynx_construction_dev'
     AND current_database() !~ '_test$' THEN
    RAISE EXCEPTION 'Rollback refused outside the authorized development database or a disposable _test database';
  END IF;

  IF NOT EXISTS (
       SELECT 1 FROM "__EFMigrationsHistory"
       WHERE "MigrationId" = '20261124114145_P1M03Masters'
     )
     OR NOT EXISTS (
       SELECT 1 FROM "__EFMigrationsHistory"
       WHERE "MigrationId" = '20261124115631_P1M03MasterConcurrency'
     )
     OR NOT EXISTS (
       SELECT 1 FROM "__EFMigrationsHistory"
       WHERE "MigrationId" = '20261124121636_P1M03VersionedMasters'
     )
     OR NOT EXISTS (
       SELECT 1 FROM "__EFMigrationsHistory"
       WHERE "MigrationId" = '20261124122738_P1M03PartyOwnership'
     ) THEN
    RAISE EXCEPTION 'Rollback refused: all four P1-M03 migrations must be applied';
  END IF;

  FOR table_row IN
    SELECT tablename FROM pg_tables
    WHERE schemaname = current_schema() AND tablename LIKE 'm03\_%' ESCAPE '\'
  LOOP
    EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I LIMIT 1)', table_row.tablename)
      INTO has_rows;
    IF has_rows THEN
      RAISE EXCEPTION 'Rollback refused: %.% contains data; preserve it and resolve dependencies manually',
        current_schema(), table_row.tablename;
    END IF;
  END LOOP;
END
$rollback_guard$;

DROP TABLE m03_category_tax_override;
DROP TABLE m03_charge_type;
DROP TABLE m03_contact;
DROP TABLE m03_driver;
DROP TABLE m03_equipment;
DROP TABLE m03_import_row_error;
DROP TABLE m03_party_bank_account;
DROP TABLE m03_party_merge;
DROP TABLE m03_party_role;
DROP TABLE m03_plant;
DROP TABLE m03_price_list_line;
DROP TABLE m03_rmc_rate_card;
DROP TABLE m03_site;
DROP TABLE m03_tax_rate;
DROP TABLE m03_unit_conversion;
DROP TABLE m03_vehicle;
DROP TABLE m03_import_batch;
DROP TABLE m03_product;
DROP TABLE m03_party;
DROP TABLE m03_unit;
DROP TABLE m03_payment_term;
DROP TABLE m03_price_list;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20261124115631_P1M03MasterConcurrency',
  '20261124121636_P1M03VersionedMasters',
  '20261124122738_P1M03PartyOwnership',
  '20261124114145_P1M03Masters'
);

COMMIT;
