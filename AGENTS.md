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
- Do not create a GitHub repository, add a remote, or push without a later explicit request.
- Do not modify files outside this project root.

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
1. Read the relevant BRD sections and existing code before edits.
2. Summarize scope, files to change, migration impact, risks and acceptance criteria.
3. Implement the smallest complete and testable step.
4. Run available builds and tests; report commands and results accurately.
5. Stop at the requested scope boundary and ask for approval before progressing to another module/phase.
6. Never delete or overwrite existing work without confirming necessity.
