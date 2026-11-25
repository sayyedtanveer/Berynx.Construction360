-- M03 and M04 indexes, checks, and constraints
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    DROP INDEX "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    DROP INDEX "IX_m03_price_list_TenantId_CustomerGroup_ValidFrom";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    DROP INDEX "IX_m03_price_list_TenantId_CustomerPartyId_ValidFrom";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    DROP INDEX "IX_m03_price_list_TenantId_ValidFrom";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~" ON m03_rmc_rate_card ("TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "PriceBasis", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    ALTER TABLE m03_rmc_rate_card ADD CONSTRAINT ck_m03_rmc_rate_card_basis CHECK ("PriceBasis" IN ('ExPlant','Delivered','Site'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerGroup_PriceBasis_ValidFrom" ON m03_price_list ("TenantId", "CustomerGroup", "PriceBasis", "ValidFrom") WHERE "CustomerGroup" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerPartyId_PriceBasis_ValidFrom" ON m03_price_list ("TenantId", "CustomerPartyId", "PriceBasis", "ValidFrom") WHERE "CustomerPartyId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_PriceBasis_ValidFrom" ON m03_price_list ("TenantId", "PriceBasis", "ValidFrom") WHERE "IsDefault";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    ALTER TABLE m03_price_list ADD CONSTRAINT ck_m03_price_list_basis CHECK ("PriceBasis" IN ('ExPlant','Delivered','Site'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_assignment_audit_TenantId_EnquiryId_At" ON m04_assignment_audit ("TenantId", "EnquiryId", "At");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_assignment_rule_TenantId_Source_RegionStateCode_Priority" ON m04_assignment_rule ("TenantId", "Source", "RegionStateCode", "Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_TenantId_ContactId" ON m04_enquiry ("TenantId", "ContactId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_TenantId_CustomerPartyId" ON m04_enquiry ("TenantId", "CustomerPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m04_enquiry_TenantId_IdempotencyKey" ON m04_enquiry ("TenantId", "IdempotencyKey") WHERE "IdempotencyKey" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE UNIQUE INDEX "IX_m04_enquiry_TenantId_Number" ON m04_enquiry ("TenantId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_TenantId_OwnerUserId_Status_BranchId" ON m04_enquiry ("TenantId", "OwnerUserId", "Status", "BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_TenantId_SiteId" ON m04_enquiry ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_TenantId_Status_NextActionAt" ON m04_enquiry ("TenantId", "Status", "NextActionAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_activity_TenantId_EnquiryId_At" ON m04_enquiry_activity ("TenantId", "EnquiryId", "At");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_line_TenantId_EnquiryId" ON m04_enquiry_line ("TenantId", "EnquiryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_enquiry_line_TenantId_ProductId" ON m04_enquiry_line ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_follow_up_TenantId_EnquiryId" ON m04_follow_up ("TenantId", "EnquiryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE INDEX "IX_m04_follow_up_TenantId_Status_DueAt_OwnerUserId" ON m04_follow_up ("TenantId", "Status", "DueAt", "OwnerUserId");
    END IF;
END $EF$;

COMMIT;
