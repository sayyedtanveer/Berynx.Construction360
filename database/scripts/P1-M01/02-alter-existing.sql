-- P1-M01 has no standalone alter-existing step.
-- Its complete ordered migration SQL, including OnboardingDelivery alterations,
-- is applied by 01-schema.sql from the checked-in EF-generated source script.
SELECT 'P1-M01: no separate alter-existing operations' AS deployment_note;
