-- Seed only module identifiers that are implemented and routable today.
-- M01 and M02 are core; M03 can be included in a plan or licensed as an add-on.

BEGIN;

INSERT INTO platform_module
    ("ModuleCode", "Name", "Description", "IsCore", "IsActive", "SortOrder")
VALUES
    ('M01', 'Company onboarding', 'Tenant setup, configuration and go-live.', TRUE, TRUE, 10),
    ('M02', 'Identity & access', 'Users, roles, permissions and authentication.', TRUE, TRUE, 20),
    ('M03', 'Masters', 'Construction customer, supplier, pricing and fleet masters.', FALSE, TRUE, 30)
ON CONFLICT ("ModuleCode") DO NOTHING;

COMMIT;
