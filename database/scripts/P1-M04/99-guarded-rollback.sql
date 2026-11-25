-- GUARDED ROLLBACK for shared migration 20261125092036_P1M04M05Foundation.
-- Manual owner execution only. Never run unless a verified backup exists,
-- a maintenance window is active, and no later migration depends on this one.
-- This script refuses rollback if the migration is absent, any M04/M05 table
-- contains rows, or either M03 PriceBasis column has values other than the
-- migration's legacy default. DROP TABLE is deliberately not CASCADE.

BEGIN;

LOCK TABLE
    m03_price_list,
    m03_rmc_rate_card,
    m04_assignment_cursor,
    m04_assignment_rule,
    m04_enquiry,
    m04_number_counter,
    m04_assignment_audit,
    m04_enquiry_activity,
    m04_enquiry_line,
    m04_follow_up,
    m04_sla_alert,
    m05_quotation,
    m05_quotation_activity,
    m05_quotation_charge,
    m05_quotation_line,
    m05_quotation_link
IN ACCESS EXCLUSIVE MODE;

DO $rollback_guard$
DECLARE
    table_name text;
    row_count bigint;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20261125092036_P1M04M05Foundation'
    ) THEN
        RAISE EXCEPTION 'Rollback refused: shared migration history row is absent';
    END IF;

    IF EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" > '20261125092036_P1M04M05Foundation'
    ) THEN
        RAISE EXCEPTION 'Rollback refused: a later EF migration is recorded';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
        'm04_assignment_cursor',
        'm04_assignment_rule',
        'm04_enquiry',
        'm04_number_counter',
        'm04_assignment_audit',
        'm04_enquiry_activity',
        'm04_enquiry_line',
        'm04_follow_up',
        'm04_sla_alert',
        'm05_quotation',
        'm05_quotation_activity',
        'm05_quotation_charge',
        'm05_quotation_line',
        'm05_quotation_link'
    ] LOOP
        EXECUTE format('SELECT count(*) FROM %I', table_name) INTO row_count;
        IF row_count <> 0 THEN
            RAISE EXCEPTION 'Rollback refused: %.% contains % row(s)', current_schema(), table_name, row_count;
        END IF;
    END LOOP;

    IF EXISTS (SELECT 1 FROM m03_price_list WHERE "PriceBasis" <> 'ExPlant')
       OR EXISTS (SELECT 1 FROM m03_rmc_rate_card WHERE "PriceBasis" <> 'ExPlant') THEN
        RAISE EXCEPTION 'Rollback refused: M03 PriceBasis contains non-default values';
    END IF;
END $rollback_guard$;

DROP TABLE m04_assignment_audit;
DROP TABLE m04_assignment_cursor;
DROP TABLE m04_assignment_rule;
DROP TABLE m04_enquiry_activity;
DROP TABLE m04_enquiry_line;
DROP TABLE m04_follow_up;
DROP TABLE m04_number_counter;
DROP TABLE m04_sla_alert;
DROP TABLE m05_quotation_activity;
DROP TABLE m05_quotation_charge;
DROP TABLE m05_quotation_line;
DROP TABLE m05_quotation_link;
DROP TABLE m05_quotation;
DROP TABLE m04_enquiry;

DROP INDEX "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~";
ALTER TABLE m03_rmc_rate_card DROP CONSTRAINT ck_m03_rmc_rate_card_basis;
DROP INDEX "IX_m03_price_list_TenantId_CustomerGroup_PriceBasis_ValidFrom";
DROP INDEX "IX_m03_price_list_TenantId_CustomerPartyId_PriceBasis_ValidFrom";
DROP INDEX "IX_m03_price_list_TenantId_PriceBasis_ValidFrom";
ALTER TABLE m03_price_list DROP CONSTRAINT ck_m03_price_list_basis;
ALTER TABLE m03_rmc_rate_card DROP COLUMN "PriceBasis";
ALTER TABLE m03_price_list DROP COLUMN "PriceBasis";

CREATE UNIQUE INDEX "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~"
    ON m03_rmc_rate_card
       ("TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "EffectiveFrom");
CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerGroup_ValidFrom"
    ON m03_price_list ("TenantId", "CustomerGroup", "ValidFrom")
    WHERE "CustomerGroup" IS NOT NULL;
CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerPartyId_ValidFrom"
    ON m03_price_list ("TenantId", "CustomerPartyId", "ValidFrom")
    WHERE "CustomerPartyId" IS NOT NULL;
CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_ValidFrom"
    ON m03_price_list ("TenantId", "ValidFrom")
    WHERE "IsDefault";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261125092036_P1M04M05Foundation';

COMMIT;
