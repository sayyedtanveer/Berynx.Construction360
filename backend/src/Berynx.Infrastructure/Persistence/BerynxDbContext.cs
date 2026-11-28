using System.Linq.Expressions;
using Berynx.Domain.Onboarding;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Crm;
using Berynx.Domain.Quotations;
using Berynx.Domain.SalesOrders;
using Berynx.Domain.Procurement;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Invoicing;
using Berynx.Domain.Receivables;
using Berynx.Domain.Accounting;
using Berynx.Domain.Gst;
using Berynx.Domain.Expenses;
using Berynx.Domain.Reports;
using Berynx.Domain.Documents;
using Berynx.Domain.Promotion;
using Berynx.Domain.Quality;
using Berynx.Application.Tenancy;
using Berynx.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;

public sealed class BerynxDbContext(
    DbContextOptions<BerynxDbContext> options,
    ICurrentTenantContext currentTenant) : IdentityUserContext<M02IdentityUser, Guid>(options)
{
    private Guid CurrentTenantId => currentTenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<M02IdentityUser>(e =>
        {
            e.ToTable("m02_identity_user");
            e.Property(x => x.TotpSecretProtected).HasColumnName("TotpSecretProtected");
            e.Property(x => x.TotpPendingSecretProtected).HasColumnName("TotpPendingSecretProtected");
            e.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
        });
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("m02_identity_user_claim");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("m02_identity_user_login");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("m02_identity_user_token");
        modelBuilder.Entity<Tenant>(e => {
            e.ToTable("onboarding_tenant");
            e.HasKey(t => t.TenantId);
            e.Property(t => t.Version).IsConcurrencyToken();
            e.Property(t => t.Configuration).HasColumnType("jsonb");
            e.HasIndex(t => t.CurrentSubscriptionId);
            e.HasOne<TenantSubscription>().WithMany()
                .HasForeignKey(t => new { t.TenantId, t.CurrentSubscriptionId })
                .HasPrincipalKey(s => new { s.TenantId, s.SubscriptionId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PlatformPlan>(e =>
        {
            e.ToTable("platform_plan", table =>
                table.HasCheckConstraint("CK_platform_plan_quotas",
                    "\"MaxUsers\" > 0 AND \"MaxStorageBytes\" > 0 AND \"MaxDocumentsPerMonth\" > 0 AND \"MaxMessagesPerMonth\" > 0 AND \"MaxApiRequestsPerMonth\" > 0 AND \"TrialDays\" > 0 AND \"GracePeriodDays\" >= 0 AND (\"PriceAmount\" IS NULL OR \"PriceAmount\" >= 0)"));
            e.HasKey(x => x.PlanCode);
            e.Property(x => x.PlanCode).HasMaxLength(48);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.PriceAmount).HasPrecision(18, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<PlatformModule>(e =>
        {
            e.ToTable("platform_module");
            e.HasKey(x => x.ModuleCode);
            e.Property(x => x.ModuleCode).HasMaxLength(48);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(1000);
        });
        modelBuilder.Entity<PlatformPlanModule>(e =>
        {
            e.ToTable("platform_plan_module");
            e.HasKey(x => new { x.PlanCode, x.ModuleCode });
            e.HasOne<PlatformPlan>().WithMany().HasForeignKey(x => x.PlanCode)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PlatformModule>().WithMany().HasForeignKey(x => x.ModuleCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TenantSubscription>(e =>
        {
            e.ToTable("platform_tenant_subscription", table =>
            {
                table.HasCheckConstraint("CK_platform_tenant_subscription_status",
                    "\"Status\" IN ('Trial', 'Active', 'Grace', 'Suspended', 'Cancelled')");
                table.HasCheckConstraint("CK_platform_tenant_subscription_term",
                    "\"EndsAt\" IS NULL OR \"StartsAt\" < \"EndsAt\"");
                table.HasCheckConstraint("CK_platform_tenant_subscription_grace",
                    "\"GraceEndsAt\" IS NULL OR (\"EndsAt\" IS NOT NULL AND \"GraceEndsAt\" >= \"EndsAt\")");
            });
            e.HasKey(x => new { x.TenantId, x.SubscriptionId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PlatformPlan>().WithMany().HasForeignKey(x => x.PlanCode)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PlanCode).HasMaxLength(48);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.TermsSnapshot).HasColumnType("jsonb");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.IsCurrent })
                .IsUnique()
                .HasFilter("\"IsCurrent\" = TRUE");
            e.HasIndex(x => new { x.TenantId, x.Status, x.EndsAt });
        });
        modelBuilder.Entity<TenantModuleEntitlement>(e =>
        {
            e.ToTable("platform_tenant_entitlement", table =>
                table.HasCheckConstraint("CK_platform_tenant_entitlement_source",
                    "\"Source\" IN ('Plan', 'AddOn')"));
            e.HasKey(x => new { x.TenantId, x.ModuleCode });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PlatformModule>().WithMany().HasForeignKey(x => x.ModuleCode)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Source).HasMaxLength(24);
            e.HasIndex(x => new { x.TenantId, x.IsEnabled, x.EffectiveUntil });
        });
        modelBuilder.Entity<SaaSLicenseAuditEvent>(e =>
        {
            e.ToTable("platform_license_audit");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ActorEmail).HasMaxLength(256);
            e.Property(x => x.Action).HasMaxLength(80);
            e.Property(x => x.EntityType).HasMaxLength(80);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.Before).HasColumnType("jsonb");
            e.Property(x => x.After).HasColumnType("jsonb");
            e.Property(x => x.Origin).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.At });
        });
        modelBuilder.Entity<TenantApiUsage>(e =>
        {
            e.ToTable("platform_tenant_api_usage", table =>
                table.HasCheckConstraint("CK_platform_tenant_api_usage_count", "\"RequestCount\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.MonthStart });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MonthStart).HasColumnType("date");
        });
        modelBuilder.Entity<OpeningItem>(e => {
            e.ToTable("onboarding_opening");
            e.HasKey(x => new { x.TenantId, x.Id });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Debit).HasPrecision(18,2);
            e.Property(x => x.Credit).HasPrecision(18,2);
        });
        modelBuilder.Entity<SetupEvent>(e => {
            e.ToTable("onboarding_event");
            e.HasKey(x => new { x.TenantId, x.Id });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Origin).HasColumnType("jsonb");
            e.Property(x => x.Before).HasColumnType("jsonb");
            e.Property(x => x.After).HasColumnType("jsonb");
        });
        modelBuilder.Entity<M02User>(e => {
            e.ToTable("m02_user");
            e.HasKey(x => new { x.TenantId, x.UserId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.ProviderSubject }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Mobile }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdentityUserId }).IsUnique();
            e.HasOne<M02IdentityUser>().WithMany().HasForeignKey(x => x.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02Role>(e => {
            e.ToTable("m02_role");
            e.HasKey(x => new { x.TenantId, x.RoleId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02Role>().WithMany().HasForeignKey(x => new { x.TenantId, x.ClonedFromRoleId })
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.RoleKey }).IsUnique();
        });
        modelBuilder.Entity<M02RolePermission>(e => {
            e.ToTable("m02_role_permission");
            e.HasKey(x => new { x.TenantId, x.RoleId, x.PermissionCode });
            e.HasOne<M02Role>().WithMany().HasForeignKey(x => new { x.TenantId, x.RoleId })
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<M02PermissionCatalog>().WithMany().HasForeignKey(x => x.PermissionCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02UserRole>(e => {
            e.ToTable("m02_user_role");
            e.HasKey(x => new { x.TenantId, x.AssignmentId });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId })
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<M02Role>().WithMany().HasForeignKey(x => new { x.TenantId, x.RoleId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.UserId, x.RoleId, x.ScopeType, x.ScopeId }).IsUnique();
        });
        modelBuilder.Entity<M02ApprovalRule>(e => {
            e.ToTable("m02_approval_rule");
            e.HasKey(x => new { x.TenantId, x.RuleId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.AmountGreaterThan).HasPrecision(18, 2);
            e.Property(x => x.DiscountPercentGreaterThan).HasPrecision(9, 4);
            e.Property(x => x.MarginPercentBelow).HasPrecision(9, 4);
        });
        modelBuilder.Entity<M02ApprovalLevel>(e => {
            e.ToTable("m02_approval_level");
            e.HasKey(x => new { x.TenantId, x.RuleId, x.Level });
            e.HasOne<M02ApprovalRule>().WithMany().HasForeignKey(x => new { x.TenantId, x.RuleId })
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<M02Role>().WithMany().HasForeignKey(x => new { x.TenantId, x.ApproverRoleId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.ApproverUserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02Role>().WithMany().HasForeignKey(x => new { x.TenantId, x.EscalationRoleId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.EscalationUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02ApprovalRequest>(e => {
            e.ToTable("m02_approval_request");
            e.HasKey(x => new { x.TenantId, x.RequestId });
            e.HasOne<M02ApprovalRule>().WithMany().HasForeignKey(x => new { x.TenantId, x.RuleId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.RequesterUserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.OwnerUserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.MarginPercent).HasPrecision(9, 4);
            e.Property(x => x.Snapshot).HasColumnType("jsonb");
        });
        modelBuilder.Entity<M02ApprovalDecision>(e => {
            e.ToTable("m02_approval_decision");
            e.HasKey(x => new { x.TenantId, x.DecisionId });
            e.HasOne<M02ApprovalRequest>().WithMany().HasForeignKey(x => new { x.TenantId, x.RequestId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.ActorUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02ApprovalTaskAssignment>(e => {
            e.ToTable("m02_approval_task_assignment");
            e.HasKey(x => new { x.TenantId, x.RequestId, x.Level });
            e.HasOne<M02ApprovalRequest>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.RequestId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ApproverUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.AssignedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02Delegation>(e => {
            e.ToTable("m02_delegation");
            e.HasKey(x => new { x.TenantId, x.DelegationId });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.DelegatorUserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.DelegateUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M02AuditEvent>(e => {
            e.ToTable("m02_audit_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Before).HasColumnType("jsonb");
            e.Property(x => x.After).HasColumnType("jsonb");
        });
        modelBuilder.Entity<M02Notification>(e => {
            e.ToTable("m02_notification");
            e.HasKey(x => new { x.TenantId, x.NotificationId });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecipientUserId })
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Payload).HasColumnType("jsonb");
        });
        modelBuilder.Entity<M02ProviderSession>(e => {
            e.ToTable("m02_provider_session");
            e.HasKey(x => new { x.TenantId, x.ProviderSessionId });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId })
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<M02AuthSession>(e =>
        {
            e.ToTable("m02_auth_session");
            e.HasKey(x => new { x.TenantId, x.SessionId });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02IdentityUser>().WithMany().HasForeignKey(x => x.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.UserId, x.RevokedAt });
        });
        modelBuilder.Entity<M02RefreshToken>(e =>
        {
            e.ToTable("m02_refresh_token");
            e.HasKey(x => new { x.TenantId, x.TokenId });
            e.HasOne<M02AuthSession>().WithMany().HasForeignKey(x => new { x.TenantId, x.SessionId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.TokenHash }).IsUnique();
            e.Property(x => x.TokenHash).HasMaxLength(64);
        });
        modelBuilder.Entity<M02AuthChallenge>(e =>
        {
            e.ToTable("m02_auth_challenge");
            e.HasKey(x => x.ChallengeId);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.Purpose).HasMaxLength(32);
            e.Property(x => x.TokenHash).HasMaxLength(64);
        });
        modelBuilder.Entity<M02PermissionCatalog>(e => {
            e.ToTable("m02_permission_catalog");
            e.HasKey(x => x.PermissionCode);
        });
        modelBuilder.Entity<M02SystemRoleTemplate>(e => {
            e.ToTable("m02_system_role_template");
            e.HasKey(x => x.RoleKey);
        });
        modelBuilder.Entity<M02SystemRolePermission>(e => {
            e.ToTable("m02_system_role_permission");
            e.HasKey(x => new { x.RoleKey, x.PermissionCode });
        });

        modelBuilder.Entity<M03Party>(e =>
        {
            e.ToTable("m03_party");
            e.HasKey(x => new { x.TenantId, x.PartyId });
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.TradeName).HasMaxLength(200);
            e.Property(x => x.PartyType).HasMaxLength(32);
            e.Property(x => x.Gstin).HasMaxLength(15);
            e.Property(x => x.Pan).HasMaxLength(10);
            e.Property(x => x.CustomerCategory).HasMaxLength(80);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.StateCode).HasMaxLength(2);
            e.Property(x => x.BillingMode).HasMaxLength(24);
            e.Property(x => x.CustomerGroup).HasMaxLength(100);
            e.Property(x => x.TagsJson).HasColumnType("jsonb");
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Gstin });
            e.HasIndex(x => new { x.TenantId, x.Pan });
            e.HasIndex(x => new { x.TenantId, x.LegalName, x.City });
            e.HasOne<M03PaymentTerm>().WithMany().HasForeignKey(x => new { x.TenantId, x.PaymentTermId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03PriceList>().WithMany().HasForeignKey(x => new { x.TenantId, x.DefaultPriceListId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, UserId = x.SalespersonUserId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.SalespersonUserId });
        });
        modelBuilder.Entity<M03PartyRole>(e =>
        {
            e.ToTable("m03_party_role");
            e.HasKey(x => new { x.TenantId, x.PartyId, x.RoleCode });
            e.Property(x => x.RoleCode).HasMaxLength(32);
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartyId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M03PartyBankAccount>(e =>
        {
            e.ToTable("m03_party_bank_account");
            e.HasKey(x => new { x.TenantId, x.BankAccountId });
            e.Property(x => x.AccountHolder).HasMaxLength(200);
            e.Property(x => x.AccountNumberProtected).HasMaxLength(512);
            e.Property(x => x.Ifsc).HasMaxLength(11);
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<M03PartyMerge>(e =>
        {
            e.ToTable("m03_party_merge");
            e.HasKey(x => new { x.TenantId, x.MergeId });
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.MergedBy).HasMaxLength(200);
            e.HasIndex(x => new { x.TenantId, x.SourcePartyId }).IsUnique();
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourcePartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SurvivorPartyId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M03Site>(e =>
        {
            e.ToTable("m03_site");
            e.HasKey(x => new { x.TenantId, x.SiteId });
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.StateCode).HasMaxLength(2);
            e.Property(x => x.Gstin).HasMaxLength(15);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, PartyId = x.CustomerPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId });
        });
        modelBuilder.Entity<M03Contact>(e =>
        {
            e.ToTable("m03_contact");
            e.HasKey(x => new { x.TenantId, x.ContactId });
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Role).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Mobile).HasMaxLength(20);
            e.Property(x => x.ConsentSource).HasMaxLength(200);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.PartyId, x.Mobile });
        });
        modelBuilder.Entity<M03Plant>(e =>
        {
            e.ToTable("m03_plant");
            e.HasKey(x => new { x.TenantId, x.PlantId });
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.CapacityPerHour).HasPrecision(18, 3);
            e.Property(x => x.GradesJson).HasColumnType("jsonb");
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, PartyId = x.SupplierPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.SupplierPartyId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId });
        });
        modelBuilder.Entity<M03Product>(e =>
        {
            e.ToTable("m03_product");
            e.HasKey(x => new { x.TenantId, x.ProductId });
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(180);
            e.Property(x => x.Category).HasMaxLength(48);
            e.Property(x => x.HsnCode).HasMaxLength(16);
            e.Property(x => x.BaseUnitCode).HasMaxLength(16);
            e.Property(x => x.CategoryAttributesJson).HasColumnType("jsonb");
            e.Property(x => x.QualityReference).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasOne<M03Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.BaseUnitCode })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M03TaxRate>(e =>
        {
            e.ToTable("m03_tax_rate", table =>
            {
                table.HasCheckConstraint("ck_m03_tax_rate_range", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100");
                table.HasCheckConstraint("ck_m03_tax_rate_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            e.HasKey(x => new { x.TenantId, x.TaxRateId });
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.HsnCode).HasMaxLength(16);
            e.Property(x => x.RatePercent).HasPrecision(7, 4);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.HsnCode, x.EffectiveFrom }).IsUnique();
        });
        modelBuilder.Entity<M03CategoryTaxOverride>(e =>
        {
            e.ToTable("m03_category_tax_override", table =>
            {
                table.HasCheckConstraint("ck_m03_category_tax_override_range", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100");
                table.HasCheckConstraint("ck_m03_category_tax_override_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                table.HasCheckConstraint("ck_m03_category_tax_override_reason", "length(trim(\"Reason\")) > 0");
            });
            e.HasKey(x => new { x.TenantId, x.OverrideId });
            e.Property(x => x.CustomerCategory).HasMaxLength(80);
            e.Property(x => x.RatePercent).HasPrecision(7, 4);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ApprovedBy).HasMaxLength(200);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.ProductId, x.CustomerCategory, x.EffectiveFrom }).IsUnique();
        });
        modelBuilder.Entity<M03Unit>(e =>
        {
            e.ToTable("m03_unit");
            e.HasKey(x => new { x.TenantId, x.UnitCode });
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.UnitCode).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Symbol).HasMaxLength(16);
        });
        modelBuilder.Entity<M03UnitConversion>(e =>
        {
            e.ToTable("m03_unit_conversion", table =>
            {
                table.HasCheckConstraint("ck_m03_unit_conversion_positive", "\"ToUnitsPerFromUnit\" > 0");
                table.HasCheckConstraint("ck_m03_unit_conversion_canonical", "\"FromUnitCode\" < \"ToUnitCode\"");
            });
            e.HasKey(x => new { x.TenantId, x.ConversionId });
            e.Property(x => x.FromUnitCode).HasMaxLength(16);
            e.Property(x => x.ToUnitCode).HasMaxLength(16);
            e.Property(x => x.ToUnitsPerFromUnit).HasPrecision(18, 9);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.FromUnitCode, x.ToUnitCode }).IsUnique();
            e.HasOne<M03Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.FromUnitCode })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.ToUnitCode })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M03PriceList>(e =>
        {
            e.ToTable("m03_price_list", table =>
            {
                table.HasCheckConstraint("ck_m03_price_list_dates", "\"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                table.HasCheckConstraint("ck_m03_price_list_scope",
                    "(CASE WHEN \"CustomerPartyId\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"CustomerGroup\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"IsDefault\" THEN 1 ELSE 0 END) = 1");
                table.HasCheckConstraint("ck_m03_price_list_basis",
                    "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");
            });
            e.HasKey(x => new { x.TenantId, x.PriceListId });
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.CustomerGroup).HasMaxLength(100);
            e.Property(x => x.PriceBasis).HasMaxLength(16).HasDefaultValue("ExPlant");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.PriceBasis, x.ValidFrom }).IsUnique()
                .HasFilter("\"CustomerPartyId\" IS NOT NULL");
            e.HasIndex(x => new { x.TenantId, x.CustomerGroup, x.PriceBasis, x.ValidFrom }).IsUnique()
                .HasFilter("\"CustomerGroup\" IS NOT NULL");
            e.HasIndex(x => new { x.TenantId, x.PriceBasis, x.ValidFrom }).IsUnique().HasFilter("\"IsDefault\"");
        });
        modelBuilder.Entity<M03PriceListLine>(e =>
        {
            e.ToTable("m03_price_list_line", table =>
            {
                table.HasCheckConstraint("ck_m03_price_line_rates", "\"Rate\" >= 0 AND (\"MinimumRate\" IS NULL OR \"MinimumRate\" >= 0)");
                table.HasCheckConstraint("ck_m03_price_line_margin", "\"MinimumMarginPercent\" IS NULL OR (\"MinimumMarginPercent\" >= 0 AND \"MinimumMarginPercent\" <= 100)");
            });
            e.HasKey(x => new { x.TenantId, x.PriceListLineId });
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.MinimumRate).HasPrecision(18, 4);
            e.Property(x => x.MinimumMarginPercent).HasPrecision(7, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<M03PriceList>().WithMany().HasForeignKey(x => new { x.TenantId, x.PriceListId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.PriceListId, x.ProductId }).IsUnique();
        });
        modelBuilder.Entity<M03RmcRateCard>(e =>
        {
            e.ToTable("m03_rmc_rate_card", table =>
            {
                table.HasCheckConstraint("ck_m03_rmc_rate_card_rate", "\"Rate\" >= 0");
                table.HasCheckConstraint("ck_m03_rmc_rate_card_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                table.HasCheckConstraint("ck_m03_rmc_rate_card_basis",
                    "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");
            });
            e.HasKey(x => new { x.TenantId, x.RateCardId });
            e.Property(x => x.Grade).HasMaxLength(32);
            e.Property(x => x.SlumpBand).HasMaxLength(32);
            e.Property(x => x.AdmixtureCode).HasMaxLength(64);
            e.Property(x => x.PumpType).HasMaxLength(32);
            e.Property(x => x.PriceBasis).HasMaxLength(16).HasDefaultValue("ExPlant");
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new
            {
                x.TenantId, x.ProductId, x.Grade, x.SlumpBand, x.AdmixtureCode, x.PumpType, x.PriceBasis, x.EffectiveFrom
            }).IsUnique();
        });
        modelBuilder.Entity<M03Vehicle>(e =>
        {
            e.ToTable("m03_vehicle", table =>
                table.HasCheckConstraint("ck_m03_vehicle_capacity", "\"Capacity\" > 0"));
            e.HasKey(x => new { x.TenantId, x.VehicleId });
            e.Property(x => x.RegistrationNumber).HasMaxLength(20);
            e.Property(x => x.Type).HasMaxLength(32);
            e.Property(x => x.Capacity).HasPrecision(18, 3);
            e.Property(x => x.CapacityUnitCode).HasMaxLength(16);
            e.Property(x => x.Ownership).HasMaxLength(16);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.RegistrationNumber }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.TransporterPartyId });
        });
        modelBuilder.Entity<M03Driver>(e =>
        {
            e.ToTable("m03_driver");
            e.HasKey(x => new { x.TenantId, x.DriverId });
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Mobile).HasMaxLength(20);
            e.Property(x => x.LicenceNumber).HasMaxLength(40);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.LicenceNumber }).IsUnique();
            e.HasOne<M03Vehicle>().WithMany().HasForeignKey(x => new { x.TenantId, x.VehicleId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M03Equipment>(e =>
        {
            e.ToTable("m03_equipment");
            e.HasKey(x => new { x.TenantId, x.EquipmentId });
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.EquipmentType).HasMaxLength(48);
            e.Property(x => x.PumpType).HasMaxLength(32);
            e.Property(x => x.Capacity).HasPrecision(18, 3);
            e.Property(x => x.CapacityUnitCode).HasMaxLength(16);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.EquipmentType, x.Name }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.OwnerPartyId });
        });
        modelBuilder.Entity<M03PaymentTerm>(e =>
        {
            e.ToTable("m03_payment_term", table =>
                table.HasCheckConstraint("ck_m03_payment_term_due_days", "\"DueDays\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.PaymentTermId });
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.LedgerCode).HasMaxLength(64);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<M03ChargeType>(e =>
        {
            e.ToTable("m03_charge_type");
            e.HasKey(x => new { x.TenantId, x.ChargeTypeId });
            e.Property(x => x.Code).HasMaxLength(48);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.LedgerCode).HasMaxLength(64);
            e.Property(x => x.HsnCode).HasMaxLength(16);
            e.Property(x => x.DefaultSurchargeRate).HasPrecision(18, 4);
            e.Property(x => x.DefaultTaxRatePercent).HasPrecision(7, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<M03ImportBatch>(e =>
        {
            e.ToTable("m03_import_batch");
            e.HasKey(x => new { x.TenantId, x.BatchId });
            e.Property(x => x.EntityType).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<M03ImportRowError>(e =>
        {
            e.ToTable("m03_import_row_error");
            e.HasKey(x => new { x.TenantId, x.RowErrorId });
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.RowJson).HasColumnType("jsonb");
            e.HasOne<M03ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.BatchId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04Enquiry>(e =>
        {
            e.ToTable("m04_enquiry", table =>
            {
                table.HasCheckConstraint("CK_m04_enquiry_status",
                    "\"Status\" IN ('New','Contacted','Follow-up','Quotation Sent','Negotiation','Won','Lost','On hold')");
                table.HasCheckConstraint("CK_m04_enquiry_open_followup",
                    "\"Status\" IN ('Won','Lost') OR \"NextActionAt\" IS NOT NULL");
                table.HasCheckConstraint("CK_m04_enquiry_won_order",
                    "\"Status\" <> 'Won' OR \"LinkedSalesOrderId\" IS NOT NULL");
                table.HasCheckConstraint("CK_m04_enquiry_source",
                    "\"Source\" IN ('Phone','WalkIn','WhatsApp','Referral','SiteVisit','Website','Campaign','Repeat')");
            });
            e.HasKey(x => new { x.TenantId, x.EnquiryId });
            e.Property(x => x.Number).HasMaxLength(32);
            e.Property(x => x.Source).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Priority).HasMaxLength(16);
            e.Property(x => x.LostReason).HasMaxLength(24);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.ExpectedValue).HasPrecision(18, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
                .HasFilter("\"IdempotencyKey\" <> ''");
            e.HasIndex(x => new { x.TenantId, x.Status, x.NextActionAt });
            e.HasIndex(x => new { x.TenantId, x.OwnerUserId, x.Status, x.BranchId });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Contact>().WithMany().HasForeignKey(x => new { x.TenantId, x.ContactId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04EnquiryLine>(e =>
        {
            e.ToTable("m04_enquiry_line", table =>
                table.HasCheckConstraint("CK_m04_enquiry_line_quantity", "\"Quantity\" > 0"));
            e.HasKey(x => new { x.TenantId, x.LineId });
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitCode).HasMaxLength(16);
            e.HasIndex(x => new { x.TenantId, x.EnquiryId });
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.EnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04EnquiryActivity>(e =>
        {
            e.ToTable("m04_enquiry_activity");
            e.HasKey(x => new { x.TenantId, x.ActivityId });
            e.Property(x => x.Kind).HasMaxLength(24);
            e.Property(x => x.Channel).HasMaxLength(24);
            e.Property(x => x.Outcome).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasIndex(x => new { x.TenantId, x.EnquiryId, x.At });
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.EnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04FollowUp>(e =>
        {
            e.ToTable("m04_follow_up", table =>
                table.HasCheckConstraint("CK_m04_follow_up_status", "\"Status\" IN ('Pending','Completed','Cancelled')"));
            e.HasKey(x => new { x.TenantId, x.FollowUpId });
            e.Property(x => x.Action).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(16);
            e.Property(x => x.Outcome).HasMaxLength(1000);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Status, x.DueAt, x.OwnerUserId });
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.EnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04AssignmentAudit>(e =>
        {
            e.ToTable("m04_assignment_audit");
            e.HasKey(x => new { x.TenantId, x.AssignmentAuditId });
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.EnquiryId, x.At });
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.EnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M04NumberCounter>(e =>
        {
            e.ToTable("m04_number_counter", table =>
                table.HasCheckConstraint("CK_m04_number_counter_values", "\"CalendarYear\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0"));
            e.HasKey(x => new { x.TenantId, x.DocumentCode, x.CalendarYear });
            e.Property(x => x.DocumentCode).HasMaxLength(24);
        });
        modelBuilder.Entity<M04AssignmentCursor>(e =>
        {
            e.ToTable("m04_assignment_cursor");
            e.HasKey(x => new { x.TenantId, x.Source });
            e.Property(x => x.Source).HasMaxLength(24);
        });
        modelBuilder.Entity<M04AssignmentRule>(e =>
        {
            e.ToTable("m04_assignment_rule");
            e.HasKey(x => new { x.TenantId, x.RuleId });
            e.Property(x => x.Source).HasMaxLength(24);
            e.Property(x => x.RegionStateCode).HasMaxLength(2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Source, x.RegionStateCode, x.Priority });
        });
        modelBuilder.Entity<M04SlaAlert>(e =>
        {
            e.ToTable("m04_sla_alert");
            e.HasKey(x => new { x.TenantId, x.EnquiryId });
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.EnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M05Quotation>(e =>
        {
            e.ToTable("m05_quotation", table =>
            {
                table.HasCheckConstraint("CK_m05_quotation_status",
                    "\"Status\" IN ('Draft','Pending approval','Approved','Sent','Accepted','Rejected','Expired','Revised','Cancelled')");
                table.HasCheckConstraint("CK_m05_quotation_price_basis",
                    "\"PriceBasis\" IN ('ExPlant','Delivered','Site')");
                table.HasCheckConstraint("CK_m05_quotation_amounts",
                    "\"Subtotal\" >= 0 AND \"DiscountTotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0");
                table.HasCheckConstraint("CK_m05_quotation_revision",
                    "\"RevisionNumber\" > 0");
            });
            e.HasKey(x => new { x.TenantId, x.QuotationId });
            e.Property(x => x.Number).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.PriceBasis).HasMaxLength(16);
            e.Property(x => x.DeliveryTerms).HasMaxLength(2000);
            e.Property(x => x.TermsAndConditions).HasMaxLength(8000);
            e.Property(x => x.InternalNotes).HasMaxLength(4000);
            e.Property(x => x.PlaceOfSupplyStateCode).HasMaxLength(2);
            e.Property(x => x.CompanyLegalNameSnapshot).HasMaxLength(200);
            e.Property(x => x.SellerGstinSnapshot).HasMaxLength(15);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200);
            e.Property(x => x.CustomerGstinSnapshot).HasMaxLength(15);
            e.Property(x => x.CustomerAddressSnapshot).HasMaxLength(2000);
            e.Property(x => x.SiteAddressSnapshot).HasMaxLength(2000);
            e.Property(x => x.CreditStatus).HasMaxLength(24);
            e.Property(x => x.CostStatus).HasMaxLength(24);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.ChargesTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.MarginPercent).HasPrecision(7, 4);
            e.Property(x => x.CreditSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.ApprovalSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.RateComparisonJson).HasColumnType("jsonb");
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.SeriesId, x.RevisionNumber }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique()
                .HasFilter("\"Number\" IS NOT NULL");
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
                .HasFilter("\"IdempotencyKey\" <> ''");
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.Status, x.CreatedAt });
            e.HasIndex(x => new { x.TenantId, x.SalespersonUserId, x.Status, x.BranchId });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Contact>().WithMany().HasForeignKey(x => new { x.TenantId, x.ContactId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceEnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M05QuotationLine>(e =>
        {
            e.ToTable("m05_quotation_line", table =>
            {
                table.HasCheckConstraint("CK_m05_quotation_line_quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_m05_quotation_line_values",
                    "\"Rate\" >= 0 AND \"DiscountPercent\" BETWEEN 0 AND 100 AND \"GstPercent\" >= 0 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.LineId });
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.UnitCode).HasMaxLength(16);
            e.Property(x => x.RmcGrade).HasMaxLength(32);
            e.Property(x => x.RmcSlumpBand).HasMaxLength(32);
            e.Property(x => x.RmcAdmixtureCode).HasMaxLength(64);
            e.Property(x => x.RmcPumpType).HasMaxLength(32);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.DiscountPercent).HasPrecision(7, 4);
            e.Property(x => x.GstPercent).HasPrecision(7, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.RateSource).HasMaxLength(64);
            e.Property(x => x.RmcGrade).HasMaxLength(32);
            e.Property(x => x.RmcSlumpBand).HasMaxLength(32);
            e.Property(x => x.RmcAdmixtureCode).HasMaxLength(32);
            e.Property(x => x.RmcPumpType).HasMaxLength(32);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.TaxTreatmentSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.QuotationId });
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M05QuotationCharge>(e =>
        {
            e.ToTable("m05_quotation_charge", table =>
            {
                table.HasCheckConstraint("CK_m05_quotation_charge_values",
                    "\"Amount\" >= 0 AND \"TaxRatePercent\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.ChargeId });
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TaxRatePercent).HasPrecision(7, 4);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.TaxTreatmentSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.QuotationId });
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03ChargeType>().WithMany().HasForeignKey(x => new { x.TenantId, x.ChargeTypeId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M05QuotationLink>(e =>
        {
            e.ToTable("m05_quotation_link");
            e.HasKey(x => new { x.TenantId, x.LinkId });
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => new { x.TenantId, x.TokenHash }).IsUnique();
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M05QuotationActivity>(e =>
        {
            e.ToTable("m05_quotation_activity");
            e.HasKey(x => new { x.TenantId, x.ActivityId });
            e.Property(x => x.Action).HasMaxLength(32);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.QuotationId, x.At });
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06SalesOrder>(e =>
        {
            e.ToTable("m06_sales_order", table =>
            {
                table.HasCheckConstraint("CK_m06_sales_order_status",
                    "\"Status\" IN ('Draft','Pending approval','Confirmed','In fulfilment','Completed','Short-closed','On hold','Cancelled')");
                table.HasCheckConstraint("CK_m06_sales_order_type",
                    "\"OrderType\" IN ('RMC','Material','Mixed')");
                table.HasCheckConstraint("CK_m06_sales_order_amounts",
                    "\"Subtotal\" >= 0 AND \"DiscountTotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.SalesOrderId });
            e.Property(x => x.Number).HasMaxLength(40);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200);
            e.Property(x => x.SiteAddressSnapshot).HasMaxLength(2000);
            e.Property(x => x.CustomerPoNumber).HasMaxLength(100);
            e.Property(x => x.CustomerPoAttachmentReference).HasMaxLength(500);
            e.Property(x => x.SourceQuotationNumber).HasMaxLength(40);
            e.Property(x => x.OrderType).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.PaymentTermsSnapshot).HasMaxLength(2000);
            e.Property(x => x.DeliveryTermsSnapshot).HasMaxLength(2000);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.ClosureReason).HasMaxLength(1000);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.ChargesTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.EstimatedMarginPercent).HasPrecision(7, 4);
            e.Property(x => x.CreditStatus).HasMaxLength(24);
            e.Property(x => x.CostStatus).HasMaxLength(24);
            e.Property(x => x.CreditSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.ApprovalSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
                .HasFilter("\"IdempotencyKey\" <> ''");
            e.HasIndex(x => new { x.TenantId, x.Status, x.OrderDate, x.BranchId });
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.Status });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceQuotationId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M04Enquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceEnquiryId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06SalesOrderLine>(e =>
        {
            e.ToTable("m06_sales_order_line", table =>
            {
                table.HasCheckConstraint("CK_m06_sales_order_line_values",
                    "\"OrderedQuantity\" > 0 AND \"Rate\" >= 0 AND \"DiscountPercent\" BETWEEN 0 AND 100 AND \"GstPercent\" >= 0 AND \"DeliveredQuantity\" >= 0 AND \"InvoicedQuantity\" >= 0 AND \"PaidAmount\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.LineId });
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(200);
            e.Property(x => x.ProductCategorySnapshot).HasMaxLength(48);
            e.Property(x => x.UnitCode).HasMaxLength(16);
            e.Property(x => x.OrderedQuantity).HasPrecision(18, 3);
            e.Property(x => x.DeliveredQuantity).HasPrecision(18, 3);
            e.Property(x => x.InvoicedQuantity).HasPrecision(18, 3);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(7, 4);
            e.Property(x => x.GstPercent).HasPrecision(7, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId });
            e.HasIndex(x => new { x.TenantId, x.SourceQuotationLineId });
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M05QuotationLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceQuotationLineId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06SalesOrderCharge>(e =>
        {
            e.ToTable("m06_sales_order_charge", table =>
                table.HasCheckConstraint("CK_m06_sales_order_charge_values",
                    "\"Amount\" >= 0 AND \"TaxRatePercent\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.ChargeId });
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TaxRatePercent).HasPrecision(7, 4);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId });
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M05QuotationCharge>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceQuotationChargeId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06QuotationAllocation>(e =>
        {
            e.ToTable("m06_quotation_allocation", table =>
                table.HasCheckConstraint("CK_m06_quotation_allocation_quantity", "\"Quantity\" > 0"));
            e.HasKey(x => new { x.TenantId, x.AllocationId });
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.HasIndex(x => new { x.TenantId, x.QuotationLineId });
            e.HasIndex(x => new { x.TenantId, x.SalesOrderLineId }).IsUnique();
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M05QuotationLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationLineId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06QuotationChargeAllocation>(e =>
        {
            e.ToTable("m06_quotation_charge_allocation", table =>
                table.HasCheckConstraint("CK_m06_quotation_charge_allocation_values",
                    "\"Amount\" >= 0 AND \"CgstAmount\" >= 0 AND \"SgstAmount\" >= 0 AND \"IgstAmount\" >= 0 AND \"TaxAmount\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.AllocationId });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.QuotationChargeId });
            e.HasIndex(x => new { x.TenantId, x.SalesOrderChargeId }).IsUnique();
            e.HasOne<M05Quotation>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M05QuotationCharge>().WithMany().HasForeignKey(x => new { x.TenantId, x.QuotationChargeId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderCharge>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderChargeId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06DeliverySchedule>(e =>
        {
            e.ToTable("m06_delivery_schedule", table =>
            {
                table.HasCheckConstraint("CK_m06_delivery_schedule_quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_m06_delivery_schedule_status",
                    "\"Status\" IN ('Pending approval','Planned','Released','Cancelled')");
            });
            e.HasKey(x => new { x.TenantId, x.ScheduleId });
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.TimeWindow).HasMaxLength(80);
            e.Property(x => x.OperationalNotes).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.DeliveryDate });
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Plant>().WithMany().HasForeignKey(x => new { x.TenantId, x.PreferredPlantId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06Amendment>(e =>
        {
            e.ToTable("m06_amendment", table =>
            {
                table.HasCheckConstraint("CK_m06_amendment_status",
                    "\"Status\" IN ('Pending approval','Approved','Rejected','Cancelled')");
                table.HasCheckConstraint("CK_m06_amendment_revision", "\"RevisionNumber\" > 0");
            });
            e.HasKey(x => new { x.TenantId, x.AmendmentId });
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.BeforeSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.ProposedSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.Status).HasMaxLength(24);
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.RevisionNumber }).IsUnique();
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06Activity>(e =>
        {
            e.ToTable("m06_activity");
            e.HasKey(x => new { x.TenantId, x.ActivityId });
            e.Property(x => x.Action).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.At });
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M06LinkedDocument>(e =>
        {
            e.ToTable("m06_linked_document");
            e.HasKey(x => new { x.TenantId, x.LinkId });
            e.Property(x => x.SourceModule).HasMaxLength(8);
            e.Property(x => x.DocumentType).HasMaxLength(32);
            e.Property(x => x.DocumentNumber).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.SourceModule, x.DocumentId }).IsUnique();
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07PurchaseOrder>(e =>
        {
            e.ToTable("m07_purchase_order", table =>
                table.HasCheckConstraint("CK_m07_purchase_order_amounts",
                    "\"Subtotal\" >= 0 AND \"ChargesTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.PurchaseOrderId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(240);
            e.Property(x => x.SupplierGstinSnapshot).HasMaxLength(15);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.PaymentTermsSnapshot).HasMaxLength(1000);
            e.Property(x => x.DeliveryTermsSnapshot).HasMaxLength(1000);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.ApprovalSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.CreatedByUserId).IsRequired();
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.ChargesTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.SupplierPartyId, x.OrderDate, x.Status });
            e.HasIndex(x => new { x.TenantId, x.SourceSalesOrderId });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceSalesOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07PurchaseOrderLine>(e =>
        {
            e.ToTable("m07_purchase_order_line", table =>
                table.HasCheckConstraint("CK_m07_purchase_order_line_amounts",
                    "\"OrderedQuantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"Rate\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.PurchaseOrderLineId });
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(48);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(240);
            e.Property(x => x.ProductCategorySnapshot).HasMaxLength(40);
            e.Property(x => x.UnitCode).HasMaxLength(24);
            e.Property(x => x.OrderedQuantity).HasPrecision(18, 3);
            e.Property(x => x.ReceivedQuantity).HasPrecision(18, 3);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.GstPercent).HasPrecision(7, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.PurchaseOrderId });
            e.HasIndex(x => new { x.TenantId, x.SourceSalesOrderLineId });
            e.HasOne<M07PurchaseOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SourceSalesOrderLineId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07PurchaseOrderCharge>(e =>
        {
            e.ToTable("m07_purchase_order_charge", table =>
                table.HasCheckConstraint("CK_m07_purchase_order_charge_amounts",
                    "\"Amount\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxAmount\" >= 0 AND \"Total\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.ChargeId });
            e.Property(x => x.Description).HasMaxLength(240);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.GstPercent).HasPrecision(7, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasOne<M07PurchaseOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07GoodsReceipt>(e =>
        {
            e.ToTable("m07_goods_receipt", table =>
            {
                table.HasCheckConstraint("CK_m07_goods_receipt_status",
                    "\"Status\" IN ('Draft', 'Pending approval', 'Posted', 'Cancelled')");
                table.HasCheckConstraint("CK_m07_goods_receipt_weights",
                    "(\"GrossWeight\" IS NULL OR \"GrossWeight\" >= 0) AND (\"TareWeight\" IS NULL OR \"TareWeight\" >= 0) AND (\"NetWeight\" IS NULL OR \"NetWeight\" >= 0)");
            });
            e.HasKey(x => new { x.TenantId, x.GoodsReceiptId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.VehicleNumber).HasMaxLength(32);
            e.Property(x => x.DriverName).HasMaxLength(120);
            e.Property(x => x.WeighbridgeSlipNumber).HasMaxLength(80);
            e.Property(x => x.GrossWeight).HasPrecision(18, 3);
            e.Property(x => x.TareWeight).HasPrecision(18, 3);
            e.Property(x => x.NetWeight).HasPrecision(18, 3);
            e.Property(x => x.AttachmentReference).HasMaxLength(500);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.PurchaseOrderId, x.Status });
            e.HasOne<M07PurchaseOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPartyId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07GoodsReceiptLine>(e =>
        {
            e.ToTable("m07_goods_receipt_line", table =>
                table.HasCheckConstraint("CK_m07_goods_receipt_line_quantities",
                    "\"ReceivedQuantity\" > 0 AND \"DamagedQuantity\" >= 0 AND \"DamagedQuantity\" <= \"ReceivedQuantity\" AND \"ShortQuantity\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.GoodsReceiptLineId });
            e.Property(x => x.ReceivedQuantity).HasPrecision(18, 3);
            e.Property(x => x.DamagedQuantity).HasPrecision(18, 3);
            e.Property(x => x.ShortQuantity).HasPrecision(18, 3);
            e.Property(x => x.ExceptionReason).HasMaxLength(500);
            e.Property(x => x.LotOrHeatNumber).HasMaxLength(100);
            e.Property(x => x.ManufacturerName).HasMaxLength(160);
            e.Property(x => x.MtcReference).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.GoodsReceiptId });
            e.HasIndex(x => new { x.TenantId, x.PurchaseOrderLineId });
            e.HasOne<M07GoodsReceipt>().WithMany().HasForeignKey(x => new { x.TenantId, x.GoodsReceiptId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M07PurchaseOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PurchaseOrderLineId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07SupplierInvoice>(e =>
        {
            e.ToTable("m07_supplier_invoice", table =>
            {
                table.HasCheckConstraint("CK_m07_supplier_invoice_amounts",
                    "\"Subtotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"Total\" >= 0 AND \"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"Total\" AND \"TdsWithheld\" >= 0");
                table.HasCheckConstraint("CK_m07_supplier_invoice_status",
                    "\"Status\" IN ('Draft', 'Matched', 'Pending approval', 'Posted', 'Part-paid', 'Paid', 'Cancelled')");
            });
            e.HasKey(x => new { x.TenantId, x.SupplierInvoiceId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.SupplierInvoiceNumber).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.TdsWithheld).HasPrecision(18, 2);
            e.Property(x => x.AttachmentReference).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.SupplierPartyId, x.SupplierInvoiceNumber, x.InvoiceDate }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.SupplierPartyId, x.DueDate, x.Status });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPartyId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M07PurchaseOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07SupplierInvoiceLine>(e =>
        {
            e.ToTable("m07_supplier_invoice_line", table =>
                table.HasCheckConstraint("CK_m07_supplier_invoice_line_amounts",
                    "\"Quantity\" > 0 AND \"Rate\" >= 0 AND \"GstPercent\" BETWEEN 0 AND 100 AND \"TaxableAmount\" >= 0 AND \"TaxAmount\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.InvoiceLineId });
            e.Property(x => x.Description).HasMaxLength(240);
            e.Property(x => x.HsnCode).HasMaxLength(8);
            e.Property(x => x.UnitCode).HasMaxLength(24);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.GstPercent).HasPrecision(7, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.SupplierInvoiceId });
            e.HasOne<M07SupplierInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierInvoiceId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M07PurchaseOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PurchaseOrderLineId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M07GoodsReceiptLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.GoodsReceiptLineId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07SupplierPayment>(e =>
        {
            e.ToTable("m07_supplier_payment", table =>
                table.HasCheckConstraint("CK_m07_supplier_payment_amounts",
                    "\"GrossAmount\" > 0 AND \"TdsAmount\" >= 0 AND \"BankCharges\" >= 0 AND \"NetBankAmount\" >= 0 AND \"TdsAmount\" + \"BankCharges\" <= \"GrossAmount\""));
            e.HasKey(x => new { x.TenantId, x.SupplierPaymentId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.PaymentMode).HasMaxLength(32);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.GrossAmount).HasPrecision(18, 2);
            e.Property(x => x.TdsAmount).HasPrecision(18, 2);
            e.Property(x => x.BankCharges).HasPrecision(18, 2);
            e.Property(x => x.NetBankAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.SupplierPartyId, x.PaymentDate });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPartyId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07SupplierPaymentAllocation>(e =>
        {
            e.ToTable("m07_supplier_payment_allocation", table =>
                table.HasCheckConstraint("CK_m07_supplier_payment_allocation_amount", "\"Amount\" > 0"));
            e.HasKey(x => new { x.TenantId, x.AllocationId });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.SupplierPaymentId, x.SupplierInvoiceId }).IsUnique();
            e.HasOne<M07SupplierPayment>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPaymentId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M07SupplierInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierInvoiceId })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M07Activity>(e =>
        {
            e.ToTable("m07_activity");
            e.HasKey(x => new { x.TenantId, x.ActivityId });
            e.Property(x => x.EntityType).HasMaxLength(40);
            e.Property(x => x.Action).HasMaxLength(48);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.At });
        });
        modelBuilder.Entity<M07NumberCounter>(e =>
        {
            e.ToTable("m07_number_counter", table =>
                table.HasCheckConstraint("CK_m07_number_counter_values",
                    "\"FinancialYearStart\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0"));
            e.HasKey(x => new { x.TenantId, x.BranchId, x.DocumentCode, x.FinancialYearStart });
            e.Property(x => x.DocumentCode).HasMaxLength(32);
        });
        modelBuilder.Entity<M08Pour>(e =>
        {
            e.ToTable("m08_pour", table =>
            {
                table.HasCheckConstraint("CK_m08_pour_quantities",
                    "\"ScheduledQuantity\" > 0 AND \"DeliveredQuantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"PumpQuantity\" >= 0");
                table.HasCheckConstraint("CK_m08_pour_window", "\"WindowEnd\" > \"WindowStart\"");
            });
            e.HasKey(x => new { x.TenantId, x.PourId });
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(240);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(240);
            e.Property(x => x.SiteNameSnapshot).HasMaxLength(240);
            e.Property(x => x.ProductCategorySnapshot).HasMaxLength(40);
            e.Property(x => x.ScheduledQuantity).HasPrecision(18, 3);
            e.Property(x => x.DeliveredQuantity).HasPrecision(18, 3);
            e.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
            e.Property(x => x.Slump).HasMaxLength(40);
            e.Property(x => x.PlacementMethod).HasMaxLength(24);
            e.Property(x => x.PumpType).HasMaxLength(80);
            e.Property(x => x.PumpQuantity).HasPrecision(18, 3);
            e.Property(x => x.CustomerEngineer).HasMaxLength(120);
            e.Property(x => x.Coordinator).HasMaxLength(120);
            e.Property(x => x.SpecialInstructions).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.ClosureReason).HasMaxLength(1000);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.ScheduleId }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.WindowStart, x.Status });
            e.HasIndex(x => new { x.TenantId, x.PlantId, x.WindowStart, x.WindowEnd });
            e.HasOne<M06DeliverySchedule>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ScheduleId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SiteId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Plant>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PlantId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M08Trip>(e =>
        {
            e.ToTable("m08_trip", table =>
            {
                table.HasCheckConstraint("CK_m08_trip_quantities",
                    "\"PlannedQuantity\" > 0 AND \"LoadedQuantity\" >= 0 AND \"DeliveredQuantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"WaitingMinutes\" >= 0 AND \"WaitingCharge\" >= 0 AND \"PartLoadCharge\" >= 0");
                table.HasCheckConstraint("CK_m08_trip_weights",
                    "(\"GrossWeight\" IS NULL OR \"GrossWeight\" >= 0) AND (\"TareWeight\" IS NULL OR \"TareWeight\" >= 0) AND (\"NetWeight\" IS NULL OR \"NetWeight\" >= 0)");
            });
            e.HasKey(x => new { x.TenantId, x.TripId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.PlannedQuantity).HasPrecision(18, 3);
            e.Property(x => x.LoadedQuantity).HasPrecision(18, 3);
            e.Property(x => x.DeliveredQuantity).HasPrecision(18, 3);
            e.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
            e.Property(x => x.BatchSlipNumber).HasMaxLength(80);
            e.Property(x => x.LotOrHeatNumber).HasMaxLength(100);
            e.Property(x => x.MtcReference).HasMaxLength(500);
            e.Property(x => x.WeighbridgeSlipNumber).HasMaxLength(80);
            e.Property(x => x.GrossWeight).HasPrecision(18, 3);
            e.Property(x => x.TareWeight).HasPrecision(18, 3);
            e.Property(x => x.NetWeight).HasPrecision(18, 3);
            e.Property(x => x.ReceiverName).HasMaxLength(160);
            e.Property(x => x.ReceiverMobile).HasMaxLength(24);
            e.Property(x => x.SignatureReference).HasMaxLength(500);
            e.Property(x => x.PhotoReference).HasMaxLength(500);
            e.Property(x => x.EvidenceState).HasMaxLength(24);
            e.Property(x => x.EvidenceDecisionReason).HasMaxLength(1000);
            e.Property(x => x.ExceptionType).HasMaxLength(40);
            e.Property(x => x.ExceptionReason).HasMaxLength(1000);
            e.Property(x => x.WaitingMinutes).HasPrecision(18, 2);
            e.Property(x => x.WaitingCharge).HasPrecision(18, 2);
            e.Property(x => x.PartLoadCharge).HasPrecision(18, 2);
            e.Property(x => x.PurchaseRateSnapshot).HasPrecision(18, 4);
            e.Property(x => x.PurchaseCostSnapshot).HasPrecision(18, 2);
            e.Property(x => x.CostSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.PourId, x.Status });
            e.HasIndex(x => new { x.TenantId, x.VehicleId, x.Status });
            e.HasIndex(x => new { x.TenantId, x.DriverId, x.Status });
            e.HasOne<M02User>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ReceiverVerifiedByUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M08Pour>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PourId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Plant>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PlantId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Vehicle>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VehicleId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Driver>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.DriverId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M18Document>(e =>
        {
            e.ToTable("m18_document", table =>
            {
                table.HasCheckConstraint("CK_m18_document_size", "\"SizeBytes\" > 0");
                table.HasCheckConstraint("CK_m18_document_version", "\"VersionNumber\" > 0");
                table.HasCheckConstraint("CK_m18_document_purpose", "\"Purpose\" IN ('Signature', 'Photo')");
                table.HasCheckConstraint("CK_m18_document_scan_status", "\"ScanStatus\" IN ('Pending', 'Clean', 'Rejected')");
                table.HasCheckConstraint("CK_m18_document_storage_status", "\"StorageStatus\" IN ('Quarantined', 'Available', 'Deleted')");
            });
            e.HasKey(x => new { x.TenantId, x.DocumentId });
            e.Property(x => x.Purpose).HasMaxLength(24);
            e.Property(x => x.OriginalFileName).HasMaxLength(120);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.Property(x => x.StorageKey).HasMaxLength(100);
            e.Property(x => x.ScanStatus).HasMaxLength(24);
            e.Property(x => x.StorageStatus).HasMaxLength(24);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.TripId, x.Purpose, x.VersionNumber }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.TripId, x.Purpose, x.IsCurrent })
                .IsUnique().HasFilter("\"IsCurrent\" = true");
            e.HasIndex(x => new { x.TenantId, x.UploadedAt });
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.UploadedByUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M18Document>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PreviousDocumentId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M18DocumentEvent>(e =>
        {
            e.ToTable("m18_document_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.DocumentId, x.OccurredAt });
            e.HasIndex(x => new { x.TenantId, x.TripId, x.OccurredAt });
            e.HasOne<M18Document>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.DocumentId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ActorUserId }).OnDelete(DeleteBehavior.Restrict);
        });        modelBuilder.Entity<M08TripEvent>(e =>
        {
            e.ToTable("m08_trip_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.FromStatus).HasMaxLength(24);
            e.Property(x => x.ToStatus).HasMaxLength(24);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.Property(x => x.ActionId).HasMaxLength(100);
            e.HasIndex(x => new { x.TenantId, x.TripId, x.ReceivedAt });
            e.HasIndex(x => new { x.TenantId, x.ActionId }).IsUnique();
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M08DriverAction>(e =>
        {
            e.ToTable("m08_driver_action");
            e.HasKey(x => new { x.TenantId, x.DriverActionId });
            e.Property(x => x.ActionId).HasMaxLength(100);
            e.Property(x => x.ActionType).HasMaxLength(40);
            e.Property(x => x.State).HasMaxLength(24);
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.ActionId }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.DriverId, x.State, x.DeviceAt });
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Driver>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.DriverId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M08ReceiverChallenge>(e =>
        {
            e.ToTable("m08_receiver_challenge");
            e.HasKey(x => new { x.TenantId, x.ChallengeId });
            e.Property(x => x.DestinationMobileProtected).HasMaxLength(500);
            e.Property(x => x.CodeHash).HasMaxLength(200);
            e.HasIndex(x => new { x.TenantId, x.TripId, x.CreatedAt });
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M09DeliveryChallan>(e =>
        {
            e.ToTable("m09_delivery_challan", table =>
            {
                table.HasCheckConstraint("CK_m09_delivery_challan_quantities",
                    "\"DeliveredQuantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"UnitRateSnapshot\" >= 0 AND \"DeliveredValueSnapshot\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.ChallanId });
            e.Property(x => x.Number).HasMaxLength(48);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(240);
            e.Property(x => x.SiteAddressSnapshot).HasMaxLength(1000);
            e.Property(x => x.OrderNumberSnapshot).HasMaxLength(48);
            e.Property(x => x.CustomerPoNumberSnapshot).HasMaxLength(80);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(240);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(24);
            e.Property(x => x.DeliveredQuantity).HasPrecision(18, 3);
            e.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
            e.Property(x => x.UnitRateSnapshot).HasPrecision(18, 4);
            e.Property(x => x.TaxPercentSnapshot).HasPrecision(7, 4);
            e.Property(x => x.DeliveredValueSnapshot).HasPrecision(18, 2);
            e.Property(x => x.VehicleNumberSnapshot).HasMaxLength(32);
            e.Property(x => x.DriverNameSnapshot).HasMaxLength(160);
            e.Property(x => x.PlantNameSnapshot).HasMaxLength(160);
            e.Property(x => x.BatchSlipNumber).HasMaxLength(80);
            e.Property(x => x.ReceiverName).HasMaxLength(160);
            e.Property(x => x.ReceiverMobile).HasMaxLength(24);
            e.Property(x => x.EvidenceState).HasMaxLength(24);
            e.Property(x => x.SignatureReference).HasMaxLength(500);
            e.Property(x => x.PhotoReference).HasMaxLength(500);
            e.Property(x => x.EwayBillNumber).HasMaxLength(32);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.TripId }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.ChallanDate, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status, x.ChallanDate });
            e.HasOne<M08Trip>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M08Pour>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PourId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SiteId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M09ChallanRevision>(e =>
        {
            e.ToTable("m09_challan_revision", table =>
                table.HasCheckConstraint("CK_m09_challan_revision_quantities",
                    "\"RevisionNumber\" > 0 AND \"OriginalQuantity\" > 0 AND \"CorrectedQuantity\" > 0"));
            e.HasKey(x => new { x.TenantId, x.RevisionId });
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.OriginalQuantity).HasPrecision(18, 3);
            e.Property(x => x.CorrectedQuantity).HasPrecision(18, 3);
            e.HasIndex(x => new { x.TenantId, x.ChallanId, x.RevisionNumber }).IsUnique();
            e.HasOne<M09DeliveryChallan>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ChallanId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M09ChallanEvent>(e =>
        {
            e.ToTable("m09_challan_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.ChallanId, x.OccurredAt });
            e.HasOne<M09DeliveryChallan>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ChallanId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M09ChallanNumberCounter>(e =>
        {
            e.ToTable("m09_challan_number_counter", table =>
                table.HasCheckConstraint("CK_m09_challan_number_counter_values",
                    "\"FinancialYearStart\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0"));
            e.HasKey(x => new { x.TenantId, x.BranchId, x.DocumentCode, x.FinancialYearStart });
            e.Property(x => x.DocumentCode).HasMaxLength(32);
        });
        modelBuilder.Entity<M10Invoice>(e =>
        {
            e.ToTable("m10_invoice", table =>
            {
                table.HasCheckConstraint("CK_m10_invoice_amounts",
                    "\"TaxableAmount\" >= 0 AND \"CgstAmount\" >= 0 AND \"SgstAmount\" >= 0 AND \"IgstAmount\" >= 0 AND \"TcsAmount\" >= 0 AND \"TotalAmount\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.InvoiceId });
            e.Property(x => x.Number).HasMaxLength(16);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.InvoiceType).HasMaxLength(32);
            e.Property(x => x.TaxTreatment).HasMaxLength(24);
            e.Property(x => x.BillingModeSnapshot).HasMaxLength(24);
            e.Property(x => x.PlaceOfSupplyStateCode).HasMaxLength(8);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(240);
            e.Property(x => x.CustomerGstinSnapshot).HasMaxLength(15);
            e.Property(x => x.BranchGstinSnapshot).HasMaxLength(15);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TcsAmount).HasPrecision(18, 2);
            e.Property(x => x.RoundOffAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.PostingSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.EInvoiceStatus).HasMaxLength(24);
            e.Property(x => x.Irn).HasMaxLength(100);
            e.Property(x => x.AckNumber).HasMaxLength(100);
            e.Property(x => x.EwayBillNumber).HasMaxLength(32);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Status, x.InvoiceDate });
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.DueDate });
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VoucherId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M10InvoiceLine>(e =>
        {
            e.ToTable("m10_invoice_line", table =>
                table.HasCheckConstraint("CK_m10_invoice_line_amounts",
                    "\"Quantity\" > 0 AND \"Rate\" >= 0 AND \"TaxableAmount\" >= 0 AND \"TaxPercent\" >= 0 AND \"TotalAmount\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.InvoiceLineId });
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(240);
            e.Property(x => x.HsnCodeSnapshot).HasMaxLength(16);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(24);
            e.Property(x => x.ChallanReferencesJson).HasColumnType("jsonb");
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(7, 4);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxSnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.InvoiceId });
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M10InvoiceChallan>(e =>
        {
            e.ToTable("m10_invoice_challan", table =>
                table.HasCheckConstraint("CK_m10_invoice_challan_values",
                    "\"Quantity\" > 0 AND \"DeliveredValueSnapshot\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.InvoiceChallanId });
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.DeliveredValueSnapshot).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.ChallanId }).IsUnique()
                .HasFilter("\"ReleasedAt\" IS NULL");
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M09DeliveryChallan>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ChallanId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M10InvoiceEvent>(e =>
        {
            e.ToTable("m10_invoice_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.InvoiceId, x.OccurredAt });
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M11Receipt>(e =>
        {
            e.ToTable("m11_receipt", table =>
                table.HasCheckConstraint("CK_m11_receipt_amounts",
                    "\"Amount\" > 0 AND \"TdsAmount\" >= 0 AND \"BankCharges\" >= 0 AND \"AllocatedAmount\" >= 0 AND \"AdvanceAmount\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.ReceiptId });
            e.Property(x => x.Number).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.PaymentMethod).HasMaxLength(24);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TdsAmount).HasPrecision(18, 2);
            e.Property(x => x.BankCharges).HasPrecision(18, 2);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            e.Property(x => x.AdvanceAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.ReceiptDate, x.Status });
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VoucherId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M11ReceiptAllocation>(e =>
        {
            e.ToTable("m11_receipt_allocation", table =>
                table.HasCheckConstraint("CK_m11_receipt_allocation_amount", "\"Amount\" > 0"));
            e.HasKey(x => new { x.TenantId, x.AllocationId });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.ReceiptId });
            e.HasIndex(x => new { x.TenantId, x.InvoiceId });
            e.HasOne<M11Receipt>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ReceiptId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M11PromiseToPay>(e =>
        {
            e.ToTable("m11_promise_to_pay", table =>
                table.HasCheckConstraint("CK_m11_promise_amount", "\"Amount\" > 0"));
            e.HasKey(x => new { x.TenantId, x.PromiseId });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Outcome).HasMaxLength(24);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.PromiseDate, x.Outcome });
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M11ReceivableEvent>(e =>
        {
            e.ToTable("m11_receivable_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.ReceiptId, x.OccurredAt });
            e.HasOne<M11Receipt>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ReceiptId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M12LedgerMapping>(e =>
        {
            e.ToTable("m12_ledger_mapping", table =>
            {
                table.HasCheckConstraint("CK_m12_ledger_mapping_dates",
                    "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            e.HasKey(x => new { x.TenantId, x.MappingId });
            e.Property(x => x.MappingCode).HasMaxLength(32);
            e.Property(x => x.Gstin).HasMaxLength(15);
            e.Property(x => x.LedgerCode).HasMaxLength(64);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Gstin, x.MappingCode, x.EffectiveFrom }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Gstin, x.MappingCode, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        });
        modelBuilder.Entity<M12LedgerMappingEvent>(e =>
        {
            e.ToTable("m12_ledger_mapping_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(32);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.MappingId, x.OccurredAt });
            e.HasOne<M12LedgerMapping>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.MappingId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M12Voucher>(e =>
        {
            e.ToTable("m12_voucher", table =>
            {
                table.HasCheckConstraint("CK_m12_voucher_amounts",
                    "\"TotalDebit\" > 0 AND \"TotalDebit\" = \"TotalCredit\"");
            });
            e.HasKey(x => new { x.TenantId, x.VoucherId });
            e.Property(x => x.Number).HasMaxLength(32);
            e.Property(x => x.VoucherType).HasMaxLength(24);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.SourceModule).HasMaxLength(32);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Narration).HasMaxLength(1000);
            e.Property(x => x.TotalDebit).HasPrecision(18, 2);
            e.Property(x => x.TotalCredit).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.VoucherDate, x.VoucherType });
            e.HasIndex(x => new { x.TenantId, x.ReversalOfVoucherId }).IsUnique()
                .HasFilter("\"ReversalOfVoucherId\" IS NOT NULL");
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ReversalOfVoucherId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M12VoucherLine>(e =>
        {
            e.ToTable("m12_voucher_line", table =>
            {
                table.HasCheckConstraint("CK_m12_voucher_line_side",
                    "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)");
            });
            e.HasKey(x => new { x.TenantId, x.VoucherLineId });
            e.Property(x => x.LedgerCode).HasMaxLength(64);
            e.Property(x => x.Narration).HasMaxLength(500);
            e.Property(x => x.Debit).HasPrecision(18, 2);
            e.Property(x => x.Credit).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.VoucherId });
            e.HasIndex(x => new { x.TenantId, x.LedgerCode, x.BranchId });
            e.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.TripId });
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VoucherId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PartyId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M12FinancialPeriod>(e =>
        {
            e.ToTable("m12_financial_period", table =>
                table.HasCheckConstraint("CK_m12_financial_period_dates", "\"PeriodEnd\" >= \"PeriodStart\""));
            e.HasKey(x => new { x.TenantId, x.PeriodStart });
            e.Property(x => x.LockReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.PeriodStart, x.PeriodEnd, x.IsLocked });
        });
        modelBuilder.Entity<M12VoucherNumberCounter>(e =>
        {
            e.ToTable("m12_voucher_number_counter", table =>
                table.HasCheckConstraint("CK_m12_voucher_number_counter_values",
                    "\"CalendarYear\" BETWEEN 2000 AND 2200 AND \"NextNumber\" > 0"));
            e.HasKey(x => new { x.TenantId, x.BranchId, x.VoucherType, x.CalendarYear });
            e.Property(x => x.VoucherType).HasMaxLength(24);
        });
        modelBuilder.Entity<M12VoucherEvent>(e =>
        {
            e.ToTable("m12_voucher_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.VoucherId, x.OccurredAt });
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VoucherId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M12PeriodEvent>(e =>
        {
            e.ToTable("m12_period_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.PeriodStart, x.OccurredAt });
        });
        modelBuilder.Entity<M13GstPeriod>(e =>
        {
            e.ToTable("m13_gst_period", table =>
                table.HasCheckConstraint("CK_m13_gst_period_dates", "\"PeriodEnd\" >= \"PeriodStart\""));
            e.HasKey(x => new { x.TenantId, x.PeriodStart });
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.ExternalAcknowledgement).HasMaxLength(160);
            e.Property(x => x.FilingSnapshotJson).HasColumnType("jsonb");
        });
        modelBuilder.Entity<M13ExceptionReview>(e =>
        {
            e.ToTable("m13_exception_review");
            e.HasKey(x => new { x.TenantId, x.ReviewId });
            e.Property(x => x.ExceptionCode).HasMaxLength(48);
            e.Property(x => x.ReviewNote).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.PeriodStart, x.InvoiceId, x.ExceptionCode }).IsUnique();
            e.HasOne<M10Invoice>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M14ExpenseCategory>(e =>
        {
            e.ToTable("m14_expense_category", table =>
                table.HasCheckConstraint("CK_m14_expense_category_type",
                    "\"Type\" IN ('Direct','Indirect','Capital','Other income')"));
            e.HasKey(x => new { x.TenantId, x.CategoryId });
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Type).HasMaxLength(24);
            e.Property(x => x.ExpenseLedgerCode).HasMaxLength(64);
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<M14Expense>(e =>
        {
            e.ToTable("m14_expense", table =>
            {
                table.HasCheckConstraint("CK_m14_expense_amount", "\"Amount\" > 0");
                table.HasCheckConstraint("CK_m14_expense_gst", "\"GstAmount\" >= 0 AND \"GstAmount\" <= \"Amount\"");
            });
            e.HasKey(x => new { x.TenantId, x.ExpenseId });
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.PaidFromLedgerCode).HasMaxLength(64);
            e.Property(x => x.SupplierGstin).HasMaxLength(15);
            e.Property(x => x.SupplierBillNumber).HasMaxLength(64);
            e.Property(x => x.AttachmentReference).HasMaxLength(500);
            e.Property(x => x.ApprovalComment).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.VendorPartyId, x.SupplierBillNumber, x.Amount }).IsUnique()
                .HasFilter("\"SupplierBillNumber\" <> ''");
            e.HasOne<M14ExpenseCategory>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VendorPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M12Voucher>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.VoucherId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M14ExpenseEvent>(e =>
        {
            e.ToTable("m14_expense_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.Detail).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.ExpenseId, x.OccurredAt });
            e.HasOne<M14Expense>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ExpenseId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M16ExportLog>(e =>
        {
            e.ToTable("m16_export_log");
            e.HasKey(x => new { x.TenantId, x.ExportId });
            e.Property(x => x.ReportCode).HasMaxLength(32);
            e.Property(x => x.FiltersJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.UserId, x.ExportedAt });
        });
        modelBuilder.Entity<M19QualityCriteria>(e =>
        {
            e.ToTable("m19_quality_criteria", table =>
            {
                table.HasCheckConstraint("CK_m19_criteria_age", "\"TestAgeDays\" BETWEEN 1 AND 365");
                table.HasCheckConstraint("CK_m19_criteria_strength", "\"MinimumStrengthMpa\" > 0");
                table.HasCheckConstraint("CK_m19_criteria_effective", "\"EffectiveUntil\" IS NULL OR \"EffectiveUntil\" >= \"EffectiveFrom\"");
            });
            e.HasKey(x => new { x.TenantId, x.CriteriaId });
            e.Property(x => x.MaterialType).HasMaxLength(24);
            e.Property(x => x.Grade).HasMaxLength(80);
            e.Property(x => x.MinimumStrengthMpa).HasPrecision(9, 2);
            e.Property(x => x.StandardReference).HasMaxLength(240);
            e.Property(x => x.ChangeReason).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.MaterialType, x.Grade, x.TestAgeDays, x.EffectiveFrom }).IsUnique();
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.CreatedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19QualitySample>(e =>
        {
            e.ToTable("m19_quality_sample", table =>
                table.HasCheckConstraint("CK_m19_sample_cube_count", "\"CubeCount\" IN (3, 6)"));
            e.HasKey(x => new { x.TenantId, x.SampleId });
            e.Property(x => x.SampleNumber).HasMaxLength(32);
            e.Property(x => x.MaterialType).HasMaxLength(24);
            e.Property(x => x.GradeSnapshot).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.SampleNumber }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Status, x.CastAt });
            e.HasIndex(x => new { x.TenantId, x.TripId, x.CreatedAt });
            e.HasOne<M08Trip>().WithMany().HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M08Pour>().WithMany().HasForeignKey(x => new { x.TenantId, x.PourId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrderLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId }).HasPrincipalKey(x => new { x.TenantId, x.LineId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.OwnerUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.CreatedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19QualityTest>(e =>
        {
            e.ToTable("m19_quality_test", table =>
            {
                table.HasCheckConstraint("CK_m19_test_age", "\"TestAgeDays\" BETWEEN 1 AND 365");
                table.HasCheckConstraint("CK_m19_test_strength", "\"StrengthMpa\" IS NULL OR \"StrengthMpa\" >= 0");
            });
            e.HasKey(x => new { x.TenantId, x.TestId });
            e.Property(x => x.MinimumStrengthMpaSnapshot).HasPrecision(9, 2);
            e.Property(x => x.StrengthMpa).HasPrecision(9, 2);
            e.Property(x => x.AcceptanceReferenceSnapshot).HasMaxLength(240);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Outcome).HasMaxLength(24);
            e.Property(x => x.ResultReason).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.SampleId, x.TestAgeDays }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status, x.DueAt });
            e.HasOne<M19QualitySample>().WithMany().HasForeignKey(x => new { x.TenantId, x.SampleId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M18Document>().WithMany().HasForeignKey(x => new { x.TenantId, x.ReportDocumentId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecordedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19Dispute>(e =>
        {
            e.ToTable("m19_dispute", table =>
            {
                table.HasCheckConstraint("CK_m19_dispute_source", "\"SalesOrderId\" IS NOT NULL OR \"TripId\" IS NOT NULL OR \"ChallanId\" IS NOT NULL OR \"SampleId\" IS NOT NULL OR \"CustomerPartyId\" IS NOT NULL OR \"SupplierPartyId\" IS NOT NULL");
                table.HasCheckConstraint("CK_m19_dispute_status", "\"Status\" IN ('Open', 'Investigating', 'Resolved', 'Escalated', 'Rejected')");
            });
            e.HasKey(x => new { x.TenantId, x.DisputeId });
            e.Property(x => x.Type).HasMaxLength(24);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.Resolution).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Status, x.OpenedAt });
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.SampleId, x.Status });
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Party>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierPartyId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M03Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M06SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M08Trip>().WithMany().HasForeignKey(x => new { x.TenantId, x.TripId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M09DeliveryChallan>().WithMany().HasForeignKey(x => new { x.TenantId, x.ChallanId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M19QualitySample>().WithMany().HasForeignKey(x => new { x.TenantId, x.SampleId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.OwnerUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.OpenedByUserId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.ResolvedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19DisputeInvoice>(e =>
        {
            e.ToTable("m19_dispute_invoice");
            e.HasKey(x => new { x.TenantId, x.DisputeId, x.InvoiceId });
            e.HasOne<M19Dispute>().WithMany().HasForeignKey(x => new { x.TenantId, x.DisputeId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M10Invoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.InvoiceId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19DisputeEvidence>(e =>
        {
            e.ToTable("m19_dispute_evidence");
            e.HasKey(x => new { x.TenantId, x.EvidenceId });
            e.HasIndex(x => new { x.TenantId, x.DisputeId, x.AddedAt });
            e.HasIndex(x => new { x.TenantId, x.DisputeId, x.DocumentId }).IsUnique();
            e.HasOne<M19Dispute>().WithMany().HasForeignKey(x => new { x.TenantId, x.DisputeId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M18Document>().WithMany().HasForeignKey(x => new { x.TenantId, x.DocumentId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.AddedByUserId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M19QualityEvent>(e =>
        {
            e.ToTable("m19_quality_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EntityType).HasMaxLength(24);
            e.Property(x => x.EventType).HasMaxLength(48);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.OccurredAt });
            e.HasOne<M02User>().WithMany().HasForeignKey(x => new { x.TenantId, x.ActorUserId }).OnDelete(DeleteBehavior.Restrict);
        });        modelBuilder.Entity<M17CatalogueItem>(e =>
        {
            e.ToTable("m17_catalogue_item", table =>
                table.HasCheckConstraint("CK_m17_catalogue_price", "\"IndicativePrice\" IS NULL OR \"IndicativePrice\" >= 0"));
            e.HasKey(x => new { x.TenantId, x.CatalogueItemId });
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.ImageReference).HasMaxLength(500);
            e.Property(x => x.KeySpecs).HasMaxLength(2000);
            e.HasIndex(x => new { x.TenantId, x.ProductId }).IsUnique();
            e.HasOne<M03Product>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M17ConsentRecord>(e =>
        {
            e.ToTable("m17_consent_record");
            e.HasKey(x => new { x.TenantId, x.ConsentId });
            e.Property(x => x.Channel).HasMaxLength(16);
            e.Property(x => x.Source).HasMaxLength(120);
            e.Property(x => x.Wording).HasMaxLength(1000);
            e.Property(x => x.OptOutSource).HasMaxLength(120);
            e.HasIndex(x => new { x.TenantId, x.ContactId, x.Channel }).IsUnique();
            e.HasOne<M03Contact>().WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ContactId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<M17PromotionEvent>(e =>
        {
            e.ToTable("m17_promotion_event");
            e.HasKey(x => new { x.TenantId, x.EventId });
            e.Property(x => x.EventType).HasMaxLength(40);
            e.Property(x => x.DetailJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType =>
                         typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType)))
        {
            var entity = Expression.Parameter(entityType.ClrType, "entity");
            var entityTenant = Expression.Property(
                entity,
                nameof(ITenantOwned.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            var tenantFilter = Expression.Lambda(
                Expression.Equal(entityTenant, currentTenantId),
                entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(tenantFilter);
        }
    }

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantBoundary();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(
            acceptAllChangesOnSuccess: true,
            cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantBoundary();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void EnforceTenantBoundary()
    {
        var tenantEntries = ChangeTracker.Entries<ITenantOwned>().ToArray();
        if (tenantEntries.Length == 0)
        {
            return;
        }

        var tenantId = currentTenant.TenantId;
        foreach (var entry in tenantEntries)
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = tenantId;
            }

            if (entry.Entity.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException(
                    "A tenant-owned entity cannot be saved outside the authenticated tenant scope.");
            }
        }
    }
}
