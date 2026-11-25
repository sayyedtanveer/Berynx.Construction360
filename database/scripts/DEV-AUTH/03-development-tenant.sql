-- Creates a minimal local-only tenant only when the database has no tenants.
-- Existing M01 tenant/company data is never modified.
BEGIN;

DO $$
DECLARE
  local_tenant uuid := '6bca9360-36a0-4d2f-9a30-000000000360';
  local_branch uuid := '6bca9360-36a0-4d2f-9a30-000000000361';
BEGIN
  IF NOT EXISTS (SELECT 1 FROM onboarding_tenant) THEN
    INSERT INTO onboarding_tenant
      ("TenantId", "Version", "Name", "Plan", "Status", "LiveAt", "CancelledAt", "Configuration")
    VALUES
      (local_tenant, 1, 'Local Development Tenant', 'Development', 'Trial', NULL, NULL,
       jsonb_build_object(
         'currency', 'INR',
         'timeZone', 'Asia/Kolkata',
         'company', NULL,
         'registrations', jsonb_build_array(),
         'branches', jsonb_build_array(jsonb_build_object(
           'id', local_branch,
           'name', 'Local Development Branch',
           'address', 'Local development only',
           'gstin', ''
         )),
         'plants', jsonb_build_array(),
         'steps', jsonb_build_array(),
         'modules', jsonb_build_array(),
         'invitations', jsonb_build_array(),
         'limits', jsonb_build_object('users', 100, 'storage', 100000000, 'documents', 10000, 'messages', 1000)
       ));
  END IF;
END $$;

-- Idempotently ensure tenants receive templates added in 02.
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";

COMMIT;
