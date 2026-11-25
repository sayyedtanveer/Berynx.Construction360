using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Berynx.Infrastructure.Persistence.Migrations;
[DbContext(typeof(BerynxDbContext))]
[Migration("20261124000100_Onboarding")]
public sealed class Onboarding : Migration
{
    protected override void Up(MigrationBuilder m) => m.Sql("""
        CREATE TABLE onboarding_tenant (
          "TenantId" uuid CONSTRAINT "PK_onboarding_tenant" PRIMARY KEY, "Version" bigint NOT NULL, "Name" text NOT NULL,
          "Plan" text NOT NULL, "Status" text NOT NULL CHECK ("Status" IN ('Trial','Active','Suspended','Cancelled')),
          "LiveAt" timestamptz, "CancelledAt" timestamptz, "Configuration" jsonb NOT NULL);
        CREATE TABLE onboarding_opening (
          "TenantId" uuid CONSTRAINT "FK_onboarding_opening_onboarding_tenant_TenantId" REFERENCES onboarding_tenant ON DELETE RESTRICT, "Id" uuid,
          "Kind" text NOT NULL, "Ledger" text NOT NULL, "Bill" text,
          "Debit" numeric(18,2) NOT NULL, "Credit" numeric(18,2) NOT NULL,
          CONSTRAINT "PK_onboarding_opening" PRIMARY KEY ("TenantId","Id"), CHECK (("Debit">0 AND "Credit"=0) OR ("Debit"=0 AND "Credit">0)));
        CREATE TABLE onboarding_event (
          "TenantId" uuid CONSTRAINT "FK_onboarding_event_onboarding_tenant_TenantId" REFERENCES onboarding_tenant ON DELETE RESTRICT, "Id" uuid, "Actor" text NOT NULL,
          "Action" text NOT NULL, "Before" jsonb NOT NULL, "After" jsonb NOT NULL, "At" timestamptz NOT NULL,
          CONSTRAINT "PK_onboarding_event" PRIMARY KEY ("TenantId","Id"));
        CREATE FUNCTION onboarding_append_only() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN RAISE EXCEPTION 'Audit records are immutable'; END; $$;
        CREATE TRIGGER immutable_setup_event BEFORE UPDATE OR DELETE ON onboarding_event
          FOR EACH ROW EXECUTE FUNCTION onboarding_append_only();
        CREATE TABLE onboarding_number (
          tenant_id uuid REFERENCES onboarding_tenant ON DELETE RESTRICT, branch_id uuid NOT NULL,
          document text NOT NULL, fiscal_year integer NOT NULL, posting_id uuid NOT NULL,
          sequence bigint NOT NULL, number text NOT NULL, gstin text NOT NULL,
          PRIMARY KEY (tenant_id,document,posting_id),
          UNIQUE (tenant_id,branch_id,document,fiscal_year,sequence),
          UNIQUE (tenant_id,branch_id,document,fiscal_year,number));
        """);
    protected override void Down(MigrationBuilder m) => m.Sql("""
        DO $$ BEGIN IF EXISTS (SELECT 1 FROM onboarding_tenant) THEN
          RAISE EXCEPTION 'Restore an approved backup to roll back an occupied onboarding database';
        END IF; END $$;
        DROP TABLE onboarding_number, onboarding_event, onboarding_opening, onboarding_tenant;
        DROP FUNCTION onboarding_append_only();
        """);
}
