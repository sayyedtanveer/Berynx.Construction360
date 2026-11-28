# P1-M15 Dashboard and Home

No module tables or EF migration are needed in this partial increment; the dashboard reads persisted M06/M08/M09/M10/M11/M14 source records. A manual SQL package is retained for module/permission registration. Do not activate for tenants until the source-module migrations are installed.

Implemented source counts/totals are permission- and branch/own-scoped. Expense costs additionally require intersecting `global.view_cost` scope. Cash/bank balances, payables, profit, trends, card preferences, role-specific card layouts, live refresh, setup checklist, full alerts and performance budgets are unavailable. No totals are fabricated for those sections.

Manual order: verify M06/M08/M09/M10/M11/M14 source migrations and SQL first, then execute `04-seed.sql`, then `05-verification.sql`. Other scripts are explanatory no-ops. No database operation occurred.
