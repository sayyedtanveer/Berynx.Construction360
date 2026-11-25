using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berynx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaaSLicensingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSubscriptionId",
                table: "onboarding_tenant",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "platform_license_audit",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Before = table.Column<string>(type: "jsonb", nullable: false),
                    After = table.Column<string>(type: "jsonb", nullable: false),
                    Origin = table.Column<string>(type: "jsonb", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_license_audit", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_platform_license_audit_onboarding_tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "onboarding_tenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_module",
                columns: table => new
                {
                    ModuleCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsCore = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_module", x => x.ModuleCode);
                });

            migrationBuilder.CreateTable(
                name: "platform_plan",
                columns: table => new
                {
                    PlanCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUsers = table.Column<long>(type: "bigint", nullable: false),
                    MaxStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    MaxDocumentsPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    MaxMessagesPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_plan", x => x.PlanCode);
                });

            migrationBuilder.CreateTable(
                name: "platform_tenant_subscription",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    PlanVersion = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GraceEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TermsSnapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_tenant_subscription", x => new { x.TenantId, x.SubscriptionId });
                    table.ForeignKey(
                        name: "FK_platform_tenant_subscription_onboarding_tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "onboarding_tenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_tenant_entitlement",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(48)", nullable: false),
                    Source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_tenant_entitlement", x => new { x.TenantId, x.ModuleCode });
                    table.ForeignKey(
                        name: "FK_platform_tenant_entitlement_onboarding_tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "onboarding_tenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_tenant_entitlement_platform_module_ModuleCode",
                        column: x => x.ModuleCode,
                        principalTable: "platform_module",
                        principalColumn: "ModuleCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_plan_module",
                columns: table => new
                {
                    PlanCode = table.Column<string>(type: "character varying(48)", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(48)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_plan_module", x => new { x.PlanCode, x.ModuleCode });
                    table.ForeignKey(
                        name: "FK_platform_plan_module_platform_module_ModuleCode",
                        column: x => x.ModuleCode,
                        principalTable: "platform_module",
                        principalColumn: "ModuleCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_plan_module_platform_plan_PlanCode",
                        column: x => x.PlanCode,
                        principalTable: "platform_plan",
                        principalColumn: "PlanCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_tenant_CurrentSubscriptionId",
                table: "onboarding_tenant",
                column: "CurrentSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_platform_license_audit_TenantId_At",
                table: "platform_license_audit",
                columns: new[] { "TenantId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_plan_module_ModuleCode",
                table: "platform_plan_module",
                column: "ModuleCode");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_entitlement_ModuleCode",
                table: "platform_tenant_entitlement",
                column: "ModuleCode");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_entitlement_TenantId_IsEnabled_EffectiveUnt~",
                table: "platform_tenant_entitlement",
                columns: new[] { "TenantId", "IsEnabled", "EffectiveUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_subscription_TenantId_IsCurrent",
                table: "platform_tenant_subscription",
                columns: new[] { "TenantId", "IsCurrent" },
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_subscription_TenantId_Status_EndsAt",
                table: "platform_tenant_subscription",
                columns: new[] { "TenantId", "Status", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_license_audit");

            migrationBuilder.DropTable(
                name: "platform_plan_module");

            migrationBuilder.DropTable(
                name: "platform_tenant_entitlement");

            migrationBuilder.DropTable(
                name: "platform_tenant_subscription");

            migrationBuilder.DropTable(
                name: "platform_plan");

            migrationBuilder.DropTable(
                name: "platform_module");

            migrationBuilder.DropIndex(
                name: "IX_onboarding_tenant_CurrentSubscriptionId",
                table: "onboarding_tenant");

            migrationBuilder.DropColumn(
                name: "CurrentSubscriptionId",
                table: "onboarding_tenant");
        }
    }
}
