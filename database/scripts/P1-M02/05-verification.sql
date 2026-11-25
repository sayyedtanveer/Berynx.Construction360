SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20261124000300_UsersRolesApprovals',
  '20261124092204_P1M02IdentityAuthentication',
  '20261124094727_P1M02SessionSecurityStamp'
)
ORDER BY "MigrationId";

SELECT to_regclass('m02_user') AS m02_user,
       to_regclass('m02_role') AS m02_role,
       to_regclass('m02_approval_request') AS m02_approval_request,
       to_regclass('m02_audit_event') AS m02_audit_event,
       to_regclass('m02_identity_user') AS m02_identity_user,
       to_regclass('m02_auth_session') AS m02_auth_session,
       to_regclass('m02_refresh_token') AS m02_refresh_token,
       to_regclass('m02_auth_challenge') AS m02_auth_challenge;

SELECT
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'm02_last_admin_user' AND NOT tgisinternal)
    AS last_admin_guard_exists,
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'm02_audit_immutable' AND NOT tgisinternal)
    AS audit_append_only_exists,
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'm02_approval_request_snapshot_immutable' AND NOT tgisinternal)
    AS approval_snapshot_guard_exists;

SELECT count(*) AS permission_catalog_rows FROM m02_permission_catalog;
SELECT count(*) AS system_role_template_rows FROM m02_system_role_template;
