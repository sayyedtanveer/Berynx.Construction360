START TRANSACTION;
ALTER TABLE m08_trip ADD "EvidenceDecisionReason" character varying(1000);

ALTER TABLE m08_trip ADD "ReceiverVerifiedByUserId" uuid;

CREATE INDEX "IX_m08_trip_TenantId_ReceiverVerifiedByUserId" ON m08_trip ("TenantId", "ReceiverVerifiedByUserId");

ALTER TABLE m08_trip ADD CONSTRAINT "FK_m08_trip_m02_user_TenantId_ReceiverVerifiedByUserId" FOREIGN KEY ("TenantId", "ReceiverVerifiedByUserId") REFERENCES m02_user ("TenantId", "UserId") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261127081534_P1M08ReceiverEvidenceReview', '10.0.15');

COMMIT;

