START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125081845_M02TenantMfaEnforcement') THEN
    ALTER TABLE m02_auth_session ADD "MfaSatisfied" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125081845_M02TenantMfaEnforcement') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261125081845_M02TenantMfaEnforcement', '10.0.15');
    END IF;
END $EF$;
COMMIT;

