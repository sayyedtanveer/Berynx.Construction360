-- Register M17 as an optional module and expose only its implemented permissions.
-- This does not add M17 to subscription plans or grant tenant entitlements.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M17','Promotion & Catalogue','Tenant catalogue management, promotions and consent records',false,true,170)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "IsCore" = false,
    "IsActive" = true,
    "SortOrder" = EXCLUDED."SortOrder";

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
    ('m17.catalogue.manage','m17','catalogue.manage','Create and manage tenant catalogue items'),
    ('m17.consent.view','m17','consent.view','View customer marketing consent records'),
    ('m17.consent.manage','m17','consent.manage','Record and update customer marketing consent')
ON CONFLICT ("PermissionCode") DO NOTHING;

SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;