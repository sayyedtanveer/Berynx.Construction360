-- P1-M03 deployment verification. Read-only; does not alter data or schema.

-- Required M01/M02 and P1-M03 EF migration history.
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20261124000100_Onboarding',
  '20261124000200_OnboardingDelivery',
  '20261124000300_UsersRolesApprovals',
  '20261124092204_P1M02IdentityAuthentication',
  '20261124094727_P1M02SessionSecurityStamp',
  '20261124114145_P1M03Masters',
  '20261124115631_P1M03MasterConcurrency',
  '20261124121636_P1M03VersionedMasters',
  '20261124122738_P1M03PartyOwnership'
)
ORDER BY "MigrationId";

-- P1-M03 relation presence.
SELECT expected.table_name,
       to_regclass(format('%I.%I', current_schema(), expected.table_name)) IS NOT NULL AS exists
FROM (VALUES
  ('m03_party'), ('m03_party_role'), ('m03_party_bank_account'), ('m03_party_merge'),
  ('m03_site'), ('m03_contact'), ('m03_plant'), ('m03_product'), ('m03_tax_rate'),
  ('m03_category_tax_override'), ('m03_unit'), ('m03_unit_conversion'),
  ('m03_price_list'), ('m03_price_list_line'), ('m03_rmc_rate_card'), ('m03_vehicle'),
  ('m03_driver'), ('m03_equipment'), ('m03_payment_term'), ('m03_charge_type'),
  ('m03_import_batch'), ('m03_import_row_error')
) AS expected(table_name)
ORDER BY expected.table_name;

-- Optimistic-concurrency columns added by the alteration migration.
SELECT table_name, column_name, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = current_schema()
  AND (table_name, column_name) IN (
    ('m03_price_list_line', 'Version'),
    ('m03_import_batch', 'Version'),
    ('m03_contact', 'Version'),
    ('m03_unit', 'Version'),
    ('m03_tax_rate', 'Version'),
    ('m03_party', 'SalespersonUserId'),
    ('m03_party', 'TagsJson')
  )
ORDER BY table_name;

-- Table-level primary, foreign-key and check constraints.
SELECT table_name, constraint_name, constraint_type
FROM information_schema.table_constraints
WHERE table_schema = current_schema()
  AND table_name LIKE 'm03_%'
ORDER BY table_name, constraint_type, constraint_name;

-- M03 indexes, including partial unique price-list scope indexes.
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname = current_schema()
  AND tablename LIKE 'm03_%'
ORDER BY tablename, indexname;

-- Tenant-scoped reference seeds. Counts should be non-zero only for tenants
-- that existed when 04-seed.sql was run; do not expose account or customer data.
SELECT 'm03_unit' AS table_name, count(*) AS rows FROM m03_unit
UNION ALL
SELECT 'm03_unit_conversion', count(*) FROM m03_unit_conversion
UNION ALL
SELECT 'm03_payment_term', count(*) FROM m03_payment_term
UNION ALL
SELECT 'm03_charge_type', count(*) FROM m03_charge_type;

-- Verify the M03 permission catalog and standard-role template grants.
SELECT "PermissionCode", "Module", "Action"
FROM m02_permission_catalog
WHERE "PermissionCode" IN (
  'masters.view', 'masters.create', 'masters.edit', 'masters.deactivate',
  'masters.approve', 'masters.import', 'credit.override', 'fleet.override'
)
ORDER BY "PermissionCode";

SELECT pg_get_constraintdef(oid) AS permission_catalog_action_check
FROM pg_constraint
WHERE conrelid = 'm02_permission_catalog'::regclass
  AND conname = 'm02_permission_catalog_Action_check';

SELECT "RoleKey", "PermissionCode"
FROM m02_system_role_permission
WHERE "PermissionCode" IN (
  'masters.view', 'masters.create', 'masters.edit', 'masters.deactivate',
  'masters.approve', 'masters.import', 'credit.override', 'fleet.override'
)
ORDER BY "RoleKey", "PermissionCode";
