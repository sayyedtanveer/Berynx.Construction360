BEGIN;

DO $rollback$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
                 WHERE "MigrationId" = '20261124000100_Onboarding')
     OR NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
                    WHERE "MigrationId" = '20261124000200_OnboardingDelivery') THEN
    RAISE EXCEPTION 'Both P1-M01 migrations must be present before rollback';
  END IF;

  IF EXISTS (SELECT 1 FROM onboarding_tenant)
     OR EXISTS (SELECT 1 FROM onboarding_opening)
     OR EXISTS (SELECT 1 FROM onboarding_event)
     OR EXISTS (SELECT 1 FROM onboarding_number)
     OR EXISTS (SELECT 1 FROM onboarding_invitation) THEN
    RAISE EXCEPTION 'P1-M01 contains tenant or onboarding data; restore an approved backup instead';
  END IF;
END
$rollback$;

DROP TRIGGER opening_lock ON onboarding_opening;
DROP FUNCTION onboarding_lock_openings();
DROP TRIGGER setup_invites ON onboarding_tenant;
DROP FUNCTION onboarding_queue_invites();
DROP TRIGGER immutable_setup_event ON onboarding_event;
DROP FUNCTION onboarding_append_only();
DROP TABLE onboarding_invitation, onboarding_number, onboarding_event,
           onboarding_opening, onboarding_tenant;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN ('20261124000200_OnboardingDelivery',
                        '20261124000100_Onboarding');

COMMIT;
