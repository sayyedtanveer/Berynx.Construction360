-- Optional M12 module registration. No plan/tenant is activated automatically.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';
INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M12','Accounting','Double-entry books, vouchers, ledgers and period control',false,true,120)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
    "IsCore"=false,"IsActive"=true,"SortOrder"=EXCLUDED."SortOrder";
INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m12.accounts.view','m12','accounts.view','View tenant chart of accounts'),
 ('m12.accounts.manage','m12','accounts.manage','Manage tenant chart of accounts'),
('m12.mappings.manage','m12','mappings.manage','Configure effective-dated accounting ledger mappings'),
 ('m12.vouchers.view','m12','vouchers.view','View accounting vouchers'),
 ('m12.vouchers.post','m12','vouchers.post','Post authorized manual vouchers'),
 ('m12.reports.view','m12','reports.view','View trial balance and accounting reports'),
 ('m12.periods.manage','m12','periods.manage','Manage accounting-period locks')
ON CONFLICT ("PermissionCode") DO NOTHING;
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
