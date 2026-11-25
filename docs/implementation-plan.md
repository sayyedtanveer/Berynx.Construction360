# Implementation Plan

**Reference:** `docs/requirements/BRD_v2.2.md` for all exact module stories, ACs, statuses and rules.

## Gate 0 — First agent task: project scaffolding ONLY
1. Inspect repository, complete BRD and AGENTS instructions, and local versions of dotnet, Node and Angular tooling.
2. Create `backend/` with buildable ASP.NET Core Web API and separate Domain/Application/Infrastructure projects, plus tests.
3. Create `frontend/` with independent Angular workspace and basic working application shell.
4. Configure environment-driven PostgreSQL connection, OpenAPI, health endpoint, errors, DI, safe `.gitignore` and starter test setup.
5. Run builds/tests if toolchain exists, record exact commands/output and blockers honestly.
6. Update README and decisions log. **STOP** and request review before modules.

## Phase delivery sequence
- **Phase 1:** P1-M01 through P1-M19, in the BRD's defined module order; modules completed one at a time.
- **Phase 2:** 10 E2 epics, preserving Phase 1 ledgers/GRNs while introducing perpetual stock and advanced finance.
- **Phase 3:** 7 E3 project-management epics with project-level cost and billing traceability.
- **Phase 4:** 7 E4 quality, safety and collaboration epics.
- **Phase 5:** 5 E5 provider, offline and analytics/AI epics.

## Permanent user-approved module workflow — all modules in Phases 1–5

Follow the workflow in [`AGENTS.md`](../AGENTS.md). Live database synchronization, database-dependent integration tests, and prior-module commits/pushes are **not prerequisites for beginning or implementing another assigned module**. The user manually executes SQL, verifies live databases and performs all Git finalization. Never automatically connect to or modify PostgreSQL, execute SQL, or commit/merge/push.

1. Read the applicable BRD, current source, relevant module guides, dependencies and decisions.
2. Implement the assigned module end-to-end in backend, frontend and business rules, reusing existing architecture and contracts.
3. Generate and review EF Core migrations and model diffs.
4. Generate PostgreSQL SQL for new tables, alterations, indexes, constraints, required seeds and verification; generate separate guarded rollback SQL. Do not execute these scripts.
5. Run available .NET Release build, NUnit unit tests, Angular production build/tests, applicable static checks and `git diff --check`. Fix confirmed non-database failures and rerun affected checks.
6. Mark database-dependent integration tests **PENDING** if the disposable PostgreSQL test database is unavailable; do not claim they passed.
7. Update the module guide and plan with acceptance coverage, migration IDs, exact SQL paths/manual execution order, test results and limitations.
8. Review security, tenant isolation, secrets, generated outputs and unrelated work; preserve all existing changes.
9. Report evidence and stop at the assigned module boundary; do not automatically begin the next module.

Application startup must not call `Database.Migrate`, `EnsureCreated` or execute DDL. Do not use `dotnet ef database update`. Follow [`database/README.md`](../database/README.md) for SQL artifact expectations and owner-run execution order. Report external-provider and pilot/UAT evidence separately; do not claim production approval without evidence.

## Agent implementation completion gate
- Story IDs and business-rule IDs mapped to tests.
- Authorization/tenant/cost leakage negative cases and audit verified.
- Concurrency, idempotency and reversal coverage for posting workflows.
- Appropriate UX empty/error/loading/mobile states and accessibility.
- EF model/migrations and generated deployment/rollback SQL reviewed.
- Available backend/frontend builds, unit tests, static checks and regression tests run; database tests are explicitly PENDING when no disposable DB is configured.
- Module documentation is current.
- SQL paths and manual execution order are provided for the owner.
- No unverified claims of successful builds/testing or provider/UAT/production approval.
- Do not execute SQL or perform Git finalization; stop before the next module.

## P1-M01 implementation review

The module implementation and acceptance matrix are recorded in [P1-M01](modules/P1-M01.md). Its PostgreSQL migration rehearsal and database-backed tests remain owner-run/pending when no test database is configured. This does not prevent implementing the next assigned module.

## P1-M02 — Users, Roles and Approvals

Implement the screen/story inventory in [P1-M02](modules/P1-M02.md) under the agent implementation workflow above. The approved authentication decision uses local ASP.NET Core Identity; the external provider is retained for email delivery only, and mobile/SMS OTP is excluded. M01/M02 SQL scripts are prepared for manual owner execution. Their live application, database integration results and Git finalization remain separately pending and are not gates on an assigned P1-M03 implementation.

## P1-M03 — Masters

The M03 implementation status and acceptance matrix are in [P1-M03](modules/P1-M03.md). Four migrations are prepared (`20261124114145_P1M03Masters`, `20261124115631_P1M03MasterConcurrency`, `20261124121636_P1M03VersionedMasters`, `20261124122738_P1M03PartyOwnership`) with ordered deployment and guarded rollback SQL under `database/scripts/P1-M03/`. The owner executes the scripts and verifies live schema. PostgreSQL-backed acceptance remains pending until run on a separate disposable `_test` database. Do not connect to or modify PostgreSQL, perform Git finalization, or begin M04 automatically.
