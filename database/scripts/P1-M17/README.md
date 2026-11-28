# P1-M17 Promotion and catalogue deployment

M17 schema is included in the combined EF-generated package at ../P1-M06-M17-FOUNDATION/01-apply.sql (migration 20261126104710_P1M06M17OperationalFoundation). Do not run a second M17 table-creation script. Execute this module seed only after the combined schema and its platform prerequisites. The seed registers the optional M17 module and its current permission codes, but does not grant subscription entitlements to tenants. Apply entitlement grants only through the established PlatformAdmin workflow.

M17 is not end-to-end complete: the API endpoint is registered and EF entities are mapped, but Angular screens and browser integration tests are not implemented/verified. M18/M19 are absent from this checkout.