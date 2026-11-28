-- Run after deployment. EF migration history remains unstamped until the generated
-- migration is added and reviewed; do not fabricate a migration history row.
SELECT "ModuleCode","Name","IsCore","IsActive"
FROM platform_module WHERE "ModuleCode" = 'M06';

SELECT "PermissionCode","Module","Action"
FROM m02_permission_catalog WHERE "Module" = 'm06' ORDER BY "PermissionCode";

SELECT c.relname AS table_name
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public' AND c.relname LIKE 'm06_%' AND c.relkind = 'r'
ORDER BY c.relname;

-- This query must return zero before any PlatformAdmin grants M06:
SELECT count(*) AS enabled_m06_entitlements
FROM platform_tenant_entitlement WHERE "ModuleCode" = 'M06' AND "IsEnabled" = true;
