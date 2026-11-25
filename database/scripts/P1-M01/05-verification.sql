SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20261124000100_Onboarding',
  '20261124000200_OnboardingDelivery'
)
ORDER BY "MigrationId";

SELECT to_regclass('onboarding_tenant') AS onboarding_tenant,
       to_regclass('onboarding_opening') AS onboarding_opening,
       to_regclass('onboarding_event') AS onboarding_event,
       to_regclass('onboarding_number') AS onboarding_number,
       to_regclass('onboarding_invitation') AS onboarding_invitation;

SELECT
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'immutable_setup_event' AND NOT tgisinternal)
    AS immutable_setup_event_trigger_exists,
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'setup_invites' AND NOT tgisinternal)
    AS setup_invites_trigger_exists,
  EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'opening_lock' AND NOT tgisinternal)
    AS opening_lock_trigger_exists;
