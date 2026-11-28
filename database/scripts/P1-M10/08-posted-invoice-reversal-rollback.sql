-- Guarded rollback for 07-posted-invoice-reversal.sql.
-- Refuse rollback after any cancellation has released challans or created a reversal voucher.
BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM m10_invoice_challan
        WHERE "ReleasedAt" IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Rollback blocked: released invoice-challan reservations exist.';
    END IF;
    IF EXISTS (
        SELECT 1 FROM m12_voucher
        WHERE "ReversalOfVoucherId" IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Rollback blocked: accounting reversal vouchers exist.';
    END IF;
END $$;

ALTER TABLE m12_voucher
    DROP CONSTRAINT "FK_m12_voucher_m12_voucher_TenantId_ReversalOfVoucherId";
DROP INDEX "IX_m12_voucher_TenantId_ReversalOfVoucherId";
DROP INDEX "IX_m10_invoice_challan_TenantId_ChallanId";
ALTER TABLE m10_invoice_challan DROP COLUMN "ReleasedAt";
CREATE UNIQUE INDEX "IX_m10_invoice_challan_TenantId_ChallanId"
    ON m10_invoice_challan ("TenantId", "ChallanId");

COMMIT;
