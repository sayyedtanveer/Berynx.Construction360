BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M13','GST','GST registers, readiness checks and manual filing log',false,true,130)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m13.gst.view','m13','gst.view','View GST workspace and posted source totals'),
 ('m13.gst.export','m13','gst.export','Export internal GST source extract'),
 ('m13.exceptions.review','m13','exceptions.review','Record a GST exception review note'),
 ('m13.periods.file','m13','periods.file','Record external GST filing and lock the period')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
