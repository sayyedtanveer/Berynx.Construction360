BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M15','Dashboard and Home','Permission-aware operational dashboard over source transactions',false,true,150)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES ('m15.dashboard.view','m15','dashboard.view','View source-backed dashboard cards within assigned data scope')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
