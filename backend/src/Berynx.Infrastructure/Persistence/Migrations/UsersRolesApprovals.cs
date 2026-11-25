using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Berynx.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BerynxDbContext))]
[Migration("20261124000300_UsersRolesApprovals")]
public sealed class UsersRolesApprovals : Migration
{
    protected override void Up(MigrationBuilder m) => m.Sql("""
        CREATE TABLE m02_permission_catalog (
          "PermissionCode" text PRIMARY KEY, "Module" text NOT NULL, "Action" text NOT NULL,
          "Description" text NOT NULL, CHECK ("Action" IN ('view','create','edit','approve','cancel','export','print','view_cost'))
        );
        CREATE TABLE m02_system_role_template (
          "RoleKey" text PRIMARY KEY, "Name" text NOT NULL, "Description" text NOT NULL
        );
        CREATE TABLE m02_system_role_permission (
          "RoleKey" text NOT NULL REFERENCES m02_system_role_template("RoleKey") ON DELETE CASCADE,
          "PermissionCode" text NOT NULL REFERENCES m02_permission_catalog("PermissionCode") ON DELETE RESTRICT,
          PRIMARY KEY ("RoleKey","PermissionCode")
        );
        CREATE TABLE m02_user (
          "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
          "UserId" uuid NOT NULL, "ProviderSubject" text, "Email" text, "Mobile" text,
          "DisplayName" text NOT NULL, "Status" text NOT NULL CHECK ("Status" IN ('Invited','Active','Inactive','Revoked')),
          "InvitationDeliveryId" uuid, "InviteExpiresAt" timestamptz, "InvitationDeliveredAt" timestamptz,
          "InvitationAttempts" integer NOT NULL DEFAULT 0, "InvitationAvailableAt" timestamptz NOT NULL DEFAULT now(),
          "AcceptedAt" timestamptz, "DeactivatedAt" timestamptz, "Version" bigint NOT NULL DEFAULT 1,
          PRIMARY KEY ("TenantId","UserId"), UNIQUE ("TenantId","ProviderSubject"),
          UNIQUE ("TenantId","Email"), UNIQUE ("TenantId","Mobile"),
          CHECK ("Email" IS NOT NULL OR "Mobile" IS NOT NULL),
          CHECK ("Email" IS NULL OR ("Email"=lower(btrim("Email")) AND length("Email")<=254)),
          CHECK ("Mobile" IS NULL OR "Mobile" ~ '^\+[1-9][0-9]{7,14}$')
        );
        CREATE TABLE m02_role (
          "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
          "RoleId" uuid NOT NULL, "RoleKey" text NOT NULL, "Name" text NOT NULL, "Description" text NOT NULL DEFAULT '',
          "IsSystem" boolean NOT NULL DEFAULT false, "ClonedFromRoleId" uuid, "Version" bigint NOT NULL DEFAULT 1,
          PRIMARY KEY ("TenantId","RoleId"), UNIQUE ("TenantId","RoleKey"),
          FOREIGN KEY ("TenantId","ClonedFromRoleId") REFERENCES m02_role("TenantId","RoleId") ON DELETE RESTRICT
        );
        CREATE TABLE m02_role_permission (
          "TenantId" uuid NOT NULL, "RoleId" uuid NOT NULL, "PermissionCode" text NOT NULL,
          PRIMARY KEY ("TenantId","RoleId","PermissionCode"),
          FOREIGN KEY ("TenantId","RoleId") REFERENCES m02_role("TenantId","RoleId") ON DELETE CASCADE,
          FOREIGN KEY ("PermissionCode") REFERENCES m02_permission_catalog("PermissionCode") ON DELETE RESTRICT
        );
        CREATE TABLE m02_user_role (
          "TenantId" uuid NOT NULL, "AssignmentId" uuid NOT NULL, "UserId" uuid NOT NULL, "RoleId" uuid NOT NULL,
          "ScopeType" text NOT NULL CHECK ("ScopeType" IN ('All','Branch','Plant','Own')),
          "ScopeId" uuid, "AssignedAt" timestamptz NOT NULL, "AssignedBy" text NOT NULL,
          PRIMARY KEY ("TenantId","AssignmentId"),
          CHECK ((("ScopeType" IN ('Branch','Plant')) AND "ScopeId" IS NOT NULL) OR
                 (("ScopeType" IN ('All','Own')) AND "ScopeId" IS NULL)),
          FOREIGN KEY ("TenantId","UserId") REFERENCES m02_user("TenantId","UserId") ON DELETE CASCADE,
          FOREIGN KEY ("TenantId","RoleId") REFERENCES m02_role("TenantId","RoleId") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX ux_m02_user_role_scope ON m02_user_role
          ("TenantId","UserId","RoleId","ScopeType",COALESCE("ScopeId",'00000000-0000-0000-0000-000000000000'::uuid));
        CREATE TABLE m02_approval_rule (
          "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
          "RuleId" uuid NOT NULL, "Name" text NOT NULL, "EntityType" text NOT NULL,
          "AmountGreaterThan" numeric(18,2), "DiscountPercentGreaterThan" numeric(9,4),
          "MarginPercentBelow" numeric(9,4), "CreditStatus" text, "Enabled" boolean NOT NULL DEFAULT true,
          "Version" bigint NOT NULL DEFAULT 1, PRIMARY KEY ("TenantId","RuleId"),
          CHECK ("AmountGreaterThan" IS NOT NULL OR "DiscountPercentGreaterThan" IS NOT NULL OR
                 "MarginPercentBelow" IS NOT NULL OR "CreditStatus" IS NOT NULL),
          CHECK ("AmountGreaterThan" IS NULL OR "AmountGreaterThan">=0),
          CHECK ("DiscountPercentGreaterThan" IS NULL OR "DiscountPercentGreaterThan" BETWEEN 0 AND 100),
          CHECK ("MarginPercentBelow" IS NULL OR "MarginPercentBelow" BETWEEN 0 AND 100)
        );
        CREATE TABLE m02_approval_level (
          "TenantId" uuid NOT NULL, "RuleId" uuid NOT NULL, "Level" integer NOT NULL CHECK ("Level" > 0),
          "ApproverRoleId" uuid, "ApproverUserId" uuid, "SlaMinutes" integer NOT NULL CHECK ("SlaMinutes" > 0),
          "EscalationRoleId" uuid, "EscalationUserId" uuid,
          PRIMARY KEY ("TenantId","RuleId","Level"),
          CHECK (("ApproverRoleId" IS NOT NULL)::integer + ("ApproverUserId" IS NOT NULL)::integer = 1),
          CHECK (("EscalationRoleId" IS NOT NULL)::integer + ("EscalationUserId" IS NOT NULL)::integer <= 1),
          FOREIGN KEY ("TenantId","RuleId") REFERENCES m02_approval_rule("TenantId","RuleId") ON DELETE CASCADE,
          FOREIGN KEY ("TenantId","ApproverRoleId") REFERENCES m02_role("TenantId","RoleId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","ApproverUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","EscalationRoleId") REFERENCES m02_role("TenantId","RoleId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","EscalationUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE TABLE m02_approval_request (
          "TenantId" uuid NOT NULL, "RequestId" uuid NOT NULL, "RuleId" uuid NOT NULL,
          "SourceType" text NOT NULL, "SourceId" uuid NOT NULL, "RequesterUserId" uuid NOT NULL,
          "BranchId" uuid, "PlantId" uuid, "OwnerUserId" uuid,
          "Status" text NOT NULL CHECK ("Status" IN ('Pending','Approved','Rejected','Cancelled')),
          "CurrentLevel" integer NOT NULL CHECK ("CurrentLevel" > 0), "Amount" numeric(18,2) NOT NULL,
          "DiscountPercent" numeric(9,4), "MarginPercent" numeric(9,4), "CreditStatus" text,
          "Snapshot" jsonb NOT NULL, "DueAt" timestamptz NOT NULL, "EscalatedAt" timestamptz,
          "Version" bigint NOT NULL DEFAULT 1, "CreatedAt" timestamptz NOT NULL,
          PRIMARY KEY ("TenantId","RequestId"),
          FOREIGN KEY ("TenantId","RuleId") REFERENCES m02_approval_rule("TenantId","RuleId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","RequesterUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","OwnerUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE INDEX ix_m02_approval_inbox ON m02_approval_request ("TenantId","Status","DueAt");
        CREATE INDEX ix_m02_approval_source ON m02_approval_request ("TenantId","SourceType","SourceId");
        CREATE UNIQUE INDEX ux_m02_approval_pending_source ON m02_approval_request
          ("TenantId","SourceType","SourceId") WHERE "Status"='Pending';
        CREATE FUNCTION m02_approval_request_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN
            IF ROW(NEW."RuleId",NEW."SourceType",NEW."SourceId",NEW."RequesterUserId",NEW."BranchId",
                NEW."PlantId",NEW."OwnerUserId",NEW."Amount",NEW."DiscountPercent",NEW."MarginPercent",
                NEW."CreditStatus",NEW."Snapshot") IS DISTINCT FROM
               ROW(OLD."RuleId",OLD."SourceType",OLD."SourceId",OLD."RequesterUserId",OLD."BranchId",
                OLD."PlantId",OLD."OwnerUserId",OLD."Amount",OLD."DiscountPercent",OLD."MarginPercent",
                OLD."CreditStatus",OLD."Snapshot") THEN
              RAISE EXCEPTION 'Approval source snapshots and conditions are immutable; submit a new request';
            END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_approval_request_snapshot_immutable BEFORE UPDATE ON m02_approval_request
          FOR EACH ROW EXECUTE FUNCTION m02_approval_request_immutable();
        CREATE FUNCTION m02_validate_approval_scope() RETURNS trigger LANGUAGE plpgsql AS $$
          DECLARE cfg jsonb; found_scope boolean;
          BEGIN
            IF NEW."BranchId" IS NULL AND NEW."PlantId" IS NULL THEN RETURN NEW; END IF;
            SELECT "Configuration" INTO cfg FROM onboarding_tenant WHERE "TenantId"=NEW."TenantId";
            IF NEW."BranchId" IS NOT NULL THEN
              SELECT EXISTS(SELECT 1 FROM jsonb_array_elements(COALESCE(cfg->'branches','[]'::jsonb)) b
                WHERE b->>'id'=NEW."BranchId"::text) INTO found_scope;
              IF NOT found_scope THEN RAISE EXCEPTION 'Approval branch is not configured for this tenant'; END IF;
            END IF;
            IF NEW."PlantId" IS NOT NULL THEN
              SELECT EXISTS(SELECT 1 FROM jsonb_array_elements(COALESCE(cfg->'plants','[]'::jsonb)) p
                WHERE p->>'id'=NEW."PlantId"::text AND
                  (NEW."BranchId" IS NULL OR p->>'branchId'=NEW."BranchId"::text)) INTO found_scope;
              IF NOT found_scope THEN RAISE EXCEPTION 'Approval plant is not configured for this tenant branch'; END IF;
            END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_approval_scope_valid BEFORE INSERT OR UPDATE OF "BranchId","PlantId" ON m02_approval_request
          FOR EACH ROW EXECUTE FUNCTION m02_validate_approval_scope();
        CREATE TABLE m02_approval_decision (
          "TenantId" uuid NOT NULL, "DecisionId" uuid NOT NULL, "RequestId" uuid NOT NULL,
          "ActorUserId" uuid NOT NULL, "Level" integer NOT NULL, "Decision" text NOT NULL
            CHECK ("Decision" IN ('Approve','Reject','Cancel','Escalate')),
          "Comment" text, "At" timestamptz NOT NULL, PRIMARY KEY ("TenantId","DecisionId"),
          UNIQUE ("TenantId","RequestId","Level"),
          FOREIGN KEY ("TenantId","RequestId") REFERENCES m02_approval_request("TenantId","RequestId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","ActorUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE TABLE m02_approval_task_assignment (
          "TenantId" uuid NOT NULL, "RequestId" uuid NOT NULL, "Level" integer NOT NULL CHECK ("Level" > 0),
          "ApproverUserId" uuid NOT NULL, "AssignedByUserId" uuid NOT NULL, "AssignedAt" timestamptz NOT NULL,
          PRIMARY KEY ("TenantId","RequestId","Level"),
          FOREIGN KEY ("TenantId","RequestId") REFERENCES m02_approval_request("TenantId","RequestId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","ApproverUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","AssignedByUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE INDEX ix_m02_approval_task_owner ON m02_approval_task_assignment
          ("TenantId","ApproverUserId","RequestId","Level");
        CREATE FUNCTION m02_approval_task_assignment_guard() RETURNS trigger LANGUAGE plpgsql AS $$
          DECLARE requester uuid;
          BEGIN
            SELECT "RequesterUserId" INTO requester FROM m02_approval_request
              WHERE "TenantId"=NEW."TenantId" AND "RequestId"=NEW."RequestId";
            IF requester=NEW."ApproverUserId" THEN
              RAISE EXCEPTION 'An approval requester cannot be assigned to approve their own request';
            END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_approval_task_assignment_valid BEFORE INSERT OR UPDATE ON m02_approval_task_assignment
          FOR EACH ROW EXECUTE FUNCTION m02_approval_task_assignment_guard();
        CREATE TABLE m02_delegation (
          "TenantId" uuid NOT NULL, "DelegationId" uuid NOT NULL, "DelegatorUserId" uuid NOT NULL,
          "DelegateUserId" uuid NOT NULL, "StartsAt" timestamptz NOT NULL, "EndsAt" timestamptz NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "RevokedAt" timestamptz,
          PRIMARY KEY ("TenantId","DelegationId"), CHECK ("DelegatorUserId" <> "DelegateUserId"),
          CHECK ("StartsAt" < "EndsAt"),
          FOREIGN KEY ("TenantId","DelegatorUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT,
          FOREIGN KEY ("TenantId","DelegateUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE INDEX ix_m02_delegation_active ON m02_delegation ("TenantId","DelegateUserId","StartsAt","EndsAt")
          WHERE "RevokedAt" IS NULL;
        CREATE TABLE m02_audit_event (
          "TenantId" uuid NOT NULL REFERENCES onboarding_tenant("TenantId") ON DELETE RESTRICT,
          "EventId" uuid NOT NULL, "ActorUserId" uuid, "ActorSubject" text NOT NULL, "Action" text NOT NULL,
          "EntityType" text NOT NULL, "EntityId" uuid, "Before" jsonb NOT NULL, "After" jsonb NOT NULL,
          "Reason" text, "IpAddress" text, "Device" text, "At" timestamptz NOT NULL,
          PRIMARY KEY ("TenantId","EventId"),
          FOREIGN KEY ("TenantId","ActorUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE RESTRICT
        );
        CREATE INDEX ix_m02_audit_search ON m02_audit_event ("TenantId","At" DESC,"Action","EntityType");
        CREATE TABLE m02_notification (
          "TenantId" uuid NOT NULL, "NotificationId" uuid NOT NULL, "RecipientUserId" uuid NOT NULL,
          "Kind" text NOT NULL, "EntityId" uuid, "Payload" jsonb NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "ReadAt" timestamptz,
          PRIMARY KEY ("TenantId","NotificationId"),
          FOREIGN KEY ("TenantId","RecipientUserId") REFERENCES m02_user("TenantId","UserId") ON DELETE CASCADE
        );
        CREATE INDEX ix_m02_notification_unread ON m02_notification ("TenantId","RecipientUserId","CreatedAt" DESC)
          WHERE "ReadAt" IS NULL;
        CREATE TABLE m02_provider_session (
          "TenantId" uuid NOT NULL, "ProviderSessionId" text NOT NULL, "UserId" uuid NOT NULL,
          "IpAddress" text, "Device" text, "FirstSeenAt" timestamptz NOT NULL, "LastSeenAt" timestamptz NOT NULL,
          "ExpiresAt" timestamptz, "RevokedAt" timestamptz,
          PRIMARY KEY ("TenantId","ProviderSessionId"),
          FOREIGN KEY ("TenantId","UserId") REFERENCES m02_user("TenantId","UserId") ON DELETE CASCADE
        );
        CREATE FUNCTION m02_append_only() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN RAISE EXCEPTION 'M02 audit and approval decisions are append-only'; END; $$;
        CREATE TRIGGER m02_audit_immutable BEFORE UPDATE OR DELETE ON m02_audit_event
          FOR EACH ROW EXECUTE FUNCTION m02_append_only();
        CREATE TRIGGER m02_audit_no_truncate BEFORE TRUNCATE ON m02_audit_event
          FOR EACH STATEMENT EXECUTE FUNCTION m02_append_only();
        CREATE TRIGGER m02_approval_decision_immutable BEFORE UPDATE OR DELETE ON m02_approval_decision
          FOR EACH ROW EXECUTE FUNCTION m02_append_only();
        CREATE TRIGGER m02_decision_no_truncate BEFORE TRUNCATE ON m02_approval_decision
          FOR EACH STATEMENT EXECUTE FUNCTION m02_append_only();
        CREATE FUNCTION m02_protect_system_role() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN
            IF OLD."IsSystem" THEN RAISE EXCEPTION 'System roles cannot be changed or deleted; clone the role instead'; END IF;
            IF TG_OP='DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_system_role_immutable BEFORE UPDATE OR DELETE ON m02_role
          FOR EACH ROW EXECUTE FUNCTION m02_protect_system_role();
        CREATE FUNCTION m02_protect_system_role_permissions() RETURNS trigger LANGUAGE plpgsql AS $$
          DECLARE rid uuid; tid uuid;
          BEGIN
            IF current_setting('berynx.m02_seed',true)='on' THEN
              IF TG_OP='DELETE' THEN RETURN OLD; END IF;
              RETURN NEW;
            END IF;
            IF TG_OP='DELETE' THEN rid=OLD."RoleId"; tid=OLD."TenantId"; ELSE rid=NEW."RoleId"; tid=NEW."TenantId"; END IF;
            IF EXISTS (SELECT 1 FROM m02_role WHERE "TenantId"=tid AND "RoleId"=rid AND "IsSystem") THEN
              RAISE EXCEPTION 'System role permissions are immutable; clone the role instead';
            END IF;
            IF TG_OP='DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_system_role_permission_immutable BEFORE INSERT OR UPDATE OR DELETE ON m02_role_permission
          FOR EACH ROW EXECUTE FUNCTION m02_protect_system_role_permissions();
        CREATE FUNCTION m02_seed_tenant(p_tenant uuid) RETURNS void LANGUAGE plpgsql AS $$
          DECLARE previous_seed text;
          BEGIN
            previous_seed=current_setting('berynx.m02_seed',true);
            PERFORM set_config('berynx.m02_seed','on',true);
            INSERT INTO m02_role ("TenantId","RoleId","RoleKey","Name","Description","IsSystem","Version")
              SELECT p_tenant,gen_random_uuid(),"RoleKey","Name","Description",true,1
              FROM m02_system_role_template
              ON CONFLICT ("TenantId","RoleKey") DO NOTHING;
            INSERT INTO m02_role_permission ("TenantId","RoleId","PermissionCode")
              SELECT p_tenant,r."RoleId",p."PermissionCode"
              FROM m02_role r JOIN m02_system_role_permission p ON p."RoleKey"=r."RoleKey"
              WHERE r."TenantId"=p_tenant
              ON CONFLICT DO NOTHING;
            PERFORM set_config('berynx.m02_seed',COALESCE(previous_seed,''),true);
          END; $$;
        CREATE FUNCTION m02_seed_new_tenant() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN PERFORM m02_seed_tenant(NEW."TenantId"); RETURN NEW; END; $$;
        CREATE TRIGGER m02_seed_tenant_roles AFTER INSERT ON onboarding_tenant
          FOR EACH ROW EXECUTE FUNCTION m02_seed_new_tenant();
        CREATE FUNCTION m02_validate_scope() RETURNS trigger LANGUAGE plpgsql AS $$
          DECLARE cfg jsonb; found_scope boolean;
          BEGIN
            IF NEW."ScopeType" IN ('All','Own') THEN RETURN NEW; END IF;
            SELECT "Configuration" INTO cfg FROM onboarding_tenant WHERE "TenantId"=NEW."TenantId";
            IF NEW."ScopeType"='Branch' THEN
              SELECT EXISTS(SELECT 1 FROM jsonb_array_elements(COALESCE(cfg->'branches','[]'::jsonb)) b
                WHERE b->>'id'=NEW."ScopeId"::text) INTO found_scope;
            ELSE
              SELECT EXISTS(SELECT 1 FROM jsonb_array_elements(COALESCE(cfg->'plants','[]'::jsonb)) p
                WHERE p->>'id'=NEW."ScopeId"::text) INTO found_scope;
            END IF;
            IF NOT found_scope THEN RAISE EXCEPTION 'Scope ID is not configured for this tenant'; END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_scope_valid BEFORE INSERT OR UPDATE ON m02_user_role
          FOR EACH ROW EXECUTE FUNCTION m02_validate_scope();
        CREATE FUNCTION m02_guard_last_admin() RETURNS trigger LANGUAGE plpgsql AS $$
          DECLARE tid uuid; uid uuid; remaining bigint;
          BEGIN
            IF TG_TABLE_NAME='m02_user' THEN
              IF OLD."Status" <> 'Active' OR NEW."Status"='Active' THEN RETURN NEW; END IF;
              tid=OLD."TenantId"; uid=OLD."UserId";
            ELSE
              IF TG_OP='UPDATE' AND OLD."RoleId"=NEW."RoleId" AND OLD."UserId"=NEW."UserId" THEN RETURN NEW; END IF;
              IF NOT EXISTS (SELECT 1 FROM m02_role WHERE "TenantId"=OLD."TenantId" AND "RoleId"=OLD."RoleId" AND "RoleKey"='TenantAdmin') THEN
                IF TG_OP='DELETE' THEN RETURN OLD; END IF;
                RETURN NEW;
              END IF;
              tid=OLD."TenantId"; uid=OLD."UserId";
            END IF;
            PERFORM 1 FROM onboarding_tenant WHERE "TenantId"=tid FOR UPDATE;
            IF TG_TABLE_NAME='m02_user' THEN
              SELECT count(DISTINCT u."UserId") INTO remaining
                FROM m02_user u JOIN m02_user_role ur ON ur."TenantId"=u."TenantId" AND ur."UserId"=u."UserId"
                JOIN m02_role r ON r."TenantId"=ur."TenantId" AND r."RoleId"=ur."RoleId"
                WHERE u."TenantId"=tid AND u."Status"='Active' AND r."RoleKey"='TenantAdmin'
                  AND u."UserId"<>uid;
            ELSE
              SELECT count(DISTINCT u."UserId") INTO remaining
                FROM m02_user u JOIN m02_user_role ur ON ur."TenantId"=u."TenantId" AND ur."UserId"=u."UserId"
                JOIN m02_role r ON r."TenantId"=ur."TenantId" AND r."RoleId"=ur."RoleId"
                WHERE u."TenantId"=tid AND u."Status"='Active' AND r."RoleKey"='TenantAdmin'
                  AND ur."AssignmentId"<>OLD."AssignmentId";
            END IF;
            IF remaining=0 THEN RAISE EXCEPTION 'At least one active Tenant Admin must remain'; END IF;
            IF TG_OP='DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
          END; $$;
        CREATE TRIGGER m02_last_admin_user BEFORE UPDATE OF "Status" ON m02_user
          FOR EACH ROW EXECUTE FUNCTION m02_guard_last_admin();
        CREATE TRIGGER m02_last_admin_role BEFORE UPDATE OR DELETE ON m02_user_role
          FOR EACH ROW EXECUTE FUNCTION m02_guard_last_admin();
        """);

    protected override void Down(MigrationBuilder m) => m.Sql("""
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
        """);
}
