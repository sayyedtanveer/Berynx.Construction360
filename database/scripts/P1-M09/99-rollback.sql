-- Guarded manual rollback. Preserve operational records: refuse to drop non-empty tables.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM m09_delivery_challan LIMIT 1) THEN
        RAISE EXCEPTION 'M09 rollback blocked: delivery challans exist; archive/reconcile them first';
    END IF;
END $$;
BEGIN;
DROP TABLE IF EXISTS m09_challan_event;
DROP TABLE IF EXISTS m09_challan_revision;
DROP TABLE IF EXISTS m09_delivery_challan;
DROP TABLE IF EXISTS m09_challan_number_counter;
DELETE FROM m02_permission_catalog WHERE "PermissionCode"='m09.challans.view';
DELETE FROM platform_module WHERE "ModuleCode"='M09' AND NOT EXISTS
    (SELECT 1 FROM platform_plan_module WHERE "ModuleCode"='M09')
    AND NOT EXISTS (SELECT 1 FROM platform_tenant_entitlement WHERE "ModuleCode"='M09');
COMMIT;
