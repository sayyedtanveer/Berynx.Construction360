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

## Module loop
Schema/migrations → domain status/business rules → application handlers → API/OpenAPI → Angular screens → test every AC → seed/demo → document `docs/modules/<module>.md` → validate against phase gates. Do not pre-create future-phase menus, APIs or tables.

## Definition of done
- Story IDs and business-rule IDs mapped to tests.
- Authorization/tenant/cost leakage negative cases and audit verified.
- Concurrency, idempotency and reversal coverage for posting workflows.
- Appropriate UX empty/error/loading/mobile states and accessibility.
- No unverified claims of successful builds or testing.
- Stage-by-stage signoff before extending scope.
