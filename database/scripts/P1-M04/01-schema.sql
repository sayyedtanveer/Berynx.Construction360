-- P1-M04 table creation
-- Generated from EF migration 20261125092036_P1M04M05Foundation.
-- Do not apply the P1-M05 schema package before P1-M04.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_assignment_cursor (
        "TenantId" uuid NOT NULL,
        "Source" character varying(24) NOT NULL,
        "LastOwnerUserId" uuid,
        CONSTRAINT "PK_m04_assignment_cursor" PRIMARY KEY ("TenantId", "Source")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_assignment_rule (
        "TenantId" uuid NOT NULL,
        "RuleId" uuid NOT NULL,
        "Source" character varying(24) NOT NULL,
        "RegionStateCode" character varying(2),
        "OwnerUserId" uuid NOT NULL,
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m04_assignment_rule" PRIMARY KEY ("TenantId", "RuleId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_enquiry (
        "TenantId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "Number" character varying(32) NOT NULL,
        "Source" character varying(24) NOT NULL,
        "ContactId" uuid,
        "CustomerPartyId" uuid,
        "SiteId" uuid,
        "BranchId" uuid,
        "LinkedSalesOrderId" uuid,
        "OwnerUserId" uuid NOT NULL,
        "Status" character varying(24) NOT NULL,
        "Priority" character varying(16) NOT NULL,
        "RequiredBy" date,
        "ExpectedValue" numeric(18,2),
        "NextActionAt" timestamp with time zone,
        "FirstResponseDueAt" timestamp with time zone NOT NULL,
        "FirstRespondedAt" timestamp with time zone,
        "LostReason" character varying(24),
        "Notes" character varying(4000) NOT NULL,
        "IdempotencyKey" character varying(80) NOT NULL,
        "Version" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m04_enquiry" PRIMARY KEY ("TenantId", "EnquiryId"),
        CONSTRAINT "CK_m04_enquiry_open_followup" CHECK ("Status" IN ('Won','Lost') OR "NextActionAt" IS NOT NULL),
        CONSTRAINT "CK_m04_enquiry_source" CHECK ("Source" IN ('Phone','WalkIn','WhatsApp','Referral','SiteVisit','Website','Campaign','Repeat')),
        CONSTRAINT "CK_m04_enquiry_status" CHECK ("Status" IN ('New','Contacted','Follow-up','Quotation Sent','Negotiation','Won','Lost','On hold')),
        CONSTRAINT "CK_m04_enquiry_won_order" CHECK ("Status" <> 'Won' OR "LinkedSalesOrderId" IS NOT NULL),
        CONSTRAINT "FK_m04_enquiry_m03_contact_TenantId_ContactId" FOREIGN KEY ("TenantId", "ContactId") REFERENCES m03_contact ("TenantId", "ContactId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m04_enquiry_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m04_enquiry_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_number_counter (
        "TenantId" uuid NOT NULL,
        "DocumentCode" character varying(24) NOT NULL,
        "CalendarYear" integer NOT NULL,
        "NextNumber" bigint NOT NULL,
        CONSTRAINT "PK_m04_number_counter" PRIMARY KEY ("TenantId", "DocumentCode", "CalendarYear"),
        CONSTRAINT "CK_m04_number_counter_values" CHECK ("CalendarYear" BETWEEN 2000 AND 2200 AND "NextNumber" > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_assignment_audit (
        "TenantId" uuid NOT NULL,
        "AssignmentAuditId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "PreviousOwnerUserId" uuid NOT NULL,
        "NewOwnerUserId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "At" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m04_assignment_audit" PRIMARY KEY ("TenantId", "AssignmentAuditId"),
        CONSTRAINT "FK_m04_assignment_audit_m04_enquiry_TenantId_EnquiryId" FOREIGN KEY ("TenantId", "EnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_enquiry_activity (
        "TenantId" uuid NOT NULL,
        "ActivityId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "ActorUserId" uuid,
        "Kind" character varying(24) NOT NULL,
        "Channel" character varying(24) NOT NULL,
        "Outcome" character varying(500) NOT NULL,
        "Notes" character varying(4000) NOT NULL,
        "At" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m04_enquiry_activity" PRIMARY KEY ("TenantId", "ActivityId"),
        CONSTRAINT "FK_m04_enquiry_activity_m04_enquiry_TenantId_EnquiryId" FOREIGN KEY ("TenantId", "EnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_enquiry_line (
        "TenantId" uuid NOT NULL,
        "LineId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "UnitCode" character varying(16) NOT NULL,
        "RmcGrade" text,
        "RmcSlumpBand" text,
        "RmcAdmixtureCode" text,
        "RmcPumpType" text,
        CONSTRAINT "PK_m04_enquiry_line" PRIMARY KEY ("TenantId", "LineId"),
        CONSTRAINT "CK_m04_enquiry_line_quantity" CHECK ("Quantity" > 0),
        CONSTRAINT "FK_m04_enquiry_line_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m04_enquiry_line_m04_enquiry_TenantId_EnquiryId" FOREIGN KEY ("TenantId", "EnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_follow_up (
        "TenantId" uuid NOT NULL,
        "FollowUpId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "OwnerUserId" uuid NOT NULL,
        "DueAt" timestamp with time zone NOT NULL,
        "Action" character varying(24) NOT NULL,
        "Status" character varying(16) NOT NULL,
        "CompletedAt" timestamp with time zone,
        "Outcome" character varying(1000),
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m04_follow_up" PRIMARY KEY ("TenantId", "FollowUpId"),
        CONSTRAINT "CK_m04_follow_up_status" CHECK ("Status" IN ('Pending','Completed','Cancelled')),
        CONSTRAINT "FK_m04_follow_up_m04_enquiry_TenantId_EnquiryId" FOREIGN KEY ("TenantId", "EnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261125092036_P1M04M05Foundation') THEN
    CREATE TABLE m04_sla_alert (
        "TenantId" uuid NOT NULL,
        "EnquiryId" uuid NOT NULL,
        "AlertedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m04_sla_alert" PRIMARY KEY ("TenantId", "EnquiryId"),
        CONSTRAINT "FK_m04_sla_alert_m04_enquiry_TenantId_EnquiryId" FOREIGN KEY ("TenantId", "EnquiryId") REFERENCES m04_enquiry ("TenantId", "EnquiryId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

COMMIT;
