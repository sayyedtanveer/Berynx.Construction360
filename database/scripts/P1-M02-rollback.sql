START TRANSACTION;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000300_UsersRolesApprovals') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM m02_user) OR EXISTS (SELECT 1 FROM m02_role) OR
         EXISTS (SELECT 1 FROM m02_approval_rule) OR EXISTS (SELECT 1 FROM m02_approval_request) OR
         EXISTS (SELECT 1 FROM m02_approval_decision) OR EXISTS (SELECT 1 FROM m02_delegation) OR
         EXISTS (SELECT 1 FROM m02_notification) OR EXISTS (SELECT 1 FROM m02_provider_session) OR
         EXISTS (SELECT 1 FROM m02_audit_event) THEN
        RAISE EXCEPTION 'M02 contains business/security history; restore an approved backup instead of dropping it';
      END IF;
    END $$;
    DROP TRIGGER m02_last_admin_role ON m02_user_role;
    DROP TRIGGER m02_last_admin_user ON m02_user;
    DROP FUNCTION m02_guard_last_admin();
    DROP TRIGGER m02_scope_valid ON m02_user_role;
    DROP FUNCTION m02_validate_scope();
    DROP TRIGGER m02_approval_scope_valid ON m02_approval_request;
    DROP FUNCTION m02_validate_approval_scope();
    DROP TRIGGER m02_approval_task_assignment_valid ON m02_approval_task_assignment;
    DROP FUNCTION m02_approval_task_assignment_guard();
    DROP TRIGGER m02_approval_request_snapshot_immutable ON m02_approval_request;
    DROP FUNCTION m02_approval_request_immutable();
    DROP TRIGGER m02_seed_tenant_roles ON onboarding_tenant;
    DROP FUNCTION m02_seed_new_tenant();
    DROP FUNCTION m02_seed_tenant(uuid);
    DROP TRIGGER m02_system_role_permission_immutable ON m02_role_permission;
    DROP FUNCTION m02_protect_system_role_permissions();
    DROP TRIGGER m02_system_role_immutable ON m02_role;
    DROP FUNCTION m02_protect_system_role();
    DROP TRIGGER m02_approval_decision_immutable ON m02_approval_decision;
    DROP TRIGGER m02_decision_no_truncate ON m02_approval_decision;
    DROP TRIGGER m02_audit_immutable ON m02_audit_event;
    DROP TRIGGER m02_audit_no_truncate ON m02_audit_event;
    DROP FUNCTION m02_append_only();
    DROP TABLE m02_provider_session, m02_notification, m02_audit_event, m02_delegation,
      m02_approval_task_assignment, m02_approval_decision, m02_approval_request,
      m02_approval_level, m02_approval_rule,
      m02_user_role, m02_role_permission, m02_role, m02_user, m02_system_role_permission,
      m02_system_role_template, m02_permission_catalog;
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000300_UsersRolesApprovals') THEN
    DELETE FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20261124000300_UsersRolesApprovals';
    END IF;
END $EF$;
COMMIT;

