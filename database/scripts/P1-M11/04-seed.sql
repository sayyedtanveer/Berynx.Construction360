-- Optional M11 module registration; no plan/tenant entitlement is granted.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M11','Receipts & Collections','Receipt drafts, customer allocation and collection tracking',false,true,110)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m11.receipts.view','m11','receipts.view','View receipt drafts and posted receipts'),
 ('m11.receipts.create','m11','receipts.create','Create receipt drafts and allocations'),
 ('m11.receipts.post','m11','receipts.post','Post receipts through the shared accounting engine'),
 ('m11.collections.manage','m11','collections.manage','Manage promise-to-pay records')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
