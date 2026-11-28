SELECT to_regclass('public.m12_voucher') AS voucher_table,
       to_regclass('public.m12_voucher_line') AS voucher_line_table,
       to_regclass('public.m12_financial_period') AS period_table,
       to_regclass('public.m12_voucher_number_counter') AS counter_table;
SELECT COUNT(*) AS unbalanced_posted_vouchers
FROM m12_voucher v
WHERE v."Status"='Posted'
  AND (v."TotalDebit" <> v."TotalCredit" OR v."TotalDebit" <>
       (SELECT COALESCE(SUM(l."Debit"),0) FROM m12_voucher_line l
        WHERE l."TenantId"=v."TenantId" AND l."VoucherId"=v."VoucherId"));
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M12';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm12.%';
