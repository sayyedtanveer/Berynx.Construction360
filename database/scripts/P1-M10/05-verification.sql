SELECT to_regclass('public.m10_invoice') AS invoice_table,
       to_regclass('public.m10_invoice_line') AS invoice_line_table,
       to_regclass('public.m10_invoice_challan') AS invoice_challan_table,
       to_regclass('public.m10_invoice_event') AS invoice_event_table;
SELECT COUNT(*) AS duplicate_challan_reservations
FROM (SELECT "TenantId","ChallanId" FROM m10_invoice_challan
      GROUP BY "TenantId","ChallanId" HAVING COUNT(*) > 1) d;
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M10';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm10.invoices.%';
