# Berynx Construction 360

Multi-tenant SaaS construction ERP, India-first. First pilot: HAFA INFRA (per BRD).

## Source documents
- `docs/requirements/BRD_v2.2.md`: complete master business requirements baseline, including Phase 1 modules and Phase 2–5 epics.
- `AGENTS.md`: coding-agent instructions.
- `docs/architecture/overview.md`: proposed architecture contracts.
- `docs/implementation-plan.md`: implementation gates and phase ordering.
- `docs/decisions.md`: proposals, decisions and unresolved questions.
- `docs/testing/strategy.md`: testing expectations.

## Intended project structure (not yet scaffolded)

```text
Berynx.Construction360/
├── backend/                     # separate .NET solution and tests
│   ├── src/
│   │   ├── Berynx.Api/
│   │   ├── Berynx.Application/
│   │   ├── Berynx.Domain/
│   │   └── Berynx.Infrastructure/
│   └── tests/
├── frontend/                    # independent Angular workspace
│   ├── angular.json
│   └── src/
├── docs/
├── AGENTS.md
└── README.md
```

Open repository root for both applications, `backend/` for .NET only, or `frontend/` for Angular only. Keep one root Git repository initially. The agent should initialize buildable skeletons and stop before implementing Phase 1.

**Status:** documentation starter only; no app code, build, testing or DB provisioned yet.
