SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M15';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode"='m15.dashboard.view';
-- Verify deployed source-module tables before enabling M15: M06, M08, M09, M10, M11, M14.
SELECT to_regclass('public.m06_sales_order') AS sales_orders,
       to_regclass('public.m08_trip') AS trips,
       to_regclass('public.m09_delivery_challan') AS challans,
       to_regclass('public.m10_invoice') AS invoices,
       to_regclass('public.m11_receipt') AS receipts,
       to_regclass('public.m14_expense') AS expenses;
