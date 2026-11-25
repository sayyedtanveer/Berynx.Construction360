-- Local-development role templates. Passwords and Identity users are never seeded here.
BEGIN;

INSERT INTO m02_system_role_template ("RoleKey", "Name", "Description") VALUES
  ('BranchManager', 'Branch Manager', 'Manage master data within assigned branches'),
  ('SalesManager', 'Sales Manager', 'Manage customer and sales-facing master data'),
  ('ProcurementManager', 'Procurement Manager', 'Manage supplier and procurement-facing master data'),
  ('FleetManager', 'Fleet Manager', 'Manage fleet and equipment master data'),
  ('ReadOnly', 'Read-only', 'View permitted tenant information without write access')
ON CONFLICT ("RoleKey") DO NOTHING;

-- TenantAdmin inherits all current catalog permissions. Other fixtures receive
-- only permissions already defined by the M02/M03 application catalogs.
INSERT INTO m02_system_role_permission ("RoleKey", "PermissionCode")
SELECT 'TenantAdmin', "PermissionCode" FROM m02_permission_catalog
ON CONFLICT ("RoleKey", "PermissionCode") DO NOTHING;

INSERT INTO m02_system_role_permission ("RoleKey", "PermissionCode")
SELECT grants."RoleKey", grants."PermissionCode"
FROM (VALUES
  ('BranchManager', 'masters.view'), ('BranchManager', 'masters.create'),
  ('BranchManager', 'masters.edit'), ('BranchManager', 'onboarding.view'),
  ('BranchManager', 'users.view'), ('BranchManager', 'approvals.view'),
  ('SalesManager', 'masters.view'), ('SalesManager', 'masters.create'),
  ('SalesManager', 'masters.edit'), ('SalesManager', 'approvals.view'),
  ('ProcurementManager', 'masters.view'), ('ProcurementManager', 'masters.create'),
  ('ProcurementManager', 'masters.edit'), ('ProcurementManager', 'approvals.view'),
  ('FleetManager', 'masters.view'), ('FleetManager', 'masters.edit'),
  ('FleetManager', 'fleet.override'), ('FleetManager', 'approvals.view'),
  ('Accountant', 'opening.view'), ('Accountant', 'opening.edit'),
  ('Accountant', 'usage.view'), ('Accountant', 'approvals.view'),
  ('Accountant', 'approvals.approve'), ('Accountant', 'audit.view'),
  ('Accountant', 'global.view_cost'),
  ('ReadOnly', 'masters.view'), ('ReadOnly', 'onboarding.view'),
  ('ReadOnly', 'opening.view'), ('ReadOnly', 'usage.view'),
  ('ReadOnly', 'users.view'), ('ReadOnly', 'roles.view'),
  ('ReadOnly', 'approvals.view'), ('ReadOnly', 'profile.view'),
  ('ReadOnly', 'sessions.view'), ('ReadOnly', 'audit.view')
) AS grants("RoleKey", "PermissionCode")
JOIN m02_permission_catalog AS catalog
  ON catalog."PermissionCode" = grants."PermissionCode"
ON CONFLICT ("RoleKey", "PermissionCode") DO NOTHING;

-- Refresh catalog role grants for all existing tenants. The function preserves
-- user data and creates only missing immutable system-role rows/grants.
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";

COMMIT;
