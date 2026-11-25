-- P1-M05 indexes and EF migration-history registration
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_TenantId_ContactId" ON m05_quotation ("TenantId", "ContactId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_TenantId_CustomerPartyId_Status_CreatedAt" ON m05_quotation ("TenantId", "CustomerPartyId", "Status", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m05_quotation_TenantId_IdempotencyKey" ON m05_quotation ("TenantId", "IdempotencyKey") WHERE "IdempotencyKey" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m05_quotation_TenantId_Number" ON m05_quotation ("TenantId", "Number") WHERE "Number" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_TenantId_SalespersonUserId_Status_BranchId" ON m05_quotation ("TenantId", "SalespersonUserId", "Status", "BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m05_quotation_TenantId_SeriesId_RevisionNumber" ON m05_quotation ("TenantId", "SeriesId", "RevisionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_TenantId_SiteId" ON m05_quotation ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_TenantId_SourceEnquiryId" ON m05_quotation ("TenantId", "SourceEnquiryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_activity_TenantId_QuotationId_At" ON m05_quotation_activity ("TenantId", "QuotationId", "At");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_charge_TenantId_ChargeTypeId" ON m05_quotation_charge ("TenantId", "ChargeTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_charge_TenantId_QuotationId" ON m05_quotation_charge ("TenantId", "QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_line_TenantId_ProductId" ON m05_quotation_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_line_TenantId_QuotationId" ON m05_quotation_line ("TenantId", "QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m05_quotation_link_TenantId_QuotationId" ON m05_quotation_link ("TenantId", "QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m05_quotation_link_TenantId_TokenHash" ON m05_quotation_link ("TenantId", "TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261125092036_P1M04M05Foundation', '10.0.15');
    END IF;
END $EF$;

COMMIT;
