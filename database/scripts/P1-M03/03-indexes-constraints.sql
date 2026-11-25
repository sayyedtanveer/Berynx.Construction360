-- Generated from all four P1-M03 EF Core migrations.
-- Table-level primary, foreign-key and check constraints are created in 01-schema.sql.
-- This file creates EF indexes and records both migrations after schema setup.
BEGIN;

CREATE UNIQUE INDEX "IX_m03_category_tax_override_TenantId_ProductId_CustomerCatego~" ON m03_category_tax_override ("TenantId", "ProductId", "CustomerCategory", "EffectiveFrom");

CREATE UNIQUE INDEX "IX_m03_charge_type_TenantId_Code" ON m03_charge_type ("TenantId", "Code");

CREATE INDEX "IX_m03_contact_TenantId_PartyId_Mobile" ON m03_contact ("TenantId", "PartyId", "Mobile");

CREATE UNIQUE INDEX "IX_m03_driver_TenantId_LicenceNumber" ON m03_driver ("TenantId", "LicenceNumber");

CREATE INDEX "IX_m03_driver_TenantId_VehicleId" ON m03_driver ("TenantId", "VehicleId");

CREATE UNIQUE INDEX "IX_m03_equipment_TenantId_EquipmentType_Name" ON m03_equipment ("TenantId", "EquipmentType", "Name");

CREATE INDEX "IX_m03_equipment_TenantId_OwnerPartyId" ON m03_equipment ("TenantId", "OwnerPartyId");

CREATE INDEX "IX_m03_import_row_error_TenantId_BatchId" ON m03_import_row_error ("TenantId", "BatchId");

CREATE INDEX "IX_m03_party_TenantId_DefaultPriceListId" ON m03_party ("TenantId", "DefaultPriceListId");

CREATE INDEX "IX_m03_party_TenantId_Gstin" ON m03_party ("TenantId", "Gstin");

CREATE INDEX "IX_m03_party_TenantId_LegalName_City" ON m03_party ("TenantId", "LegalName", "City");

CREATE INDEX "IX_m03_party_TenantId_Pan" ON m03_party ("TenantId", "Pan");

CREATE INDEX "IX_m03_party_TenantId_PaymentTermId" ON m03_party ("TenantId", "PaymentTermId");

CREATE INDEX "IX_m03_party_TenantId_SalespersonUserId" ON m03_party ("TenantId", "SalespersonUserId");

ALTER TABLE m03_party
  ADD CONSTRAINT "FK_m03_party_m02_user_TenantId_SalespersonUserId"
  FOREIGN KEY ("TenantId", "SalespersonUserId")
  REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT;

CREATE INDEX "IX_m03_party_bank_account_TenantId_PartyId" ON m03_party_bank_account ("TenantId", "PartyId");

CREATE UNIQUE INDEX "IX_m03_party_merge_TenantId_SourcePartyId" ON m03_party_merge ("TenantId", "SourcePartyId");

CREATE INDEX "IX_m03_party_merge_TenantId_SurvivorPartyId" ON m03_party_merge ("TenantId", "SurvivorPartyId");

CREATE UNIQUE INDEX "IX_m03_payment_term_TenantId_Name" ON m03_payment_term ("TenantId", "Name");

CREATE INDEX "IX_m03_plant_TenantId_BranchId" ON m03_plant ("TenantId", "BranchId");

CREATE UNIQUE INDEX "IX_m03_plant_TenantId_SupplierPartyId_Name" ON m03_plant ("TenantId", "SupplierPartyId", "Name");

CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerGroup_ValidFrom" ON m03_price_list ("TenantId", "CustomerGroup", "ValidFrom") WHERE "CustomerGroup" IS NOT NULL;

CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_CustomerPartyId_ValidFrom" ON m03_price_list ("TenantId", "CustomerPartyId", "ValidFrom") WHERE "CustomerPartyId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_m03_price_list_TenantId_ValidFrom" ON m03_price_list ("TenantId", "ValidFrom") WHERE "IsDefault";

CREATE UNIQUE INDEX "IX_m03_price_list_line_TenantId_PriceListId_ProductId" ON m03_price_list_line ("TenantId", "PriceListId", "ProductId");

CREATE INDEX "IX_m03_price_list_line_TenantId_ProductId" ON m03_price_list_line ("TenantId", "ProductId");

CREATE INDEX "IX_m03_product_TenantId_BaseUnitCode" ON m03_product ("TenantId", "BaseUnitCode");

CREATE UNIQUE INDEX "IX_m03_product_TenantId_Code" ON m03_product ("TenantId", "Code");

CREATE UNIQUE INDEX "IX_m03_rmc_rate_card_TenantId_ProductId_Grade_SlumpBand_Admixt~" ON m03_rmc_rate_card ("TenantId", "ProductId", "Grade", "SlumpBand", "AdmixtureCode", "PumpType", "EffectiveFrom");

CREATE INDEX "IX_m03_site_TenantId_BranchId" ON m03_site ("TenantId", "BranchId");

CREATE UNIQUE INDEX "IX_m03_site_TenantId_CustomerPartyId_Name" ON m03_site ("TenantId", "CustomerPartyId", "Name");

CREATE UNIQUE INDEX "IX_m03_tax_rate_TenantId_HsnCode_EffectiveFrom" ON m03_tax_rate ("TenantId", "HsnCode", "EffectiveFrom");

CREATE UNIQUE INDEX "IX_m03_unit_conversion_TenantId_FromUnitCode_ToUnitCode" ON m03_unit_conversion ("TenantId", "FromUnitCode", "ToUnitCode");

CREATE INDEX "IX_m03_unit_conversion_TenantId_ToUnitCode" ON m03_unit_conversion ("TenantId", "ToUnitCode");

CREATE UNIQUE INDEX "IX_m03_vehicle_TenantId_RegistrationNumber" ON m03_vehicle ("TenantId", "RegistrationNumber");

CREATE INDEX "IX_m03_vehicle_TenantId_TransporterPartyId" ON m03_vehicle ("TenantId", "TransporterPartyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261124114145_P1M03Masters', '10.0.15');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261124115631_P1M03MasterConcurrency', '10.0.15');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261124121636_P1M03VersionedMasters', '10.0.15');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261124122738_P1M03PartyOwnership', '10.0.15');

COMMIT;
