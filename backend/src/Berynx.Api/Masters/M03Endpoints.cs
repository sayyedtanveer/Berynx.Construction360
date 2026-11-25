using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Berynx.Api.IdentityAccess;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Masters;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Masters;

public static class M03Endpoints
{
    public sealed record PartyWrite(
        string LegalName, string? TradeName, string PartyType, string? Gstin, string? Pan,
        string? CustomerCategory, string? City, string? StateCode, string? BillingAddress,
        string? CustomerGroup, decimal CreditLimit, int CreditDays, Guid? PaymentTermId,
        Guid? DefaultPriceListId, string CreditPolicy,
        string CreditStatus, bool IsMsme, string? UdyamNumber, string? TdsCategory, string[] Roles, long Version,
        Guid? SalespersonUserId = null, string? TagsJson = null);
    public sealed record SiteWrite(Guid CustomerPartyId, Guid? BranchId, string Name, string Address, string City,
        string StateCode, string? Gstin, decimal? Latitude, decimal? Longitude, Guid? DefaultPlantId,
        string AccessNotes, string Status, long Version);
    public sealed record ContactWrite(Guid PartyId, Guid? SiteId, string Name, string Role, string? Email,
        string? Mobile, bool EmailConsent, bool SmsConsent, bool WhatsAppConsent, string? ConsentSource, long Version);
    public sealed record PlantWrite(Guid SupplierPartyId, Guid? BranchId, string Name, string Address, string City,
        string StateCode, decimal? CapacityPerHour, string[]? Grades, long Version = 0);
    public sealed record ProductWrite(string Code, string Name, string Category, string HsnCode,
        string BaseUnitCode, string CategoryAttributesJson, string QualityReference, bool Traceable, long Version);
    public sealed record TaxRateWrite(string HsnCode, decimal RatePercent, DateOnly EffectiveFrom,
        DateOnly? EffectiveTo, string Reason, long Version = 0);
    public sealed record TaxRateLookup(
        Guid ProductId, string? CustomerCategory, DateOnly? AsOf,
        string? SellerGstin = null, string? BuyerGstin = null, string? PlaceOfSupplyStateCode = null);
    public sealed record CategoryTaxOverrideWrite(Guid ProductId, string CustomerCategory, decimal RatePercent,
        DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Reason);
    public sealed record PriceListWrite(string Name, Guid? CustomerPartyId, string? CustomerGroup, bool IsDefault,
        DateOnly ValidFrom, DateOnly? ValidTo, long Version);
    public sealed record PriceLineWrite(Guid ProductId, decimal Rate, decimal? MinimumRate,
        decimal? MinimumMarginPercent, long Version);
    public sealed record PriceLookup(Guid CustomerPartyId, Guid ProductId, DateOnly? AsOf);
    public sealed record PriceApprovalWrite(Guid CustomerPartyId, Guid ProductId, decimal ProposedRate,
        decimal Quantity, decimal? MarginPercent, string Reason);
    public sealed record RmcRateWrite(Guid ProductId, string Grade, string SlumpBand, string AdmixtureCode,
        string PumpType, decimal Rate, Guid? AdmixtureChargeTypeId, Guid? PumpChargeTypeId,
        DateOnly EffectiveFrom, DateOnly? EffectiveTo, long Version);
    public sealed record RmcLookup(Guid ProductId, string Grade, string SlumpBand, string AdmixtureCode,
        string PumpType, DateOnly? AsOf);
    public sealed record UnitWrite(string UnitCode, string Name, string Symbol, long Version = 0);
    public sealed record ConversionWrite(string FromUnitCode, string ToUnitCode, decimal Factor, long Version = 0);
    public sealed record PaymentTermWrite(string Name, int DueDays, string? LedgerCode, long Version);
    public sealed record ChargeTypeWrite(string Code, string Name, string? LedgerCode, string? HsnCode,
        decimal? DefaultSurchargeRate, decimal? DefaultTaxRatePercent, long Version);
    public sealed record VehicleWrite(string RegistrationNumber, string Type, decimal Capacity,
        string CapacityUnitCode, string Ownership, Guid? TransporterPartyId, DateOnly? InsuranceExpiresOn,
        DateOnly? PucExpiresOn, DateOnly? FitnessExpiresOn, long Version);
    public sealed record DriverWrite(string Name, string Mobile, string LicenceNumber, DateOnly LicenceExpiresOn,
        Guid? VehicleId, long Version);
    public sealed record EquipmentWrite(string Name, string EquipmentType, string PumpType, decimal? Capacity,
        string? CapacityUnitCode, Guid? OwnerPartyId, DateOnly? InsuranceExpiresOn, DateOnly? FitnessExpiresOn,
        long Version);
    public sealed record CreditOverrideWrite(string Reason, decimal Amount, decimal? MarginPercent);
    public sealed record PartyBankAccountUpdate(
        string AccountHolder, string? AccountNumber, string Ifsc, string? BankName, bool IsPrimary, long Version);
    public sealed record MergePartyWrite(Guid SurvivorPartyId, long SourceVersion, long SurvivorVersion, string Reason);
    public sealed record ConfirmImportWrite(string DuplicateHandling);
    private sealed record ImportUndoItem(string EntityType, Guid Id, bool Created, long ExpectedVersion,
        string? BeforeJson);
    private sealed record ImportBatchData(Dictionary<string, string>[] Rows, ImportUndoItem[] Undo);
    private sealed record ImportPartyUndo(M03Party Party, string[] Roles);
    private sealed record FleetExpiryAlert(
        string Type, Guid Id, string Name, string Document, DateOnly ExpiresOn, int DaysRemaining, string Urgency);
    public sealed record OnboardingAccountWrite(string Name, string Kind, string? Upi, string? AccountNumber,
        string? Ifsc);
    public sealed record PartyBankAccountWrite(Guid PartyId, string AccountHolder, string AccountNumber,
        string Ifsc, string? BankName, bool IsPrimary);

    public static void MapM03(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m03")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule(
                "M03",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/parties", ListParties);
        api.MapGet("/capabilities", Capabilities);
        api.MapPost("/parties", CreateParty);
        api.MapGet("/parties/duplicates", FindPartyMobileDuplicates);
        api.MapPut("/parties/{partyId:guid}", UpdateParty);
        api.MapPost("/parties/{partyId:guid}/deactivate", DeactivateParty);
        api.MapPost("/parties/{partyId:guid}/merge", MergeParty);
        api.MapGet("/parties/{partyId:guid}/duplicates", FindDuplicates);
        api.MapGet("/customers/{partyId:guid}/360", Customer360);
        api.MapGet("/customers/{partyId:guid}/credit-evaluation", EvaluateCredit);
        api.MapPost("/customers/{partyId:guid}/credit-override", RequestCreditOverride);
        api.MapGet("/parties/{partyId:guid}/bank-accounts", ListPartyBankAccounts);
        api.MapGet("/parties/{partyId:guid}/bank-accounts/audit", PartyBankAccountAudit);
        api.MapPost("/party-bank-accounts", CreatePartyBankAccount);
        api.MapPut("/party-bank-accounts/{bankAccountId:guid}", UpdatePartyBankAccount);
        api.MapPost("/party-bank-accounts/{bankAccountId:guid}/deactivate", DeactivatePartyBankAccount);

        api.MapGet("/sites", ListSites);
        api.MapPost("/sites", CreateSite);
        api.MapPut("/sites/{siteId:guid}", UpdateSite);
        api.MapPost("/sites/{siteId:guid}/deactivate", DeactivateSite);
        api.MapGet("/contacts", ListContacts);
        api.MapPost("/contacts", CreateContact);
        api.MapPut("/contacts/{contactId:guid}", UpdateContact);
        api.MapPost("/contacts/{contactId:guid}/deactivate", DeactivateContact);

        api.MapGet("/plants", ListPlants);
        api.MapPost("/plants", CreatePlant);
        api.MapPut("/plants/{plantId:guid}", UpdatePlant);
        api.MapPost("/plants/{plantId:guid}/deactivate", DeactivatePlant);
        api.MapGet("/products", ListProducts);
        api.MapPost("/products", CreateProduct);
        api.MapPut("/products/{productId:guid}", UpdateProduct);
        api.MapPost("/products/{productId:guid}/deactivate", DeactivateProduct);
        api.MapGet("/tax-rates", ListTaxRates);
        api.MapPost("/tax-rates", CreateTaxRate);
        api.MapPost("/tax-rates/{taxRateId:guid}/deactivate", DeactivateTaxRate);
        api.MapPost("/tax-rates/resolve", ResolveTaxRate);
        api.MapGet("/category-tax-overrides", ListCategoryTaxOverrides);
        api.MapPost("/category-tax-overrides", CreateCategoryTaxOverride);

        api.MapGet("/price-lists", ListPriceLists);
        api.MapPost("/price-lists", CreatePriceList);
        api.MapPut("/price-lists/{priceListId:guid}", UpdatePriceList);
        api.MapPost("/price-lists/{priceListId:guid}/deactivate", DeactivatePriceList);
        api.MapGet("/price-lists/{priceListId:guid}/lines", ListPriceLines);
        api.MapPost("/price-lists/{priceListId:guid}/lines", UpsertPriceLine);
        api.MapPost("/prices/resolve", ResolvePrice);
        api.MapPost("/prices/request-approval", RequestPriceApproval);
        api.MapGet("/rmc-rate-cards", ListRmcRateCards);
        api.MapPost("/rmc-rate-cards", CreateRmcRateCard);
        api.MapPost("/rmc-rate-cards/{rateCardId:guid}/deactivate", DeactivateRmcRateCard);
        api.MapPost("/rmc-rate-cards/resolve", ResolveRmcRateCard);

        api.MapGet("/vehicles", ListVehicles);
        api.MapPost("/vehicles", CreateVehicle);
        api.MapPut("/vehicles/{vehicleId:guid}", UpdateVehicle);
        api.MapPost("/vehicles/{vehicleId:guid}/deactivate", DeactivateVehicle);
        api.MapGet("/drivers", ListDrivers);
        api.MapPost("/drivers", CreateDriver);
        api.MapPut("/drivers/{driverId:guid}", UpdateDriver);
        api.MapPost("/drivers/{driverId:guid}/deactivate", DeactivateDriver);
        api.MapGet("/equipment", ListEquipment);
        api.MapPost("/equipment", CreateEquipment);
        api.MapPut("/equipment/{equipmentId:guid}", UpdateEquipment);
        api.MapPost("/equipment/{equipmentId:guid}/deactivate", DeactivateEquipment);
        api.MapGet("/fleet/expiries", FleetExpiries);
        api.MapGet("/fleet/vehicles/{vehicleId:guid}/dispatch-eligibility", DispatchEligibility);
        api.MapGet("/fleet/drivers/{driverId:guid}/eligibility", DriverEligibility);
        api.MapGet("/fleet/equipment/{equipmentId:guid}/eligibility", EquipmentEligibility);
        api.MapPost("/fleet/vehicles/{vehicleId:guid}/expiry-override", RequestFleetOverride);
        api.MapPost("/fleet/expiry-notifications/refresh", RefreshFleetExpiryNotifications);

        api.MapGet("/units", ListUnits);
        api.MapPost("/units", CreateUnit);
        api.MapPut("/units/{unitCode}", UpdateUnit);
        api.MapPost("/units/{unitCode}/deactivate", DeactivateUnit);
        api.MapGet("/unit-conversions", ListConversions);
        api.MapPost("/unit-conversions", CreateConversion);
        api.MapPost("/unit-conversions/{conversionId:guid}/deactivate", DeactivateConversion);
        api.MapGet("/payment-terms", ListPaymentTerms);
        api.MapPost("/payment-terms", CreatePaymentTerm);
        api.MapPost("/payment-terms/{paymentTermId:guid}/deactivate", DeactivatePaymentTerm);
        api.MapGet("/charge-types", ListChargeTypes);
        api.MapPost("/charge-types", CreateChargeType);
        api.MapPost("/charge-types/{chargeTypeId:guid}/deactivate", DeactivateChargeType);
        api.MapGet("/bank-cash-accounts", ListOnboardingAccounts);
        api.MapPut("/bank-cash-accounts", ReplaceOnboardingAccounts);
        api.MapGet("/imports/templates/{entityType}", ImportTemplate);
        api.MapPost("/imports/{entityType}/preview", PreviewImport).DisableAntiforgery();
        api.MapPost("/imports/{batchId:guid}/confirm", ConfirmImport);
        api.MapPost("/imports/{batchId:guid}/undo", UndoImport);
    }

