# Berynx Construction 360 — Backend

Independent ASP.NET Core Web API solution targeting `net10.0`. The solution is
a modular-monolith scaffold only; it contains no ERP module, business entity,
or database migration.

## Project references

```text
Berynx.Domain
    ↑
Berynx.Application
    ↑                 ↑
Berynx.Infrastructure ┘
    ↑
Berynx.Api
```

The API is the composition root and references Application and Infrastructure.
Infrastructure references Application and Domain. Domain has no project
dependencies. Unit and integration test projects are kept separate.

## Configure and run

Set `ConnectionStrings__DefaultConnection` and JWT settings in the process
environment; `backend/.env.example` documents their names without containing
credentials. The API requires a connection string at startup and validates
JWT signatures using a configured key. With no key, authenticated requests
fail closed. No login or token-issuing endpoint exists yet.

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=berynx_dev;Username=<local-user>;Password=<local-password>"
$env:Jwt__Issuer = "https://localhost:5001"
$env:Jwt__Audience = "berynx-construction360"
$env:Jwt__SigningKey = "<random-secret-of-at-least-32-UTF8-bytes>"
dotnet run --project backend/src/Berynx.Api --urls http://localhost:5000
```

`GET /health` is an anonymous liveness check. `/api/v1` is the API path-version
convention. Swagger is exposed only in Development. Development CORS permits
only `http://localhost:4200`; no production wildcard policy is registered.

## Migrations

EF Core migrations belong to `Berynx.Infrastructure`; the API is the startup
project and `BerynxDbContext` is the migration context. From the repository
root, after installing .NET 10 and setting the database environment variable:

```powershell
cd backend
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/Berynx.Infrastructure --startup-project src/Berynx.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Berynx.Infrastructure --startup-project src/Berynx.Api
```

Do not create or apply a migration until an approved module requires a schema
change. No migration was created or applied during Gate 0.

## Tests

```powershell
dotnet test backend/Berynx.Construction360.slnx
```

The unit tests cover trusted tenant-claim resolution. The integration-test
project hosts the API in-process and checks the liveness endpoint without
opening a database connection. PostgreSQL-backed persistence tests belong with
the first module that adds mapped business data.
