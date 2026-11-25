CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000100_Onboarding') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000100_Onboarding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261124000100_Onboarding', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000200_OnboardingDelivery') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261124000200_OnboardingDelivery') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261124000200_OnboardingDelivery', '10.0.15');
    END IF;
END $EF$;
COMMIT;

