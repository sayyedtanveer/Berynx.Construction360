-- Additive schema for BR-M10-06 posted invoice cancellation and M12 reversal linkage.
-- Apply only after the existing M10/M12 schema is deployed and reviewed.
-- Equivalent EF migration: 20261127181300_P1M10PostedInvoiceReversal.
BEGIN;

ALTER TABLE m10_invoice_challan
    ADD COLUMN "ReleasedAt" timestamp with time zone NULL;

DROP INDEX "IX_m10_invoice_challan_TenantId_ChallanId";
CREATE UNIQUE INDEX "IX_m10_invoice_challan_TenantId_ChallanId"
    ON m10_invoice_challan ("TenantId", "ChallanId")
    WHERE "ReleasedAt" IS NULL;

CREATE UNIQUE INDEX "IX_m12_voucher_TenantId_ReversalOfVoucherId"
    ON m12_voucher ("TenantId", "ReversalOfVoucherId")
    WHERE "ReversalOfVoucherId" IS NOT NULL;

ALTER TABLE m12_voucher
    ADD CONSTRAINT "FK_m12_voucher_m12_voucher_TenantId_ReversalOfVoucherId"
    FOREIGN KEY ("TenantId", "ReversalOfVoucherId")
    REFERENCES m12_voucher ("TenantId", "VoucherId")
    ON DELETE RESTRICT;

COMMIT;
