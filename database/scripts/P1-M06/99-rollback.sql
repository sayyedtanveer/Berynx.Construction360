-- Guarded rollback. This refuses to remove M06 if the tenant has used the module.
BEGIN;
DO $rollback$
BEGIN
    IF EXISTS (SELECT 1 FROM m06_sales_order) THEN
        RAISE EXCEPTION 'Rollback blocked: sales orders exist; preserve business records and use a reviewed data-retirement plan';
    END IF;
    IF EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode" = 'M06') THEN
        RAISE EXCEPTION 'Rollback blocked: tenant entitlement history exists; disable the entitlement through PlatformAdmin first';
    END IF;
    DROP TABLE IF EXISTS m06_linked_document;
    DROP TABLE IF EXISTS m06_activity;
    DROP TABLE IF EXISTS m06_amendment;
    DROP TABLE IF EXISTS m06_delivery_schedule;
    DROP TABLE IF EXISTS m06_quotation_charge_allocation;
    DROP TABLE IF EXISTS m06_quotation_allocation;
    DROP TABLE IF EXISTS m06_sales_order_charge;
    DROP TABLE IF EXISTS m06_sales_order_line;
    DROP TABLE IF EXISTS m06_sales_order;
    DELETE FROM m02_role_permission WHERE "PermissionCode" LIKE 'm06.%';
    DELETE FROM m02_system_role_permission WHERE "PermissionCode" LIKE 'm06.%';
    DELETE FROM m02_permission_catalog WHERE "Module" = 'm06';
    DELETE FROM platform_module WHERE "ModuleCode" = 'M06';
END $rollback$;
COMMIT;
