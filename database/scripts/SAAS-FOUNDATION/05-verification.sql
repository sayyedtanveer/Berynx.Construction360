-- Read-only deployment verification. Expected module rows: M01, M02 and M03.

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20261125072458_SaaSLicensingFoundation',
    '20261125073502_SaaSUsageQuota'
)
ORDER BY "MigrationId";

SELECT table_name
FROM information_schema.tables
WHERE table_schema = current_schema()
  AND table_name IN (
      'platform_license_audit',
      'platform_module',
      'platform_plan',
      'platform_plan_module',
      'platform_tenant_subscription',
      'platform_tenant_entitlement',
      'platform_tenant_api_usage'
  )
ORDER BY table_name;

SELECT "ModuleCode", "Name", "IsCore", "IsActive"
FROM platform_module
ORDER BY "SortOrder", "ModuleCode";

SELECT conname, contype, convalidated
FROM pg_constraint
WHERE conrelid IN (
    'onboarding_tenant'::regclass,
    'platform_license_audit'::regclass,
    'platform_module'::regclass,
    'platform_plan'::regclass,
    'platform_plan_module'::regclass,
    'platform_tenant_subscription'::regclass,
    'platform_tenant_entitlement'::regclass,
    'platform_tenant_api_usage'::regclass
)
ORDER BY conrelid::regclass::text, conname;

SELECT
    t."TenantId",
    t."Name",
    t."CurrentSubscriptionId",
    s."PlanCode",
    s."Status",
    s."StartsAt",
    s."EndsAt"
FROM onboarding_tenant AS t
LEFT JOIN platform_tenant_subscription AS s
    ON s."TenantId" = t."TenantId"
   AND s."SubscriptionId" = t."CurrentSubscriptionId"
ORDER BY t."Name";
