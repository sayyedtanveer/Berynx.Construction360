-- M03 price-basis columns with legacy-row defaults
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    ALTER TABLE m03_rmc_rate_card ADD "PriceBasis" character varying(16) NOT NULL DEFAULT 'ExPlant';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    ALTER TABLE m03_price_list ADD "PriceBasis" character varying(16) NOT NULL DEFAULT 'ExPlant';
    END IF;
END $EF$;

COMMIT;
