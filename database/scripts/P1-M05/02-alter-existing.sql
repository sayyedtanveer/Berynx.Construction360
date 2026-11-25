-- M05 existing table alterations
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

-- ============================================================================
-- REVIEW NOTES (not executed; for manual deployment planning only):
-- ============================================================================
-- 
-- 1. M03 PriceBasis columns: Already handled by P1-M04/02-alter-existing.sql.
--    No duplication needed here.
--
-- 2. M04 Enquiry → M05 Quotation navigation:
--    - M05.quotation has SourceEnquiryId to track which enquiry spawned it.
--    - M04.enquiry has LinkedSalesOrderId but NO reverse tracking for quotations.
--    - M04 Detail endpoint does not query or return related quotations.
--    - BRD lifecycle: Enquiry → Quotation → Sales Order
--
--    DECISION: No ALTER added pending clarification:
--    - If one enquiry can spawn multiple quotations (revisions, alternatives),
--      tracking requires either a JSONB array or a separate junction table.
--    - If quotations are query-only (JOIN on SourceEnquiryId), no ALTER needed.
--    - Current implementation: query-based, no M04 schema change required.
--
-- 3. EF Migration 20261125092036_P1M04M05Foundation does not include any
--    M04 enquiry alterations, confirming the query-based relationship design.
--
-- ============================================================================

-- No existing table alterations are required for M05.
-- M03 pricing alterations are delivered by P1-M04/02-alter-existing.sql.

COMMIT;
