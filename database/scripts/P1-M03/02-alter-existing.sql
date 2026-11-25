-- Generated from EF Core migrations 20261124115631_P1M03MasterConcurrency,
-- 20261124121636_P1M03VersionedMasters and 20261124122738_P1M03PartyOwnership.
-- Also extends the existing M02 permission action check for M03 permission seeds.
BEGIN;

ALTER TABLE m03_price_list_line ADD COLUMN IF NOT EXISTS "Version" bigint NOT NULL DEFAULT 1;

ALTER TABLE m03_import_batch ADD COLUMN IF NOT EXISTS "Version" bigint NOT NULL DEFAULT 1;

ALTER TABLE m03_contact ADD COLUMN IF NOT EXISTS "Version" bigint NOT NULL DEFAULT 1;

ALTER TABLE m03_unit ADD COLUMN IF NOT EXISTS "Version" bigint NOT NULL DEFAULT 1;

ALTER TABLE m03_tax_rate ADD COLUMN IF NOT EXISTS "Version" bigint NOT NULL DEFAULT 1;

ALTER TABLE m03_party ADD COLUMN IF NOT EXISTS "SalespersonUserId" uuid;

ALTER TABLE m03_party ADD COLUMN IF NOT EXISTS "TagsJson" jsonb NOT NULL DEFAULT '[]'::jsonb;

-- M03 introduces permissions whose action names were not in the original M02
-- permission-catalog check. Broaden the existing M02 check before M03 seeds run.
ALTER TABLE m02_permission_catalog
  DROP CONSTRAINT IF EXISTS "m02_permission_catalog_Action_check";

ALTER TABLE m02_permission_catalog
  ADD CONSTRAINT "m02_permission_catalog_Action_check"
  CHECK ("Action" IN (
    'view', 'create', 'edit', 'approve', 'cancel', 'export', 'print', 'view_cost',
    'deactivate', 'import', 'override'
  ));

COMMIT;
