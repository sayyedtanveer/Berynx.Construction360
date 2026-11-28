SELECT to_regclass('public.m09_delivery_challan') AS challan_table,
       to_regclass('public.m09_challan_number_counter') AS number_counter,
       to_regclass('public.m09_challan_revision') AS revision_table,
       to_regclass('public.m09_challan_event') AS event_table;
SELECT COUNT(*) AS duplicate_trip_challans
FROM (
    SELECT "TenantId","TripId" FROM m09_delivery_challan
    GROUP BY "TenantId","TripId" HAVING COUNT(*) > 1
) d;
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M09';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode"='m09.challans.view';
