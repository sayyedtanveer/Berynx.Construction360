-- Review tenant selection and role provisioning before running the local tool.
SELECT "TenantId", "Name", "Status", "Plan",
       "Configuration"->'branches' AS branches
FROM onboarding_tenant
ORDER BY "Name", "TenantId";

SELECT template."RoleKey", template."Name",
       count(DISTINCT permission."PermissionCode") AS permission_count
FROM m02_system_role_template AS template
LEFT JOIN m02_system_role_permission AS permission
  ON permission."RoleKey" = template."RoleKey"
GROUP BY template."RoleKey", template."Name"
ORDER BY template."RoleKey";

SELECT tenant."TenantId", role."RoleKey", count(DISTINCT role_permission."PermissionCode") AS permission_count
FROM onboarding_tenant AS tenant
JOIN m02_role AS role ON role."TenantId" = tenant."TenantId"
LEFT JOIN m02_role_permission AS role_permission
  ON role_permission."TenantId" = role."TenantId"
 AND role_permission."RoleId" = role."RoleId"
WHERE role."RoleKey" IN (
  'TenantAdmin', 'BranchManager', 'SalesManager', 'ProcurementManager',
  'FleetManager', 'Accountant', 'ReadOnly'
)
GROUP BY tenant."TenantId", role."RoleKey"
ORDER BY tenant."TenantId", role."RoleKey";

SELECT 'Identity passwords are provisioned only by the local .NET tool using UserManager.' AS identity_password_note;
