SELECT to_regclass('public.m13_gst_period') AS gst_period_table,
       to_regclass('public.m13_exception_review') AS exception_review_table;
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M13';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm13.%';
