-- Guarded rollback for 52 tables created by 20261126104710_P1M06M17OperationalFoundation.
-- Refuses to proceed if the migration is not recorded or any new table contains data.
BEGIN;
DO $rollback_guard$
DECLARE
    table_name text;
    has_rows boolean;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation'
    ) THEN
        RAISE EXCEPTION 'Rollback blocked: migration is not recorded in __EFMigrationsHistory';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
    'm06_activity',
    'm06_amendment',
    'm06_delivery_schedule',
    'm06_linked_document',
    'm06_quotation_allocation',
    'm06_quotation_charge_allocation',
    'm06_sales_order',
    'm06_sales_order_charge',
    'm06_sales_order_line',
    'm07_activity',
    'm07_goods_receipt',
    'm07_goods_receipt_line',
    'm07_number_counter',
    'm07_purchase_order',
    'm07_purchase_order_charge',
    'm07_purchase_order_line',
    'm07_supplier_invoice',
    'm07_supplier_invoice_line',
    'm07_supplier_payment',
    'm07_supplier_payment_allocation',
    'm08_driver_action',
    'm08_pour',
    'm08_receiver_challenge',
    'm08_trip',
    'm08_trip_event',
    'm09_challan_event',
    'm09_challan_number_counter',
    'm09_challan_revision',
    'm09_delivery_challan',
    'm10_invoice',
    'm10_invoice_challan',
    'm10_invoice_event',
    'm10_invoice_line',
    'm11_promise_to_pay',
    'm11_receipt',
    'm11_receipt_allocation',
    'm11_receivable_event',
    'm12_financial_period',
    'm12_period_event',
    'm12_voucher',
    'm12_voucher_event',
    'm12_voucher_line',
    'm12_voucher_number_counter',
    'm13_exception_review',
    'm13_gst_period',
    'm14_expense',
    'm14_expense_category',
    'm14_expense_event',
    'm16_export_log',
    'm17_catalogue_item',
    'm17_consent_record',
    'm17_promotion_event'
    ] LOOP
        IF to_regclass(format('%I.%I', current_schema(), table_name)) IS NOT NULL THEN
            EXECUTE format(
                'SELECT EXISTS (SELECT 1 FROM %I.%I LIMIT 1)',
                current_schema(), table_name
            ) INTO has_rows;
            IF has_rows THEN
                RAISE EXCEPTION 'Rollback blocked: table % contains data', table_name;
            END IF;
        END IF;
    END LOOP;
END;
$rollback_guard$;
DROP TABLE m06_activity;

DROP TABLE m06_amendment;

DROP TABLE m06_linked_document;

DROP TABLE m06_quotation_allocation;

DROP TABLE m06_quotation_charge_allocation;

DROP TABLE m07_activity;

DROP TABLE m07_number_counter;

DROP TABLE m07_purchase_order_charge;

DROP TABLE m07_supplier_invoice_line;

DROP TABLE m07_supplier_payment_allocation;

DROP TABLE m08_driver_action;

DROP TABLE m08_receiver_challenge;

DROP TABLE m08_trip_event;

DROP TABLE m09_challan_event;

DROP TABLE m09_challan_number_counter;

DROP TABLE m09_challan_revision;

DROP TABLE m10_invoice_challan;

DROP TABLE m10_invoice_event;

DROP TABLE m10_invoice_line;

DROP TABLE m11_promise_to_pay;

DROP TABLE m11_receipt_allocation;

DROP TABLE m11_receivable_event;

DROP TABLE m12_financial_period;

DROP TABLE m12_period_event;

DROP TABLE m12_voucher_event;

DROP TABLE m12_voucher_line;

DROP TABLE m12_voucher_number_counter;

DROP TABLE m13_exception_review;

DROP TABLE m13_gst_period;

DROP TABLE m14_expense_event;

DROP TABLE m16_export_log;

DROP TABLE m17_catalogue_item;

DROP TABLE m17_consent_record;

DROP TABLE m17_promotion_event;

DROP TABLE m06_sales_order_charge;

DROP TABLE m07_goods_receipt_line;

DROP TABLE m07_supplier_invoice;

DROP TABLE m07_supplier_payment;

DROP TABLE m09_delivery_challan;

DROP TABLE m11_receipt;

DROP TABLE m10_invoice;

DROP TABLE m14_expense;

DROP TABLE m07_goods_receipt;

DROP TABLE m07_purchase_order_line;

DROP TABLE m08_trip;

DROP TABLE m12_voucher;

DROP TABLE m14_expense_category;

DROP TABLE m07_purchase_order;

DROP TABLE m08_pour;

DROP TABLE m06_delivery_schedule;

DROP TABLE m06_sales_order_line;

DROP TABLE m06_sales_order;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261126104710_P1M06M17OperationalFoundation';


COMMIT;
