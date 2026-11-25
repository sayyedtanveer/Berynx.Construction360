using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Berynx.Infrastructure.Persistence.Migrations;
[DbContext(typeof(BerynxDbContext))]
[Migration("20261124000200_OnboardingDelivery")]
public sealed class OnboardingDelivery : Migration
{
    protected override void Up(MigrationBuilder m) => m.Sql("""
        ALTER TABLE onboarding_event ADD COLUMN "Origin" jsonb NOT NULL DEFAULT jsonb_build_object();
        ALTER TABLE onboarding_number ADD COLUMN posted_at timestamptz NOT NULL DEFAULT now();
        CREATE TABLE onboarding_invitation (
          tenant_id uuid NOT NULL REFERENCES onboarding_tenant, email text NOT NULL, role text NOT NULL,
          delivery_id uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(), delivered_at timestamptz,
          available_at timestamptz NOT NULL DEFAULT now(), attempts integer NOT NULL DEFAULT 0,
          PRIMARY KEY(tenant_id,email));
        CREATE FUNCTION onboarding_queue_invites() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          INSERT INTO onboarding_invitation(tenant_id,email,role)
          SELECT NEW."TenantId",lower(i->>'email'),i->>'role'
          FROM jsonb_array_elements(NEW."Configuration"->'invitations') i
          ON CONFLICT(tenant_id,email) DO NOTHING;
          RETURN NEW;
        END; $$;
        CREATE TRIGGER setup_invites AFTER INSERT OR UPDATE OF "Configuration" ON onboarding_tenant
        FOR EACH ROW EXECUTE FUNCTION onboarding_queue_invites();
        CREATE FUNCTION onboarding_lock_openings() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE tid uuid;
        BEGIN
          IF TG_OP='DELETE' THEN tid=OLD."TenantId"; ELSE tid=NEW."TenantId"; END IF;
          PERFORM 1 FROM onboarding_tenant WHERE "TenantId"=tid AND "LiveAt" IS NULL AND "Status" IN ('Active','Trial') FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Opening balances are locked'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF; RETURN NEW;
        END; $$;
        CREATE TRIGGER opening_lock BEFORE INSERT OR UPDATE OR DELETE ON onboarding_opening
        FOR EACH ROW EXECUTE FUNCTION onboarding_lock_openings();
        """);
    protected override void Down(MigrationBuilder m) => m.Sql("""
        DO $$ BEGIN IF EXISTS (SELECT 1 FROM onboarding_tenant) THEN RAISE EXCEPTION 'Occupied database requires backup restore'; END IF; END $$;
        DROP TRIGGER opening_lock ON onboarding_opening; DROP FUNCTION onboarding_lock_openings();
        DROP TRIGGER setup_invites ON onboarding_tenant; DROP FUNCTION onboarding_queue_invites();
        DROP TABLE onboarding_invitation; ALTER TABLE onboarding_event DROP COLUMN "Origin"; ALTER TABLE onboarding_number DROP COLUMN posted_at;
        """);
}
