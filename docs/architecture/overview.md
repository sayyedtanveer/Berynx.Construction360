# Architecture Overview

**Status:** Proposed technical guidance, not already-implemented code. Master BRD v2.2 controls business behavior.

## Stack
- Backend ASP.NET Core Web API / C#, PostgreSQL and EF Core.
- Frontend Angular / TypeScript, responsive web/PWA.
- JWT authentication and server-side RBAC/branch/record scope.
- Modular monolith, DDD domain boundaries and CQRS when useful.
- Prefer .NET 10 LTS if available; select Angular/Node versions only after verifying compatibility. Record actual versions.

## Dependencies
- Domain: aggregates, entities, value objects, invariants, events; independent of EF and HTTP.
- Application → Domain: use cases, commands/queries, validators and abstractions.
- Infrastructure → Application/Domain: PostgreSQL/EF Core, adapters, transactional outbox.
- API: composition root, controllers/endpoints, JWT, permissions, OpenAPI, errors.
- Angular: feature modules, forms, typed API calls; authoritative calculations remain on backend.

## Non-negotiable contracts
1. Tenant ID comes from authenticated server context, not untrusted client input; enforce tenant isolation on all reads and writes and test it.
2. Permission checks must cover APIs, exports, search and cost/margin visibility; frontend hiding alone is not security.
3. Money/quantity rates follow BRD decimal precision; do not use floating point for source-of-truth totals.
4. State transitions are explicit and audited; posted financial records cannot be silently edited.
5. Document conversion retains provenance and approved snapshots; ledgers derive from deterministic transactional posting.
6. Retries/concurrent operations need idempotency, optimistic concurrency, unique constraints and safe reversal.
7. Standard error responses and trace IDs, structured logs, OpenAPI, health checks and secret-free configuration.
8. Events and external provider communication go through reliable outbox/retry mechanisms as applicable.
9. Phase 1 GRN/periodic inventory and Phase 2 perpetual stock valuation require a documented tested cutover.
10. Indian compliance rules and thresholds must remain configurable and validated with the tenant's CA.

## Runtime separation
Backend owns persistence and APIs. Frontend consumes HTTP APIs with development proxy or explicit CORS. Each child has its own packages, build and launch commands, but both can share the top-level Git repo.