    private static async Task<IResult> ListParties(
        string? role, string? search, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var query = db.Set<M03Party>().AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.LegalName.ToLower().Contains(value) ||
                (x.TradeName != null && x.TradeName.ToLower().Contains(value)) ||
                (x.Gstin != null && x.Gstin.Contains(search.Trim().ToUpper())));
        }
        var parties = await query.OrderBy(x => x.LegalName).Take(500).ToListAsync(ct);
        var roles = await db.Set<M03PartyRole>().AsNoTracking()
            .Where(x => parties.Select(p => p.PartyId).Contains(x.PartyId))
            .ToListAsync(ct);
        var filtered = parties.Where(p => string.IsNullOrWhiteSpace(role) ||
            roles.Any(r => r.PartyId == p.PartyId && r.RoleCode == role));
        return Results.Ok(filtered.Select(p => new
        {
            p.PartyId, p.LegalName, p.TradeName, p.PartyType, p.Gstin, p.Pan, p.City, p.StateCode,
            p.CustomerCategory, p.CustomerGroup, p.SalespersonUserId, p.TagsJson, p.BillingAddress,
            p.PaymentTermId, p.DefaultPriceListId,
            p.CreditLimit, p.CreditDays, p.CreditPolicy, p.CreditStatus,
            p.IsMsme, p.UdyamNumber, p.TdsCategory, p.Version,
            roles = roles.Where(r => r.PartyId == p.PartyId).Select(r => r.RoleCode)
        }));
    }

    private static async Task<IResult> Capabilities(
        M03MasterService masters, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        return Results.Ok(new
        {
            canCreate = await masters.HasTenantWidePermission(user.UserId, "masters.create", ct),
            canEdit = await masters.HasTenantWidePermission(user.UserId, "masters.edit", ct),
            canDeactivate = await masters.HasTenantWidePermission(user.UserId, "masters.deactivate", ct),
            canViewAudit = await masters.HasTenantWidePermission(user.UserId, "audit.view", ct)
        });
    }

    private static async Task<IResult> CreateParty(
        PartyWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        ValidateParty(request);
        var (gstin, pan, stateCode) = ResolvePartyTaxIdentity(request.Gstin, request.Pan, request.StateCode);
        var now = DateTimeOffset.UtcNow;
        var party = new M03Party
        {
            TenantId = user.TenantId, PartyId = Guid.NewGuid(), LegalName = request.LegalName.Trim(),
            TradeName = Clean(request.TradeName), PartyType = request.PartyType, Gstin = EmptyToNull(gstin),
            Pan = pan, CustomerCategory = Clean(request.CustomerCategory), City = Clean(request.City),
            StateCode = stateCode, BillingAddress = Clean(request.BillingAddress),
            CustomerGroup = Clean(request.CustomerGroup), SalespersonUserId = request.SalespersonUserId,
            TagsJson = NormalizePartyTags(request.TagsJson), CreditLimit = request.CreditLimit,
            CreditDays = request.CreditDays, PaymentTermId = request.PaymentTermId,
            DefaultPriceListId = request.DefaultPriceListId, CreditPolicy = request.CreditPolicy,
            CreditStatus = request.CreditStatus, IsMsme = request.IsMsme,
            UdyamNumber = Clean(request.UdyamNumber), TdsCategory = Clean(request.TdsCategory),
            CreatedAt = now, UpdatedAt = now
        };
        await ValidatePartyReferences(request, party.PartyId, db, ct);
        db.Add(party);
        foreach (var role in NormalizeRoles(request.Roles))
            db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = party.PartyId, RoleCode = role });
        var duplicates = await FindPartyDuplicates(db, party, ct);
        masters.Audit(user, "Create", "M03Party", party.PartyId, null, party);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/parties/{party.PartyId}", new
        {
            party.PartyId, party.Version, duplicateWarnings = duplicates
        });
    }

    private static async Task<IResult> UpdateParty(
        Guid partyId, PartyWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        ValidateParty(request);
        var party = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == partyId, ct)
            ?? throw new KeyNotFoundException("Party was not found.");
        if (party.Version != request.Version)
            throw new DbUpdateConcurrencyException();
        await ValidatePartyReferences(request, partyId, db, ct);
        var before = new { party.LegalName, party.Gstin, party.Pan, party.CreditLimit, party.CreditDays, party.CreditPolicy, party.CreditStatus };
        party.LegalName = request.LegalName.Trim();
        party.TradeName = Clean(request.TradeName);
        party.PartyType = request.PartyType;
        var (gstin, pan, stateCode) = ResolvePartyTaxIdentity(request.Gstin, request.Pan, request.StateCode);
        party.Gstin = EmptyToNull(gstin);
        party.Pan = pan;
        party.CustomerCategory = Clean(request.CustomerCategory);
        party.City = Clean(request.City);
        party.StateCode = stateCode;
        party.BillingAddress = Clean(request.BillingAddress);
        party.CustomerGroup = Clean(request.CustomerGroup);
        party.SalespersonUserId = request.SalespersonUserId;
        party.TagsJson = NormalizePartyTags(request.TagsJson);
        party.CreditLimit = request.CreditLimit;
        party.CreditDays = request.CreditDays;
        party.PaymentTermId = request.PaymentTermId;
        party.DefaultPriceListId = request.DefaultPriceListId;
        party.CreditPolicy = request.CreditPolicy;
        party.CreditStatus = request.CreditStatus;
        party.IsMsme = request.IsMsme;
        party.UdyamNumber = Clean(request.UdyamNumber);
        party.TdsCategory = Clean(request.TdsCategory);
        party.Version++;
        party.UpdatedAt = DateTimeOffset.UtcNow;
        await ReplacePartyRoles(user, db, partyId, request.Roles, ct);
        masters.Audit(user, "Update", "M03Party", partyId, before, party);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { party.PartyId, party.Version });
    }

    private static async Task<IResult> DeactivateParty(
        Guid partyId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var party = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == partyId, ct)
            ?? throw new KeyNotFoundException("Party was not found.");
        if (party.Version != version) throw new DbUpdateConcurrencyException();
        party.IsActive = false;
        party.Version++;
        party.UpdatedAt = DateTimeOffset.UtcNow;
        masters.Audit(user, "Deactivate", "M03Party", partyId, new { party.IsActive }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> FindDuplicates(
        Guid partyId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var party = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == partyId, ct)
            ?? throw new KeyNotFoundException("Party was not found.");
        return Results.Ok(await FindPartyDuplicates(db, party, ct));
    }

    private static async Task<IResult> MergeParty(
        Guid partyId, MergePartyWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500 || request.SurvivorPartyId == partyId)
            throw new ValidationException("Choose a different survivor and provide a merge reason.");
        var source = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == partyId, ct)
            ?? throw new KeyNotFoundException("Duplicate party was not found.");
        var survivor = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == request.SurvivorPartyId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Surviving party was not found.");
        if (source.Version != request.SourceVersion || survivor.Version != request.SurvivorVersion)
            throw new DbUpdateConcurrencyException();
        if (!source.IsActive) throw new ValidationException("Only an active duplicate can be merged.");
        var before = new { source.PartyId, source.LegalName, source.Gstin, source.Pan, survivorPartyId = survivor.PartyId };
        var sourceRoles = await db.Set<M03PartyRole>().Where(x => x.PartyId == partyId).ToListAsync(ct);
        foreach (var role in sourceRoles)
        {
            if (!await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == survivor.PartyId && x.RoleCode == role.RoleCode, ct))
                db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = survivor.PartyId, RoleCode = role.RoleCode });
        }
        foreach (var site in await db.Set<M03Site>().Where(x => x.CustomerPartyId == partyId).ToListAsync(ct))
            site.CustomerPartyId = survivor.PartyId;
        foreach (var contact in await db.Set<M03Contact>().Where(x => x.PartyId == partyId).ToListAsync(ct))
            contact.PartyId = survivor.PartyId;
        foreach (var plant in await db.Set<M03Plant>().Where(x => x.SupplierPartyId == partyId).ToListAsync(ct))
            plant.SupplierPartyId = survivor.PartyId;
        foreach (var bank in await db.Set<M03PartyBankAccount>().Where(x => x.PartyId == partyId).ToListAsync(ct))
            bank.PartyId = survivor.PartyId;
        foreach (var priceList in await db.Set<M03PriceList>().Where(x => x.CustomerPartyId == partyId).ToListAsync(ct))
        {
            if (await db.Set<M03PriceList>().AnyAsync(x => x.PriceListId != priceList.PriceListId &&
                    x.CustomerPartyId == survivor.PartyId && x.IsActive &&
                    x.ValidFrom <= (priceList.ValidTo ?? DateOnly.MaxValue) &&
                    (x.ValidTo == null || x.ValidTo >= priceList.ValidFrom), ct))
                throw new ValidationException("Merge blocked because customer price lists would conflict.");
            priceList.CustomerPartyId = survivor.PartyId;
        }
        source.IsActive = false;
        source.CreditStatus = "Merged";
        source.Version++;
        source.UpdatedAt = DateTimeOffset.UtcNow;
        survivor.Version++;
        survivor.UpdatedAt = DateTimeOffset.UtcNow;
        db.Add(new M03PartyMerge
        {
            TenantId = user.TenantId, MergeId = Guid.NewGuid(), SourcePartyId = partyId,
            SurvivorPartyId = survivor.PartyId, Reason = request.Reason.Trim(),
            MergedBy = user.UserId.ToString(), MergedAt = DateTimeOffset.UtcNow
        });
        masters.Audit(user, "Merge", "M03Party", partyId, before,
            new { survivor.PartyId, request.Reason }, request.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { sourcePartyId = partyId, survivorPartyId = survivor.PartyId });
    }

    private static async Task<IResult> Customer360(
        Guid partyId, M03MasterService masters, BerynxDbContext db,
        IM03CreditFactsProvider creditFactsProvider, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var party = await db.Set<M03Party>().AsNoTracking().SingleOrDefaultAsync(x => x.PartyId == partyId, ct)
            ?? throw new KeyNotFoundException("Customer was not found.");
        if (!await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == partyId && x.RoleCode == "Customer", ct))
            throw new KeyNotFoundException("Customer was not found.");
        var sites = await db.Set<M03Site>().AsNoTracking().Where(x => x.CustomerPartyId == partyId)
            .OrderBy(x => x.Name).ToListAsync(ct);
        var contacts = await db.Set<M03Contact>().AsNoTracking().Where(x => x.PartyId == partyId)
            .OrderBy(x => x.Name).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pricingAgreements = await db.Set<M03PriceList>().AsNoTracking()
            .Where(x => x.IsActive && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today) &&
                (x.CustomerPartyId == partyId ||
                 (x.CustomerPartyId == null && x.CustomerGroup != null && x.CustomerGroup == party.CustomerGroup) ||
                 x.IsDefault))
            .OrderByDescending(x => x.CustomerPartyId == partyId)
            .ThenByDescending(x => x.ValidFrom)
            .Select(x => new { x.PriceListId, x.Name, x.CustomerPartyId, x.CustomerGroup, x.IsDefault, x.ValidFrom, x.ValidTo })
            .ToListAsync(ct);
        var canViewAudit = await masters.HasTenantWidePermission(user.UserId, "audit.view", ct);
        var siteIds = sites.Select(x => x.SiteId).ToArray();
        var contactIds = contacts.Select(x => x.ContactId).ToArray();
        var activity = canViewAudit
            ? await db.Set<M02AuditEvent>().AsNoTracking()
                .Where(x => (x.EntityType == "M03Party" && x.EntityId == partyId) ||
                    (x.EntityType == "M03Site" && siteIds.Contains(x.EntityId ?? Guid.Empty)) ||
                    (x.EntityType == "M03Contact" && contactIds.Contains(x.EntityId ?? Guid.Empty)))
                .OrderByDescending(x => x.At).Take(100)
                .Select(x => new { x.EventId, x.At, x.ActorSubject, x.Action, x.EntityType, x.EntityId, x.Reason })
                .ToListAsync(ct)
            : [];
        var facts = await creditFactsProvider.GetFacts(partyId, ct);
        var creditDecision = M03CreditControl.Evaluate(
            new M03CreditProfile(party.PartyId, party.CreditLimit, party.CreditDays,
                party.CreditPolicy, party.CreditStatus),
            facts, 0m, await masters.HasTenantWidePermission(user.UserId, "credit.override", ct));
        return Results.Ok(new
        {
            party = new
            {
                party.PartyId, party.LegalName, party.TradeName, party.PartyType, party.Gstin, party.Pan,
                party.City, party.StateCode, party.BillingAddress, party.CustomerCategory, party.CustomerGroup,
                party.SalespersonUserId, party.TagsJson, party.PaymentTermId, party.DefaultPriceListId,
                party.CreditLimit, party.CreditDays, party.CreditPolicy, party.CreditStatus,
                party.IsActive, party.Version
            },
            sites,
            contacts,
            pricingAgreements,
            activity,
            activityAvailable = canViewAudit,
            creditDecision,
            creditFacts = new
            {
                available = facts.Available,
                outstanding = facts.Outstanding,
                overdue = facts.Overdue,
                unavailableReason = facts.Available ? null : facts.UnavailableReason
            },
            metrics = new
            {
                available = false,
                invoicedAmount = (decimal?)null,
                collections = (decimal?)null,
                lastOrderAt = (DateTimeOffset?)null,
                ytdSales = (decimal?)null,
                ytdProfit = (decimal?)null,
                unavailableReason = "Not available until sales and accounting transaction modules are activated."
            },
            linkedDocuments = new
            {
                available = false,
                message = "Not available until transaction and document modules are activated."
            },
            unavailableIntegrations = new[]
            {
                "Enquiries", "Quotations", "Orders", "Deliveries", "Invoices", "Receipts", "Ledger", "Documents"
            }
        });
    }

    private static async Task<IResult> EvaluateCredit(
        Guid partyId, decimal? amount, M03MasterService masters,
        M03CreditEvaluationService creditEvaluation, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        if (amount is < 0)
            throw new ValidationException("Proposed amount cannot be negative.");
        var assessment = await creditEvaluation.Evaluate(
            partyId, amount ?? 0m,
            await masters.HasTenantWidePermission(user.UserId, "credit.override", ct), ct);
        return Results.Ok(assessment.Decision);
    }

    private static async Task<IResult> RequestCreditOverride(
        Guid partyId, CreditOverrideWrite request, M03MasterService masters, BerynxDbContext db,
        IM02ApprovalEngine approvals, M03CreditEvaluationService creditEvaluation, CancellationToken ct)
    {
        var user = await masters.Require("credit.override", ct);
        await masters.RequireTenantWide(user, "credit.override", ct);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500 || request.Amount < 0)
            throw new ValidationException("Enter a reason and a non-negative amount.");
        var party = await db.Set<M03Party>().SingleOrDefaultAsync(
            x => x.PartyId == partyId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Customer was not found.");
        if (!await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == partyId && x.RoleCode == "Customer", ct))
            throw new KeyNotFoundException("Customer was not found.");
        var assessment = await creditEvaluation.Evaluate(
            partyId, request.Amount, hasOverridePermission: true, ct);
        var (facts, decision) = (assessment.Facts, assessment.Decision);
        if (!facts.Available)
            return Results.Conflict(new
            {
                detail = "A credit override cannot be requested until actual transaction-backed exposure is available."
            });
        if (decision.Status is not ("Blocked" or "ApprovalRequired"))
            throw new ValidationException("The current credit evaluation does not require an override.");
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            party.PartyId, party.LegalName, party.CreditStatus, party.CreditLimit,
            requestedAmount = request.Amount, request.MarginPercent, request.Reason,
            facts, decision
        });
        var approvalId = await approvals.Submit(new ApprovalSubmission(
            "M03CreditOverride", Guid.NewGuid(), null, null, user.UserId, request.Amount, null,
            request.MarginPercent, party.CreditStatus, snapshot), ct);
        masters.Audit(user, "CreditOverrideRequested", "M03Party", partyId,
            new { party.CreditStatus, party.CreditLimit },
            new { request.Amount, request.MarginPercent, approvalId, request.Reason }, request.Reason);
        await db.SaveChangesAsync(ct);
        return approvalId is null
            ? Results.Conflict(new { detail = "No active M02 credit-override approval rule is configured; the override was not approved." })
            : Results.Accepted($"/api/v1/m02/approvals/{approvalId}", new { approvalRequestId = approvalId });
    }

    private static async Task<IResult> ListSites(
        Guid? customerPartyId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        var sites = await db.Set<M03Site>().AsNoTracking().Where(x =>
                x.Status == "Active" && (customerPartyId == null || x.CustomerPartyId == customerPartyId))
            .OrderBy(x => x.Name).Take(500).ToListAsync(ct);
        var allowed = new List<M03Site>();
        foreach (var site in sites)
        {
            if (await mastersScope(masters, user, "masters.view", site.BranchId, site.DefaultPlantId, ct))
                allowed.Add(site);
        }
        return Results.Ok(allowed);
    }

    private static async Task<IResult> CreateSite(
        SiteWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireBranch(user, "masters.create", request.BranchId, request.DefaultPlantId, ct);
        ValidateSite(request);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.CustomerPartyId && x.IsActive, ct) ||
            !await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == request.CustomerPartyId && x.RoleCode == "Customer", ct))
            throw new ValidationException("Select an active customer.");
        var (siteGstin, stateCode) = ResolveStateFromGstin(request.Gstin, request.StateCode);
        var site = new M03Site
        {
            TenantId = user.TenantId, SiteId = Guid.NewGuid(), CustomerPartyId = request.CustomerPartyId,
            BranchId = request.BranchId, Name = request.Name.Trim(), Address = request.Address.Trim(),
            City = request.City.Trim(), StateCode = stateCode,
            Gstin = EmptyToNull(siteGstin),
            Latitude = request.Latitude, Longitude = request.Longitude, DefaultPlantId = request.DefaultPlantId,
            AccessNotes = request.AccessNotes.Trim(), Status = "Active", UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Add(site);
        masters.Audit(user, "Create", "M03Site", site.SiteId, null, site);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/sites/{site.SiteId}", site);
    }

    private static async Task<IResult> UpdateSite(
        Guid siteId, SiteWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        var site = await db.Set<M03Site>().SingleOrDefaultAsync(x => x.SiteId == siteId, ct)
            ?? throw new KeyNotFoundException("Site was not found.");
        await masters.RequireBranch(user, "masters.edit", site.BranchId, site.DefaultPlantId, ct);
        await masters.RequireBranch(user, "masters.edit", request.BranchId, request.DefaultPlantId, ct);
        ValidateSite(request);
        if (site.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.CustomerPartyId && x.IsActive, ct) ||
            !await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == request.CustomerPartyId && x.RoleCode == "Customer", ct))
            throw new ValidationException("Select an active customer.");
        var (siteGstin, stateCode) = ResolveStateFromGstin(request.Gstin, request.StateCode);
        var before = new { site.Name, site.Address, site.StateCode, site.Gstin, site.Status };
        site.CustomerPartyId = request.CustomerPartyId;
        site.BranchId = request.BranchId;
        site.Name = request.Name.Trim();
        site.Address = request.Address.Trim();
        site.City = request.City.Trim();
        site.StateCode = stateCode;
        site.Gstin = EmptyToNull(siteGstin);
        site.Latitude = request.Latitude;
        site.Longitude = request.Longitude;
        site.DefaultPlantId = request.DefaultPlantId;
        site.AccessNotes = request.AccessNotes.Trim();
        site.Status = request.Status;
        site.Version++;
        site.UpdatedAt = DateTimeOffset.UtcNow;
        masters.Audit(user, "Update", "M03Site", siteId, before, site);
        await db.SaveChangesAsync(ct);
        return Results.Ok(site);
    }

    private static async Task<IResult> DeactivateSite(
        Guid siteId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        var site = await db.Set<M03Site>().SingleOrDefaultAsync(x => x.SiteId == siteId, ct)
            ?? throw new KeyNotFoundException("Site was not found.");
        await masters.RequireBranch(user, "masters.deactivate", site.BranchId, site.DefaultPlantId, ct);
        if (site.Version != version) throw new DbUpdateConcurrencyException();
        site.Status = "Inactive";
        site.Version++;
        masters.Audit(user, "Deactivate", "M03Site", siteId, new { site.Status }, new { status = "Inactive" });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListContacts(
        Guid? partyId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = await db.Set<M03Contact>().AsNoTracking()
            .Where(x => x.IsActive && (partyId == null || x.PartyId == partyId))
            .OrderBy(x => x.Name).Take(1000).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateContact(
        ContactWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        ValidateContact(request);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.PartyId && x.IsActive, ct))
            throw new ValidationException("Select an active party.");
        var contact = new M03Contact
        {
            TenantId = user.TenantId, ContactId = Guid.NewGuid(), PartyId = request.PartyId, SiteId = request.SiteId,
            Name = request.Name.Trim(), Role = request.Role.Trim(), Email = Clean(request.Email),
            Mobile = M03MasterService.NormalizeMobile(request.Mobile), EmailConsent = request.EmailConsent,
            SmsConsent = request.SmsConsent, WhatsAppConsent = request.WhatsAppConsent,
            ConsentSource = Clean(request.ConsentSource),
            ConsentUpdatedAt = DateTimeOffset.UtcNow, Version = 1
        };
        if (request.SiteId is not null && !await db.Set<M03Site>().AnyAsync(x =>
                x.SiteId == request.SiteId && x.CustomerPartyId == request.PartyId && x.Status == "Active", ct))
            throw new ValidationException("Select an active site belonging to the selected party.");
        var duplicateMobileParties = await db.Set<M03Contact>().Where(x =>
                x.IsActive && x.Mobile == contact.Mobile && contact.Mobile != null)
            .Select(x => new { x.PartyId, x.Name, x.Mobile }).Distinct().ToListAsync(ct);
        db.Add(contact);
        masters.Audit(user, "Create", "M03Contact", contact.ContactId, null, contact);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/contacts/{contact.ContactId}", new
        {
            contact.ContactId, contact.PartyId, duplicateWarnings = duplicateMobileParties
        });
    }

    private static async Task<IResult> UpdateContact(
        Guid contactId, ContactWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        ValidateContact(request);
        var contact = await db.Set<M03Contact>().SingleOrDefaultAsync(x => x.ContactId == contactId, ct)
            ?? throw new KeyNotFoundException("Contact was not found.");
        if (contact.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.PartyId && x.IsActive, ct))
            throw new ValidationException("Select an active party.");
        if (request.SiteId is not null && !await db.Set<M03Site>().AnyAsync(x =>
                x.SiteId == request.SiteId && x.CustomerPartyId == request.PartyId && x.Status == "Active", ct))
            throw new ValidationException("Select an active site belonging to the selected party.");
        var before = new { contact.Name, contact.Role, contact.Email, contact.Mobile, contact.EmailConsent, contact.SmsConsent, contact.WhatsAppConsent };
        contact.PartyId = request.PartyId;
        contact.SiteId = request.SiteId;
        contact.Name = request.Name.Trim();
        contact.Role = request.Role.Trim();
        contact.Email = Clean(request.Email);
        contact.Mobile = M03MasterService.NormalizeMobile(request.Mobile);
        contact.EmailConsent = request.EmailConsent;
        contact.SmsConsent = request.SmsConsent;
        contact.WhatsAppConsent = request.WhatsAppConsent;
        contact.ConsentSource = Clean(request.ConsentSource);
        contact.ConsentUpdatedAt = DateTimeOffset.UtcNow;
        contact.Version++;
        masters.Audit(user, "Update", "M03Contact", contactId, before, contact);
        await db.SaveChangesAsync(ct);
        return Results.Ok(contact);
    }

    private static async Task<IResult> DeactivateContact(
        Guid contactId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var contact = await db.Set<M03Contact>().SingleOrDefaultAsync(x => x.ContactId == contactId, ct)
            ?? throw new KeyNotFoundException("Contact was not found.");
        if (contact.Version != version) throw new DbUpdateConcurrencyException();
        contact.IsActive = false;
        contact.Version++;
        masters.Audit(user, "Deactivate", "M03Contact", contactId, new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListPlants(
        M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        var rows = await db.Set<M03Plant>().AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Take(500).ToListAsync(ct);
        var allowed = new List<M03Plant>();
        foreach (var row in rows)
            if (await mastersScope(masters, user, "masters.view", row.BranchId, row.PlantId, ct))
                allowed.Add(row);
        return Results.Ok(allowed);
    }

    private static async Task<IResult> CreatePlant(
        PlantWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireBranch(user, "masters.create", request.BranchId, null, ct);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.City) || request.CapacityPerHour is < 0)
            throw new ValidationException("Plant name, address, city and a non-negative capacity are required.");
        ValidatePlant(request);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.SupplierPartyId && x.IsActive, ct) ||
            !await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == request.SupplierPartyId && x.RoleCode == "Supplier", ct))
            throw new ValidationException("Link the plant to an active supplier party.");
        var plant = new M03Plant
        {
            TenantId = user.TenantId, PlantId = Guid.NewGuid(), SupplierPartyId = request.SupplierPartyId,
            BranchId = request.BranchId, Name = request.Name.Trim(), Address = request.Address.Trim(),
            City = request.City.Trim(), StateCode = NormalizeState(request.StateCode),
            CapacityPerHour = request.CapacityPerHour,
            GradesJson = JsonSerializer.Serialize(request.Grades ?? []), IsActive = true, Version = 1
        };
        db.Add(plant);
        masters.Audit(user, "Create", "M03Plant", plant.PlantId, null, plant);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/plants/{plant.PlantId}", plant);
    }

    private static async Task<IResult> UpdatePlant(
        Guid plantId, PlantWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        var plant = await db.Set<M03Plant>().SingleOrDefaultAsync(x => x.PlantId == plantId, ct)
            ?? throw new KeyNotFoundException("Plant was not found.");
        await masters.RequireBranch(user, "masters.edit", plant.BranchId, plant.PlantId, ct);
        await masters.RequireBranch(user, "masters.edit", request.BranchId, plant.PlantId, ct);
        if (plant.Version != request.Version) throw new DbUpdateConcurrencyException();
        ValidatePlant(request);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.SupplierPartyId && x.IsActive, ct) ||
            !await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == request.SupplierPartyId && x.RoleCode == "Supplier", ct))
            throw new ValidationException("Link the plant to an active supplier party.");
        var before = new { plant.SupplierPartyId, plant.Name, plant.Address, plant.City, plant.StateCode, plant.BranchId };
        plant.SupplierPartyId = request.SupplierPartyId;
        plant.BranchId = request.BranchId;
        plant.Name = request.Name.Trim();
        plant.Address = request.Address.Trim();
        plant.City = request.City.Trim();
        plant.StateCode = NormalizeState(request.StateCode);
        plant.CapacityPerHour = request.CapacityPerHour;
        plant.GradesJson = JsonSerializer.Serialize(request.Grades ?? []);
        plant.Version++;
        masters.Audit(user, "Update", "M03Plant", plant.PlantId, before, plant);
        await db.SaveChangesAsync(ct);
        return Results.Ok(plant);
    }

    private static async Task<IResult> DeactivatePlant(
        Guid plantId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        var plant = await db.Set<M03Plant>().SingleOrDefaultAsync(x => x.PlantId == plantId, ct)
            ?? throw new KeyNotFoundException("Plant was not found.");
        await masters.RequireBranch(user, "masters.deactivate", plant.BranchId, plant.PlantId, ct);
        if (plant.Version != version) throw new DbUpdateConcurrencyException();
        plant.IsActive = false;
        plant.Version++;
        masters.Audit(user, "Deactivate", "M03Plant", plant.PlantId,
            new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListProducts(
        string? category, string? search, bool includeInactive, M03MasterService masters,
        BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var query = db.Set<M03Product>().AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(value) || x.Code.ToLower().Contains(value) ||
                x.HsnCode.Contains(search.Trim()));
        }
        return Results.Ok(await query.OrderBy(x => x.Name).Take(1000).ToListAsync(ct));
    }

    private static async Task<IResult> CreateProduct(
        ProductWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        ValidateProduct(request);
        if (!await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == request.BaseUnitCode && x.IsActive, ct))
            throw new ValidationException("Select an active base unit.");
        if (await db.Set<M03Product>().AnyAsync(x => x.Code == request.Code.Trim(), ct))
            throw new ValidationException("A product with this code already exists.");
        var product = new M03Product
        {
            TenantId = user.TenantId, ProductId = Guid.NewGuid(), Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(), Category = request.Category, HsnCode = request.HsnCode.Trim(),
            BaseUnitCode = request.BaseUnitCode.Trim().ToUpperInvariant(),
            CategoryAttributesJson = M03MasterService.NormalizeProductAttributes(request.Category, request.CategoryAttributesJson),
            QualityReference = request.QualityReference.Trim(), Traceable = request.Traceable,
            Version = 1, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Add(product);
        masters.Audit(user, "Create", "M03Product", product.ProductId, null, product);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/products/{product.ProductId}", product);
    }

    private static async Task<IResult> UpdateProduct(
        Guid productId, ProductWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        ValidateProduct(request);
        var product = await db.Set<M03Product>().SingleOrDefaultAsync(x => x.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (product.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (product.BaseUnitCode != request.BaseUnitCode &&
            await db.Set<M03PriceListLine>().AnyAsync(x => x.ProductId == productId, ct))
            throw new ValidationException("The base unit cannot change after a product is referenced by a price list.");
        var before = new { product.Name, product.HsnCode, product.BaseUnitCode, product.CategoryAttributesJson, product.Traceable };
        product.Code = request.Code.Trim().ToUpperInvariant();
        product.Name = request.Name.Trim();
        product.Category = request.Category;
        product.HsnCode = request.HsnCode.Trim();
        product.BaseUnitCode = request.BaseUnitCode.Trim().ToUpperInvariant();
        product.CategoryAttributesJson = M03MasterService.NormalizeProductAttributes(request.Category, request.CategoryAttributesJson);
        product.QualityReference = request.QualityReference.Trim();
        product.Traceable = request.Traceable;
        product.Version++;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        masters.Audit(user, "Update", "M03Product", productId, before, product);
        await db.SaveChangesAsync(ct);
        return Results.Ok(product);
    }

    private static async Task<IResult> DeactivateProduct(
        Guid productId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var product = await db.Set<M03Product>().SingleOrDefaultAsync(x => x.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (product.Version != version) throw new DbUpdateConcurrencyException();
        product.IsActive = false;
        product.Version++;
        masters.Audit(user, "Deactivate", "M03Product", productId,
            new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListTaxRates(
        string? hsnCode, DateOnly? asOf, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var query = db.Set<M03TaxRate>().AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(hsnCode)) query = query.Where(x => x.HsnCode == hsnCode.Trim());
        if (asOf is { } date)
            query = query.Where(x => x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date));
        return Results.Ok(await query.OrderBy(x => x.HsnCode).ThenByDescending(x => x.EffectiveFrom).ToListAsync(ct));
    }

    private static async Task<IResult> CreateTaxRate(
        TaxRateWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.approve", ct);
        await masters.RequireTenantWide(user, "masters.approve", ct);
        ValidateRatePeriod(request.RatePercent, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (string.IsNullOrWhiteSpace(request.HsnCode) || request.HsnCode.Length > 16)
            throw new ValidationException("Enter an HSN code of at most 16 characters.");
        if (await db.Set<M03TaxRate>().AnyAsync(x => x.HsnCode == request.HsnCode.Trim() &&
                x.EffectiveFrom == request.EffectiveFrom, ct))
            throw new ValidationException("A tax rate already exists for this HSN and effective-from date.");
        await EnsureNoTaxOverlap(db, request.HsnCode.Trim(), request.EffectiveFrom, request.EffectiveTo, null, ct);
        var rate = new M03TaxRate
        {
            TenantId = user.TenantId, TaxRateId = Guid.NewGuid(), HsnCode = request.HsnCode.Trim(),
            RatePercent = request.RatePercent, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            Reason = request.Reason.Trim(), IsApproved = true, Version = 1, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(rate);
        masters.Audit(user, "Create", "M03TaxRate", rate.TaxRateId, null, rate, request.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/tax-rates/{rate.TaxRateId}", rate);
    }

    private static async Task<IResult> DeactivateTaxRate(
        Guid taxRateId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var rate = await db.Set<M03TaxRate>().SingleOrDefaultAsync(x => x.TaxRateId == taxRateId, ct)
            ?? throw new KeyNotFoundException("Tax rate was not found.");
        if (rate.Version != version) throw new DbUpdateConcurrencyException();
        var before = new { rate.IsActive, rate.Version };
        rate.IsActive = false;
        rate.Version++;
        masters.Audit(user, "Deactivate", "M03TaxRate", taxRateId, before,
            new { rate.IsActive, rate.Version });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ResolveTaxRate(
        TaxRateLookup request, M03MasterService masters, BerynxDbContext db, OnboardingStore onboarding,
        CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var sellerGstin = M03MasterService.NormalizeGstin(request.SellerGstin);
        var buyerGstin = M03MasterService.NormalizeGstin(request.BuyerGstin);
        var suppliedPlaceOfSupply = string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateCode)
            ? null
            : NormalizeState(request.PlaceOfSupplyStateCode);
        var placeOfSupply = suppliedPlaceOfSupply ??
            (string.IsNullOrEmpty(buyerGstin) ? null : buyerGstin[..2]);
        if (!string.IsNullOrEmpty(buyerGstin) && placeOfSupply is not null &&
            buyerGstin[..2] != placeOfSupply)
            throw new ValidationException("Place of supply must match the buyer GSTIN state prefix.");
        if (!string.IsNullOrEmpty(sellerGstin))
        {
            var setup = OnboardingStore.Decode(await onboarding.Current());
            var registeredGstins = setup.Registrations.Concat(setup.Branches.Select(x => x.Gstin))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(M03MasterService.NormalizeGstin)
                .ToHashSet(StringComparer.Ordinal);
            if (!registeredGstins.Contains(sellerGstin))
                throw new ValidationException("The seller GSTIN is not registered in this tenant's M01 setup.");
        }
        var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
            x => x.ProductId == request.ProductId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Product was not found.");
        var date = request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(request.CustomerCategory))
        {
            var overrideRate = await db.Set<M03CategoryTaxOverride>().AsNoTracking().Where(x =>
                    x.ProductId == product.ProductId && x.CustomerCategory == request.CustomerCategory &&
                    x.IsApproved && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
                .SingleOrDefaultAsync(ct);
            if (overrideRate is not null)
                return Results.Ok(new
                {
                    product.ProductId, product.HsnCode, ratePercent = overrideRate.RatePercent,
                    source = "ApprovedCustomerCategoryOverride", overrideRate.EffectiveFrom,
                    overrideRate.EffectiveTo, overrideRate.Reason,
                    sellerGstin, buyerGstin, placeOfSupplyStateCode = placeOfSupply
                });
        }
        var rate = await db.Set<M03TaxRate>().AsNoTracking().Where(x =>
                x.HsnCode == product.HsnCode && x.IsActive && x.IsApproved &&
                x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .SingleOrDefaultAsync(ct);
        return rate is null
            ? Results.NotFound(new { detail = "No effective approved tax rate exists for this HSN." })
            : Results.Ok(new
            {
                product.ProductId, product.HsnCode, ratePercent = rate.RatePercent, source = "Hsn",
                rate.EffectiveFrom, rate.EffectiveTo, rate.Reason,
                sellerGstin, buyerGstin, placeOfSupplyStateCode = placeOfSupply
            });
    }

    private static async Task<IResult> ListCategoryTaxOverrides(
        Guid? productId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        return Results.Ok(await db.Set<M03CategoryTaxOverride>().AsNoTracking()
            .Where(x => productId == null || x.ProductId == productId)
            .OrderBy(x => x.ProductId).ThenBy(x => x.CustomerCategory).ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct));
    }

    private static async Task<IResult> CreateCategoryTaxOverride(
        CategoryTaxOverrideWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.approve", ct);
        await masters.RequireTenantWide(user, "masters.approve", ct);
        ValidateRatePeriod(request.RatePercent, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (string.IsNullOrWhiteSpace(request.CustomerCategory) || request.CustomerCategory.Length > 80)
            throw new ValidationException("Select a customer category.");
        if (!await db.Set<M03Product>().AnyAsync(x => x.ProductId == request.ProductId && x.IsActive, ct))
            throw new KeyNotFoundException("Product was not found.");
        await EnsureCategoryTaxNoOverlap(db, request.ProductId, request.CustomerCategory, request.EffectiveFrom,
            request.EffectiveTo, null, ct);
        var row = new M03CategoryTaxOverride
        {
            TenantId = user.TenantId, OverrideId = Guid.NewGuid(), ProductId = request.ProductId,
            CustomerCategory = request.CustomerCategory.Trim(), RatePercent = request.RatePercent,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            Reason = request.Reason.Trim(), IsApproved = true, ApprovedBy = user.Email ?? user.UserId.ToString(),
            ApprovedAt = DateTimeOffset.UtcNow
        };
        db.Add(row);
        masters.Audit(user, "ApproveAndCreate", "M03CategoryTaxOverride", row.OverrideId, null, row, request.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/category-tax-overrides/{row.OverrideId}", row);
    }

    private static async Task<IResult> ListPriceLists(
        M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        return Results.Ok(await db.Set<M03PriceList>().AsNoTracking().OrderBy(x => x.Name)
            .Take(500).ToListAsync(ct));
    }

    private static async Task<IResult> CreatePriceList(
        PriceListWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        ValidatePriceList(request);
        await EnsurePriceListNoOverlap(db, request, null, ct);
        var row = new M03PriceList
        {
            TenantId = user.TenantId, PriceListId = Guid.NewGuid(), Name = request.Name.Trim(),
            CustomerPartyId = request.CustomerPartyId, CustomerGroup = Clean(request.CustomerGroup),
            IsDefault = request.IsDefault, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo,
            IsActive = true, Version = 1
        };
        db.Add(row);
        masters.Audit(user, "Create", "M03PriceList", row.PriceListId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/price-lists/{row.PriceListId}", row);
    }

    private static async Task<IResult> UpdatePriceList(
        Guid priceListId, PriceListWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        ValidatePriceList(request);
        var row = await db.Set<M03PriceList>().SingleOrDefaultAsync(x => x.PriceListId == priceListId, ct)
            ?? throw new KeyNotFoundException("Price list was not found.");
        if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
        await EnsurePriceListNoOverlap(db, request, priceListId, ct);
        var before = new { row.Name, row.CustomerPartyId, row.CustomerGroup, row.IsDefault, row.ValidFrom, row.ValidTo };
        row.Name = request.Name.Trim();
        row.CustomerPartyId = request.CustomerPartyId;
        row.CustomerGroup = Clean(request.CustomerGroup);
        row.IsDefault = request.IsDefault;
        row.ValidFrom = request.ValidFrom;
        row.ValidTo = request.ValidTo;
        row.Version++;
        masters.Audit(user, "Update", "M03PriceList", priceListId, before, row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivatePriceList(
        Guid priceListId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03PriceList>().SingleOrDefaultAsync(x => x.PriceListId == priceListId, ct)
            ?? throw new KeyNotFoundException("Price list was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03PriceList", priceListId,
            new { isActive = true }, new { row.IsActive, row.Version });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpsertPriceLine(
        Guid priceListId, PriceLineWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        ValidatePriceLine(request);
        var canViewCost = await masters.HasPermission(user.UserId, "global.view_cost", ct);
        if (!canViewCost && (request.MinimumRate is not null || request.MinimumMarginPercent is not null))
            throw new UnauthorizedAccessException("Only users with view_cost can manage minimum-rate and margin guards.");
        if (!await db.Set<M03PriceList>().AnyAsync(x => x.PriceListId == priceListId && x.IsActive, ct) ||
            !await db.Set<M03Product>().AnyAsync(x => x.ProductId == request.ProductId && x.IsActive, ct))
            throw new KeyNotFoundException("Price list or product was not found.");
        var row = await db.Set<M03PriceListLine>().SingleOrDefaultAsync(
            x => x.PriceListId == priceListId && x.ProductId == request.ProductId, ct);
        var isNew = row is null;
        if (row is not null && row.Version != request.Version) throw new DbUpdateConcurrencyException();
        var before = row is null ? null : new { row.Rate, row.MinimumRate, row.MinimumMarginPercent };
        if (row is null)
        {
            row = new M03PriceListLine
            {
                TenantId = user.TenantId, PriceListLineId = Guid.NewGuid(), PriceListId = priceListId,
                ProductId = request.ProductId, Version = 1
            };
            db.Add(row);
        }
        row.Rate = request.Rate;
        if (canViewCost)
        {
            row.MinimumRate = request.MinimumRate;
            row.MinimumMarginPercent = request.MinimumMarginPercent;
        }
        if (!isNew) row.Version++;
        masters.Audit(user, isNew ? "Create" : "Update", "M03PriceListLine",
            row.PriceListLineId, before, row);
        await db.SaveChangesAsync(ct);
        var showCost = await masters.HasPermission(user.UserId, "global.view_cost", ct);
        return Results.Ok(new
        {
            row.PriceListLineId, row.PriceListId, row.ProductId, row.Rate, row.Version,
            minimumRate = showCost ? row.MinimumRate : null,
            minimumMarginPercent = showCost ? row.MinimumMarginPercent : null
        });
    }

    private static async Task<IResult> ListPriceLines(
        Guid priceListId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        if (!await db.Set<M03PriceList>().AnyAsync(x => x.PriceListId == priceListId, ct))
            throw new KeyNotFoundException("Price list was not found.");
        var canViewCost = await masters.HasTenantWidePermission(user.UserId, "global.view_cost", ct);
        return Results.Ok(await db.Set<M03PriceListLine>().AsNoTracking()
            .Where(x => x.PriceListId == priceListId)
            .OrderBy(x => x.ProductId)
            .Select(x => new
            {
                x.PriceListLineId, x.PriceListId, x.ProductId, x.Rate,
                minimumRate = canViewCost ? x.MinimumRate : null,
                minimumMarginPercent = canViewCost ? x.MinimumMarginPercent : null,
                x.Version
            }).ToListAsync(ct));
    }

    private static async Task<IResult> ResolvePrice(
        PriceLookup request, M03MasterService masters, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        return Results.Ok(await masters.ResolvePrice(request.CustomerPartyId, request.ProductId,
            request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));
    }

    private static async Task<IResult> RequestPriceApproval(
        PriceApprovalWrite request, M03MasterService masters, BerynxDbContext db,
        IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        if (request.ProposedRate < 0 || request.Quantity <= 0 ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ValidationException("Enter a non-negative rate, positive quantity and approval reason.");
        var price = await masters.ResolvePrice(request.CustomerPartyId, request.ProductId,
            DateOnly.FromDateTime(DateTime.UtcNow), ct);
        var line = await db.Set<M03PriceListLine>().SingleAsync(
            x => x.PriceListId == price.PriceListId && x.ProductId == request.ProductId, ct);
        var requiresApproval =
            (line.MinimumRate is not null && request.ProposedRate < line.MinimumRate) ||
            (line.MinimumMarginPercent is not null && request.MarginPercent is not null &&
                request.MarginPercent < line.MinimumMarginPercent);
        if (!requiresApproval)
            return Results.Ok(new { approvalRequired = false, price });
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            request.CustomerPartyId, request.ProductId, listRate = line.Rate,
            request.ProposedRate, request.Quantity, request.MarginPercent, request.Reason,
            priceSnapshot = price
        });
        var requestId = await approvals.Submit(new ApprovalSubmission(
            "M03PriceOverride", Guid.NewGuid(), null, null, user.UserId,
            request.ProposedRate * request.Quantity, null, request.MarginPercent, null, snapshot), ct);
        return requestId is null
            ? Results.Conflict(new { detail = "No active M02 pricing approval rule is configured; the below-minimum rate is blocked." })
            : Results.Accepted($"/api/v1/m02/approvals/{requestId}", new { approvalRequired = true, approvalRequestId = requestId });
    }

    private static async Task<IResult> ListRmcRateCards(
        Guid? productId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        return Results.Ok(await db.Set<M03RmcRateCard>().AsNoTracking().Where(x =>
                productId == null || x.ProductId == productId)
            .OrderBy(x => x.Grade).ThenBy(x => x.SlumpBand).ThenBy(x => x.AdmixtureCode).ThenBy(x => x.PumpType)
            .Take(1000).ToListAsync(ct));
    }

    private static async Task<IResult> CreateRmcRateCard(
        RmcRateWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.approve", ct);
        await masters.RequireTenantWide(user, "masters.approve", ct);
        if (request.Rate < 0 || string.IsNullOrWhiteSpace(request.Grade) ||
            string.IsNullOrWhiteSpace(request.SlumpBand) || string.IsNullOrWhiteSpace(request.PumpType) ||
            request.EffectiveTo < request.EffectiveFrom)
            throw new ValidationException("Enter a valid rate-card key, rate and effective period.");
        var product = await db.Set<M03Product>().SingleOrDefaultAsync(x =>
            x.ProductId == request.ProductId && x.Category == "RMC" && x.IsActive, ct)
            ?? throw new ValidationException("RMC rate cards require an active RMC product.");
        foreach (var chargeTypeId in new[] { request.AdmixtureChargeTypeId, request.PumpChargeTypeId }.Where(x => x.HasValue))
            if (!await db.Set<M03ChargeType>().AnyAsync(x => x.ChargeTypeId == chargeTypeId && x.IsActive, ct))
                throw new ValidationException("Configure active admixture and pump surcharge charge types first.");
        var rows = db.Set<M03RmcRateCard>().Where(x => x.ProductId == product.ProductId &&
            x.Grade == request.Grade.Trim() && x.SlumpBand == request.SlumpBand.Trim() &&
            x.AdmixtureCode == request.AdmixtureCode.Trim() && x.PumpType == request.PumpType.Trim() &&
            x.IsActive);
        if (await rows.AnyAsync(x =>
                (x.EffectiveTo == null || x.EffectiveTo >= request.EffectiveFrom) &&
                (request.EffectiveTo == null || x.EffectiveFrom <= request.EffectiveTo), ct))
            throw new ValidationException("This exact RMC rate-card key already has an overlapping effective period.");
        var row = new M03RmcRateCard
        {
            TenantId = user.TenantId, RateCardId = Guid.NewGuid(), ProductId = request.ProductId,
            Grade = request.Grade.Trim(), SlumpBand = request.SlumpBand.Trim(),
            AdmixtureCode = request.AdmixtureCode.Trim(), PumpType = request.PumpType.Trim(),
            Rate = request.Rate, AdmixtureChargeTypeId = request.AdmixtureChargeTypeId,
            PumpChargeTypeId = request.PumpChargeTypeId, EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo, IsApproved = true
        };
        db.Add(row);
        masters.Audit(user, "ApproveAndCreate", "M03RmcRateCard", row.RateCardId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/rmc-rate-cards/{row.RateCardId}", row);
    }

    private static async Task<IResult> DeactivateRmcRateCard(
        Guid rateCardId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03RmcRateCard>().SingleOrDefaultAsync(x => x.RateCardId == rateCardId, ct)
            ?? throw new KeyNotFoundException("RMC rate card was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03RmcRateCard", rateCardId,
            new { isActive = true }, new { row.IsActive, row.Version });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ResolveRmcRateCard(
        RmcLookup request, M03MasterService masters, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var row = await masters.ResolveRmc(request.ProductId, request.Grade.Trim(), request.SlumpBand.Trim(),
            request.AdmixtureCode.Trim(), request.PumpType.Trim(),
            request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow), ct);
        return Results.Ok(new
        {
            row.RateCardId, row.ProductId, row.Grade, row.SlumpBand, row.AdmixtureCode, row.PumpType,
            row.Rate, row.AdmixtureChargeTypeId, row.PumpChargeTypeId, row.EffectiveFrom, row.EffectiveTo
        });
    }

    private static async Task<IResult> ListVehicles(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03Vehicle>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.RegistrationNumber).Take(1000).ToListAsync(ct));
    }

    private static async Task<IResult> CreateVehicle(
        VehicleWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) || request.RegistrationNumber.Length > 20 ||
            request.Capacity <= 0 || string.IsNullOrWhiteSpace(request.Type) ||
            string.IsNullOrWhiteSpace(request.CapacityUnitCode) ||
            request.Ownership is not ("Own" or "Hired"))
            throw new ValidationException("Enter a registration number, type, positive capacity and Own/Hired status.");
        if (request.Ownership == "Hired" && (request.TransporterPartyId is null ||
            !await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == request.TransporterPartyId && x.RoleCode == "Transporter", ct)))
            throw new ValidationException("Hired vehicles must be linked to a transporter party.");
        if (!await db.Set<M03Unit>().AnyAsync(x =>
                x.UnitCode == request.CapacityUnitCode.Trim().ToUpperInvariant() && x.IsActive, ct))
            throw new ValidationException("Select an active capacity unit.");
        var registration = NormalizeRegistration(request.RegistrationNumber);
        if (await db.Set<M03Vehicle>().AnyAsync(x => x.RegistrationNumber == registration, ct))
            throw new ValidationException("A vehicle with this registration number already exists.");
        var row = new M03Vehicle
        {
            TenantId = user.TenantId, VehicleId = Guid.NewGuid(), RegistrationNumber = registration,
            Type = request.Type.Trim(), Capacity = request.Capacity,
            CapacityUnitCode = request.CapacityUnitCode.Trim().ToUpperInvariant(),
            Ownership = request.Ownership, TransporterPartyId = request.TransporterPartyId,
            InsuranceExpiresOn = request.InsuranceExpiresOn, PucExpiresOn = request.PucExpiresOn,
            FitnessExpiresOn = request.FitnessExpiresOn, Version = 1
        };
        db.Add(row);
        masters.Audit(user, "Create", "M03Vehicle", row.VehicleId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/vehicles/{row.VehicleId}", row);
    }

    private static async Task<IResult> UpdateVehicle(
        Guid vehicleId, VehicleWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var row = await db.Set<M03Vehicle>().SingleOrDefaultAsync(x => x.VehicleId == vehicleId, ct)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) || request.RegistrationNumber.Length > 20 ||
            request.Capacity <= 0 || string.IsNullOrWhiteSpace(request.Type) ||
            request.Ownership is not ("Own" or "Hired"))
            throw new ValidationException("Enter a registration number, type, positive capacity and Own/Hired status.");
        var registration = NormalizeRegistration(request.RegistrationNumber);
        if (await db.Set<M03Vehicle>().AnyAsync(x => x.VehicleId != vehicleId &&
                x.RegistrationNumber == registration, ct))
            throw new ValidationException("A vehicle with this registration number already exists.");
        if (request.Ownership == "Hired" && (request.TransporterPartyId is null ||
            !await db.Set<M03PartyRole>().AnyAsync(x =>
                x.PartyId == request.TransporterPartyId && x.RoleCode == "Transporter", ct)))
            throw new ValidationException("Hired vehicles must be linked to a transporter party.");
        if (!await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == request.CapacityUnitCode && x.IsActive, ct))
            throw new ValidationException("Select an active capacity unit.");
        var before = new { row.RegistrationNumber, row.Type, row.Capacity, row.Ownership };
        row.RegistrationNumber = registration;
        row.Type = request.Type.Trim();
        row.Capacity = request.Capacity;
        row.CapacityUnitCode = request.CapacityUnitCode.Trim().ToUpperInvariant();
        row.Ownership = request.Ownership;
        row.TransporterPartyId = request.TransporterPartyId;
        row.InsuranceExpiresOn = request.InsuranceExpiresOn;
        row.PucExpiresOn = request.PucExpiresOn;
        row.FitnessExpiresOn = request.FitnessExpiresOn;
        row.Version++;
        masters.Audit(user, "Update", "M03Vehicle", vehicleId, before, row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivateVehicle(
        Guid vehicleId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03Vehicle>().SingleOrDefaultAsync(x => x.VehicleId == vehicleId, ct)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03Vehicle", vehicleId, new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListDrivers(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03Driver>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.Name).Take(1000).ToListAsync(ct));
    }

    private static async Task<IResult> CreateDriver(
        DriverWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.LicenceNumber))
            throw new ValidationException("Driver name and licence number are required.");
        var mobile = M03MasterService.NormalizeMobile(request.Mobile)
            ?? throw new ValidationException("Driver mobile is required.");
        if (request.VehicleId is { } vehicleId &&
            !await db.Set<M03Vehicle>().AnyAsync(x => x.VehicleId == vehicleId && x.IsActive, ct))
            throw new ValidationException("Select an active vehicle.");
        var row = new M03Driver
        {
            TenantId = user.TenantId, DriverId = Guid.NewGuid(), Name = request.Name.Trim(),
            Mobile = mobile, LicenceNumber = request.LicenceNumber.Trim().ToUpperInvariant(),
            LicenceExpiresOn = request.LicenceExpiresOn, VehicleId = request.VehicleId
        };
        db.Add(row);
        masters.Audit(user, "Create", "M03Driver", row.DriverId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/drivers/{row.DriverId}", row);
    }

    private static async Task<IResult> UpdateDriver(
        Guid driverId, DriverWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var row = await db.Set<M03Driver>().SingleOrDefaultAsync(x => x.DriverId == driverId, ct)
            ?? throw new KeyNotFoundException("Driver was not found.");
        if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.LicenceNumber))
            throw new ValidationException("Driver name and licence number are required.");
        var mobile = M03MasterService.NormalizeMobile(request.Mobile)
            ?? throw new ValidationException("Driver mobile is required.");
        var licence = request.LicenceNumber.Trim().ToUpperInvariant();
        if (await db.Set<M03Driver>().AnyAsync(x => x.DriverId != driverId && x.LicenceNumber == licence, ct))
            throw new ValidationException("A driver with this licence number already exists.");
        if (request.VehicleId is { } vehicleId &&
            !await db.Set<M03Vehicle>().AnyAsync(x => x.VehicleId == vehicleId && x.IsActive, ct))
            throw new ValidationException("Select an active vehicle.");
        var before = new { row.Name, row.Mobile, row.LicenceNumber, row.VehicleId, row.LicenceExpiresOn };
        row.Name = request.Name.Trim();
        row.Mobile = mobile;
        row.LicenceNumber = licence;
        row.LicenceExpiresOn = request.LicenceExpiresOn;
        row.VehicleId = request.VehicleId;
        row.Version++;
        masters.Audit(user, "Update", "M03Driver", driverId, before, row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivateDriver(
        Guid driverId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03Driver>().SingleOrDefaultAsync(x => x.DriverId == driverId, ct)
            ?? throw new KeyNotFoundException("Driver was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03Driver", driverId, new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListEquipment(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03Equipment>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.Name).Take(1000).ToListAsync(ct));
    }

    private static async Task<IResult> CreateEquipment(
        EquipmentWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EquipmentType) ||
            string.IsNullOrWhiteSpace(request.PumpType) ||
            request.Capacity is < 0)
            throw new ValidationException("Equipment name, type and a non-negative capacity are required.");
        if (request.CapacityUnitCode is { } capacityUnit &&
            !await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == capacityUnit && x.IsActive, ct))
            throw new ValidationException("Select an active capacity unit.");
        if (request.OwnerPartyId is { } ownerId &&
            !await db.Set<M03Party>().AnyAsync(x => x.PartyId == ownerId && x.IsActive, ct))
            throw new ValidationException("Equipment owner party was not found.");
        var row = new M03Equipment
        {
            TenantId = user.TenantId, EquipmentId = Guid.NewGuid(), Name = request.Name.Trim(),
            EquipmentType = request.EquipmentType.Trim(), PumpType = request.PumpType.Trim(),
            Capacity = request.Capacity, CapacityUnitCode = Clean(request.CapacityUnitCode),
            OwnerPartyId = request.OwnerPartyId, InsuranceExpiresOn = request.InsuranceExpiresOn,
            FitnessExpiresOn = request.FitnessExpiresOn
        };
        db.Add(row);
        masters.Audit(user, "Create", "M03Equipment", row.EquipmentId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/equipment/{row.EquipmentId}", row);
    }

    private static async Task<IResult> UpdateEquipment(
        Guid equipmentId, EquipmentWrite request, M03MasterService masters, BerynxDbContext db,
        CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var row = await db.Set<M03Equipment>().SingleOrDefaultAsync(x => x.EquipmentId == equipmentId, ct)
            ?? throw new KeyNotFoundException("Equipment was not found.");
        if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EquipmentType) ||
            request.Capacity is < 0)
            throw new ValidationException("Equipment name, type and a non-negative capacity are required.");
        if (request.OwnerPartyId is { } ownerId &&
            !await db.Set<M03Party>().AnyAsync(x => x.PartyId == ownerId && x.IsActive, ct))
            throw new ValidationException("Equipment owner party was not found.");
        if (request.CapacityUnitCode is { } unitCode &&
            !await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == unitCode && x.IsActive, ct))
            throw new ValidationException("Select an active capacity unit.");
        var before = new { row.Name, row.EquipmentType, row.PumpType, row.Capacity, row.OwnerPartyId };
        row.Name = request.Name.Trim();
        row.EquipmentType = request.EquipmentType.Trim();
        row.PumpType = request.PumpType.Trim();
        row.Capacity = request.Capacity;
        row.CapacityUnitCode = Clean(request.CapacityUnitCode);
        row.OwnerPartyId = request.OwnerPartyId;
        row.InsuranceExpiresOn = request.InsuranceExpiresOn;
        row.FitnessExpiresOn = request.FitnessExpiresOn;
        row.Version++;
        masters.Audit(user, "Update", "M03Equipment", equipmentId, before, row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivateEquipment(
        Guid equipmentId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03Equipment>().SingleOrDefaultAsync(x => x.EquipmentId == equipmentId, ct)
            ?? throw new KeyNotFoundException("Equipment was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03Equipment", equipmentId, new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> FleetExpiries(
        int? days, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = days is 7 or 15 or 30 ? days.Value : 30;
        return Results.Ok(await GetFleetExpiryAlerts(db, today, horizon, ct));
    }

    private static async Task<FleetExpiryAlert[]> GetFleetExpiryAlerts(
        BerynxDbContext db, DateOnly today, int horizon, CancellationToken ct)
    {
        var end = today.AddDays(horizon);
        var vehicles = await db.Set<M03Vehicle>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var drivers = await db.Set<M03Driver>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var equipment = await db.Set<M03Equipment>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var alerts = vehicles.SelectMany(v => new[]
            {
                new { Type = "Vehicle", Id = v.VehicleId, Name = v.RegistrationNumber, Document = "Insurance", Expires = v.InsuranceExpiresOn },
                new { Type = "Vehicle", Id = v.VehicleId, Name = v.RegistrationNumber, Document = "PUC", Expires = v.PucExpiresOn },
                new { Type = "Vehicle", Id = v.VehicleId, Name = v.RegistrationNumber, Document = "Fitness", Expires = v.FitnessExpiresOn }
            })
            .Concat(drivers.Select(d => new { Type = "Driver", Id = d.DriverId, Name = d.Name, Document = "Licence", Expires = (DateOnly?)d.LicenceExpiresOn }))
            .Concat(equipment.SelectMany(e => new[]
            {
                new { Type = "Equipment", Id = e.EquipmentId, Name = e.Name, Document = "Insurance", Expires = e.InsuranceExpiresOn },
                new { Type = "Equipment", Id = e.EquipmentId, Name = e.Name, Document = "Fitness", Expires = e.FitnessExpiresOn }
            }))
            .Where(x => x.Expires is { } expiry && expiry <= end)
            .Select(x => new FleetExpiryAlert(
                x.Type, x.Id, x.Name, x.Document, x.Expires!.Value,
                x.Expires.Value.DayNumber - today.DayNumber,
                x.Expires < today ? "Expired" :
                    x.Expires == today ? "Today" :
                    x.Expires <= today.AddDays(7) ? "7 days" :
                    x.Expires <= today.AddDays(15) ? "15 days" : "30 days"))
            .OrderBy(x => x.DaysRemaining)
            .ToArray();
        return alerts;
    }

    private static async Task<IResult> DispatchEligibility(
        Guid vehicleId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var row = await db.Set<M03Vehicle>().AsNoTracking().SingleOrDefaultAsync(
            x => x.VehicleId == vehicleId, ct) ?? throw new KeyNotFoundException("Vehicle was not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = new[]
        {
            ("Insurance", row.InsuranceExpiresOn), ("PUC", row.PucExpiresOn), ("Fitness", row.FitnessExpiresOn)
        }.Where(x => x.Item2 is { } date && date < today).Select(x => x.Item1).ToArray();
        return Results.Ok(new { vehicleId, eligible = row.IsActive && expired.Length == 0, expiredDocuments = expired });
    }

    private static async Task<IResult> DriverEligibility(
        Guid driverId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var row = await db.Set<M03Driver>().AsNoTracking().SingleOrDefaultAsync(
            x => x.DriverId == driverId, ct) ?? throw new KeyNotFoundException("Driver was not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = row.LicenceExpiresOn < today;
        return Results.Ok(new
        {
            driverId,
            eligible = row.IsActive && !expired,
            documents = new[]
            {
                new { name = "Licence", expiresOn = row.LicenceExpiresOn, status = expired ? "Expired" : "Valid" }
            }
        });
    }

    private static async Task<IResult> EquipmentEligibility(
        Guid equipmentId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var row = await db.Set<M03Equipment>().AsNoTracking().SingleOrDefaultAsync(
            x => x.EquipmentId == equipmentId, ct) ?? throw new KeyNotFoundException("Equipment was not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = new[]
        {
            ("Insurance", row.InsuranceExpiresOn), ("Fitness", row.FitnessExpiresOn)
        }.Where(x => x.Item2 is { } date && date < today).Select(x => x.Item1).ToArray();
        return Results.Ok(new { equipmentId, eligible = row.IsActive && expired.Length == 0, expiredDocuments = expired });
    }

    private static async Task<IResult> RefreshFleetExpiryNotifications(
        int? days, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = days is 7 or 15 or 30 ? days.Value : 30;
        var alerts = await GetFleetExpiryAlerts(db, today, horizon, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        var keys = alerts.Select(alert => $"M03FleetExpiry:{alert.Document}:{alert.ExpiresOn:yyyyMMdd}")
            .Distinct().ToArray();
        var entityIds = alerts.Select(x => x.Id).Distinct().ToArray();
        var existing = await db.Set<M02Notification>().AsNoTracking()
            .Where(x => keys.Contains(x.Kind) && x.EntityId.HasValue && entityIds.Contains(x.EntityId.Value))
            .Select(x => new { x.Kind, x.EntityId, x.RecipientUserId })
            .ToListAsync(ct);
        var recipients = await db.Set<M02User>().Where(x => x.Status == "Active").ToListAsync(ct);
        var visibleRecipients = new List<Guid>();
        foreach (var recipient in recipients)
            if (await masters.HasTenantWidePermission(recipient.UserId, "masters.view", ct))
                visibleRecipients.Add(recipient.UserId);

        var created = 0;
        foreach (var alert in alerts)
        {
            var kind = $"M03FleetExpiry:{alert.Document}:{alert.ExpiresOn:yyyyMMdd}";
            foreach (var recipientId in visibleRecipients)
            {
                if (existing.Any(x => x.Kind == kind && x.EntityId == alert.Id && x.RecipientUserId == recipientId))
                    continue;
                var notificationId = Guid.NewGuid();
                db.Add(new M02Notification
                {
                    TenantId = user.TenantId,
                    NotificationId = notificationId,
                    RecipientUserId = recipientId,
                    Kind = kind,
                    EntityId = alert.Id,
                    Payload = JsonSerializer.Serialize(new
                    {
                        alert.Type, alert.Name, alert.Document, alert.ExpiresOn,
                        alert.DaysRemaining, alert.Urgency
                    }),
                    CreatedAt = DateTimeOffset.UtcNow
                });
                masters.Audit(user, "FleetExpiryNotificationCreated", "M02Notification", notificationId,
                    null, new { recipientId, alert.Type, alert.Id, alert.Document, alert.ExpiresOn });
                created++;
            }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(new
        {
            created,
            alerts = alerts.Length,
            recipients = visibleRecipients.Count,
            externalDeliveryVerified = false,
            deliveryStatus = "Internal M02 notifications were created; external provider delivery is not configured or verified."
        });
    }

    private static async Task<IResult> RequestFleetOverride(
        Guid vehicleId, CreditOverrideWrite request, M03MasterService masters, BerynxDbContext db,
        IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var user = await masters.Require("fleet.override", ct);
        await masters.RequireTenantWide(user, "fleet.override", ct);
        var row = await db.Set<M03Vehicle>().SingleOrDefaultAsync(x => x.VehicleId == vehicleId, ct)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ValidationException("A reason is required for an expired-document dispatch override.");
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            vehicleId, row.RegistrationNumber, row.InsuranceExpiresOn, row.PucExpiresOn,
            row.FitnessExpiresOn, request.Reason
        });
        var requestId = await approvals.Submit(new ApprovalSubmission(
            "M03FleetExpiryOverride", vehicleId, null, null, user.UserId, 0, null, null,
            "ExpiredDocument", snapshot), ct);
        return requestId is null
            ? Results.Conflict(new { detail = "No active M02 fleet-expiry approval rule is configured; dispatch remains blocked." })
            : Results.Accepted($"/api/v1/m02/approvals/{requestId}", new { approvalRequestId = requestId });
    }

    private static async Task<IResult> ListUnits(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03Unit>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.UnitCode).ToListAsync(ct));
    }

    private static async Task<IResult> CreateUnit(
        UnitWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        var code = NormalizeUnitCode(request.UnitCode);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Symbol) ||
            request.Name.Length > 80 || request.Symbol.Length > 16)
            throw new ValidationException("Unit name and symbol are required.");
        if (await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == code, ct))
            throw new ValidationException("A unit with this code already exists.");
        var row = new M03Unit { TenantId = user.TenantId, UnitCode = code, Name = request.Name.Trim(),
            Symbol = request.Symbol.Trim(), Version = 1 };
        db.Add(row);
        masters.Audit(user, "Create", "M03Unit", Guid.Empty, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/units/{row.UnitCode}", row);
    }

    private static async Task<IResult> UpdateUnit(
        string unitCode, UnitWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var code = NormalizeUnitCode(unitCode);
        var row = await db.Set<M03Unit>().SingleOrDefaultAsync(x => x.UnitCode == code, ct)
            ?? throw new KeyNotFoundException("Unit was not found.");
        if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
        if (NormalizeUnitCode(request.UnitCode) != code || string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Length > 80 || string.IsNullOrWhiteSpace(request.Symbol) || request.Symbol.Length > 16)
            throw new ValidationException("Unit code is immutable; provide a valid name and symbol.");
        var before = new { row.Name, row.Symbol };
        row.Name = request.Name.Trim();
        row.Symbol = request.Symbol.Trim();
        row.Version++;
        masters.Audit(user, "Update", "M03Unit", Guid.Empty, before,
            new { row.UnitCode, row.Name, row.Symbol, row.Version });
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivateUnit(
        string unitCode, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var code = NormalizeUnitCode(unitCode);
        var row = await db.Set<M03Unit>().SingleOrDefaultAsync(x => x.UnitCode == code, ct)
            ?? throw new KeyNotFoundException("Unit was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        if (await db.Set<M03Product>().AnyAsync(x => x.BaseUnitCode == code, ct) ||
            await db.Set<M03UnitConversion>().AnyAsync(x => x.FromUnitCode == code || x.ToUnitCode == code, ct))
            throw new ValidationException("A unit referenced by products or conversions cannot be deactivated.");
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03Unit", Guid.Empty, new { row.UnitCode, isActive = true },
            new { row.UnitCode, isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListConversions(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03UnitConversion>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.FromUnitCode).ThenBy(x => x.ToUnitCode).ToListAsync(ct));
    }

    private static async Task<IResult> CreateConversion(
        ConversionWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        var from = NormalizeUnitCode(request.FromUnitCode);
        var to = NormalizeUnitCode(request.ToUnitCode);
        if (from == to || request.Factor <= 0)
            throw new ValidationException("Choose two different units and a positive conversion factor.");
        if (!await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == from && x.IsActive, ct) ||
            !await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == to && x.IsActive, ct))
            throw new ValidationException("Both units must be active.");
        var factor = request.Factor;
        if (string.CompareOrdinal(from, to) > 0)
        {
            (from, to) = (to, from);
            factor = decimal.Round(1m / factor, 9, MidpointRounding.AwayFromZero);
        }
        if (factor <= 0 || decimal.Round(factor, 9) != factor)
            throw new ValidationException("The normalized conversion factor must fit 9 decimal places.");
        var existing = await db.Set<M03UnitConversion>().SingleOrDefaultAsync(x =>
            x.FromUnitCode == from && x.ToUnitCode == to, ct);
        if (existing is not null)
        {
            if (existing.Version != request.Version) throw new DbUpdateConcurrencyException();
            var before = new { existing.ToUnitsPerFromUnit, existing.IsActive };
            existing.ToUnitsPerFromUnit = factor;
            existing.IsActive = true;
            existing.Version++;
            masters.Audit(user, "Update", "M03UnitConversion", existing.ConversionId, before, existing);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new
            {
                existing.ConversionId, existing.FromUnitCode, existing.ToUnitCode,
                existing.ToUnitsPerFromUnit,
                reverseFactor = decimal.Round(1m / existing.ToUnitsPerFromUnit, 9, MidpointRounding.AwayFromZero),
                existing.Version
            });
        }
        var row = new M03UnitConversion
        {
            TenantId = user.TenantId, ConversionId = Guid.NewGuid(), FromUnitCode = from,
            ToUnitCode = to, ToUnitsPerFromUnit = factor
        };
        db.Add(row);
        masters.Audit(user, "Create", "M03UnitConversion", row.ConversionId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/unit-conversions/{row.ConversionId}", new
        {
            row.ConversionId, row.FromUnitCode, row.ToUnitCode, row.ToUnitsPerFromUnit,
            reverseFactor = decimal.Round(1m / row.ToUnitsPerFromUnit, 9, MidpointRounding.AwayFromZero)
        });
    }

    private static async Task<IResult> DeactivateConversion(
        Guid conversionId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03UnitConversion>().SingleOrDefaultAsync(x => x.ConversionId == conversionId, ct)
            ?? throw new KeyNotFoundException("Unit conversion was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03UnitConversion", conversionId,
            new { isActive = true }, new { row.IsActive, row.Version });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListPaymentTerms(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03PaymentTerm>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.Name).ToListAsync(ct));
    }

    private static async Task<IResult> CreatePaymentTerm(
        PaymentTermWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.DueDays is < 0 or > 3650)
            throw new ValidationException("Enter a payment-term name and due days from 0 to 3650.");
        var row = await db.Set<M03PaymentTerm>().SingleOrDefaultAsync(x => x.Name == request.Name.Trim(), ct);
        if (row is not null)
        {
            if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
            var before = new { row.DueDays, row.LedgerCode, row.IsActive };
            row.DueDays = request.DueDays;
            row.LedgerCode = Clean(request.LedgerCode);
            row.IsActive = true;
            row.Version++;
            masters.Audit(user, "Update", "M03PaymentTerm", row.PaymentTermId, before, row);
        }
        else
        {
            row = new M03PaymentTerm
            {
                TenantId = user.TenantId, PaymentTermId = Guid.NewGuid(), Name = request.Name.Trim(),
                DueDays = request.DueDays, LedgerCode = Clean(request.LedgerCode)
            };
            db.Add(row);
            masters.Audit(user, "Create", "M03PaymentTerm", row.PaymentTermId, null, row);
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivatePaymentTerm(
        Guid paymentTermId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03PaymentTerm>().SingleOrDefaultAsync(x => x.PaymentTermId == paymentTermId, ct)
            ?? throw new KeyNotFoundException("Payment term was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03PaymentTerm", paymentTermId,
            new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListChargeTypes(
        bool includeInactive, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var rows = db.Set<M03ChargeType>().AsNoTracking();
        if (!includeInactive) rows = rows.Where(x => x.IsActive);
        return Results.Ok(await rows.OrderBy(x => x.Name).ToListAsync(ct));
    }

    private static async Task<IResult> CreateChargeType(
        ChargeTypeWrite request, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 48 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 ||
            request.DefaultSurchargeRate is < 0 || request.DefaultTaxRatePercent is < 0 or > 100)
            throw new ValidationException("Enter a charge code/name and valid non-negative surcharge/GST rate.");
        var row = await db.Set<M03ChargeType>().SingleOrDefaultAsync(x => x.Code == request.Code.Trim(), ct);
        if (row is not null)
        {
            if (row.Version != request.Version) throw new DbUpdateConcurrencyException();
            var before = new { row.Name, row.LedgerCode, row.HsnCode, row.DefaultSurchargeRate, row.DefaultTaxRatePercent };
            row.Name = request.Name.Trim();
            row.LedgerCode = Clean(request.LedgerCode);
            row.HsnCode = Clean(request.HsnCode);
            row.DefaultSurchargeRate = request.DefaultSurchargeRate;
            row.DefaultTaxRatePercent = request.DefaultTaxRatePercent;
            row.IsActive = true;
            row.Version++;
            masters.Audit(user, "Update", "M03ChargeType", row.ChargeTypeId, before, row);
        }
        else
        {
            row = new M03ChargeType
            {
                TenantId = user.TenantId, ChargeTypeId = Guid.NewGuid(), Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(), LedgerCode = Clean(request.LedgerCode), HsnCode = Clean(request.HsnCode),
                DefaultSurchargeRate = request.DefaultSurchargeRate,
                DefaultTaxRatePercent = request.DefaultTaxRatePercent
            };
            db.Add(row);
            masters.Audit(user, "Create", "M03ChargeType", row.ChargeTypeId, null, row);
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> DeactivateChargeType(
        Guid chargeTypeId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03ChargeType>().SingleOrDefaultAsync(x => x.ChargeTypeId == chargeTypeId, ct)
            ?? throw new KeyNotFoundException("Charge type was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03ChargeType", chargeTypeId,
            new { isActive = true }, new { isActive = false });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListOnboardingAccounts(
        M03MasterService masters, OnboardingStore onboarding, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var tenant = await onboarding.Current();
        var setup = OnboardingStore.Decode(tenant);
        var accounts = setup.Accounts.Select(x => new
        {
            x.Name, x.Kind, x.Upi, x.Ifsc,
            accountNumber = string.IsNullOrEmpty(x.AccountNumber) ? null :
                $"••••{x.AccountNumber[^Math.Min(4, x.AccountNumber.Length)..]}"
        });
        return Results.Ok(new { tenant.Version, accounts });
    }

    private static async Task<IResult> ReplaceOnboardingAccounts(
        long version, OnboardingAccountWrite[] accounts, M03MasterService masters, OnboardingStore onboarding,
        CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        if (accounts.Length > 50 || accounts.Any(x => string.IsNullOrWhiteSpace(x.Name) ||
            x.Name.Length > 100 || x.Kind is not ("Bank" or "Cash") ||
            x.AccountNumber?.Length > 64 || x.Ifsc?.Length > 11 || x.Upi?.Length > 100))
            throw new ValidationException("Check bank/cash account names, types and field lengths.");
        await onboarding.Mutate(version, "Update bank/cash accounts", user.UserId.ToString(), (_, setup) =>
        {
            setup.Accounts = accounts.Select(x =>
            {
                var name = x.Name.Trim();
                var existing = setup.Accounts.SingleOrDefault(a => a.Name == name && a.Kind == x.Kind);
                var accountNumber = Clean(x.AccountNumber);
                if (accountNumber?.StartsWith("••••", StringComparison.Ordinal) == true)
                    accountNumber = existing?.AccountNumber
                        ?? throw new ValidationException("A masked account number can only be retained for the same account name and type.");
                return new Account(name, x.Kind, Clean(x.Upi), accountNumber, Clean(x.Ifsc));
            }).ToList();
        });
        return Results.NoContent();
    }

    private static IResult ImportTemplate(string entityType)
    {
        var (fileName, headers) = NormalizeImportType(entityType) switch
        {
            "customers" => ("customers-template.csv", "legalName,tradeName,gstin,pan,customerCategory,city,stateCode,billingAddress,customerGroup,creditLimit,creditDays,creditPolicy,creditStatus"),
            "suppliers" => ("suppliers-template.csv", "legalName,tradeName,gstin,pan,city,stateCode,billingAddress,isMsme,udyamNumber,tdsCategory"),
            "sites" => ("sites-template.csv", "customerPartyId,branchId,name,address,city,stateCode,gstin,latitude,longitude,accessNotes"),
            "products" => ("products-template.csv", "code,name,category,hsnCode,baseUnitCode,categoryAttributesJson,qualityReference,traceable"),
            _ => throw new ValidationException("Choose customers, suppliers, sites or products.")
        };
        return Results.File(System.Text.Encoding.UTF8.GetBytes(headers + Environment.NewLine),
            "text/csv; charset=utf-8", fileName);
    }

    private static async Task<IResult> PreviewImport(
        string entityType, IFormFile file, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.import", ct);
        await masters.RequireTenantWide(user, "masters.import", ct);
        var type = NormalizeImportType(entityType);
        if (file.Length is <= 0 or > 5_000_000)
            throw new ValidationException("Upload a non-empty Excel or CSV file no larger than 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".xlsx" or ".csv"))
            throw new ValidationException("Only .xlsx and .csv import files are supported.");
        await using var stream = file.OpenReadStream();
        var parsed = extension == ".xlsx"
            ? await ReadXlsx(stream, ct)
            : await ReadCsv(stream, ct);
        if (parsed.Rows.Count > 5000)
            throw new ValidationException("A single import preview may contain up to 5,000 data rows.");
        var valid = new List<Dictionary<string, string>>();
        var rowErrors = new List<M03ImportRowError>();
        var duplicateWarnings = new List<object>();
        var batchId = Guid.NewGuid();
        var rowNumber = 1;
        foreach (var row in parsed.Rows)
        {
            rowNumber++;
            try
            {
                await ValidateImportRow(type, row, db, ct);
                var duplicates = await ImportDuplicates(type, row, db, ct);
                if (duplicates.Length > 0)
                    duplicateWarnings.Add(new { row = rowNumber, existing = duplicates });
                valid.Add(row);
            }
            catch (Exception error) when (error is ValidationException or FormatException or KeyNotFoundException or
                                          JsonException or OverflowException)
            {
                rowErrors.Add(new M03ImportRowError
                {
                    TenantId = user.TenantId, RowErrorId = Guid.NewGuid(), BatchId = batchId,
                    RowNumber = rowNumber,
                    Message = error.Message, RowJson = JsonSerializer.Serialize(row)
                });
            }
        }
        var batch = new M03ImportBatch
        {
            TenantId = user.TenantId, BatchId = batchId, EntityType = type, Status = "Preview",
            SnapshotJson = JsonSerializer.Serialize(valid), CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(batch);
        db.AddRange(rowErrors);
        masters.Audit(user, "Preview", "M03ImportBatch", batch.BatchId, null,
            new { batch.EntityType, validRows = valid.Count, errorRows = rowErrors.Count });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            batch.BatchId, batch.EntityType, validRows = valid.Count, errorRows = rowErrors,
            duplicateWarnings, duplicateHandlingOptions = new[] { "skip", "update" }
        });
    }

    private static async Task<IResult> ConfirmImport(
        Guid batchId, ConfirmImportWrite request, M03MasterService masters, BerynxDbContext db,
        CancellationToken ct)
    {
        var user = await masters.Require("masters.import", ct);
        await masters.RequireTenantWide(user, "masters.import", ct);
        if (request.DuplicateHandling is not ("skip" or "update"))
            throw new ValidationException("Choose duplicate handling: skip or update.");
        var batch = await db.Set<M03ImportBatch>().SingleOrDefaultAsync(x => x.BatchId == batchId, ct)
            ?? throw new KeyNotFoundException("Import batch was not found.");
        if (batch.Status != "Preview")
            throw new ValidationException("Only an unconfirmed import preview can be confirmed.");
        var rows = JsonSerializer.Deserialize<Dictionary<string, string>[]>(batch.SnapshotJson) ?? [];
        var undo = new List<ImportUndoItem>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        foreach (var row in rows)
        {
            switch (batch.EntityType)
            {
                case "customers":
                case "suppliers":
                    (created, updated, skipped) = await ImportPartyRow(batch.EntityType, row,
                        request.DuplicateHandling, user, masters, db, undo, created, updated, skipped, ct);
                    break;
                case "products":
                    (created, updated, skipped) = await ImportProductRow(row,
                        request.DuplicateHandling, user, masters, db, undo, created, updated, skipped, ct);
                    break;
                case "sites":
                    (created, updated, skipped) = await ImportSiteRow(row,
                        request.DuplicateHandling, user, masters, db, undo, created, updated, skipped, ct);
                    break;
            }
        }
        batch.Status = "Confirmed";
        batch.ConfirmedAt = DateTimeOffset.UtcNow;
        batch.Version++;
        batch.SnapshotJson = JsonSerializer.Serialize(new ImportBatchData(rows, undo.ToArray()));
        masters.Audit(user, "Confirm", "M03ImportBatch", batchId, new { status = "Preview" },
            new { batch.Status, created, updated, skipped });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { batch.BatchId, batch.Status, created, updated, skipped, undoUntil = batch.ConfirmedAt.Value.AddHours(24) });
    }

    private static async Task<IResult> UndoImport(
        Guid batchId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.import", ct);
        await masters.RequireTenantWide(user, "masters.import", ct);
        var batch = await db.Set<M03ImportBatch>().SingleOrDefaultAsync(x => x.BatchId == batchId, ct)
            ?? throw new KeyNotFoundException("Import batch was not found.");
        if (batch.Status != "Confirmed" || batch.ConfirmedAt is null ||
            batch.ConfirmedAt.Value.AddHours(24) < DateTimeOffset.UtcNow)
            throw new ValidationException("A confirmed batch can only be undone within 24 hours.");
        var data = JsonSerializer.Deserialize<ImportBatchData>(batch.SnapshotJson)
            ?? throw new ValidationException("The import undo snapshot is unavailable.");
        foreach (var item in data.Undo.Reverse())
        {
            switch (item.EntityType)
            {
                case "party":
                    var party = await db.Set<M03Party>().SingleOrDefaultAsync(x => x.PartyId == item.Id, ct)
                        ?? throw new ValidationException("An imported party is no longer available for undo.");
                    if (item.Created)
                    {
                        if (party.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        party.IsActive = false;
                        party.CreditStatus = "Inactive";
                        party.Version++;
                    }
                    else if (item.BeforeJson is not null)
                    {
                        if (party.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        var before = JsonSerializer.Deserialize<ImportPartyUndo>(item.BeforeJson)
                            ?? throw new ValidationException("The imported party undo snapshot is invalid.");
                        db.Entry(party).CurrentValues.SetValues(
                            before.Party);
                        party.Version = item.ExpectedVersion + 1;
                        party.UpdatedAt = DateTimeOffset.UtcNow;
                        var existingRoles = await db.Set<M03PartyRole>()
                            .Where(x => x.PartyId == party.PartyId).ToListAsync(ct);
                        db.RemoveRange(existingRoles.Where(x => !before.Roles.Contains(x.RoleCode)));
                        foreach (var role in before.Roles.Where(code => existingRoles.All(x => x.RoleCode != code)))
                            db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = party.PartyId, RoleCode = role });
                    }
                    break;
                case "product":
                    var product = await db.Set<M03Product>().SingleOrDefaultAsync(x => x.ProductId == item.Id, ct)
                        ?? throw new ValidationException("An imported product is no longer available for undo.");
                    if (item.Created)
                    {
                        if (product.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        product.IsActive = false;
                        product.Version++;
                    }
                    else if (item.BeforeJson is not null)
                    {
                        if (product.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        db.Entry(product).CurrentValues.SetValues(
                            JsonSerializer.Deserialize<M03Product>(item.BeforeJson)!);
                        product.Version = item.ExpectedVersion + 1;
                        product.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    break;
                case "site":
                    var site = await db.Set<M03Site>().SingleOrDefaultAsync(x => x.SiteId == item.Id, ct)
                        ?? throw new ValidationException("An imported site is no longer available for undo.");
                    if (item.Created)
                    {
                        if (site.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        site.Status = "Inactive";
                        site.Version++;
                    }
                    else if (item.BeforeJson is not null)
                    {
                        if (site.Version != item.ExpectedVersion) throw new DbUpdateConcurrencyException();
                        db.Entry(site).CurrentValues.SetValues(
                            JsonSerializer.Deserialize<M03Site>(item.BeforeJson)!);
                        site.Version = item.ExpectedVersion + 1;
                        site.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    break;
            }
            masters.Audit(user, "UndoImport", item.EntityType, item.Id, null,
                new { batchId, item.Created, item.BeforeJson });
        }
        batch.Status = "Undone";
        batch.UndoneAt = DateTimeOffset.UtcNow;
        batch.Version++;
        masters.Audit(user, "Undo", "M03ImportBatch", batchId, new { status = "Confirmed" },
            new { batch.Status });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { batch.BatchId, batch.Status, batch.UndoneAt });
    }

    private static string NormalizeImportType(string entityType) => entityType.Trim().ToLowerInvariant() switch
    {
        "customers" or "customer" => "customers",
        "suppliers" or "supplier" => "suppliers",
        "sites" or "site" => "sites",
        "products" or "product" => "products",
        _ => throw new ValidationException("Choose customers, suppliers, sites or products.")
    };

    private static async Task<(List<Dictionary<string, string>> Rows, string[] Headers)> ReadCsv(
        Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct);
        var records = ParseCsv(text);
        if (records.Count < 2)
            throw new ValidationException("The import file must include a header and at least one data row.");
        var headers = records[0].Select(NormalizeHeader).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
            throw new ValidationException("The file has an empty or duplicate column heading.");
        return (records.Skip(1).Where(row => row.Any(x => !string.IsNullOrWhiteSpace(x)))
            .Select(row => MapImportRow(headers, row)).ToList(), headers);
    }

    private static async Task<(List<Dictionary<string, string>> Rows, string[] Headers)> ReadXlsx(
        Stream stream, CancellationToken ct)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new ValidationException("The workbook must contain a first worksheet.");
        if (worksheet.Length > 20_000_000)
            throw new ValidationException("The first worksheet expands beyond the allowed import size.");
        var strings = new List<string>();
        if (archive.GetEntry("xl/sharedStrings.xml") is { } sharedEntry)
        {
            if (sharedEntry.Length > 20_000_000)
                throw new ValidationException("The workbook shared strings exceed the allowed import size.");
            await using var sharedStream = sharedEntry.Open();
            var shared = await XDocument.LoadAsync(sharedStream, LoadOptions.None, ct);
            strings = shared.Descendants().Where(x => x.Name.LocalName == "si")
                .Select(item => string.Concat(item.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value)))
                .ToList();
        }
        await using var sheetStream = worksheet.Open();
        var document = await XDocument.LoadAsync(sheetStream, LoadOptions.None, ct);
        var records = document.Descendants().Where(x => x.Name.LocalName == "row")
            .Select(row => row.Elements().Where(cell => cell.Name.LocalName == "c")
                .ToDictionary(cell => ColumnIndex((string?)cell.Attribute("r") ?? ""),
                    cell => XlsxCellValue(cell, strings)))
            .Where(row => row.Count > 0).ToList();
        if (records.Count < 2)
            throw new ValidationException("The workbook must include a header and at least one data row.");
        var headerCells = records[0];
        var maxColumn = headerCells.Keys.Max();
        var headers = Enumerable.Range(0, maxColumn + 1)
            .Select(index => NormalizeHeader(headerCells.GetValueOrDefault(index, ""))).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
            throw new ValidationException("The workbook has an empty or duplicate column heading.");
        return (records.Skip(1).Where(row => row.Values.Any(x => !string.IsNullOrWhiteSpace(x)))
            .Select(row => MapImportRow(headers,
                Enumerable.Range(0, headers.Length).Select(index => row.GetValueOrDefault(index, "")).ToArray()))
            .ToList(), headers);
    }

    private static string XlsxCellValue(XElement cell, IReadOnlyList<string> shared)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr")
            return string.Concat(cell.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value));
        var value = cell.Elements().FirstOrDefault(x => x.Name.LocalName == "v")?.Value ?? "";
        if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < shared.Count)
            return shared[index];
        return value;
    }

    private static int ColumnIndex(string reference)
    {
        var index = 0;
        foreach (var character in reference.TakeWhile(char.IsLetter))
            index = checked(index * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        if (index == 0) throw new ValidationException("A workbook cell has an invalid reference.");
        return index - 1;
    }

    private static List<string[]> ParseCsv(string text)
    {
        var records = new List<string[]>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"' && field.Length == 0) quoted = true;
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(field.ToString());
                field.Clear();
                records.Add(row.ToArray());
                row.Clear();
            }
            else field.Append(character);
        }
        if (quoted) throw new ValidationException("The CSV file contains an unclosed quoted field.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            records.Add(row.ToArray());
        }
        return records;
    }

    private static string NormalizeHeader(string header) =>
        header.Trim().TrimStart('\uFEFF').ToLowerInvariant();

    private static Dictionary<string, string> MapImportRow(string[] headers, string[] values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Length; index++)
            row[headers[index]] = index < values.Length ? values[index].Trim() : "";
        return row;
    }

    private static string ImportValue(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) ? value.Trim() : "";

    private static async Task ValidateImportRow(
        string type, Dictionary<string, string> row, BerynxDbContext db, CancellationToken ct)
    {
        switch (type)
        {
            case "customers":
            case "suppliers":
                if (string.IsNullOrWhiteSpace(ImportValue(row, "legalname")))
                    throw new ValidationException("Legal name is required.");
                if (ImportValue(row, "legalname").Length > 200)
                    throw new ValidationException("Legal name must be at most 200 characters.");
                var gstin = ImportValue(row, "gstin");
                var pan = ImportValue(row, "pan");
                if (gstin.Length > 0)
                {
                    var normalized = M03MasterService.NormalizeGstin(gstin);
                    var normalizedPan = M03MasterService.NormalizePan(pan);
                    if (normalizedPan is not null && normalized.Substring(2, 10) != normalizedPan)
                        throw new ValidationException("The PAN does not match the PAN encoded in the GSTIN.");
                    row["gstin"] = normalized;
                }
                else _ = M03MasterService.NormalizePan(pan);
                if (ImportValue(row, "statecode").Length > 0)
                    _ = NormalizeState(ImportValue(row, "statecode"));
                if (ImportValue(row, "creditlimit").Length > 0 &&
                    (!decimal.TryParse(ImportValue(row, "creditlimit"), System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var limit) || limit < 0))
                    throw new ValidationException("Credit limit must be a non-negative decimal.");
                if (ImportValue(row, "creditdays").Length > 0 &&
                    (!int.TryParse(ImportValue(row, "creditdays"), out var days) || days < 0 || days > 3650))
                    throw new ValidationException("Credit days must be between 0 and 3650.");
                if (ImportValue(row, "udyamnumber").Length > 0 &&
                    !System.Text.RegularExpressions.Regex.IsMatch(ImportValue(row, "udyamnumber").Trim().ToUpperInvariant(),
                        "^UDYAM-[A-Z]{2}-[0-9]{2}-[0-9]{7}$"))
                    throw new ValidationException("Udyam number must use UDYAM-XX-00-0000000 format.");
                break;
            case "products":
                foreach (var required in new[] { "code", "name", "category", "hsncode", "baseunitcode" })
                    if (string.IsNullOrWhiteSpace(ImportValue(row, required)))
                        throw new ValidationException($"{required} is required.");
                var category = ImportValue(row, "category");
                var attributes = string.IsNullOrWhiteSpace(ImportValue(row, "categoryattributesjson"))
                    ? "{}" : ImportValue(row, "categoryattributesjson");
                row["categoryattributesjson"] = M03MasterService.NormalizeProductAttributes(category, attributes);
                var unit = NormalizeUnitCode(ImportValue(row, "baseunitcode"));
                if (!await db.Set<M03Unit>().AnyAsync(x => x.UnitCode == unit && x.IsActive, ct))
                    throw new ValidationException($"Base unit {unit} is not configured.");
                row["baseunitcode"] = unit;
                break;
            case "sites":
                if (!Guid.TryParse(ImportValue(row, "customerpartyid"), out var customerId) ||
                    !await db.Set<M03PartyRole>().AnyAsync(x =>
                        x.PartyId == customerId && x.RoleCode == "Customer", ct))
                    throw new ValidationException("A valid customerPartyId is required.");
                foreach (var required in new[] { "name", "address", "city", "statecode" })
                    if (string.IsNullOrWhiteSpace(ImportValue(row, required)))
                        throw new ValidationException($"{required} is required.");
                _ = NormalizeState(ImportValue(row, "statecode"));
                if (ImportValue(row, "gstin").Length > 0)
                    row["gstin"] = M03MasterService.NormalizeGstin(ImportValue(row, "gstin"));
                if (ImportValue(row, "branchid").Length > 0 &&
                    !Guid.TryParse(ImportValue(row, "branchid"), out _))
                    throw new ValidationException("Branch ID must be a valid identifier.");
                var latitude = ParseNullableDecimal(ImportValue(row, "latitude"));
                var longitude = ParseNullableDecimal(ImportValue(row, "longitude"));
                if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
                    throw new ValidationException("Site coordinates are outside the valid latitude/longitude range.");
                break;
        }
    }

    private static async Task<object[]> ImportDuplicates(
        string type, Dictionary<string, string> row, BerynxDbContext db, CancellationToken ct)
    {
        if (type is "customers" or "suppliers")
        {
            var name = ImportValue(row, "legalname");
            var gstin = ImportValue(row, "gstin");
            var pan = ImportValue(row, "pan").ToUpperInvariant();
            var city = ImportValue(row, "city");
            var parties = await db.Set<M03Party>().AsNoTracking().Where(x => x.IsActive &&
                    ((!string.IsNullOrEmpty(gstin) && x.Gstin == gstin) ||
                     (!string.IsNullOrEmpty(pan) && x.Pan == pan) ||
                     (!string.IsNullOrEmpty(city) && x.City != null &&
                      x.City.ToLower() == city.ToLower() && x.LegalName.ToLower() == name.ToLower())))
                .Select(x => new { x.PartyId, x.LegalName, x.Gstin, x.Pan, x.City })
                .ToListAsync(ct);
            return parties.Cast<object>().ToArray();
        }
        if (type == "products")
        {
            var products = await db.Set<M03Product>().AsNoTracking().Where(x =>
                    x.IsActive && x.Code == ImportValue(row, "code").ToUpperInvariant())
                .Select(x => new { x.ProductId, x.Code, x.Name }).ToListAsync(ct);
            return products.Cast<object>().ToArray();
        }
        if (type == "sites" && Guid.TryParse(ImportValue(row, "customerpartyid"), out var customerId))
        {
            var sites = await db.Set<M03Site>().AsNoTracking().Where(x =>
                    x.CustomerPartyId == customerId && x.Name.ToLower() == ImportValue(row, "name").ToLower())
                .Select(x => new { x.SiteId, x.CustomerPartyId, x.Name }).ToListAsync(ct);
            return sites.Cast<object>().ToArray();
        }
        return [];
    }

    private static async Task<(int Created, int Updated, int Skipped)> ImportPartyRow(
        string type, Dictionary<string, string> values, string duplicateHandling, M02User user,
        M03MasterService masters, BerynxDbContext db, List<ImportUndoItem> undo,
        int created, int updated, int skipped, CancellationToken ct)
    {
        var role = type == "customers" ? "Customer" : "Supplier";
        var existing = await FindImportParty(values, db, ct);
        if (existing is not null && duplicateHandling == "skip")
            return (created, updated, skipped + 1);
        if (existing is not null)
        {
            var rolesBefore = await db.Set<M03PartyRole>().Where(x => x.PartyId == existing.PartyId)
                .Select(x => x.RoleCode).ToArrayAsync(ct);
            var before = JsonSerializer.Serialize(new ImportPartyUndo(existing, rolesBefore));
            ImportPartyValues(existing, values);
            existing.Version++;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            if (!await db.Set<M03PartyRole>().AnyAsync(x =>
                    x.PartyId == existing.PartyId && x.RoleCode == role, ct))
                db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = existing.PartyId, RoleCode = role });
            undo.Add(new ImportUndoItem("party", existing.PartyId, false, existing.Version, before));
            masters.Audit(user, "ImportUpdate", "M03Party", existing.PartyId, before, existing);
            return (created, updated + 1, skipped);
        }
        var party = new M03Party
        {
            TenantId = user.TenantId, PartyId = Guid.NewGuid(), LegalName = ImportValue(values, "legalname").Trim(),
            PartyType = "Business", CreditPolicy = "Warn", CreditStatus = "Active",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        ImportPartyValues(party, values);
        db.Add(party);
        db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = party.PartyId, RoleCode = role });
        undo.Add(new ImportUndoItem("party", party.PartyId, true, party.Version, null));
        masters.Audit(user, "ImportCreate", "M03Party", party.PartyId, null, party);
        return (created + 1, updated, skipped);
    }

    private static async Task<M03Party?> FindImportParty(
        Dictionary<string, string> values, BerynxDbContext db, CancellationToken ct)
    {
        var gstin = ImportValue(values, "gstin");
        var pan = ImportValue(values, "pan").ToUpperInvariant();
        var name = ImportValue(values, "legalname").ToLowerInvariant();
        var city = ImportValue(values, "city").ToLowerInvariant();
        return await db.Set<M03Party>().Where(x => x.IsActive &&
                ((!string.IsNullOrEmpty(gstin) && x.Gstin == gstin) ||
                 (!string.IsNullOrEmpty(pan) && x.Pan == pan) ||
                 (!string.IsNullOrEmpty(city) && x.City != null &&
                  x.City.ToLower() == city && x.LegalName.ToLower() == name)))
            .OrderBy(x => x.PartyId).FirstOrDefaultAsync(ct);
    }

    private static void ImportPartyValues(M03Party party, Dictionary<string, string> values)
    {
        SetIfProvided(values, "legalname", value => party.LegalName = value);
        SetIfProvided(values, "tradename", value => party.TradeName = value);
        SetIfProvided(values, "gstin", value => party.Gstin = value);
        SetIfProvided(values, "pan", value => party.Pan = M03MasterService.NormalizePan(value));
        SetIfProvided(values, "customercategory", value => party.CustomerCategory = value);
        SetIfProvided(values, "city", value => party.City = value);
        SetIfProvided(values, "statecode", value => party.StateCode = NormalizeState(value));
        SetIfProvided(values, "billingaddress", value => party.BillingAddress = value);
        SetIfProvided(values, "customergroup", value => party.CustomerGroup = value);
        SetIfProvided(values, "creditpolicy", value =>
        {
            if (value is not ("Warn" or "SoftBlock" or "HardBlock"))
                throw new ValidationException("Credit policy must be Warn, SoftBlock or HardBlock.");
            party.CreditPolicy = value;
        });
        SetIfProvided(values, "creditstatus", value =>
        {
            if (value is not ("Active" or "OnHold" or "Blocked"))
                throw new ValidationException("Credit status must be Active, OnHold or Blocked.");
            party.CreditStatus = value;
        });
        SetIfProvided(values, "creditlimit", value =>
        {
            if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount < 0)
                throw new ValidationException("Credit limit must be a non-negative decimal.");
            party.CreditLimit = amount;
        });
        SetIfProvided(values, "creditdays", value =>
        {
            if (!int.TryParse(value, out var days) || days < 0 || days > 3650)
                throw new ValidationException("Credit days must be between 0 and 3650.");
            party.CreditDays = days;
        });
        SetIfProvided(values, "ismsme", value => party.IsMsme = ParseBoolean(value));
        SetIfProvided(values, "udyamnumber", value => party.UdyamNumber = value);
        SetIfProvided(values, "tdscategory", value => party.TdsCategory = value);
    }

    private static async Task<(int Created, int Updated, int Skipped)> ImportProductRow(
        Dictionary<string, string> values, string duplicateHandling, M02User user,
        M03MasterService masters, BerynxDbContext db, List<ImportUndoItem> undo,
        int created, int updated, int skipped, CancellationToken ct)
    {
        var code = ImportValue(values, "code").Trim().ToUpperInvariant();
        var row = await db.Set<M03Product>().SingleOrDefaultAsync(x => x.Code == code, ct);
        if (row is not null && duplicateHandling == "skip")
            return (created, updated, skipped + 1);
        var attributes = ImportValue(values, "categoryattributesjson");
        if (row is not null)
        {
            var before = JsonSerializer.Serialize(row);
            ApplyProductValues(row, values, attributes);
            row.Version++;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            undo.Add(new ImportUndoItem("product", row.ProductId, false, row.Version, before));
            masters.Audit(user, "ImportUpdate", "M03Product", row.ProductId, before, row);
            return (created, updated + 1, skipped);
        }
        row = new M03Product
        {
            TenantId = user.TenantId, ProductId = Guid.NewGuid(), Version = 1, UpdatedAt = DateTimeOffset.UtcNow
        };
        ApplyProductValues(row, values, attributes);
        db.Add(row);
        undo.Add(new ImportUndoItem("product", row.ProductId, true, row.Version, null));
        masters.Audit(user, "ImportCreate", "M03Product", row.ProductId, null, row);
        return (created + 1, updated, skipped);
    }

    private static void ApplyProductValues(M03Product product, Dictionary<string, string> values, string attributes)
    {
        product.Code = ImportValue(values, "code").Trim().ToUpperInvariant();
        product.Name = ImportValue(values, "name").Trim();
        product.Category = ImportValue(values, "category").Trim();
        product.HsnCode = ImportValue(values, "hsncode").Trim();
        product.BaseUnitCode = NormalizeUnitCode(ImportValue(values, "baseunitcode"));
        product.CategoryAttributesJson = M03MasterService.NormalizeProductAttributes(product.Category,
            string.IsNullOrWhiteSpace(attributes) ? "{}" : attributes);
        product.QualityReference = ImportValue(values, "qualityreferenc").Trim();
        if (values.ContainsKey("qualityreference"))
            product.QualityReference = ImportValue(values, "qualityreference").Trim();
        if (values.ContainsKey("traceable")) product.Traceable = ParseBoolean(ImportValue(values, "traceable"));
    }

    private static async Task<(int Created, int Updated, int Skipped)> ImportSiteRow(
        Dictionary<string, string> values, string duplicateHandling, M02User user,
        M03MasterService masters, BerynxDbContext db, List<ImportUndoItem> undo,
        int created, int updated, int skipped, CancellationToken ct)
    {
        var customerId = Guid.Parse(ImportValue(values, "customerpartyid"));
        var name = ImportValue(values, "name").Trim();
        var row = await db.Set<M03Site>().SingleOrDefaultAsync(x =>
            x.CustomerPartyId == customerId && x.Name == name, ct);
        if (row is not null && duplicateHandling == "skip")
            return (created, updated, skipped + 1);
        if (row is null)
        {
            row = new M03Site
            {
                TenantId = user.TenantId, SiteId = Guid.NewGuid(), Version = 1,
                Status = "Active", UpdatedAt = DateTimeOffset.UtcNow
            };
            ApplySiteValues(row, values);
            db.Add(row);
            undo.Add(new ImportUndoItem("site", row.SiteId, true, row.Version, null));
            masters.Audit(user, "ImportCreate", "M03Site", row.SiteId, null, row);
            return (created + 1, updated, skipped);
        }
        var before = JsonSerializer.Serialize(row);
        ApplySiteValues(row, values);
        row.Version++;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        undo.Add(new ImportUndoItem("site", row.SiteId, false, row.Version, before));
        masters.Audit(user, "ImportUpdate", "M03Site", row.SiteId, before, row);
        return (created, updated + 1, skipped);
    }

    private static void ApplySiteValues(M03Site site, Dictionary<string, string> values)
    {
        site.CustomerPartyId = Guid.Parse(ImportValue(values, "customerpartyid"));
        site.BranchId = Guid.TryParse(ImportValue(values, "branchid"), out var branchId) ? branchId : null;
        site.Name = ImportValue(values, "name").Trim();
        site.Address = ImportValue(values, "address").Trim();
        site.City = ImportValue(values, "city").Trim();
        site.StateCode = NormalizeState(ImportValue(values, "statecode"));
        site.Gstin = string.IsNullOrWhiteSpace(ImportValue(values, "gstin"))
            ? null : M03MasterService.NormalizeGstin(ImportValue(values, "gstin"));
        site.Latitude = ParseNullableDecimal(ImportValue(values, "latitude"));
        site.Longitude = ParseNullableDecimal(ImportValue(values, "longitude"));
        site.AccessNotes = ImportValue(values, "accessnotes").Trim();
    }

    private static void SetIfProvided(
        Dictionary<string, string> values, string key, Action<string> set)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            set(value.Trim());
    }

    private static bool ParseBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "true" or "1" or "yes" or "y" => true,
        "false" or "0" or "no" or "n" => false,
        _ => throw new ValidationException("Use true/false, yes/no or 1/0 for boolean fields.")
    };

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
            throw new ValidationException("Enter a numeric value using invariant decimal notation.");
        return result;
    }

    private static async Task<IResult> ListPartyBankAccounts(
        Guid partyId, M03MasterService masters, BerynxDbContext db, IDataProtectionProvider dataProtection,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == partyId, ct))
            throw new KeyNotFoundException("Party was not found.");
        var protector = dataProtection.CreateProtector($"Berynx.M03.PartyBank.{tenant.TenantId}");
        var rows = await db.Set<M03PartyBankAccount>().AsNoTracking()
            .Where(x => x.PartyId == partyId && x.IsActive).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new
        {
            x.BankAccountId, x.PartyId, x.AccountHolder, x.Ifsc, x.BankName, x.IsPrimary, x.Version,
            maskedAccountNumber = Mask(protector.Unprotect(x.AccountNumberProtected))
        }));
    }

    private static async Task<IResult> PartyBankAccountAudit(
        Guid partyId, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("audit.view", ct);
        await masters.RequireTenantWide(user, "audit.view", ct);
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == partyId, ct))
            throw new KeyNotFoundException("Party was not found.");
        var accountIds = await db.Set<M03PartyBankAccount>().AsNoTracking()
            .Where(x => x.PartyId == partyId).Select(x => x.BankAccountId).ToArrayAsync(ct);
        return Results.Ok(await db.Set<M02AuditEvent>().AsNoTracking()
            .Where(x => x.EntityType == "M03PartyBankAccount" && accountIds.Contains(x.EntityId!.Value))
            .OrderByDescending(x => x.At).Take(100)
            .Select(x => new { x.EventId, x.At, x.ActorSubject, x.Action, x.Reason, x.EntityId })
            .ToListAsync(ct));
    }

    private static async Task<IResult> CreatePartyBankAccount(
        PartyBankAccountWrite request, M03MasterService masters, BerynxDbContext db,
        IDataProtectionProvider dataProtection, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var user = await masters.Require("masters.create", ct);
        await masters.RequireTenantWide(user, "masters.create", ct);
        if (string.IsNullOrWhiteSpace(request.AccountHolder) || request.AccountHolder.Length > 200 ||
            string.IsNullOrWhiteSpace(request.AccountNumber) || request.AccountNumber.Length > 64 ||
            request.AccountNumber.Contains('•') || request.AccountNumber.Contains('*') ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.Ifsc.Trim().ToUpperInvariant(), "^[A-Z]{4}0[A-Z0-9]{6}$"))
            throw new ValidationException("Enter account holder, account number and valid IFSC.");
        if (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.PartyId && x.IsActive, ct))
            throw new KeyNotFoundException("Party was not found.");
        var protector = dataProtection.CreateProtector($"Berynx.M03.PartyBank.{tenant.TenantId}");
        var row = new M03PartyBankAccount
        {
            TenantId = user.TenantId, BankAccountId = Guid.NewGuid(), PartyId = request.PartyId,
            AccountHolder = request.AccountHolder.Trim(),
            AccountNumberProtected = protector.Protect(request.AccountNumber.Trim()),
            Ifsc = request.Ifsc.Trim().ToUpperInvariant(), BankName = Clean(request.BankName),
            IsPrimary = request.IsPrimary
        };
        if (row.IsPrimary)
        {
            foreach (var existing in await db.Set<M03PartyBankAccount>()
                         .Where(x => x.PartyId == request.PartyId && x.IsPrimary && x.IsActive).ToListAsync(ct))
            {
                existing.IsPrimary = false;
                existing.Version++;
                masters.Audit(user, "PrimaryAccountChanged", "M03PartyBankAccount", existing.BankAccountId,
                    new { isPrimary = true }, new { isPrimary = false, existing.Version });
            }
        }
        db.Add(row);
        masters.Audit(user, "Create", "M03PartyBankAccount", row.BankAccountId, null,
            new { row.PartyId, row.AccountHolder, row.Ifsc, row.BankName, row.IsPrimary, accountNumber = "redacted" });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m03/parties/{request.PartyId}/bank-accounts",
            new { row.BankAccountId, row.AccountHolder, row.Ifsc, row.BankName, row.IsPrimary, maskedAccountNumber = Mask(request.AccountNumber) });
    }

    private static async Task<IResult> UpdatePartyBankAccount(
        Guid bankAccountId, PartyBankAccountUpdate request, M03MasterService masters, BerynxDbContext db,
        IDataProtectionProvider dataProtection, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var user = await masters.Require("masters.edit", ct);
        await masters.RequireTenantWide(user, "masters.edit", ct);
        if (string.IsNullOrWhiteSpace(request.AccountHolder) || request.AccountHolder.Length > 200 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                request.Ifsc.Trim().ToUpperInvariant(), "^[A-Z]{4}0[A-Z0-9]{6}$"))
            throw new ValidationException("Enter an account holder and valid IFSC.");

        var row = await db.Set<M03PartyBankAccount>()
            .SingleOrDefaultAsync(x => x.BankAccountId == bankAccountId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Party bank account was not found.");
        if (row.Version != request.Version)
            throw new DbUpdateConcurrencyException();

        var replacingAccountNumber = !string.IsNullOrWhiteSpace(request.AccountNumber);
        if (replacingAccountNumber && (request.AccountNumber!.Length > 64 ||
            request.AccountNumber.Contains('•') || request.AccountNumber.Contains('*')))
            throw new ValidationException("Enter a new unmasked account number; masked display values cannot be saved.");

        var before = new
        {
            row.AccountHolder, row.Ifsc, row.BankName, row.IsPrimary,
            accountNumber = "redacted", row.Version
        };
        if (request.IsPrimary)
        {
            foreach (var existing in await db.Set<M03PartyBankAccount>()
                         .Where(x => x.PartyId == row.PartyId && x.BankAccountId != bankAccountId &&
                             x.IsPrimary && x.IsActive).ToListAsync(ct))
            {
                existing.IsPrimary = false;
                existing.Version++;
                masters.Audit(user, "PrimaryAccountChanged", "M03PartyBankAccount", existing.BankAccountId,
                    new { isPrimary = true }, new { isPrimary = false, existing.Version });
            }
        }
        row.AccountHolder = request.AccountHolder.Trim();
        row.Ifsc = request.Ifsc.Trim().ToUpperInvariant();
        row.BankName = Clean(request.BankName);
        row.IsPrimary = request.IsPrimary;
        if (replacingAccountNumber)
        {
            var protector = dataProtection.CreateProtector($"Berynx.M03.PartyBank.{tenant.TenantId}");
            row.AccountNumberProtected = protector.Protect(request.AccountNumber!.Trim());
        }
        row.Version++;
        masters.Audit(user, "Update", "M03PartyBankAccount", bankAccountId, before,
            new
            {
                row.AccountHolder, row.Ifsc, row.BankName, row.IsPrimary,
                accountNumber = replacingAccountNumber ? "replaced and redacted" : "unchanged",
                row.Version
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            row.BankAccountId, row.PartyId, row.AccountHolder, row.Ifsc, row.BankName,
            row.IsPrimary, row.Version
        });
    }

    private static async Task<IResult> DeactivatePartyBankAccount(
        Guid bankAccountId, long version, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.deactivate", ct);
        await masters.RequireTenantWide(user, "masters.deactivate", ct);
        var row = await db.Set<M03PartyBankAccount>().SingleOrDefaultAsync(x => x.BankAccountId == bankAccountId, ct)
            ?? throw new KeyNotFoundException("Party bank account was not found.");
        if (row.Version != version) throw new DbUpdateConcurrencyException();
        row.IsActive = false;
        row.Version++;
        masters.Audit(user, "Deactivate", "M03PartyBankAccount", bankAccountId,
            new { row.IsActive, row.AccountHolder, row.Ifsc }, new { isActive = false, row.AccountHolder, row.Ifsc });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<bool> mastersScope(
        M03MasterService masters, M02User user, string permission, Guid? branchId, Guid? plantId,
        CancellationToken ct)
    {
        try
        {
            await masters.RequireBranch(user, permission, branchId, plantId, ct);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void ValidateParty(PartyWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.LegalName) || request.LegalName.Length > 200 ||
            request.PartyType is not ("Business" or "Individual" or "Institution") ||
            request.CreditLimit < 0 || request.CreditDays is < 0 or > 3650 ||
            request.CreditPolicy is not ("Warn" or "SoftBlock" or "HardBlock") ||
            request.CreditStatus is not ("Active" or "OnHold" or "Blocked") ||
            request.Roles is null || request.Roles.Length == 0)
            throw new ValidationException("Check the party name, type, credit policy and at least one party role.");
        _ = NormalizeRoles(request.Roles);
        _ = NormalizePartyTags(request.TagsJson);
    }

    private static string NormalizePartyTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return "[]";
        string[]? tags;
        try
        {
            tags = JsonSerializer.Deserialize<string[]>(tagsJson);
        }
        catch (JsonException)
        {
            throw new ValidationException("Tags must be a JSON array of strings.");
        }
        if (tags is null || tags.Length > 50 ||
            tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Trim().Length > 40))
            throw new ValidationException("Use at most 50 non-empty tags of 40 characters or fewer.");
        return JsonSerializer.Serialize(tags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string[] NormalizeRoles(string[] roles)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
            { "Customer", "Supplier", "Transporter", "Contractor", "ContactOnly", "PumpVendor" };
        var normalized = roles.Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (normalized.Any(x => !allowed.Contains(x)))
            throw new ValidationException("Party roles must be Customer, Supplier, Transporter, Contractor, ContactOnly or PumpVendor.");
        return normalized;
    }

    private static async Task ReplacePartyRoles(
        M02User user, BerynxDbContext db, Guid partyId, string[] requested, CancellationToken ct)
    {
        var roles = NormalizeRoles(requested);
        var existing = await db.Set<M03PartyRole>().Where(x => x.PartyId == partyId).ToListAsync(ct);
        if (existing.Any(x => x.RoleCode == "Customer" && !roles.Contains("Customer")) &&
            await db.Set<M03Site>().AnyAsync(x => x.CustomerPartyId == partyId, ct))
            throw new ValidationException("A party with customer sites must retain its Customer role.");
        if (existing.Any(x => x.RoleCode == "Supplier" && !roles.Contains("Supplier")) &&
            await db.Set<M03Plant>().AnyAsync(x => x.SupplierPartyId == partyId, ct))
            throw new ValidationException("A party with supplier plants must retain its Supplier role.");
        db.RemoveRange(existing.Where(x => !roles.Contains(x.RoleCode)));
        foreach (var role in roles.Where(role => existing.All(x => x.RoleCode != role)))
            db.Add(new M03PartyRole { TenantId = user.TenantId, PartyId = partyId, RoleCode = role });
    }

    private static async Task<object[]> FindPartyDuplicates(
        BerynxDbContext db, M03Party candidate, CancellationToken ct)
    {
        var query = db.Set<M03Party>().AsNoTracking().Where(x => x.IsActive && x.PartyId != candidate.PartyId &&
            ((!string.IsNullOrWhiteSpace(candidate.Gstin) && x.Gstin == candidate.Gstin) ||
             (!string.IsNullOrWhiteSpace(candidate.Pan) && x.Pan == candidate.Pan) ||
             (!string.IsNullOrWhiteSpace(candidate.City) && x.City != null &&
              x.City.ToLower() == candidate.City.ToLower() &&
              x.LegalName.ToLower() == candidate.LegalName.ToLower())));
        var rows = await query.OrderBy(x => x.LegalName).Take(50).ToListAsync(ct);
        return rows.Select(row => new
            {
                row.PartyId, row.LegalName, row.TradeName, row.Gstin, row.Pan, row.City,
                matchedBy = new[]
                {
                    !string.IsNullOrWhiteSpace(candidate.Gstin) && row.Gstin == candidate.Gstin ? "GSTIN" : null,
                    !string.IsNullOrWhiteSpace(candidate.Pan) && row.Pan == candidate.Pan ? "PAN" : null,
                    !string.IsNullOrWhiteSpace(candidate.City) && row.City?.Equals(candidate.City, StringComparison.OrdinalIgnoreCase) == true &&
                    row.LegalName.Equals(candidate.LegalName, StringComparison.OrdinalIgnoreCase) ? "NameAndCity" : null
                }.Where(x => x is not null).ToArray()
            })
            .Where(x => x.matchedBy.Length > 0)
            .Cast<object>()
            .ToArray();
    }

    private static async Task<IResult> FindPartyMobileDuplicates(
        string mobile, M03MasterService masters, BerynxDbContext db, CancellationToken ct)
    {
        var user = await masters.Require("masters.view", ct);
        await masters.RequireTenantWide(user, "masters.view", ct);
        var normalized = M03MasterService.NormalizeMobile(mobile);
        var rows = await db.Set<M03Contact>().AsNoTracking().Where(x => x.IsActive && x.Mobile == normalized)
            .Select(x => new { x.PartyId, x.ContactId, x.Name, x.Mobile }).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task ValidatePartyReferences(
        PartyWrite request, Guid partyId, BerynxDbContext db, CancellationToken ct)
    {
        if (request.PaymentTermId is { } termId &&
            !await db.Set<M03PaymentTerm>().AnyAsync(x => x.PaymentTermId == termId && x.IsActive, ct))
            throw new ValidationException("Select an active payment term.");
        if (request.DefaultPriceListId is { } priceListId &&
            !await db.Set<M03PriceList>().AnyAsync(x => x.PriceListId == priceListId && x.IsActive &&
                (x.CustomerPartyId == partyId || x.CustomerPartyId == null), ct))
            throw new ValidationException("Select an active price list for this customer.");
        if (request.SalespersonUserId is { } salespersonId &&
            !await db.Set<M02User>().AnyAsync(x => x.UserId == salespersonId && x.Status == "Active", ct))
            throw new ValidationException("Select an active salesperson in this tenant.");
    }

    private static void ValidateSite(SiteWrite request)
    {
        if (request.CustomerPartyId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Length > 160 || string.IsNullOrWhiteSpace(request.Address) ||
            request.Address.Length > 500 || string.IsNullOrWhiteSpace(request.City) ||
            request.City.Length > 100 ||
            (string.IsNullOrWhiteSpace(request.StateCode) && string.IsNullOrWhiteSpace(request.Gstin)) ||
            (request.Latitude is < -90 or > 90) || (request.Longitude is < -180 or > 180) ||
            request.Status is not ("Active" or "Inactive" or "OnHold"))
            throw new ValidationException("Check customer site identity, address, state, geo coordinates and status.");
    }

    private static void ValidatePlant(PlantWrite request)
    {
        if (request.SupplierPartyId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Length > 160 || string.IsNullOrWhiteSpace(request.Address) ||
            request.Address.Length > 500 || string.IsNullOrWhiteSpace(request.City) ||
            request.City.Length > 100 || request.CapacityPerHour is < 0 ||
            (request.Grades ?? []).Any(string.IsNullOrWhiteSpace))
            throw new ValidationException("Check supplier, plant name/location, capacity and grade values.");
        _ = NormalizeState(request.StateCode);
    }

    private static void ValidateContact(ContactWrite request)
    {
        if (request.PartyId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 150 ||
            string.IsNullOrWhiteSpace(request.Role) || request.Role.Length > 80)
            throw new ValidationException("Contact name, role and party are required.");
        if (Clean(request.Email) is null && Clean(request.Mobile) is null)
            throw new ValidationException("Provide an email address or mobile number.");
        if (Clean(request.Email) is { } email &&
            (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new ValidationException("Enter a valid contact email address.");
        _ = M03MasterService.NormalizeMobile(request.Mobile);
        if ((request.EmailConsent || request.SmsConsent || request.WhatsAppConsent) &&
            string.IsNullOrWhiteSpace(request.ConsentSource))
            throw new ValidationException("A recorded communication consent requires its source.");
    }

    private static void ValidateProduct(ProductWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 64 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 180 ||
            string.IsNullOrWhiteSpace(request.HsnCode) || request.HsnCode.Length > 16 ||
            string.IsNullOrWhiteSpace(request.BaseUnitCode) || request.BaseUnitCode.Length > 16 ||
            request.QualityReference.Length > 500)
            throw new ValidationException("Enter a product code, name, HSN, base unit and valid quality reference.");
    }

    private static void ValidateRatePeriod(decimal rate, DateOnly from, DateOnly? to, string reason)
    {
        if (rate is < 0 or > 100 || to < from || string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw new ValidationException("Enter a 0–100% rate, valid effective dates and a reason.");
    }

    private static async Task EnsureNoTaxOverlap(
        BerynxDbContext db, string hsn, DateOnly from, DateOnly? to, Guid? excluding, CancellationToken ct)
    {
        var rows = db.Set<M03TaxRate>().Where(x => x.HsnCode == hsn && x.IsActive);
        if (excluding is { } id) rows = rows.Where(x => x.TaxRateId != id);
        if (await rows.AnyAsync(x => (x.EffectiveTo == null || x.EffectiveTo >= from) &&
                (to == null || x.EffectiveFrom <= to), ct))
            throw new ValidationException("Effective-dated tax-rate periods cannot overlap for the same HSN.");
    }

    private static async Task EnsureCategoryTaxNoOverlap(
        BerynxDbContext db, Guid productId, string category, DateOnly from, DateOnly? to,
        Guid? excluding, CancellationToken ct)
    {
        var rows = db.Set<M03CategoryTaxOverride>().Where(x =>
            x.ProductId == productId && x.CustomerCategory == category);
        if (excluding is { } id) rows = rows.Where(x => x.OverrideId != id);
        if (await rows.AnyAsync(x => (x.EffectiveTo == null || x.EffectiveTo >= from) &&
                (to == null || x.EffectiveFrom <= to), ct))
            throw new ValidationException("Effective-dated overrides cannot overlap for this product/customer category.");
    }

    private static void ValidatePriceList(PriceListWrite request)
    {
        var scopes = (request.CustomerPartyId.HasValue ? 1 : 0) +
                     (!string.IsNullOrWhiteSpace(request.CustomerGroup) ? 1 : 0) +
                     (request.IsDefault ? 1 : 0);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 160 || scopes != 1 ||
            request.ValidTo < request.ValidFrom)
            throw new ValidationException("A price list requires one scope (customer, group or default) and valid effective dates.");
    }

    private static async Task EnsurePriceListNoOverlap(
        BerynxDbContext db, PriceListWrite request, Guid? excluding, CancellationToken ct)
    {
        if (request.CustomerPartyId is { } partyId &&
            !await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == partyId && x.RoleCode == "Customer", ct))
            throw new ValidationException("A customer-specific price list requires a customer party.");
        var lists = db.Set<M03PriceList>().Where(x => x.IsActive);
        if (excluding is { } id) lists = lists.Where(x => x.PriceListId != id);
        lists = request.CustomerPartyId is { } customer
            ? lists.Where(x => x.CustomerPartyId == customer)
            : request.CustomerGroup is { Length: > 0 } group
                ? lists.Where(x => x.CustomerGroup == group)
                : lists.Where(x => x.IsDefault);
        if (await lists.AnyAsync(x => (x.ValidTo == null || x.ValidTo >= request.ValidFrom) &&
                (request.ValidTo == null || x.ValidFrom <= request.ValidTo), ct))
            throw new ValidationException("Price-list validity periods for the same scope cannot overlap.");
    }

    private static void ValidatePriceLine(PriceLineWrite request)
    {
        if (request.Rate < 0 || request.MinimumRate is < 0 ||
            request.MinimumMarginPercent is < 0 or > 100)
            throw new ValidationException("Enter a non-negative rate and minimum rate, and margin from 0 to 100%.");
    }

    private static string NormalizeRegistration(string value)
    {
        var result = value.Trim().ToUpperInvariant().Replace(" ", "");
        if (!System.Text.RegularExpressions.Regex.IsMatch(result, "^[A-Z0-9-]{4,20}$"))
            throw new ValidationException("Enter a valid vehicle registration number.");
        return result;
    }

    private static string NormalizeUnitCode(string value)
    {
        var code = value.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z][A-Z0-9._-]{0,15}$"))
            throw new ValidationException("Unit codes must start with a letter and contain up to 16 safe characters.");
        return code;
    }

    private static (string Gstin, string? Pan, string StateCode) ResolvePartyTaxIdentity(
        string? rawGstin, string? rawPan, string? rawStateCode)
    {
        var gstin = M03MasterService.NormalizeGstin(rawGstin);
        if (string.IsNullOrEmpty(gstin))
            return (gstin, M03MasterService.NormalizePan(rawPan), NormalizeState(rawStateCode));

        var gstinState = gstin[..2];
        var gstinPan = gstin.Substring(2, 10);
        var suppliedPan = M03MasterService.NormalizePan(rawPan);
        if (suppliedPan is not null && suppliedPan != gstinPan)
            throw new ValidationException("The PAN does not match the PAN encoded in the GSTIN.");
        var suppliedState = string.IsNullOrWhiteSpace(rawStateCode)
            ? gstinState
            : NormalizeState(rawStateCode);
        if (suppliedState != gstinState)
            throw new ValidationException("The state code must match the GSTIN prefix.");
        return (gstin, gstinPan, gstinState);
    }

    private static (string Gstin, string StateCode) ResolveStateFromGstin(
        string? rawGstin, string? rawStateCode)
    {
        var gstin = M03MasterService.NormalizeGstin(rawGstin);
        var stateCode = string.IsNullOrWhiteSpace(rawStateCode) && !string.IsNullOrEmpty(gstin)
            ? gstin[..2]
            : NormalizeState(rawStateCode);
        if (!string.IsNullOrEmpty(gstin) && gstin[..2] != stateCode)
            throw new ValidationException("The state code must match the GSTIN prefix.");
        return (gstin, stateCode);
    }

    private static string NormalizeState(string? value)
    {
        var state = value?.Trim() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(state, "^[0-9]{2}$"))
            throw new ValidationException("Use the two-digit GST state code.");
        return state;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Mask(string value) =>
        string.IsNullOrEmpty(value) ? "" : $"••••{value[^Math.Min(4, value.Length)..]}";
}
