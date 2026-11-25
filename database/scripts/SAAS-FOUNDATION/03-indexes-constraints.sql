-- Referential integrity, quota/status checks, access paths and immutable audit.
-- Run after 01-schema.sql and 02-alter-existing.sql.

BEGIN;

CREATE INDEX IF NOT EXISTS "IX_onboarding_tenant_CurrentSubscriptionId"
    ON onboarding_tenant ("CurrentSubscriptionId");

CREATE INDEX IF NOT EXISTS "IX_platform_license_audit_TenantId_At"
    ON platform_license_audit ("TenantId", "At");

CREATE INDEX IF NOT EXISTS "IX_platform_plan_module_ModuleCode"
    ON platform_plan_module ("ModuleCode");

CREATE INDEX IF NOT EXISTS "IX_platform_tenant_entitlement_ModuleCode"
    ON platform_tenant_entitlement ("ModuleCode");

CREATE INDEX IF NOT EXISTS "IX_platform_tenant_entitlement_TenantId_IsEnabled_EffectiveUnt~"
    ON platform_tenant_entitlement ("TenantId", "IsEnabled", "EffectiveUntil");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_platform_tenant_subscription_TenantId_IsCurrent"
    ON platform_tenant_subscription ("TenantId", "IsCurrent")
    WHERE "IsCurrent" = TRUE;

CREATE INDEX IF NOT EXISTS "IX_platform_tenant_subscription_TenantId_Status_EndsAt"
    ON platform_tenant_subscription ("TenantId", "Status", "EndsAt");

CREATE INDEX IF NOT EXISTS "IX_platform_tenant_subscription_PlanCode"
    ON platform_tenant_subscription ("PlanCode");

