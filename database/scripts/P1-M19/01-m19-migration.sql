START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_quality_criteria (
        "TenantId" uuid NOT NULL,
        "CriteriaId" uuid NOT NULL,
        "MaterialType" character varying(24) NOT NULL,
        "Grade" character varying(80) NOT NULL,
        "TestAgeDays" integer NOT NULL,
        "MinimumStrengthMpa" numeric(9,2) NOT NULL,
        "StandardReference" character varying(240) NOT NULL,
        "EffectiveFrom" date NOT NULL,
        "EffectiveUntil" date,
        "ChangeReason" character varying(500) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m19_quality_criteria" PRIMARY KEY ("TenantId", "CriteriaId"),
        CONSTRAINT "CK_m19_criteria_age" CHECK ("TestAgeDays" BETWEEN 1 AND 365),
        CONSTRAINT "CK_m19_criteria_effective" CHECK ("EffectiveUntil" IS NULL OR "EffectiveUntil" >= "EffectiveFrom"),
        CONSTRAINT "CK_m19_criteria_strength" CHECK ("MinimumStrengthMpa" > 0),
        CONSTRAINT "FK_m19_quality_criteria_m02_user_TenantId_CreatedByUserId" FOREIGN KEY ("TenantId", "CreatedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_quality_event (
        "TenantId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "EntityType" character varying(24) NOT NULL,
        "EntityId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "EventType" character varying(48) NOT NULL,
        "DetailJson" jsonb NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m19_quality_event" PRIMARY KEY ("TenantId", "EventId"),
        CONSTRAINT "FK_m19_quality_event_m02_user_TenantId_ActorUserId" FOREIGN KEY ("TenantId", "ActorUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_quality_sample (
        "TenantId" uuid NOT NULL,
        "SampleId" uuid NOT NULL,
        "IdempotencyKey" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "TripId" uuid NOT NULL,
        "PourId" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "SalesOrderLineId" uuid NOT NULL,
        "SiteId" uuid,
        "ProductId" uuid NOT NULL,
        "SampleNumber" character varying(32) NOT NULL,
        "MaterialType" character varying(24) NOT NULL,
        "GradeSnapshot" character varying(80) NOT NULL,
        "CastAt" timestamp with time zone NOT NULL,
        "CubeCount" integer NOT NULL,
        "OwnerUserId" uuid NOT NULL,
        "Status" character varying(24) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m19_quality_sample" PRIMARY KEY ("TenantId", "SampleId"),
        CONSTRAINT "CK_m19_sample_cube_count" CHECK ("CubeCount" IN (3, 6)),
        CONSTRAINT "FK_m19_quality_sample_m02_user_TenantId_CreatedByUserId" FOREIGN KEY ("TenantId", "CreatedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m02_user_TenantId_OwnerUserId" FOREIGN KEY ("TenantId", "OwnerUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m03_product_TenantId_ProductId" FOREIGN KEY ("TenantId", "ProductId") REFERENCES m03_product ("TenantId", "ProductId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m06_sales_order_line_TenantId_SalesOrder~" FOREIGN KEY ("TenantId", "SalesOrderLineId") REFERENCES m06_sales_order_line ("TenantId", "LineId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m08_pour_TenantId_PourId" FOREIGN KEY ("TenantId", "PourId") REFERENCES m08_pour ("TenantId", "PourId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_sample_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_dispute (
        "TenantId" uuid NOT NULL,
        "DisputeId" uuid NOT NULL,
        "IdempotencyKey" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "CustomerPartyId" uuid,
        "SupplierPartyId" uuid,
        "SiteId" uuid,
        "SalesOrderId" uuid,
        "TripId" uuid,
        "ChallanId" uuid,
        "SampleId" uuid,
        "Type" character varying(24) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "OwnerUserId" uuid NOT NULL,
        "Status" character varying(24) NOT NULL,
        "Resolution" character varying(500) NOT NULL,
        "OpenedAt" timestamp with time zone NOT NULL,
        "OpenedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "ResolvedByUserId" uuid,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m19_dispute" PRIMARY KEY ("TenantId", "DisputeId"),
        CONSTRAINT "CK_m19_dispute_source" CHECK ("SalesOrderId" IS NOT NULL OR "TripId" IS NOT NULL OR "ChallanId" IS NOT NULL OR "SampleId" IS NOT NULL OR "CustomerPartyId" IS NOT NULL OR "SupplierPartyId" IS NOT NULL),
        CONSTRAINT "CK_m19_dispute_status" CHECK ("Status" IN ('Open', 'Investigating', 'Resolved', 'Escalated', 'Rejected')),
        CONSTRAINT "FK_m19_dispute_m02_user_TenantId_OpenedByUserId" FOREIGN KEY ("TenantId", "OpenedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m02_user_TenantId_OwnerUserId" FOREIGN KEY ("TenantId", "OwnerUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m02_user_TenantId_ResolvedByUserId" FOREIGN KEY ("TenantId", "ResolvedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m03_party_TenantId_CustomerPartyId" FOREIGN KEY ("TenantId", "CustomerPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m03_party_TenantId_SupplierPartyId" FOREIGN KEY ("TenantId", "SupplierPartyId") REFERENCES m03_party ("TenantId", "PartyId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m03_site_TenantId_SiteId" FOREIGN KEY ("TenantId", "SiteId") REFERENCES m03_site ("TenantId", "SiteId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m06_sales_order_TenantId_SalesOrderId" FOREIGN KEY ("TenantId", "SalesOrderId") REFERENCES m06_sales_order ("TenantId", "SalesOrderId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m09_delivery_challan_TenantId_ChallanId" FOREIGN KEY ("TenantId", "ChallanId") REFERENCES m09_delivery_challan ("TenantId", "ChallanId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_m19_quality_sample_TenantId_SampleId" FOREIGN KEY ("TenantId", "SampleId") REFERENCES m19_quality_sample ("TenantId", "SampleId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_quality_test (
        "TenantId" uuid NOT NULL,
        "TestId" uuid NOT NULL,
        "SampleId" uuid NOT NULL,
        "TestAgeDays" integer NOT NULL,
        "DueAt" timestamp with time zone NOT NULL,
        "MinimumStrengthMpaSnapshot" numeric(9,2),
        "AcceptanceReferenceSnapshot" character varying(240) NOT NULL,
        "Status" character varying(24) NOT NULL,
        "StrengthMpa" numeric(9,2),
        "Outcome" character varying(24) NOT NULL,
        "TestedAt" timestamp with time zone,
        "ReportDocumentId" uuid,
        "RecordedByUserId" uuid,
        "ResultReason" character varying(500) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_m19_quality_test" PRIMARY KEY ("TenantId", "TestId"),
        CONSTRAINT "CK_m19_test_age" CHECK ("TestAgeDays" BETWEEN 1 AND 365),
        CONSTRAINT "CK_m19_test_strength" CHECK ("StrengthMpa" IS NULL OR "StrengthMpa" >= 0),
        CONSTRAINT "FK_m19_quality_test_m02_user_TenantId_RecordedByUserId" FOREIGN KEY ("TenantId", "RecordedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_test_m18_document_TenantId_ReportDocumentId" FOREIGN KEY ("TenantId", "ReportDocumentId") REFERENCES m18_document ("TenantId", "DocumentId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_quality_test_m19_quality_sample_TenantId_SampleId" FOREIGN KEY ("TenantId", "SampleId") REFERENCES m19_quality_sample ("TenantId", "SampleId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_dispute_evidence (
        "TenantId" uuid NOT NULL,
        "EvidenceId" uuid NOT NULL,
        "DisputeId" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "AddedByUserId" uuid NOT NULL,
        "AddedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_m19_dispute_evidence" PRIMARY KEY ("TenantId", "EvidenceId"),
        CONSTRAINT "FK_m19_dispute_evidence_m02_user_TenantId_AddedByUserId" FOREIGN KEY ("TenantId", "AddedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_evidence_m18_document_TenantId_DocumentId" FOREIGN KEY ("TenantId", "DocumentId") REFERENCES m18_document ("TenantId", "DocumentId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_evidence_m19_dispute_TenantId_DisputeId" FOREIGN KEY ("TenantId", "DisputeId") REFERENCES m19_dispute ("TenantId", "DisputeId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE TABLE m19_dispute_invoice (
        "TenantId" uuid NOT NULL,
        "DisputeId" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        CONSTRAINT "PK_m19_dispute_invoice" PRIMARY KEY ("TenantId", "DisputeId", "InvoiceId"),
        CONSTRAINT "FK_m19_dispute_invoice_m10_invoice_TenantId_InvoiceId" FOREIGN KEY ("TenantId", "InvoiceId") REFERENCES m10_invoice ("TenantId", "InvoiceId") ON DELETE RESTRICT,
        CONSTRAINT "FK_m19_dispute_invoice_m19_dispute_TenantId_DisputeId" FOREIGN KEY ("TenantId", "DisputeId") REFERENCES m19_dispute ("TenantId", "DisputeId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_BranchId_Status_OpenedAt" ON m19_dispute ("TenantId", "BranchId", "Status", "OpenedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_ChallanId" ON m19_dispute ("TenantId", "ChallanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_CustomerPartyId" ON m19_dispute ("TenantId", "CustomerPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_dispute_TenantId_IdempotencyKey" ON m19_dispute ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_OpenedByUserId" ON m19_dispute ("TenantId", "OpenedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_OwnerUserId" ON m19_dispute ("TenantId", "OwnerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_ResolvedByUserId" ON m19_dispute ("TenantId", "ResolvedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_SalesOrderId" ON m19_dispute ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_SampleId_Status" ON m19_dispute ("TenantId", "SampleId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_SiteId" ON m19_dispute ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_SupplierPartyId" ON m19_dispute ("TenantId", "SupplierPartyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_TenantId_TripId" ON m19_dispute ("TenantId", "TripId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_evidence_TenantId_AddedByUserId" ON m19_dispute_evidence ("TenantId", "AddedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_evidence_TenantId_DisputeId_AddedAt" ON m19_dispute_evidence ("TenantId", "DisputeId", "AddedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_dispute_evidence_TenantId_DisputeId_DocumentId" ON m19_dispute_evidence ("TenantId", "DisputeId", "DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_evidence_TenantId_DocumentId" ON m19_dispute_evidence ("TenantId", "DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_dispute_invoice_TenantId_InvoiceId" ON m19_dispute_invoice ("TenantId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_criteria_TenantId_CreatedByUserId" ON m19_quality_criteria ("TenantId", "CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_quality_criteria_TenantId_MaterialType_Grade_TestAgeDay~" ON m19_quality_criteria ("TenantId", "MaterialType", "Grade", "TestAgeDays", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_event_TenantId_ActorUserId" ON m19_quality_event ("TenantId", "ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_event_TenantId_EntityType_EntityId_OccurredAt" ON m19_quality_event ("TenantId", "EntityType", "EntityId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_BranchId_Status_CastAt" ON m19_quality_sample ("TenantId", "BranchId", "Status", "CastAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_CreatedByUserId" ON m19_quality_sample ("TenantId", "CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_quality_sample_TenantId_IdempotencyKey" ON m19_quality_sample ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_OwnerUserId" ON m19_quality_sample ("TenantId", "OwnerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_PourId" ON m19_quality_sample ("TenantId", "PourId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_ProductId" ON m19_quality_sample ("TenantId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_SalesOrderId" ON m19_quality_sample ("TenantId", "SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_SalesOrderLineId" ON m19_quality_sample ("TenantId", "SalesOrderLineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_quality_sample_TenantId_SampleNumber" ON m19_quality_sample ("TenantId", "SampleNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_SiteId" ON m19_quality_sample ("TenantId", "SiteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_sample_TenantId_TripId_CreatedAt" ON m19_quality_sample ("TenantId", "TripId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_test_TenantId_RecordedByUserId" ON m19_quality_test ("TenantId", "RecordedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_test_TenantId_ReportDocumentId" ON m19_quality_test ("TenantId", "ReportDocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE UNIQUE INDEX "IX_m19_quality_test_TenantId_SampleId_TestAgeDays" ON m19_quality_test ("TenantId", "SampleId", "TestAgeDays");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    CREATE INDEX "IX_m19_quality_test_TenantId_Status_DueAt" ON m19_quality_test ("TenantId", "Status", "DueAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127134458_P1M19QualityDisputes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261127134458_P1M19QualityDisputes', '10.0.15');
    END IF;
END $EF$;
COMMIT;

