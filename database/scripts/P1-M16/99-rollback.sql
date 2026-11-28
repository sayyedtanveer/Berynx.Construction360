DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m16_export_log LIMIT 1) THEN
        RAISE EXCEPTION 'M16 rollback blocked: export audit history must be retained';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m16_export_log;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm16.%';
DELETE FROM platform_module WHERE "ModuleCode"='M16'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M16')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M16');
COMMIT;
