# P1-M11 Receipts, Payments, Notes & Collections — draft workflow package

**Not deployment-ready:** EF migration and snapshot are absent. Scripts are draft only.

Dependencies: M01–M03 tenant/party/branch authorization, M10 invoice tables, and ultimately M12 accounting/posting. M10 currently cannot post invoices, so open posted invoice balances are empty. M11 draft receipts do not reduce receivables or credit exposure; only posted M11 transactions will affect ledgers.

Implemented draft capture and allocation validation only. Supplier payments, credit/debit notes, TDS/TCS, retention, cheque/PDC, refunds, bank reconciliation, write-offs, reminders and statements remain pending. Apply only after a generated migration has been reviewed for parity; no SQL or database access has occurred.
