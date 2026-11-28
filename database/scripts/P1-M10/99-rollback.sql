DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m10_invoice WHERE "Status" <> 'Draft' LIMIT 1) THEN
        RAISE EXCEPTION 'M10 rollback blocked: non-draft invoice records exist';
    END IF;
    IF EXISTS (SELECT 1 FROM m10_invoice_challan LIMIT 1) THEN
        RAISE EXCEPTION 'M10 rollback blocked: invoice drafts reserve challans';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m10_invoice_event;
DROP TABLE IF EXISTS m10_invoice_challan;
DROP TABLE IF EXISTS m10_invoice_line;
DROP TABLE IF EXISTS m10_invoice;
DELETE FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm10.invoices.%';
DELETE FROM platform_module WHERE "ModuleCode"='M10'
    AND NOT EXISTS (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M10')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M10');
COMMIT;
