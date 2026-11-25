-- P1-M03 reference rows and M02 authorization catalog extensions.
-- This script is tenant-scoped, repeatable, and contains no GST rates, credentials,
-- account numbers, company names, or customer/supplier data.
BEGIN;

INSERT INTO m02_permission_catalog ("PermissionCode", "Module", "Action", "Description")
VALUES
  ('masters.view', 'masters', 'view', 'View tenant master data'),
  ('masters.create', 'masters', 'create', 'Create tenant master data'),
  ('masters.edit', 'masters', 'edit', 'Edit tenant master data'),
  ('masters.deactivate', 'masters', 'deactivate', 'Deactivate tenant master data'),
  ('masters.approve', 'masters', 'approve', 'Approve master configuration'),
  ('masters.import', 'masters', 'import', 'Preview and import master data'),
  ('credit.override', 'credit', 'override', 'Request credit policy overrides'),
  ('fleet.override', 'fleet', 'override', 'Request fleet expiry overrides')
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO m02_system_role_permission ("RoleKey", "PermissionCode")
VALUES
  ('TenantAdmin', 'masters.view'),
  ('TenantAdmin', 'masters.create'),
  ('TenantAdmin', 'masters.edit'),
  ('TenantAdmin', 'masters.deactivate'),
  ('TenantAdmin', 'masters.approve'),
  ('TenantAdmin', 'masters.import'),
  ('TenantAdmin', 'credit.override'),
  ('TenantAdmin', 'fleet.override'),
  ('Approver', 'masters.view'),
  ('Approver', 'masters.approve'),
  ('Approver', 'credit.override'),
  ('Approver', 'fleet.override'),
  ('Accountant', 'masters.view'),
  ('ProjectManager', 'masters.view')
ON CONFLICT ("RoleKey", "PermissionCode") DO NOTHING;

-- Propagate the newly catalogued permissions into existing tenant system roles.
-- New tenant rows receive them via the existing M02 m02_seed_tenant trigger.
SELECT m02_seed_tenant("TenantId") FROM onboarding_tenant ORDER BY "TenantId";

INSERT INTO m03_unit ("TenantId", "UnitCode", "Name", "Symbol", "IsActive")
SELECT tenant."TenantId", unit."UnitCode", unit."Name", unit."Symbol", true
FROM onboarding_tenant AS tenant
CROSS JOIN (VALUES
  ('EA', 'Each', 'ea'),
  ('KG', 'Kilogram', 'kg'),
  ('TONNE', 'Metric tonne', 't'),
  ('M', 'Metre', 'm'),
  ('MM', 'Millimetre', 'mm'),
  ('M2', 'Square metre', 'm²'),
  ('M3', 'Cubic metre', 'm³'),
  ('L', 'Litre', 'L'),
  ('BAG', 'Bag', 'bag')
) AS unit("UnitCode", "Name", "Symbol")
ON CONFLICT ("TenantId", "UnitCode") DO NOTHING;

INSERT INTO m03_unit_conversion
  ("TenantId", "ConversionId", "FromUnitCode", "ToUnitCode", "ToUnitsPerFromUnit", "IsActive", "Version")
SELECT tenant."TenantId", gen_random_uuid(), conversion."FromUnitCode", conversion."ToUnitCode",
       conversion."Factor", true, 1
FROM onboarding_tenant AS tenant
CROSS JOIN (VALUES
  ('KG', 'TONNE', 0.001::numeric),
  ('M', 'MM', 1000::numeric)
) AS conversion("FromUnitCode", "ToUnitCode", "Factor")
ON CONFLICT ("TenantId", "FromUnitCode", "ToUnitCode") DO NOTHING;

INSERT INTO m03_payment_term
  ("TenantId", "PaymentTermId", "Name", "DueDays", "LedgerCode", "IsActive", "Version")
SELECT tenant."TenantId", gen_random_uuid(), 'Immediate', 0, NULL, true, 1
FROM onboarding_tenant AS tenant
ON CONFLICT ("TenantId", "Name") DO NOTHING;

INSERT INTO m03_charge_type
  ("TenantId", "ChargeTypeId", "Code", "Name", "LedgerCode", "HsnCode",
   "DefaultSurchargeRate", "DefaultTaxRatePercent", "IsActive", "Version")
SELECT tenant."TenantId", gen_random_uuid(), charge."Code", charge."Name", NULL, NULL, NULL, NULL, true, 1
FROM onboarding_tenant AS tenant
CROSS JOIN (VALUES
  ('RMC_ADMIXTURE', 'RMC admixture surcharge'),
  ('RMC_PUMP', 'RMC pump surcharge')
) AS charge("Code", "Name")
ON CONFLICT ("TenantId", "Code") DO NOTHING;

COMMIT;
