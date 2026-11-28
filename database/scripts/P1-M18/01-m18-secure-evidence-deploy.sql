START TRANSACTION;
CREATE TABLE m18_document (
    "TenantId" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TripId" uuid NOT NULL,
    "UploadedByUserId" uuid NOT NULL,
    "PreviousDocumentId" uuid,
    "Purpose" character varying(24) NOT NULL,
    "VersionNumber" integer NOT NULL,
    "OriginalFileName" character varying(120) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "StorageKey" character varying(100) NOT NULL,
    "ScanStatus" character varying(24) NOT NULL,
    "StorageStatus" character varying(24) NOT NULL,
    "IsCurrent" boolean NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    "ScannedAt" timestamp with time zone,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_m18_document" PRIMARY KEY ("TenantId", "DocumentId"),
    CONSTRAINT "CK_m18_document_purpose" CHECK ("Purpose" IN ('Signature', 'Photo')),
    CONSTRAINT "CK_m18_document_scan_status" CHECK ("ScanStatus" IN ('Pending', 'Clean', 'Rejected')),
    CONSTRAINT "CK_m18_document_size" CHECK ("SizeBytes" > 0),
    CONSTRAINT "CK_m18_document_storage_status" CHECK ("StorageStatus" IN ('Quarantined', 'Available', 'Deleted')),
    CONSTRAINT "CK_m18_document_version" CHECK ("VersionNumber" > 0),
    CONSTRAINT "FK_m18_document_m02_user_TenantId_UploadedByUserId" FOREIGN KEY ("TenantId", "UploadedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m18_document_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m18_document_m18_document_TenantId_PreviousDocumentId" FOREIGN KEY ("TenantId", "PreviousDocumentId") REFERENCES m18_document ("TenantId", "DocumentId") ON DELETE RESTRICT
);

CREATE TABLE m18_document_event (
    "TenantId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "TripId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "EventType" character varying(40) NOT NULL,
    "DetailJson" jsonb NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_m18_document_event" PRIMARY KEY ("TenantId", "EventId"),
    CONSTRAINT "FK_m18_document_event_m02_user_TenantId_ActorUserId" FOREIGN KEY ("TenantId", "ActorUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m18_document_event_m08_trip_TenantId_TripId" FOREIGN KEY ("TenantId", "TripId") REFERENCES m08_trip ("TenantId", "TripId") ON DELETE RESTRICT,
    CONSTRAINT "FK_m18_document_event_m18_document_TenantId_DocumentId" FOREIGN KEY ("TenantId", "DocumentId") REFERENCES m18_document ("TenantId", "DocumentId") ON DELETE RESTRICT
);

CREATE INDEX "IX_m18_document_TenantId_PreviousDocumentId" ON m18_document ("TenantId", "PreviousDocumentId");

CREATE UNIQUE INDEX "IX_m18_document_TenantId_TripId_Purpose_IsCurrent" ON m18_document ("TenantId", "TripId", "Purpose", "IsCurrent") WHERE "IsCurrent" = true;

CREATE UNIQUE INDEX "IX_m18_document_TenantId_TripId_Purpose_VersionNumber" ON m18_document ("TenantId", "TripId", "Purpose", "VersionNumber");

CREATE INDEX "IX_m18_document_TenantId_UploadedAt" ON m18_document ("TenantId", "UploadedAt");

CREATE INDEX "IX_m18_document_TenantId_UploadedByUserId" ON m18_document ("TenantId", "UploadedByUserId");

CREATE INDEX "IX_m18_document_event_TenantId_ActorUserId" ON m18_document_event ("TenantId", "ActorUserId");

CREATE INDEX "IX_m18_document_event_TenantId_DocumentId_OccurredAt" ON m18_document_event ("TenantId", "DocumentId", "OccurredAt");

CREATE INDEX "IX_m18_document_event_TenantId_TripId_OccurredAt" ON m18_document_event ("TenantId", "TripId", "OccurredAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261127105316_P1M18SecureEvidenceStorage', '10.0.15');

COMMIT;

