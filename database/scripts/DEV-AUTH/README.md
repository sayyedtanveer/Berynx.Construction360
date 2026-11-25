# Local development authentication fixture

This package provisions M02 role templates and, only when the database has no tenant rows, a clearly labelled local-only tenant with one branch. It does not insert Identity users, password hashes, refresh tokens, MFA secrets, or production credentials.

## Required migration and seed order

Do not use these scripts to replace EF migration deployment. Apply the reviewed module scripts manually, in this order:

1. Apply P1-M01 schema from the user's verified baseline, using `database/scripts/P1-M01/README.md`. Do not rerun M01 migrations already recorded in `__EFMigrationsHistory`.
2. Apply P1-M02 schema/auth alterations from `database/scripts/P1-M02/README.md`.
3. Run `database/scripts/P1-M02/04-seed.sql`.
4. Apply P1-M03 schema and alterations from `database/scripts/P1-M03/README.md`.
5. Run `database/scripts/P1-M03/04-seed.sql` so the M03 permission catalog exists.
6. Run `database/scripts/DEV-AUTH/01-prerequisites.sql`.
7. Run `database/scripts/DEV-AUTH/02-roles-permissions.sql`.
8. Run `database/scripts/DEV-AUTH/03-development-tenant.sql`.
9. Run `database/scripts/DEV-AUTH/04-verification.sql`, choose the intended tenant ID, and make sure a branch is configured.
10. Run the local Identity provisioning command below.

Each P1 module's `99-rollback.sql` is guarded and is not part of deployment. Never run rollback scripts as a cleanup shortcut.

## Secure local Identity provisioning

From the repository root, in an interactive PowerShell terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:BERYNX_ENABLE_DEV_AUTH_PROVISIONING = '1'
$env:BERYNX_DEV_TENANT_ID = '<TenantId selected from 04-verification.sql>'
dotnet run --project backend/tools/Berynx.DevAuthProvisioner/Berynx.DevAuthProvisioner.csproj --configuration Release
```

When `ConnectionStrings__DefaultConnection` is not present, the tool prompts for it with input hidden. It refuses all environments except `Development`, requires the explicit enable flag, and refuses any host other than localhost or any database other than `berynx_construction_dev`. It does not run migrations or DDL. The normal provisioning mode creates the eight deterministic development identities through ASP.NET Core `UserManager`, prompting for new passwords without echo. Normal provisioning never resets existing passwords or silently activates accounts. M02 memberships and role assignments are inserted idempotently; inactive or conflicting memberships stop the run.

### Test Forgot Password without email

For local Forgot Password UI testing, start the API with `ASPNETCORE_ENVIRONMENT=Development` and `BERYNX_ENABLE_LOCAL_PASSWORD_RESET=1`, then access it only through loopback (`localhost`, `127.0.0.1`, or `::1`). Only confirmed `@berynx.local` accounts qualify. The API returns a one-time reset URL to the local browser and the UI displays **Open local development reset form**; the URL is not logged or emailed. Its default return URL is `http://localhost:4200/m02/login`. An override must remain loopback on port 4200 at `/m02/login`. Leave the flag unset outside local testing; the normal Forgot Password flow continues to require configured email delivery.

### Reset one fixture password directly without email

For local testing, the same guarded tool also supports an explicit, interactive password reset for one of the documented fixture identities:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:BERYNX_ENABLE_DEV_AUTH_PROVISIONING = '1'
dotnet run --project backend/tools/Berynx.DevAuthProvisioner/Berynx.DevAuthProvisioner.csproj --configuration Release -- --reset-password platform.admin@berynx.local
```

The command still requires a localhost connection to `berynx_construction_dev`; set `ConnectionStrings__DefaultConnection` locally or enter it at the hidden prompt. It accepts only the documented fixture emails, requires an existing confirmed Identity account with active tenant membership, prompts twice for a new password without echo, and applies it through ASP.NET Core Identity's reset-token/password validation APIs. It updates the security stamp, revokes active sessions and refresh tokens, and writes an audit event for each tenant membership. It prints neither the password nor reset token. Use `--reset-password <fixture-email>` explicitly; ordinary provisioning never resets passwords.

Emails and role mappings:

| Local email | M02 role | Additional treatment |
|---|---|---|
| `platform.admin@berynx.local` | `TenantAdmin` | Global `PlatformAdmin` only when its printed identity ID is explicitly configured server-side |
| `tenant.admin@berynx.local` | `TenantAdmin` | All-tenant scope |
| `branch.manager@berynx.local` | `BranchManager` | One M02 assignment per configured branch |
| `sales.manager@berynx.local` | `SalesManager` | All-tenant scope |
| `procurement.manager@berynx.local` | `ProcurementManager` | All-tenant scope |
| `fleet.manager@berynx.local` | `FleetManager` | All-tenant scope |
| `accountant@berynx.local` | `Accountant` | Existing M02 system role |
| `read.only@berynx.local` | `ReadOnly` | No write grants |

The tool prints `PlatformAdmin__IdentityUserIds__0=<guid>` after provisioning the platform account. Supply that value through local backend environment configuration (or another trusted server-side secret/configuration provider) before starting the API. Never put a browser-provided value, password, credential hash, or this local bootstrap setting in committed source. Platform entitlement is issued only by the backend for IDs in this trusted allowlist; an ordinary tenant role cannot grant it.

The platform fixture also has an active tenant membership because this version's session model is tenant-scoped. The local `PlatformAdmin` role claim authorizes only platform routes on the server; it is not inferred from the user's M02 tenant role.

## Test login

Start the backend in `Development` with the manually supplied local connection string, JWT settings, and the server-side platform identity ID if required. Start Angular with `cd frontend; npm start`, open `http://localhost:4200`, and sign in with one of the emails above and the password entered for it during provisioning. MFA is not disabled; if enabled for an account, complete the authenticator challenge.

No database was contacted by this package authoring task. The owner must execute the SQL manually and verify login against the local application.
