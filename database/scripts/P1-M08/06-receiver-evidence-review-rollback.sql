-- Guarded rollback for 20261127081534_P1M08ReceiverEvidenceReview.
-- Refuses to run after any M08 review audit event or while reviewer/reason data exists.
-- Run only after an approved backup and
-- after stopping writes to M08; this script is for manual owner execution only.
BEGIN;
DO $rollback$
BEGIN
    IF to_regclass('public.m08_trip') IS NULL THEN
        RAISE EXCEPTION 'm08_trip is missing; refusing rollback';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema() AND table_name = 'm08_trip'
          AND column_name = 'ReceiverVerifiedByUserId'
    ) OR NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema() AND table_name = 'm08_trip'
          AND column_name = 'EvidenceDecisionReason'
    ) THEN
        RAISE EXCEPTION 'Evidence review migration is not fully installed; refusing rollback';
    END IF;
    IF EXISTS (
        SELECT 1 FROM m08_trip
        WHERE "ReceiverVerifiedByUserId" IS NOT NULL
           OR "EvidenceDecisionReason" IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Reviewer identity or decision-reason data exists; preserve it and do not roll back';
    END IF;
    IF EXISTS (
        SELECT 1 FROM m08_trip_event
        WHERE "EventType" IN ('Receiver evidence verified', 'Receiver evidence rejected', 'Receiver evidence resubmitted')
    ) THEN
        RAISE EXCEPTION 'M08 evidence-review audit events exist; preserve the review history and do not roll back';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20261127081534_P1M08ReceiverEvidenceReview'
    ) THEN
        RAISE EXCEPTION 'Migration history marker is missing; refusing rollback';
    END IF;

    ALTER TABLE m08_trip
        DROP CONSTRAINT IF EXISTS "FK_m08_trip_m02_user_TenantId_ReceiverVerifiedByUserId";
    DROP INDEX IF EXISTS "IX_m08_trip_TenantId_ReceiverVerifiedByUserId";
    ALTER TABLE m08_trip
        DROP COLUMN "ReceiverVerifiedByUserId",
        DROP COLUMN "EvidenceDecisionReason";
    DELETE FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20261127081534_P1M08ReceiverEvidenceReview';
END
$rollback$;
COMMIT;