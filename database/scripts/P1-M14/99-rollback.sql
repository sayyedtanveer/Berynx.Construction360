DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m14_expense WHERE "Status"='Posted' LIMIT 1) THEN
        RAISE EXCEPTION 'M14 rollback blocked: posted expenses must remain in accounting history';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m14_expense_event;
DROP TABLE IF EXISTS m14_expense;
DROP TABLE IF EXISTS m14_expense_category;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm14.%';
DELETE FROM platform_module WHERE "ModuleCode"='M14'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M14')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M14');
COMMIT;
