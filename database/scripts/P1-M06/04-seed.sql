-- Register M06 as an optional module and add its RBAC permission catalogue.
-- This deliberately does NOT add M06 to plans or existing tenant entitlements.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M06','Sales Orders','Sales order lifecycle, scheduling and fulfillment tracking',false,true,60)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name" = EXCLUDED."Name", "Description" = EXCLUDED."Description",
    "IsCore" = false, "IsActive" = true, "SortOrder" = EXCLUDED."SortOrder";

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m06.orders.view','m06','orders.view','View sales orders'),
 ('m06.orders.create','m06','orders.create','Create sales orders'),
 ('m06.orders.edit','m06','orders.edit','Edit draft sales orders'),
 ('m06.orders.confirm','m06','orders.confirm','Confirm sales orders'),
 ('m06.orders.approve','m06','orders.approve','Approve order overrides and amendments'),
 ('m06.orders.cancel','m06','orders.cancel','Cancel or short-close sales orders'),
 ('m06.orders.schedule','m06','orders.schedule','Schedule sales order deliveries'),
 ('m06.orders.amend','m06','orders.amend','Propose and apply order amendments'),
 ('m06.orders.hold','m06','orders.hold','Place sales orders on hold'),
 ('m06.orders.short_close','m06','orders.short_close','Short-close orders with delivered quantities'),
 ('m06.orders.short_close.reverse','m06','orders.short_close.reverse','Request approval to reverse a short-close'),
 ('m06.orders.price_override','m06','orders.price_override','Override M03 price-list rates'),
 ('m06.orders.report','m06','orders.report','View pending-order reports'),
 ('m06.orders.configure','m06','orders.configure','Configure tenant delivery tolerances'),
 ('m06.orders.export','m06','orders.export','Export sales order reports'),
 ('m06.orders.print','m06','orders.print','Print sales order confirmations')
ON CONFLICT ("PermissionCode") DO NOTHING;

SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
