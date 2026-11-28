-- Optional M10 module catalog and draft-workflow permissions; no plan/tenant grants are made.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M10','Sales Invoicing','Delivery-based invoice drafts and posting integration',false,true,100)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m10.invoices.view','m10','invoices.view','View invoices and invoice drafts'),
 ('m10.invoices.create','m10','invoices.create','Create and cancel unposted invoice drafts'),
 ('m10.invoices.post','m10','invoices.post','Post invoices through the shared accounting engine')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
