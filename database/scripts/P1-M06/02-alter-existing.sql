-- P1-M06 does not alter columns on earlier modules.
-- The M04 LinkedSalesOrderId handoff column already exists in P1-M04.
-- This file is retained as a separate ordered deployment step for auditability.
BEGIN;
SELECT 1;
COMMIT;
