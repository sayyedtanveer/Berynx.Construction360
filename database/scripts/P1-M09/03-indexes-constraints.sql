BEGIN;
CREATE INDEX ix_m09_challan_status_date
    ON m09_delivery_challan ("TenantId","Status","ChallanDate");
CREATE INDEX ix_m09_challan_customer_site_age
    ON m09_delivery_challan ("TenantId","CustomerPartyId","SiteId","ChallanDate");
CREATE INDEX ix_m09_challan_order_line
    ON m09_delivery_challan ("TenantId","SalesOrderId","SalesOrderLineId");
CREATE INDEX ix_m09_challan_revision_status
    ON m09_challan_revision ("TenantId","Status","CreatedAt");
CREATE INDEX ix_m09_challan_event_history
    ON m09_challan_event ("TenantId","ChallanId","OccurredAt");
COMMIT;
