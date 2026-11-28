-- Register M08 as an optional module and add only permissions used by this increment.
-- This deliberately does not grant M08 to plans or tenants.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO platform_module ("ModuleCode","Name","Description","IsCore","IsActive","SortOrder")
VALUES ('M08','RMC & Material Delivery','Dispatch board, pours, trips and delivery evidence',false,true,80)
ON CONFLICT ("ModuleCode") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "IsCore" = false,
    "IsActive" = true,
    "SortOrder" = EXCLUDED."SortOrder";

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES
 ('m08.dispatch.view','m08','dispatch.view','View dispatch board and trip progress'),
 ('m08.pours.schedule','m08','pours.schedule','Schedule approved M06 deliveries as pours'),
 ('m08.trips.plan','m08','trips.plan','Plan trips and assign vehicles and drivers'),
 ('m08.trips.update','m08','trips.update','Record trip lifecycle and receiver evidence'),
 ('m08.evidence.review','m08','evidence.review','Review submitted delivery evidence and record decisions')
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO m02_system_role_template ("RoleKey","Name","Description")
VALUES
 ('Dispatcher','Dispatcher','Plan and record deliveries within assigned scope'),
 ('Supervisor','Delivery Supervisor','Review delivery evidence within assigned scope')
ON CONFLICT ("RoleKey") DO NOTHING;

INSERT INTO m02_system_role_permission ("RoleKey","PermissionCode")
VALUES
 ('TenantAdmin','m08.evidence.review'),
 ('Dispatcher','m08.dispatch.view'),
 ('Dispatcher','m08.trips.plan'),
 ('Dispatcher','m08.trips.update'),
 ('Dispatcher','m08.evidence.review'),
 ('Supervisor','m08.dispatch.view'),
 ('Supervisor','m08.trips.update'),
 ('Supervisor','m08.evidence.review')
ON CONFLICT ("RoleKey","PermissionCode") DO NOTHING;

SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";
COMMIT;
