-- Read-only verification for the M19 schema and RBAC catalogue.
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M19';
SELECT "PermissionCode","Module","Action" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm19.%' ORDER BY "PermissionCode";
SELECT to_regclass(format('%I.%I',current_schema(),'m19_quality_criteria')) AS criteria_table,
       to_regclass(format('%I.%I',current_schema(),'m19_quality_sample')) AS sample_table,
       to_regclass(format('%I.%I',current_schema(),'m19_quality_test')) AS test_table,
       to_regclass(format('%I.%I',current_schema(),'m19_dispute')) AS dispute_table,
       to_regclass(format('%I.%I',current_schema(),'m19_dispute_invoice')) AS dispute_invoice_table,
       to_regclass(format('%I.%I',current_schema(),'m19_dispute_evidence')) AS dispute_evidence_table,
       to_regclass(format('%I.%I',current_schema(),'m19_quality_event')) AS quality_event_table;