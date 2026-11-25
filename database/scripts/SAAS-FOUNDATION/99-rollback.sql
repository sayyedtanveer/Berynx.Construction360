-- Guarded rollback. Do not run after plans, subscriptions, entitlements, API usage
-- or audit events have been created; export and review that data first.
BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM platform_plan)
       OR EXISTS (SELECT 1 FROM platform_tenant_subscription)
       OR EXISTS (SELECT 1 FROM platform_tenant_entitlement)
       OR EXISTS (SELECT 1 FROM platform_tenant_api_usage)
       OR EXISTS (SELECT 1 FROM platform_license_audit) THEN
        RAISE EXCEPTION 'Rollback refused: SaaS plan, tenant licence, usage or audit data exists.';
    END IF;
    IF EXISTS (SELECT 1 FROM onboarding_tenant WHERE "Status" = 'Grace') THEN
        RAISE EXCEPTION 'Rollback refused: tenant lifecycle contains Grace status.';
    END IF;
END
$$;

DROP TRIGGER IF EXISTS platform_license_audit_immutable ON platform_license_audit;
DROP FUNCTION IF EXISTS prevent_platform_license_audit_mutation();

ALTER TABLE onboarding_tenant
    DROP CONSTRAINT IF EXISTS "FK_onboarding_tenant_platform_tenant_subscription_TenantId_Cur~";
ALTER TABLE onboarding_tenant
    DROP CONSTRAINT IF EXISTS "CK_onboarding_tenant_Status_lifecycle";
ALTER TABLE onboarding_tenant
    DROP CONSTRAINT IF EXISTS "onboarding_tenant_Status_check";
ALTER TABLE onboarding_tenant
    DROP CONSTRAINT IF EXISTS onboarding_tenant_status_check;
ALTER TABLE onboarding_tenant
    ADD CONSTRAINT "onboarding_tenant_Status_check"
    CHECK ("Status" IN ('Trial', 'Active', 'Suspended', 'Cancelled'));

ALTER TABLE platform_tenant_subscription
    DROP CONSTRAINT IF EXISTS "FK_platform_tenant_subscription_platform_plan_PlanCode";

DROP TABLE IF EXISTS platform_tenant_api_usage;
DROP TABLE IF EXISTS platform_license_audit;
DROP TABLE IF EXISTS platform_plan_module;
DROP TABLE IF EXISTS platform_tenant_entitlement;
DROP TABLE IF EXISTS platform_tenant_subscription;
DROP TABLE IF EXISTS platform_plan;
DROP TABLE IF EXISTS platform_module;

-- Drop indexes separately; they are not constraints.
DROP INDEX IF EXISTS "IX_onboarding_tenant_CurrentSubscriptionId";
DROP INDEX IF EXISTS "IX_onboarding_tenant_TenantId_CurrentSubscriptionId";
ALTER TABLE onboarding_tenant
    DROP COLUMN IF EXISTS "CurrentSubscriptionId";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20261125072458_SaaSLicensingFoundation',
    '20261125073502_SaaSUsageQuota'
);

COMMIT;
