# M01–M05 Test Results

Results below are from validation run after the current M04/M05 edits on 25-Nov-2026. They describe local code only, not database deployment, provider delivery, browser UAT or production acceptance.

## Recorded validation

| Check | Result | Notes |
|---|---|---|
| Backend API Release build | **PASS** | `dotnet build backend/src/Berynx.Api/Berynx.Api.csproj -c Release --no-restore` passed; zero warnings and zero errors. |
| Backend unit tests | **PASS** | `dotnet test backend/tests/Berynx.UnitTests/Berynx.UnitTests.csproj -c Release --no-restore` passed: 67/67. An initial run collided with the parallel API build on a shared output DLL; the sequential rerun passed. |
| Angular production build | **PASS WITH WARNINGS** | `npm run build` passed. Four style-budget warnings: M02 page SCSS, shared header, login page and M04 page. No build errors. |
| Angular tests | **PASS** | `npm test -- --watch=false` passed: 12/12 files, 43/43 tests. This includes added M04 reopen and M05 multiple-line/charge request tests. |
| PostgreSQL integration/migration tests | **PENDING** | Requires owner-configured `BERYNX_TEST_POSTGRES` for a disposable localhost DB ending `_test`; no database probe, connection, migration or SQL execution was performed. |
| `git diff --check` | **UNSTAGED PASS; STAGED PRE-EXISTING ISSUE** | Unstaged diff check produced no output. Cached check reports the pre-existing trailing blank line in staged `database/scripts/M02-MFA-ENFORCEMENT/01-schema.sql`; it was not modified. |
| Worktree/Git | **Inspected; preserved** | Earlier work remains staged; current continuation edits are unstaged. No reset, clean, checkout, commit, merge or push was run. |

## Source tests found

- `backend/tests/Berynx.UnitTests/Quotations/M05QuotationCalculatorTests.cs`: decimal discount, intra/inter-state tax split, invalid quantity/discount precision.
- `backend/tests/Berynx.IntegrationTests/M02AuthenticationTests.cs`: MFA/identity coverage is present but PostgreSQL test availability is not established.
- `frontend/src/app/features/crm/m04.page.spec.ts`: M04 list/reference loading and enquiry create request coverage.
- `frontend/src/app/features/quotations/m05.page.spec.ts`: M05 list/reference loading and simple create request coverage.

These tests do not cover every BR/AC, multi-line M04 UI, M05 revision/clone line diffs, public OTP provider, cross-tenant isolation, endpoint authorization failures, scheduled SLA delivery, accessibility, performance or browser UAT.

## Commands to rerun before accepting edits

Run from repo root / corresponding workspace, with timeouts:

```text
dotnet build backend/src/Berynx.Api/Berynx.Api.csproj -c Release --no-restore
dotnet test backend/tests/Berynx.UnitTests/Berynx.UnitTests.csproj -c Release --no-restore
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
git diff --check
git diff --cached --check
```

For the PostgreSQL integration project only after the owner explicitly configures a disposable `_test` database, run its tests using the repository’s documented `BERYNX_TEST_POSTGRES` configuration; until then mark them **PENDING** and do not attempt a database connection.

## No deployment activity

No database connection, SQL execution, `dotnet ef database update`, migration application, seed operation, reset, cleanup, Git commit, merge or push was performed. SQL artifacts are for owner review/manual deployment only; rollback and verification SQL are missing, so deployment is held.
