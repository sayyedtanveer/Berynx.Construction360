BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M14','Expenses','Expense capture, approvals and M12 accounting posting',false,true,140)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m14.expenses.view','m14','expenses.view','View permitted-branch expense records'),
 ('m14.expenses.create','m14','expenses.create','Create expense requests'),
 ('m14.expenses.post','m14','expenses.post','Post approved expenses to M12'),
 ('m14.categories.manage','m14','categories.manage','Configure tenant expense categories and ledger mapping')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
