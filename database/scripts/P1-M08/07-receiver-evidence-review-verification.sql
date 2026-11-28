-- Read-only checks for the owner after manually applying the M08 permission seed and migration.
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261127081534_P1M08ReceiverEvidenceReview';

SELECT table_name, column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = current_schema() AND table_name = 'm08_trip'
  AND column_name IN ('EvidenceDecisionReason', 'ReceiverVerifiedByUserId')
ORDER BY column_name;

SELECT conname AS constraint_name, pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE conrelid = to_regclass('m08_trip')
  AND conname = 'FK_m08_trip_m02_user_TenantId_ReceiverVerifiedByUserId';

SELECT r."RoleKey", p."PermissionCode"
FROM m02_system_role_template r
JOIN m02_system_role_permission p ON p."RoleKey" = r."RoleKey"
WHERE r."RoleKey" IN ('Dispatcher', 'Supervisor')
  AND p."PermissionCode" IN ('m08.dispatch.view', 'm08.trips.plan', 'm08.trips.update', 'm08.evidence.review')
ORDER BY r."RoleKey", p."PermissionCode";