BEGIN;

DO $rollback$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
                 WHERE "MigrationId" = '20261124000300_UsersRolesApprovals')
     OR NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
                    WHERE "MigrationId" = '20261124092204_P1M02IdentityAuthentication')
     OR NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
                    WHERE "MigrationId" = '20261124094727_P1M02SessionSecurityStamp') THEN
    RAISE EXCEPTION 'All three P1-M02 migrations must be present before rollback';
  END IF;

  IF EXISTS (SELECT 1 FROM m02_user)
     OR EXISTS (SELECT 1 FROM m02_role)
     OR EXISTS (SELECT 1 FROM m02_approval_rule)
     OR EXISTS (SELECT 1 FROM m02_approval_request)
     OR EXISTS (SELECT 1 FROM m02_approval_decision)
     OR EXISTS (SELECT 1 FROM m02_delegation)
     OR EXISTS (SELECT 1 FROM m02_notification)
     OR EXISTS (SELECT 1 FROM m02_provider_session)
     OR EXISTS (SELECT 1 FROM m02_audit_event)
     OR EXISTS (SELECT 1 FROM m02_identity_user)
     OR EXISTS (SELECT 1 FROM m02_identity_user_claim)
     OR EXISTS (SELECT 1 FROM m02_identity_user_login)
     OR EXISTS (SELECT 1 FROM m02_identity_user_token)
     OR EXISTS (SELECT 1 FROM m02_auth_session)
     OR EXISTS (SELECT 1 FROM m02_refresh_token)
     OR EXISTS (SELECT 1 FROM m02_auth_challenge) THEN
    RAISE EXCEPTION 'M02 contains user, identity, approval, session, or audit data; restore an approved backup instead';
  END IF;
END
$rollback$;

DROP TRIGGER m02_last_admin_role ON m02_user_role;
DROP TRIGGER m02_last_admin_user ON m02_user;
DROP TRIGGER m02_scope_valid ON m02_user_role;
DROP TRIGGER m02_approval_scope_valid ON m02_approval_request;
DROP TRIGGER m02_approval_task_assignment_valid ON m02_approval_task_assignment;
DROP TRIGGER m02_approval_request_snapshot_immutable ON m02_approval_request;
DROP TRIGGER m02_seed_tenant_roles ON onboarding_tenant;
DROP TRIGGER m02_system_role_immutable ON m02_role;
DROP TRIGGER m02_system_role_permission_immutable ON m02_role_permission;
DROP TRIGGER m02_audit_immutable ON m02_audit_event;
DROP TRIGGER m02_audit_no_truncate ON m02_audit_event;
DROP TRIGGER m02_approval_decision_immutable ON m02_approval_decision;
DROP TRIGGER m02_decision_no_truncate ON m02_approval_decision;

DROP FUNCTION m02_guard_last_admin();
DROP FUNCTION m02_validate_scope();
DROP FUNCTION m02_validate_approval_scope();
DROP FUNCTION m02_approval_task_assignment_guard();
DROP FUNCTION m02_approval_request_immutable();
DROP FUNCTION m02_seed_new_tenant();
DROP FUNCTION m02_seed_tenant(uuid);
DROP FUNCTION m02_protect_system_role();
DROP FUNCTION m02_protect_system_role_permissions();
DROP FUNCTION m02_append_only();

DROP TABLE m02_auth_challenge;
DROP TABLE m02_refresh_token;
ALTER TABLE m02_auth_session DROP COLUMN "SecurityStamp";
DROP TABLE m02_auth_session;
ALTER TABLE m02_user DROP CONSTRAINT fk_m02_user_identity;
ALTER TABLE m02_user DROP COLUMN "IdentityUserId";
DROP TABLE m02_identity_user_token;
DROP TABLE m02_identity_user_login;
DROP TABLE m02_identity_user_claim;
DROP TABLE m02_identity_user;

DROP TABLE m02_approval_task_assignment;
DROP TABLE m02_approval_decision;
DROP TABLE m02_approval_request;
DROP TABLE m02_approval_level;
DROP TABLE m02_approval_rule;
DROP TABLE m02_delegation;
DROP TABLE m02_notification;
DROP TABLE m02_provider_session;
DROP TABLE m02_audit_event;
DROP TABLE m02_user_role;
DROP TABLE m02_role_permission;
DROP TABLE m02_role;
DROP TABLE m02_system_role_permission;
DROP TABLE m02_system_role_template;
DROP TABLE m02_permission_catalog;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20261124094727_P1M02SessionSecurityStamp',
  '20261124092204_P1M02IdentityAuthentication',
  '20261124000300_UsersRolesApprovals'
);

COMMIT;
