START TRANSACTION;
ALTER TABLE m11_receipt ADD "PostedAt" timestamp with time zone;

ALTER TABLE m11_receipt ADD "PostedByUserId" uuid;

ALTER TABLE m11_receipt ADD "VoucherId" uuid;

ALTER TABLE m10_invoice ADD "VoucherId" uuid;

ALTER TABLE m09_delivery_challan ADD "ReceiverVerifiedAt" timestamp with time zone;

CREATE TABLE m12_ledger_mapping (
    "TenantId" uuid NOT NULL,
    "MappingId" uuid NOT NULL,
    "MappingCode" character varying(32) NOT NULL,
    "BranchId" uuid NOT NULL,
    "Gstin" character varying(15) NOT NULL,
    "LedgerCode" character varying(64) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m12_ledger_mapping" PRIMARY KEY ("TenantId", "MappingId"),
    CONSTRAINT "CK_m12_ledger_mapping_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom")
);

CREATE TABLE m12_ledger_mapping_event (
    "TenantId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "MappingId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" character varying(32) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m12_ledger_mapping_event" PRIMARY KEY ("TenantId", "EventId"),
    CONSTRAINT "FK_m12_ledger_mapping_event_m12_ledger_mapping_TenantId_Mappin~" FOREIGN KEY ("TenantId", "MappingId") REFERENCES m12_ledger_mapping ("TenantId", "MappingId") ON DELETE RESTRICT
);

CREATE INDEX "IX_m11_receipt_TenantId_VoucherId" ON m11_receipt ("TenantId", "VoucherId");

CREATE INDEX "IX_m10_invoice_TenantId_VoucherId" ON m10_invoice ("TenantId", "VoucherId");

CREATE UNIQUE INDEX "IX_m12_ledger_mapping_TenantId_BranchId_Gstin_MappingCode_Effe~" ON m12_ledger_mapping ("TenantId", "BranchId", "Gstin", "MappingCode", "EffectiveFrom");

CREATE INDEX "IX_m12_ledger_mapping_TenantId_BranchId_Gstin_MappingCode_IsAc~" ON m12_ledger_mapping ("TenantId", "BranchId", "Gstin", "MappingCode", "IsActive", "EffectiveFrom", "EffectiveTo");

CREATE INDEX "IX_m12_ledger_mapping_event_TenantId_MappingId_OccurredAt" ON m12_ledger_mapping_event ("TenantId", "MappingId", "OccurredAt");

ALTER TABLE m10_invoice ADD CONSTRAINT "FK_m10_invoice_m12_voucher_TenantId_VoucherId" FOREIGN KEY ("TenantId", "VoucherId") REFERENCES m12_voucher ("TenantId", "VoucherId") ON DELETE RESTRICT;

ALTER TABLE m11_receipt ADD CONSTRAINT "FK_m11_receipt_m12_voucher_TenantId_VoucherId" FOREIGN KEY ("TenantId", "VoucherId") REFERENCES m12_voucher ("TenantId", "VoucherId") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261126151858_P1M12TenantLedgerMappingsAndPostingLinks', '10.0.15');

COMMIT;

