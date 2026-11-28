DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m12_voucher WHERE "Status"='Posted' LIMIT 1) THEN
        RAISE EXCEPTION 'M12 rollback blocked: posted accounting vouchers are immutable; reverse/reconcile first';
    END IF;
    IF EXISTS (SELECT 1 FROM m12_financial_period WHERE "IsLocked" LIMIT 1) THEN
        RAISE EXCEPTION 'M12 rollback blocked: accounting periods are locked';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m12_period_event;
DROP TABLE IF EXISTS m12_voucher_event;
DROP TABLE IF EXISTS m12_voucher_line;
DROP TABLE IF EXISTS m12_voucher;
DROP TABLE IF EXISTS m12_financial_period;
DROP TABLE IF EXISTS m12_voucher_number_counter;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm12.%';
DELETE FROM platform_module WHERE "ModuleCode"='M12'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M12')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M12');
COMMIT;
