BEGIN;
DELETE FROM m02_permission_catalog WHERE "PermissionCode"='m15.dashboard.view';
DELETE FROM platform_module WHERE "ModuleCode"='M15'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M15')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M15');
COMMIT;
