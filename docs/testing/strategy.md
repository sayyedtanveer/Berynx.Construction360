# Test Strategy

Master BRD v2.2 contains the acceptance criteria. Test each implemented story and record evidence.

- **Unit:** domain invariants, status machines, decimals/rounding, validation.
- **Application:** authorization, orchestration, approvals, business calculations, idempotency.
- **PostgreSQL integration:** actual EF mappings/migrations, constraints, transactions, tenant filters.
- **API:** JWT roles/scopes, contract/error shapes, concurrency and filters.
- **Frontend:** Angular forms, status/error/loading/permission states, responsive accessibility.
- **End to end:** enquiry → quotation → order → PO/GRN → dispatch → challan → invoice → receipt → ledger/GST/reports.

## Required negative tests
Cross-tenant access, missing cost-view permission, duplicate simultaneous posting, FY-period lock, unauthorized reversals, missing/late offline action retries, conflicting order/stock quantities, inconsistent ledger/dashboard totals.

For each module: list test command, environment, passed/failed/skipped and specific blocker. Never claim verification without execution.
