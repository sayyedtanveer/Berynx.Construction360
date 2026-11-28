DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m11_receipt WHERE "Status" <> 'Draft' LIMIT 1) THEN
        RAISE EXCEPTION 'M11 rollback blocked: posted receipt or reversal exists';
    END IF;
    IF EXISTS (SELECT 1 FROM m11_receipt_allocation LIMIT 1) THEN
        RAISE EXCEPTION 'M11 rollback blocked: receipt allocation history exists';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m11_receivable_event;
DROP TABLE IF EXISTS m11_promise_to_pay;
DROP TABLE IF EXISTS m11_receipt_allocation;
DROP TABLE IF EXISTS m11_receipt;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm11.%';
DELETE FROM platform_module WHERE "ModuleCode"='M11'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M11')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M11');
COMMIT;
