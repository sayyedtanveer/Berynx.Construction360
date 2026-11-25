-- P1-M02 reference permissions and immutable system-role templates.
-- No user, password, token, tenant identity or production credential is seeded here.
BEGIN;
SET LOCAL berynx.m02_seed = 'on';

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
SELECT module || '.' || action, module, action, initcap(module) || ' ' || replace(action, '_', ' ')
FROM (VALUES
  ('onboarding'), ('opening'), ('usage'), ('users'), ('roles'),
  ('approvals'), ('profile'), ('sessions'), ('audit')
) AS modules(module)
CROSS JOIN (VALUES
  ('view'), ('create'), ('edit'), ('approve'), ('cancel'), ('export'), ('print')
) AS actions(action)
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO m02_permission_catalog ("PermissionCode","Module","Action","Description")
VALUES ('global.view_cost','global','view_cost','View cost and margin information')
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO m02_system_role_template ("RoleKey","Name","Description") VALUES
  ('TenantAdmin','Tenant Admin','Manage tenant users, roles, configuration and approvals'),
  ('Accountant','Accountant','Manage opening balances and accounting-related approvals'),
  ('Approver','Approver','Review approval requests within assigned data scope'),
  ('Auditor','Auditor','Read and export tenant audit history'),
  ('ProjectManager','Project Manager','View workspace setup and assigned approvals')
ON CONFLICT ("RoleKey") DO NOTHING;

INSERT INTO m02_system_role_permission ("RoleKey","PermissionCode")
SELECT 'TenantAdmin', "PermissionCode" FROM m02_permission_catalog
ON CONFLICT ("RoleKey","PermissionCode") DO NOTHING;

INSERT INTO m02_system_role_permission ("RoleKey","PermissionCode") VALUES
  ('Accountant','opening.view'), ('Accountant','opening.edit'),
  ('Accountant','usage.view'), ('Accountant','approvals.view'), ('Accountant','approvals.approve'),
  ('Accountant','audit.view'), ('Accountant','profile.view'), ('Accountant','profile.edit'),
  ('Accountant','sessions.view'), ('Accountant','sessions.edit'), ('Accountant','global.view_cost'),
  ('Approver','approvals.view'), ('Approver','approvals.approve'),
  ('Approver','profile.view'), ('Approver','profile.edit'),
  ('Approver','sessions.view'), ('Approver','sessions.edit'),
  ('Auditor','audit.view'), ('Auditor','audit.export'),
  ('Auditor','profile.view'), ('Auditor','sessions.view'),
  ('ProjectManager','onboarding.view'), ('ProjectManager','opening.view'),
  ('ProjectManager','users.view'), ('ProjectManager','approvals.view'),
  ('ProjectManager','profile.view'), ('ProjectManager','profile.edit'),
  ('ProjectManager','sessions.view'), ('ProjectManager','sessions.edit')
ON CONFLICT ("RoleKey","PermissionCode") DO NOTHING;

-- Provision defaults for tenants already created by P1-M01. A trigger provisions
-- the same catalog roles for tenants created after these reference rows exist.
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";

COMMIT;
