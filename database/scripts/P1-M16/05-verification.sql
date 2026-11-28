SELECT to_regclass('public.m16_export_log') AS report_export_audit_table;
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M16';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm16.%';
SELECT "ReportCode",COUNT(*) AS exports,MAX("ExportedAt") AS latest_export
FROM m16_export_log GROUP BY "ReportCode" ORDER BY "ReportCode";
