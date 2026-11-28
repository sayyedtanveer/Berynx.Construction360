-- Register M07 as an optional module and add only the permissions implemented by this increment.
-- This does not grant M07 to plans or tenants.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M07','Procurement','Purchase orders and purchase-order-matched goods receipts',false,true,70)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "IsCore" = false,
    "IsActive" = true,
    "SortOrder" = EXCLUDED."SortOrder";

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m07.purchase_orders.view','m07','purchase_orders.view','View purchase orders'),
 ('m07.purchase_orders.create','m07','purchase_orders.create','Create draft purchase orders'),
 ('m07.purchase_orders.submit','m07','purchase_orders.submit','Submit purchase orders for approval'),
 ('m07.purchase_orders.approve','m07','purchase_orders.approve','Release M02-approved purchase orders'),
 ('m07.purchase_orders.send','m07','purchase_orders.send','Record supplier dispatch or acknowledgement'),
 ('m07.purchase_orders.cancel','m07','purchase_orders.cancel','Cancel purchase orders without posted receipts'),
 ('m07.purchase_orders.close','m07','purchase_orders.close','Close purchase orders and release open balance'),
 ('m07.goods_receipts.view','m07','goods_receipts.view','View goods receipts'),
 ('m07.goods_receipts.create','m07','goods_receipts.create','Create draft goods receipts'),
 ('m07.goods_receipts.post','m07','goods_receipts.post','Post goods-receipt quantities'),
 ('m07.goods_receipts.configure','m07','goods_receipts.configure','Configure goods-receipt tolerances')
ON CONFLICT ("PermissionCode") DO NOTHING;

SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
