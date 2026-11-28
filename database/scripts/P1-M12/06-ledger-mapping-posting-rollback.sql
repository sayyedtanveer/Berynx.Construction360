-- Guarded rollback for migration 20261126151858_P1M12TenantLedgerMappingsAndPostingLinks.
-- Manual owner execution only. This intentionally aborts if mappings, verified-delivery
-- timestamps, or source-to-voucher posting links contain business data.
BEGIN;
LOCK TABLE m09_delivery_challan, m10_invoice, m11_receipt,
           m12_ledger_mapping, m12_ledger_mapping_event
    IN SHARE ROW EXCLUSIVE MODE;
DO $rollback_guard$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20261126151858_P1M12TenantLedgerMappingsAndPostingLinks'
    ) THEN
        RAISE EXCEPTION 'Expected migration 20261126151858_P1M12TenantLedgerMappingsAndPostingLinks is not applied.';
    END IF;
    IF (SELECT max("MigrationId") FROM "__EFMigrationsHistory") <>
        '20261126151858_P1M12TenantLedgerMappingsAndPostingLinks' THEN
        RAISE EXCEPTION 'A later migration exists; rollback later migrations first.';
    END IF;
    IF EXISTS (SELECT 1 FROM m12_ledger_mapping) OR
       EXISTS (SELECT 1 FROM m12_ledger_mapping_event) THEN
        RAISE EXCEPTION 'Ledger mapping data exists; preserve it and reverse through a reviewed forward migration.';
    END IF;
    IF EXISTS (SELECT 1 FROM m10_invoice WHERE "VoucherId" IS NOT NULL) OR
       EXISTS (SELECT 1 FROM m11_receipt WHERE "VoucherId" IS NOT NULL) THEN
        RAISE EXCEPTION 'Posted source documents reference accounting vouchers; do not remove the posting links.';
    END IF;
    IF EXISTS (SELECT 1 FROM m11_receipt WHERE "PostedAt" IS NOT NULL OR "PostedByUserId" IS NOT NULL) THEN
        RAISE EXCEPTION 'Receipt posting metadata exists; do not remove the audit fields.';
    END IF;
    IF EXISTS (SELECT 1 FROM m09_delivery_challan WHERE "ReceiverVerifiedAt" IS NOT NULL) THEN
        RAISE EXCEPTION 'Receiver verification timestamps exist; preserve delivery audit evidence.';
    END IF;
END;
$rollback_guard$;

ALTER TABLE IF EXISTS m10_invoice
    DROP CONSTRAINT IF EXISTS "FK_m10_invoice_m12_voucher_TenantId_VoucherId";
ALTER TABLE IF EXISTS m11_receipt
    DROP CONSTRAINT IF EXISTS "FK_m11_receipt_m12_voucher_TenantId_VoucherId";
DROP TABLE IF EXISTS m12_ledger_mapping_event;
DROP TABLE IF EXISTS m12_ledger_mapping;
DROP INDEX IF EXISTS "IX_m11_receipt_TenantId_VoucherId";
DROP INDEX IF EXISTS "IX_m10_invoice_TenantId_VoucherId";
ALTER TABLE IF EXISTS m11_receipt DROP COLUMN IF EXISTS "PostedAt";
ALTER TABLE IF EXISTS m11_receipt DROP COLUMN IF EXISTS "PostedByUserId";
ALTER TABLE IF EXISTS m11_receipt DROP COLUMN IF EXISTS "VoucherId";
ALTER TABLE IF EXISTS m10_invoice DROP COLUMN IF EXISTS "VoucherId";
ALTER TABLE IF EXISTS m09_delivery_challan DROP COLUMN IF EXISTS "ReceiverVerifiedAt";
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261126151858_P1M12TenantLedgerMappingsAndPostingLinks';
COMMIT;