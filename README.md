# Berynx Construction 360

Multi-tenant SaaS construction ERP, India-first. First pilot: HAFA INFRA (per BRD).

## Source documents
- `docs/requirements/BRD_v2.2.md`: complete master business requirements baseline, including Phase 1 modules and Phase 2–5 epics.
- `AGENTS.md`: coding-agent instructions.
- `docs/architecture/overview.md`: proposed architecture contracts.
- `docs/implementation-plan.md`: implementation gates and phase ordering.
- `docs/decisions.md`: proposals, decisions and unresolved questions.
- `docs/testing/strategy.md`: testing expectations.

## Gate 0 project structure

```text
Berynx.Construction360/
├── backend/                     # independent .NET 10 solution and tests
│   ├── src/
│   │   ├── Berynx.Api/
│   │   ├── Berynx.Application/
│   │   ├── Berynx.Domain/
│   │   └── Berynx.Infrastructure/
│   └── tests/
├── frontend/                    # independent Angular 22 standalone workspace
│   ├── angular.json
│   └── src/
├── docs/
├── AGENTS.md
└── README.md
```

Open the repository root for both applications, `backend/` for .NET only, or
`frontend/` for Angular only. Both applications use this single root Git
repository. Gate 0 creates shared application infrastructure only; no ERP
modules or business tables are included.

## Toolchain

- Backend target: .NET 10 LTS (`net10.0`). The local machine currently has only
  .NET SDK `8.0.200`; install .NET 10 SDK before restoring/building the backend.
- Frontend: Angular `22.3.0`, TypeScript `6.0.3`, Node `24.19.0`, npm `11.6.2`.
  Angular 22's published Node engine range includes Node 24.15 and newer.
- PostgreSQL 16 and 17 Windows services are running locally and port 5432 is
  listening. No PostgreSQL CLI or application credentials were available during
  setup, so no database connection or migration was attempted.

## Run locally

Set database and JWT values in the current PowerShell session; use credentials
from your local PostgreSQL setup and keep them out of Git:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=berynx_dev;Username=<local-user>;Password=<local-password>"
$env:Jwt__Issuer = "https://localhost:5001"
$env:Jwt__Audience = "berynx-construction360"
$env:Jwt__SigningKey = "<random-secret-of-at-least-32-UTF8-bytes>"
dotnet run --project backend/src/Berynx.Api --urls http://localhost:5000
```

In a separate terminal:

```powershell
cd frontend
npm install
npm start
```

The Angular dev server is at `http://localhost:4200`; it proxies `/api` and
`/health` to the API at port 5000. The API exposes development-only Swagger at
`/swagger` and an anonymous liveness endpoint at `/health`.

## Verification commands

Run from the repository root after installing the .NET 10 SDK:

```powershell
dotnet restore backend/Berynx.Construction360.slnx
dotnet build backend/Berynx.Construction360.slnx --no-restore
dotnet test backend/Berynx.Construction360.slnx --no-build
```

Run frontend verification from `frontend/`:

```powershell
npm run build
npm test
```

**Status:** Gate 0 scaffolding is in place. Continue to Phase 1 only after
review and approval; begin with P1-M01 per the implementation plan.
