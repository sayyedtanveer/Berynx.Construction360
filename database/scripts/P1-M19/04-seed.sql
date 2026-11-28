-- Register the optional M19 capability and server-side RBAC permissions.
-- No subscription-plan or tenant entitlement is granted by this seed.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M19','Quality & disputes','Quality samples, acceptance criteria and linked dispute handling',false,true,190)
ON CONFLICT ("ModuleCode") DO UPDATE SET
  "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
  "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description") VALUES
 ('m19.samples.view','m19','samples.view','View quality samples, test schedules and reports'),
 ('m19.samples.create','m19','samples.create','Register quality samples against completed deliveries'),
 ('m19.samples.result','m19','samples.result','Record and correct authorized laboratory results'),
 ('m19.criteria.view','m19','criteria.view','View tenant acceptance criteria'),
 ('m19.criteria.manage','m19','criteria.manage','Create effective-dated, reasoned acceptance criteria'),
 ('m19.disputes.view','m19','disputes.view','View customer and supplier disputes'),
 ('m19.disputes.create','m19','disputes.create','Open disputes linked to tenant source records'),
 ('m19.disputes.update','m19','disputes.update','Investigate and update dispute status'),
 ('m19.disputes.resolve','m19','disputes.resolve','Resolve disputes and release eligible invoice holds'),
 ('m19.reports.view','m19','reports.view','View authorized quality and dispute summaries')
ON CONFLICT ("PermissionCode") DO NOTHING;

SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;