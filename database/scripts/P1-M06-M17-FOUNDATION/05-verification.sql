-- Read-only verification for migration 20261126104710_P1M06M17OperationalFoundation.
-- Run only after manual execution of 01-apply.sql and the applicable module seed scripts.

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation';

WITH expected(table_name) AS (VALUES
    ('m06_activity'),
    ('m06_amendment'),
    ('m06_delivery_schedule'),
    ('m06_linked_document'),
    ('m06_quotation_allocation'),
    ('m06_quotation_charge_allocation'),
    ('m06_sales_order'),
    ('m06_sales_order_charge'),
    ('m06_sales_order_line'),
    ('m07_activity'),
    ('m07_goods_receipt'),
    ('m07_goods_receipt_line'),
    ('m07_number_counter'),
    ('m07_purchase_order'),
    ('m07_purchase_order_charge'),
    ('m07_purchase_order_line'),
    ('m07_supplier_invoice'),
    ('m07_supplier_invoice_line'),
    ('m07_supplier_payment'),
    ('m07_supplier_payment_allocation'),
    ('m08_driver_action'),
    ('m08_pour'),
    ('m08_receiver_challenge'),
    ('m08_trip'),
    ('m08_trip_event'),
    ('m09_challan_event'),
    ('m09_challan_number_counter'),
    ('m09_challan_revision'),
    ('m09_delivery_challan'),
    ('m10_invoice'),
    ('m10_invoice_challan'),
    ('m10_invoice_event'),
    ('m10_invoice_line'),
    ('m11_promise_to_pay'),
    ('m11_receipt'),
    ('m11_receipt_allocation'),
    ('m11_receivable_event'),
    ('m12_financial_period'),
    ('m12_period_event'),
    ('m12_voucher'),
    ('m12_voucher_event'),
    ('m12_voucher_line'),
    ('m12_voucher_number_counter'),
    ('m13_exception_review'),
    ('m13_gst_period'),
    ('m14_expense'),
    ('m14_expense_category'),
    ('m14_expense_event'),
    ('m16_export_log'),
    ('m17_catalogue_item'),
    ('m17_consent_record'),
    ('m17_promotion_event')
)
SELECT expected.table_name,
       to_regclass(format('%I.%I', current_schema(), expected.table_name)) IS NOT NULL AS exists_in_current_schema
FROM expected
ORDER BY expected.table_name;

WITH expected(table_name) AS (VALUES
    ('m06_activity'),
    ('m06_amendment'),
    ('m06_delivery_schedule'),
    ('m06_linked_document'),
    ('m06_quotation_allocation'),
    ('m06_quotation_charge_allocation'),
    ('m06_sales_order'),
    ('m06_sales_order_charge'),
    ('m06_sales_order_line'),
    ('m07_activity'),
    ('m07_goods_receipt'),
    ('m07_goods_receipt_line'),
    ('m07_number_counter'),
    ('m07_purchase_order'),
    ('m07_purchase_order_charge'),
    ('m07_purchase_order_line'),
    ('m07_supplier_invoice'),
    ('m07_supplier_invoice_line'),
    ('m07_supplier_payment'),
    ('m07_supplier_payment_allocation'),
    ('m08_driver_action'),
    ('m08_pour'),
    ('m08_receiver_challenge'),
    ('m08_trip'),
    ('m08_trip_event'),
    ('m09_challan_event'),
    ('m09_challan_number_counter'),
    ('m09_challan_revision'),
    ('m09_delivery_challan'),
    ('m10_invoice'),
    ('m10_invoice_challan'),
    ('m10_invoice_event'),
    ('m10_invoice_line'),
    ('m11_promise_to_pay'),
    ('m11_receipt'),
    ('m11_receipt_allocation'),
    ('m11_receivable_event'),
    ('m12_financial_period'),
    ('m12_period_event'),
    ('m12_voucher'),
    ('m12_voucher_event'),
    ('m12_voucher_line'),
    ('m12_voucher_number_counter'),
    ('m13_exception_review'),
    ('m13_gst_period'),
    ('m14_expense'),
    ('m14_expense_category'),
    ('m14_expense_event'),
    ('m16_export_log'),
    ('m17_catalogue_item'),
    ('m17_consent_record'),
    ('m17_promotion_event')
)
SELECT count(*) FILTER (
           WHERE to_regclass(format('%I.%I', current_schema(), table_name)) IS NOT NULL
       ) AS expected_tables_found,
       count(*) AS expected_tables_total
FROM expected;

SELECT count(*) AS expected_indexes_found
FROM pg_indexes
WHERE schemaname = current_schema()
  AND indexname LIKE 'IX_m06_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m07_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m08_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m09_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m10_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m11_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m12_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m13_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m14_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m16_%'
   OR schemaname = current_schema()
  AND indexname LIKE 'IX_m17_%';