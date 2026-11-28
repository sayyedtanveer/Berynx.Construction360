SELECT to_regclass('public.m14_expense') AS expense_table,
       to_regclass('public.m14_expense_category') AS category_table,
       to_regclass('public.m14_expense_event') AS event_table;
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M14';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm14.%';
SELECT COUNT(*) AS posted_without_m12_voucher FROM m14_expense
WHERE "Status"='Posted' AND "VoucherId" IS NULL;
