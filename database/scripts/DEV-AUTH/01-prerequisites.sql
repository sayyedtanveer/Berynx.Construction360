-- Read-only guard for the local development auth fixture. No schema or data changes.
DO $$
DECLARE
  missing text[];
BEGIN
  IF to_regclass('public.onboarding_tenant') IS NULL
     OR to_regclass('public.m02_user') IS NULL
     OR to_regclass('public.m02_role') IS NULL
     OR to_regclass('public.m02_role_permission') IS NULL
     OR to_regclass('public.m02_user_role') IS NULL
     OR to_regclass('public.m02_identity_user') IS NULL
     OR to_regclass('public.m02_permission_catalog') IS NULL
     OR to_regclass('public.m02_system_role_template') IS NULL
     OR to_regprocedure('public.m02_seed_tenant(uuid)') IS NULL THEN
    RAISE EXCEPTION 'Apply the reviewed M01 and M02 schemas, M02 seed, and M03 schema/permission seed before DEV-AUTH scripts';
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
    WHERE conrelid = 'public.m02_permission_catalog'::regclass
      AND conname = 'm02_permission_catalog_Action_check'
      AND pg_get_constraintdef(oid) LIKE '%deactivate%'
  ) THEN
    RAISE EXCEPTION 'Apply P1-M03/02-alter-existing.sql before development auth roles';
  END IF;
END $$;

SELECT 'DEV-AUTH prerequisites are present' AS result;
