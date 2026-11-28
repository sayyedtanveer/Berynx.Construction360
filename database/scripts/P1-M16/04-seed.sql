BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M16','Reports','Scoped transaction-backed reports and audited CSV exports',false,true,160)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m16.reports.view','m16','reports.view','View permitted source-backed reports'),
 ('m16.reports.export','m16','reports.export','Export permitted report rows to CSV')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