CREATE INDEX IF NOT EXISTS "IX_onboarding_tenant_TenantId_CurrentSubscriptionId"
    ON onboarding_tenant ("TenantId", "CurrentSubscriptionId");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_license_audit_onboarding_tenant_TenantId') THEN
        ALTER TABLE platform_license_audit
            ADD CONSTRAINT "FK_platform_license_audit_onboarding_tenant_TenantId"
            FOREIGN KEY ("TenantId") REFERENCES onboarding_tenant ("TenantId") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_tenant_subscription_onboarding_tenant_TenantId') THEN
        ALTER TABLE platform_tenant_subscription
            ADD CONSTRAINT "FK_platform_tenant_subscription_onboarding_tenant_TenantId"
            FOREIGN KEY ("TenantId") REFERENCES onboarding_tenant ("TenantId") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_tenant_subscription_platform_plan_PlanCode') THEN
        ALTER TABLE platform_tenant_subscription
            ADD CONSTRAINT "FK_platform_tenant_subscription_platform_plan_PlanCode"
            FOREIGN KEY ("PlanCode") REFERENCES platform_plan ("PlanCode") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_tenant_entitlement_onboarding_tenant_TenantId') THEN
        ALTER TABLE platform_tenant_entitlement
            ADD CONSTRAINT "FK_platform_tenant_entitlement_onboarding_tenant_TenantId"
            FOREIGN KEY ("TenantId") REFERENCES onboarding_tenant ("TenantId") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_tenant_entitlement_platform_module_ModuleCode') THEN
        ALTER TABLE platform_tenant_entitlement
            ADD CONSTRAINT "FK_platform_tenant_entitlement_platform_module_ModuleCode"
            FOREIGN KEY ("ModuleCode") REFERENCES platform_module ("ModuleCode") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_plan_module_platform_module_ModuleCode') THEN
        ALTER TABLE platform_plan_module
            ADD CONSTRAINT "FK_platform_plan_module_platform_module_ModuleCode"
            FOREIGN KEY ("ModuleCode") REFERENCES platform_module ("ModuleCode") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_plan_module_platform_plan_PlanCode') THEN
        ALTER TABLE platform_plan_module
            ADD CONSTRAINT "FK_platform_plan_module_platform_plan_PlanCode"
            FOREIGN KEY ("PlanCode") REFERENCES platform_plan ("PlanCode") ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_platform_tenant_api_usage_onboarding_tenant_TenantId') THEN
        ALTER TABLE platform_tenant_api_usage
            ADD CONSTRAINT "FK_platform_tenant_api_usage_onboarding_tenant_TenantId"
            FOREIGN KEY ("TenantId") REFERENCES onboarding_tenant ("TenantId") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_onboarding_tenant_platform_tenant_subscription_TenantId_Cur~') THEN
        ALTER TABLE onboarding_tenant
            ADD CONSTRAINT "FK_onboarding_tenant_platform_tenant_subscription_TenantId_Cur~"
            FOREIGN KEY ("TenantId", "CurrentSubscriptionId")
            REFERENCES platform_tenant_subscription ("TenantId", "SubscriptionId")
            ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_tenant_subscription_grace') THEN
        ALTER TABLE platform_tenant_subscription
            ADD CONSTRAINT "CK_platform_tenant_subscription_grace"
            CHECK ("GraceEndsAt" IS NULL OR ("EndsAt" IS NOT NULL AND "GraceEndsAt" >= "EndsAt"));
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_tenant_subscription_status') THEN
        ALTER TABLE platform_tenant_subscription
            ADD CONSTRAINT "CK_platform_tenant_subscription_status"
            CHECK ("Status" IN ('Trial', 'Active', 'Grace', 'Suspended', 'Cancelled'));
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_tenant_subscription_term') THEN
        ALTER TABLE platform_tenant_subscription
            ADD CONSTRAINT "CK_platform_tenant_subscription_term"
            CHECK ("EndsAt" IS NULL OR "StartsAt" < "EndsAt");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_tenant_entitlement_source') THEN
        ALTER TABLE platform_tenant_entitlement
            ADD CONSTRAINT "CK_platform_tenant_entitlement_source"
            CHECK ("Source" IN ('Plan', 'AddOn'));
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_tenant_api_usage_count') THEN
        ALTER TABLE platform_tenant_api_usage
            ADD CONSTRAINT "CK_platform_tenant_api_usage_count"
            CHECK ("RequestCount" >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_platform_plan_quotas') THEN
        ALTER TABLE platform_plan
            ADD CONSTRAINT "CK_platform_plan_quotas"
            CHECK (
                "MaxUsers" > 0 AND "MaxStorageBytes" > 0 AND
                "MaxDocumentsPerMonth" > 0 AND "MaxMessagesPerMonth" > 0 AND
                "MaxApiRequestsPerMonth" > 0 AND "TrialDays" > 0 AND
                "GracePeriodDays" >= 0 AND
                ("PriceAmount" IS NULL OR "PriceAmount" >= 0)
            );
    END IF;
END
$$;

DO $$
DECLARE
    old_constraint text;
BEGIN
    FOREACH old_constraint IN ARRAY ARRAY[
        'onboarding_tenant_Status_check',
        'onboarding_tenant_status_check',
        'CK_onboarding_tenant_Status_lifecycle'
    ]
    LOOP
        IF EXISTS (
            SELECT 1
            FROM pg_constraint
            WHERE conrelid = 'onboarding_tenant'::regclass
              AND conname = old_constraint
        ) THEN
            EXECUTE format(
                'ALTER TABLE onboarding_tenant DROP CONSTRAINT %I',
                old_constraint
            );
        END IF;
    END LOOP;
END
$$;

ALTER TABLE onboarding_tenant
    ADD CONSTRAINT "CK_onboarding_tenant_Status_lifecycle"
    CHECK ("Status" IN ('Trial', 'Active', 'Grace', 'Suspended', 'Cancelled'));

CREATE OR REPLACE FUNCTION prevent_platform_license_audit_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Platform licence audit rows are immutable.';
END;
$$;

DROP TRIGGER IF EXISTS platform_license_audit_immutable ON platform_license_audit;
CREATE TRIGGER platform_license_audit_immutable
BEFORE UPDATE OR DELETE ON platform_license_audit
FOR EACH ROW EXECUTE FUNCTION prevent_platform_license_audit_mutation();

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
    ('20261125072458_SaaSLicensingFoundation', '10.0.15'),
    ('20261125073502_SaaSUsageQuota', '10.0.15')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
