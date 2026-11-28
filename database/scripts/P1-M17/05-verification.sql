-- Read-only M17 schema and catalogue seed verification.
SELECT "ModuleCode", "IsCore", "IsActive"
FROM platform_module WHERE "ModuleCode" = 'M17';
SELECT "PermissionCode", "Module", "Action"
FROM m02_permission_catalog
WHERE "PermissionCode" IN ('m17.catalogue.manage','m17.consent.view','m17.consent.manage')
ORDER BY "PermissionCode";
SELECT to_regclass(format('%I.%I', current_schema(), 'm17_catalogue_item')) AS catalogue_table,
       to_regclass(format('%I.%I', current_schema(), 'm17_consent_record')) AS consent_table,
       to_regclass(format('%I.%I', current_schema(), 'm17_promotion_event')) AS promotion_event_table;