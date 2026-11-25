-- Read-only verification for the shared M04/M05 migration.
-- Run after all six deployment fragments, not between fragments.
-- No schema or data is modified by this script.

WITH expected_tables(table_name) AS (
    VALUES
        ('m04_assignment_cursor'),
        ('m04_assignment_rule'),
        ('m04_enquiry'),
        ('m04_number_counter'),
        ('m04_assignment_audit'),
        ('m04_enquiry_activity'),
        ('m04_enquiry_line'),
        ('m04_follow_up'),
        ('m04_sla_alert')
), table_checks AS (
    SELECT
        table_name AS check_name,
        to_regclass(format('%I.%I', current_schema(), table_name)) IS NOT NULL AS passed,
        CASE WHEN to_regclass(format('%I.%I', current_schema(), table_name)) IS NOT NULL
             THEN 'present' ELSE 'MISSING' END AS detail
    FROM expected_tables
), required_columns(table_name, column_name) AS (
    VALUES
        ('m03_price_list', 'PriceBasis'),
        ('m03_rmc_rate_card', 'PriceBasis'),
        ('m04_enquiry', 'NextActionAt'),
        ('m04_enquiry', 'IdempotencyKey'),
        ('m04_enquiry_line', 'RmcGrade'),
        ('m04_follow_up', 'DueAt')
), column_checks AS (
    SELECT
        format('%s.%s', table_name, column_name) AS check_name,
        EXISTS (
            SELECT 1
            FROM information_schema.columns c
            WHERE c.table_schema = current_schema()
              AND c.table_name = required_columns.table_name
              AND c.column_name = required_columns.column_name
        ) AS passed,
        CASE WHEN EXISTS (
            SELECT 1
            FROM information_schema.columns c
            WHERE c.table_schema = current_schema()
              AND c.table_name = required_columns.table_name
              AND c.column_name = required_columns.column_name
        ) THEN 'present' ELSE 'MISSING' END AS detail
    FROM required_columns
), history_check AS (
    SELECT
        'EF migration history' AS check_name,
        EXISTS (
            SELECT 1 FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '20261125092036_P1M04M05Foundation'
        ) AS passed,
        CASE WHEN EXISTS (
            SELECT 1 FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '20261125092036_P1M04M05Foundation'
        ) THEN 'shared migration recorded' ELSE 'MISSING migration record' END AS detail
)
SELECT check_name, passed, detail FROM table_checks
UNION ALL
SELECT check_name, passed, detail FROM column_checks
UNION ALL
SELECT check_name, passed, detail FROM history_check
ORDER BY check_name;
