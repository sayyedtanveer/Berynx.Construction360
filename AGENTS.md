# Berynx Construction 360 — Project Instructions

## Source of truth
- Read `docs/requirements/BRD_v2.2.md` before proposing or implementing any project module.
- v2.2 completion and conflict-resolution sections take priority over any conflicting v2.1 material retained within the file. Do not silently invent new business policies; document design decisions in `docs/decisions.md`.
- The BRD describes requirements; do not assume a capability is implemented because it is specified.
- Implement only the explicitly requested phase/module; do not pre-create unused features, routes, menus, tables or APIs.

## Initial workspace
- Parent folder: `Berynx.Construction360` (the already-open Zed project root).
- Backend is an independent .NET solution under `backend/`.
- Frontend is an independent Angular workspace under `frontend/` (no .NET solution is needed for Angular).
- Shared documentation stays at root under `docs/`; initial reference documents are read-only unless the user requests changes.
- Do not create a GitHub repository, add/replace a remote, or commit, merge or push. The user performs database deployment and Git finalization.
- Do not modify files outside this project root.

## Permanent user-approved module workflow — Phases 1–5

For each assigned module, follow these steps and then stop. Missing live database access or uncommitted earlier modules does not block implementation. The user manually executes SQL, verifies the live database and performs Git finalization.

1. Read the applicable BRD sections, existing module documentation, decisions, dependencies and current code. Implement only the assigned module.
2. Implement the module end-to-end across domain/business rules, backend/API and Angular UI, reusing existing contracts and services.
3. Generate EF Core migrations and review model diffs for unintended changes, data loss and migration drift.
4. Generate and review PostgreSQL deployment SQL for new tables, alterations, indexes, constraints, required seeds and verification. Generate separate guarded rollback SQL. Never execute SQL or connect to a database.
5. Run available .NET Release builds, NUnit unit tests, Angular production build/tests, applicable static checks and `git diff --check`. Fix confirmed non-database defects and rerun affected checks.
6. If a disposable PostgreSQL test database is not configured, label database-dependent integration tests **PENDING**, not passed. Do not repeatedly probe for database access.
7. Update module documentation with acceptance coverage, migrations, exact SQL paths and manual execution order, tests and all limitations/dependencies.
8. Review security, tenant isolation, secrets, generated artifacts and unrelated changes; preserve all pre-existing work.
9. Report implementation evidence and stop at the assigned module. Do not begin the next module automatically.

### Database and Git boundaries

- The development database, if manually used by the owner, is `127.0.0.1:5432`, database `berynx_construction_dev`, user `postgres`. Never automatically connect to or modify it. Never print, request in chat, store in source, or document credentials.
- Produce SQL scripts for manual owner execution; application startup must not run `Database.Migrate`, `EnsureCreated` or DDL. Do not use `dotnet ef database update`.
- PostgreSQL integration tests may use only a separately configured disposable localhost database ending in `_test`. If unavailable, report exact configuration as pending without attempting a database connection.
- Do not automatically commit, merge or push. The user owns Git finalization and manual SQL execution. Never discard unrelated changes or include secrets/build outputs.
- Provider integration and pilot/UAT evidence are reported separately. Do not claim provider-backed delivery or production approval unless actually verified.

## Platform baseline
- Backend: ASP.NET Core Web API / C#, PostgreSQL / EF Core, JWT-based authentication, modular monolith, DDD boundaries and CQRS where useful.
- Frontend: Angular / TypeScript, responsive and accessible UI, installable PWA at the appropriate phase.
- Follow clean dependencies: Domain independent; Application -> Domain; Infrastructure -> Domain/Application; API as composition root.
- Use supported, compatible SDK and CLI versions, record exact versions in the README. Prefer .NET 10 LTS if the machine supports it.
- Tests required for implemented functionality; do not claim tests/build passed without actually running them.

## Mandatory business safeguards
- Every tenant-owned record and query must respect server-enforced tenant isolation; RBAC must be enforced server-side.
- Store money, quantities and rates as decimal/numeric with precision specified in the BRD; never use float for monetary arithmetic.
- Posted financial records are immutable and adjusted only by authorized reversal/correction flows; preserve audit history.
- No hardcoded GSTINs, prices, tax rates, invoice series, company names or approval thresholds.
- Operational transaction, accounting, reporting and dashboard views must derive from a consistent source of truth; avoid duplicated business calculations in Angular.
- Use explicit state transitions, immutable snapshots when necessary, idempotency and concurrency safeguards.

## Working protocol
1. Read applicable requirements and existing code, then summarize scope, files, migration impact, risks and acceptance criteria.
2. Follow the permanent user-approved module workflow above. Do not block implementation on live database access, earlier-module Git finalization, or database-dependent tests.
3. Implement only the requested module and preserve existing/unrelated work.
4. Report commands and results accurately; mark unavailable database tests pending, never passed.
5. Provide SQL paths and manual execution order, do not execute SQL, and stop before the next module.
6. Never delete or overwrite existing work without confirming necessity.
