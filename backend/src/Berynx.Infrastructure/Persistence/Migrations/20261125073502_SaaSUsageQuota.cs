using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaaSUsageQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaxApiRequestsPerMonth",
                table: "platform_plan",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "platform_tenant_api_usage",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthStart = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_tenant_api_usage", x => new { x.TenantId, x.MonthStart });
                    table.CheckConstraint("CK_platform_tenant_api_usage_count", "\"RequestCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_platform_tenant_api_usage_onboarding_tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "onboarding_tenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_subscription_PlanCode",
                table: "platform_tenant_subscription",
                column: "PlanCode");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_tenant_subscription_grace",
                table: "platform_tenant_subscription",
                sql: "\"GraceEndsAt\" IS NULL OR (\"EndsAt\" IS NOT NULL AND \"GraceEndsAt\" >= \"EndsAt\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_tenant_subscription_status",
                table: "platform_tenant_subscription",
                sql: "\"Status\" IN ('Trial', 'Active', 'Grace', 'Suspended', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_tenant_subscription_term",
                table: "platform_tenant_subscription",
                sql: "\"EndsAt\" IS NULL OR \"StartsAt\" < \"EndsAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_tenant_entitlement_source",
                table: "platform_tenant_entitlement",
                sql: "\"Source\" IN ('Plan', 'AddOn')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_plan_quotas",
                table: "platform_plan",
                sql: "\"MaxUsers\" > 0 AND \"MaxStorageBytes\" > 0 AND \"MaxDocumentsPerMonth\" > 0 AND \"MaxMessagesPerMonth\" > 0 AND \"MaxApiRequestsPerMonth\" > 0 AND \"TrialDays\" > 0 AND \"GracePeriodDays\" >= 0 AND (\"PriceAmount\" IS NULL OR \"PriceAmount\" >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_tenant_TenantId_CurrentSubscriptionId",
                table: "onboarding_tenant",
                columns: new[] { "TenantId", "CurrentSubscriptionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_onboarding_tenant_platform_tenant_subscription_TenantId_Cur~",
                table: "onboarding_tenant",
                columns: new[] { "TenantId", "CurrentSubscriptionId" },
                principalTable: "platform_tenant_subscription",
                principalColumns: new[] { "TenantId", "SubscriptionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_tenant_subscription_platform_plan_PlanCode",
                table: "platform_tenant_subscription",
                column: "PlanCode",
                principalTable: "platform_plan",
                principalColumn: "PlanCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DO $$
                DECLARE old_constraint text;
                BEGIN
                    FOREACH old_constraint IN ARRAY ARRAY[
                        'onboarding_tenant_Status_check',
                        'onboarding_tenant_status_check',
                        'CK_onboarding_tenant_Status_lifecycle'
                    ]
                    LOOP
                        IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conrelid = 'onboarding_tenant'::regclass
                              AND conname = old_constraint
                        ) THEN
                            EXECUTE format(
                                'ALTER TABLE onboarding_tenant DROP CONSTRAINT %I',
                                old_constraint
                            );
                        END IF;
                    END LOOP;
                END
                $$;

                ALTER TABLE onboarding_tenant
                    ADD CONSTRAINT "CK_onboarding_tenant_Status_lifecycle"
                    CHECK ("Status" IN ('Trial', 'Active', 'Grace', 'Suspended', 'Cancelled'));
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION prevent_platform_license_audit_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'Platform licence audit rows are immutable.';
                END;
                $$;

                CREATE TRIGGER platform_license_audit_immutable
                BEFORE UPDATE OR DELETE ON platform_license_audit
                FOR EACH ROW EXECUTE FUNCTION prevent_platform_license_audit_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS platform_license_audit_immutable ON platform_license_audit;
                DROP FUNCTION IF EXISTS prevent_platform_license_audit_mutation();
                DO $$
                DECLARE lifecycle_constraint text;
                BEGIN
                    FOREACH lifecycle_constraint IN ARRAY ARRAY[
                        'onboarding_tenant_Status_check',
                        'onboarding_tenant_status_check',
                        'CK_onboarding_tenant_Status_lifecycle'
                    ]
                    LOOP
                        IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conrelid = 'onboarding_tenant'::regclass
                              AND conname = lifecycle_constraint
                        ) THEN
                            EXECUTE format(
                                'ALTER TABLE onboarding_tenant DROP CONSTRAINT %I',
                                lifecycle_constraint
                            );
                        END IF;
                    END LOOP;
                END
                $$;

                ALTER TABLE onboarding_tenant
                    ADD CONSTRAINT "onboarding_tenant_Status_check"
                    CHECK ("Status" IN ('Trial', 'Active', 'Suspended', 'Cancelled'));
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_onboarding_tenant_platform_tenant_subscription_TenantId_Cur~",
                table: "onboarding_tenant");

            migrationBuilder.DropForeignKey(
                name: "FK_platform_tenant_subscription_platform_plan_PlanCode",
                table: "platform_tenant_subscription");

            migrationBuilder.DropTable(
                name: "platform_tenant_api_usage");

            migrationBuilder.DropIndex(
                name: "IX_platform_tenant_subscription_PlanCode",
                table: "platform_tenant_subscription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_tenant_subscription_grace",
                table: "platform_tenant_subscription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_tenant_subscription_status",
                table: "platform_tenant_subscription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_tenant_subscription_term",
                table: "platform_tenant_subscription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_tenant_entitlement_source",
                table: "platform_tenant_entitlement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_plan_quotas",
                table: "platform_plan");

            migrationBuilder.DropIndex(
                name: "IX_onboarding_tenant_TenantId_CurrentSubscriptionId",
                table: "onboarding_tenant");

            migrationBuilder.DropColumn(
                name: "MaxApiRequestsPerMonth",
                table: "platform_plan");
        }
    }
}
