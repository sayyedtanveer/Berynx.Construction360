-- Add SaaS metadata to the existing M01 tenant and the new plan catalogue.
-- Run after 01-schema.sql.

BEGIN;

ALTER TABLE onboarding_tenant
    ADD COLUMN IF NOT EXISTS "CurrentSubscriptionId" uuid;

ALTER TABLE platform_plan
    ADD COLUMN IF NOT EXISTS "MaxApiRequestsPerMonth" bigint NOT NULL DEFAULT 0;

COMMIT;
