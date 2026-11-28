DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m13_gst_period WHERE "Status"='Filed' LIMIT 1) THEN
        RAISE EXCEPTION 'M13 rollback blocked: filed GST-period history must be retained';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m13_exception_review;
DROP TABLE IF EXISTS m13_gst_period;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm13.%';
DELETE FROM platform_module WHERE "ModuleCode"='M13'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M13')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M13');
COMMIT;
