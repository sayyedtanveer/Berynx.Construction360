-- P1-M06 indexes, checks and tenant-safe references.
-- EF migration history is intentionally not stamped until the generated EF migration is reviewed.
BEGIN;

DO $m06$
BEGIN
    IF to_regclass('public.m06_sales_order') IS NULL THEN
        RAISE EXCEPTION 'P1-M06 schema is missing; apply 01-schema.sql first';
    END IF;
    ALTER TABLE m06_sales_order ADD CONSTRAINT "CK_m06_sales_order_status"
        CHECK ("Status" IN ('Draft','Pending approval','Confirmed','In fulfilment','Completed','Short-closed','On hold','Cancelled'));
    ALTER TABLE m06_sales_order ADD CONSTRAINT "CK_m06_sales_order_type"
        CHECK ("OrderType" IN ('RMC','Material','Mixed'));
    ALTER TABLE m06_sales_order ADD CONSTRAINT "CK_m06_sales_order_amounts"
        CHECK ("Subtotal" >= 0 AND "DiscountTotal" >= 0 AND "ChargesTotal" >= 0 AND "TaxTotal" >= 0 AND "Total" >= 0);
    ALTER TABLE m06_sales_order ADD CONSTRAINT "FK_m06_sales_order_m03_party"
        FOREIGN KEY ("TenantId","CustomerPartyId") REFERENCES m03_party ("TenantId","PartyId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order ADD CONSTRAINT "FK_m06_sales_order_m03_site"
        FOREIGN KEY ("TenantId","SiteId") REFERENCES m03_site ("TenantId","SiteId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order ADD CONSTRAINT "FK_m06_sales_order_m05_quotation"
        FOREIGN KEY ("TenantId","SourceQuotationId") REFERENCES m05_quotation ("TenantId","QuotationId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order ADD CONSTRAINT "FK_m06_sales_order_m04_enquiry"
        FOREIGN KEY ("TenantId","SourceEnquiryId") REFERENCES m04_enquiry ("TenantId","EnquiryId") ON DELETE RESTRICT;

    ALTER TABLE m06_sales_order_line ADD CONSTRAINT "CK_m06_sales_order_line_values"
        CHECK ("OrderedQuantity" > 0 AND "Rate" >= 0 AND "DiscountPercent" BETWEEN 0 AND 100 AND "GstPercent" >= 0 AND "DeliveredQuantity" >= 0 AND "InvoicedQuantity" >= 0 AND "PaidAmount" >= 0);
    ALTER TABLE m06_sales_order_line ADD CONSTRAINT "FK_m06_sales_order_line_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order_line ADD CONSTRAINT "FK_m06_sales_order_line_product"
        FOREIGN KEY ("TenantId","ProductId") REFERENCES m03_product ("TenantId","ProductId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order_line ADD CONSTRAINT "FK_m06_sales_order_line_quote_line"
        FOREIGN KEY ("TenantId","SourceQuotationLineId") REFERENCES m05_quotation_line ("TenantId","LineId") ON DELETE RESTRICT;

    ALTER TABLE m06_sales_order_charge ADD CONSTRAINT "CK_m06_sales_order_charge_values"
        CHECK ("Amount" >= 0 AND "TaxRatePercent" >= 0 AND "TaxAmount" >= 0 AND "Total" >= 0);
    ALTER TABLE m06_sales_order_charge ADD CONSTRAINT "FK_m06_sales_order_charge_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_sales_order_charge ADD CONSTRAINT "FK_m06_sales_order_charge_quote_charge"
        FOREIGN KEY ("TenantId","SourceQuotationChargeId") REFERENCES m05_quotation_charge ("TenantId","ChargeId") ON DELETE RESTRICT;

    ALTER TABLE m06_quotation_allocation ADD CONSTRAINT "CK_m06_quotation_allocation_quantity" CHECK ("Quantity" > 0);
    ALTER TABLE m06_quotation_allocation ADD CONSTRAINT "FK_m06_quotation_allocation_quote"
        FOREIGN KEY ("TenantId","QuotationId") REFERENCES m05_quotation ("TenantId","QuotationId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_allocation ADD CONSTRAINT "FK_m06_quotation_allocation_quote_line"
        FOREIGN KEY ("TenantId","QuotationLineId") REFERENCES m05_quotation_line ("TenantId","LineId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_allocation ADD CONSTRAINT "FK_m06_quotation_allocation_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_allocation ADD CONSTRAINT "FK_m06_quotation_allocation_order_line"
        FOREIGN KEY ("TenantId","SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId","LineId") ON DELETE RESTRICT;

    ALTER TABLE m06_quotation_charge_allocation ADD CONSTRAINT "CK_m06_quotation_charge_allocation_values"
        CHECK ("Amount" >= 0 AND "CgstAmount" >= 0 AND "SgstAmount" >= 0 AND "IgstAmount" >= 0 AND "TaxAmount" >= 0);
    ALTER TABLE m06_quotation_charge_allocation ADD CONSTRAINT "FK_m06_quote_charge_allocation_quote"
        FOREIGN KEY ("TenantId","QuotationId") REFERENCES m05_quotation ("TenantId","QuotationId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_charge_allocation ADD CONSTRAINT "FK_m06_quote_charge_allocation_quote_charge"
        FOREIGN KEY ("TenantId","QuotationChargeId") REFERENCES m05_quotation_charge ("TenantId","ChargeId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_charge_allocation ADD CONSTRAINT "FK_m06_quote_charge_allocation_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_quotation_charge_allocation ADD CONSTRAINT "FK_m06_quote_charge_allocation_order_charge"
        FOREIGN KEY ("TenantId","SalesOrderChargeId") REFERENCES m06_sales_order_charge ("TenantId","ChargeId") ON DELETE RESTRICT;

    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "CK_m06_delivery_schedule_quantity" CHECK ("Quantity" > 0);
    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "CK_m06_delivery_schedule_status"
        CHECK ("Status" IN ('Pending approval','Planned','Released','Cancelled'));
    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "FK_m06_delivery_schedule_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "FK_m06_delivery_schedule_line"
        FOREIGN KEY ("TenantId","SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId","LineId") ON DELETE RESTRICT;
    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "FK_m06_delivery_schedule_site"
        FOREIGN KEY ("TenantId","SiteId") REFERENCES m03_site ("TenantId","SiteId") ON DELETE RESTRICT;
    ALTER TABLE m06_delivery_schedule ADD CONSTRAINT "FK_m06_delivery_schedule_plant"
        FOREIGN KEY ("TenantId","PreferredPlantId") REFERENCES m03_plant ("TenantId","PlantId") ON DELETE RESTRICT;

    ALTER TABLE m06_amendment ADD CONSTRAINT "CK_m06_amendment_status"
        CHECK ("Status" IN ('Pending approval','Approved','Rejected','Cancelled'));
    ALTER TABLE m06_amendment ADD CONSTRAINT "CK_m06_amendment_revision" CHECK ("RevisionNumber" > 0);
    ALTER TABLE m06_amendment ADD CONSTRAINT "FK_m06_amendment_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_activity ADD CONSTRAINT "FK_m06_activity_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
    ALTER TABLE m06_linked_document ADD CONSTRAINT "FK_m06_linked_document_order"
        FOREIGN KEY ("TenantId","SalesOrderId") REFERENCES m06_sales_order ("TenantId","SalesOrderId") ON DELETE RESTRICT;
END $m06$;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_Number" ON m06_sales_order ("TenantId","Number");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_IdempotencyKey"
    ON m06_sales_order ("TenantId","IdempotencyKey") WHERE "IdempotencyKey" <> '';
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_Status_OrderDate_BranchId"
    ON m06_sales_order ("TenantId","Status","OrderDate","BranchId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_CustomerPartyId_Status"
    ON m06_sales_order ("TenantId","CustomerPartyId","Status");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_SiteId"
    ON m06_sales_order ("TenantId","SiteId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_SourceQuotationId"
    ON m06_sales_order ("TenantId","SourceQuotationId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_TenantId_SourceEnquiryId"
    ON m06_sales_order ("TenantId","SourceEnquiryId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_line_TenantId_SalesOrderId"
    ON m06_sales_order_line ("TenantId","SalesOrderId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_line_TenantId_ProductId"
    ON m06_sales_order_line ("TenantId","ProductId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_line_TenantId_SourceQuotationLineId"
    ON m06_sales_order_line ("TenantId","SourceQuotationLineId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_charge_TenantId_SalesOrderId"
    ON m06_sales_order_charge ("TenantId","SalesOrderId");
CREATE INDEX IF NOT EXISTS "IX_m06_sales_order_charge_TenantId_SourceQuotationChargeId"
    ON m06_sales_order_charge ("TenantId","SourceQuotationChargeId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_allocation_TenantId_QuotationId"
    ON m06_quotation_allocation ("TenantId","QuotationId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_allocation_TenantId_QuotationLineId"
    ON m06_quotation_allocation ("TenantId","QuotationLineId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_allocation_TenantId_SalesOrderId"
    ON m06_quotation_allocation ("TenantId","SalesOrderId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_quotation_allocation_TenantId_SalesOrderLineId"
    ON m06_quotation_allocation ("TenantId","SalesOrderLineId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_charge_allocation_TenantId_QuotationId"
    ON m06_quotation_charge_allocation ("TenantId","QuotationId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_charge_allocation_TenantId_QuotationChargeId"
    ON m06_quotation_charge_allocation ("TenantId","QuotationChargeId");
CREATE INDEX IF NOT EXISTS "IX_m06_quotation_charge_allocation_TenantId_SalesOrderId"
    ON m06_quotation_charge_allocation ("TenantId","SalesOrderId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_quotation_charge_allocation_TenantId_SalesOrderChargeId"
    ON m06_quotation_charge_allocation ("TenantId","SalesOrderChargeId");
CREATE INDEX IF NOT EXISTS "IX_m06_delivery_schedule_TenantId_SalesOrderId_DeliveryDate"
    ON m06_delivery_schedule ("TenantId","SalesOrderId","DeliveryDate");
CREATE INDEX IF NOT EXISTS "IX_m06_delivery_schedule_TenantId_SalesOrderLineId"
    ON m06_delivery_schedule ("TenantId","SalesOrderLineId");
CREATE INDEX IF NOT EXISTS "IX_m06_delivery_schedule_TenantId_SiteId"
    ON m06_delivery_schedule ("TenantId","SiteId");
CREATE INDEX IF NOT EXISTS "IX_m06_delivery_schedule_TenantId_PreferredPlantId"
    ON m06_delivery_schedule ("TenantId","PreferredPlantId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_amendment_TenantId_SalesOrderId_RevisionNumber"
    ON m06_amendment ("TenantId","SalesOrderId","RevisionNumber");
CREATE INDEX IF NOT EXISTS "IX_m06_activity_TenantId_SalesOrderId_At"
    ON m06_activity ("TenantId","SalesOrderId","At");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_m06_linked_document_TenantId_Order_Module_Document"
    ON m06_linked_document ("TenantId","SalesOrderId","SourceModule","DocumentId");

COMMIT;
