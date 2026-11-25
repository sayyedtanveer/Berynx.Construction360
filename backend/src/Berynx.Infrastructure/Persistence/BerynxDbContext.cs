using System.Linq.Expressions;
using Berynx.Domain.Onboarding;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
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
            });
            e.HasKey(x => new { x.TenantId, x.PriceListId });
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.CustomerGroup).HasMaxLength(100);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.CustomerPartyId, x.ValidFrom }).IsUnique()
                .HasFilter("\"CustomerPartyId\" IS NOT NULL");
            e.HasIndex(x => new { x.TenantId, x.CustomerGroup, x.ValidFrom }).IsUnique()
                .HasFilter("\"CustomerGroup\" IS NOT NULL");
            e.HasIndex(x => new { x.TenantId, x.ValidFrom }).IsUnique().HasFilter("\"IsDefault\"");
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
            });
            e.HasKey(x => new { x.TenantId, x.RateCardId });
            e.Property(x => x.Grade).HasMaxLength(32);
            e.Property(x => x.SlumpBand).HasMaxLength(32);
            e.Property(x => x.AdmixtureCode).HasMaxLength(64);
            e.Property(x => x.PumpType).HasMaxLength(32);
            e.Property(x => x.Rate).HasPrecision(18, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<M03Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new
            {
                x.TenantId, x.ProductId, x.Grade, x.SlumpBand, x.AdmixtureCode, x.PumpType, x.EffectiveFrom
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
