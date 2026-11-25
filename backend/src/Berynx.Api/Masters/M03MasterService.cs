using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Api.Onboarding;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Masters;

public sealed record M03ResolvedPrice(
    Guid PriceListId,
    string Name,
    string Precedence,
    decimal Rate,
    decimal? MinimumRate,
    decimal? MinimumMarginPercent,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool ProposedRateBelowMinimum);

public sealed record M03ResolvedTaxRate(
    Guid ProductId,
    string HsnCode,
    decimal RatePercent,
    string Source,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Reason,
    string SellerGstin,
    string? BuyerGstin,
    string? PlaceOfSupplyStateCode,
    Guid? SourceRateId);

public sealed class M03MasterService(
    BerynxDbContext db,
    TenantAuthorizationService authorization,
    ICurrentTenantContext tenant,
    IHttpContextAccessor http,
    OnboardingStore onboarding)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<M02User> Require(string permission, CancellationToken cancellationToken)
    {
        await authorization.Require("M03", permission, cancellationToken);
        return await authorization.CurrentUser(cancellationToken);
    }

    public async Task RequireTenantWide(
        M02User user,
        string permission,
        CancellationToken cancellationToken)
    {
        if (!await authorization.CanAccess("M03", user.UserId, permission, null, null, null, cancellationToken))
            throw new UnauthorizedAccessException("This master action requires tenant-wide scope.");
    }

    public async Task RequireBranch(
        M02User user,
        string permission,
        Guid? branchId,
        Guid? plantId,
        CancellationToken cancellationToken)
    {
        if (!await authorization.CanAccess("M03", user.UserId, permission, branchId, plantId, null, cancellationToken))
            throw new UnauthorizedAccessException("This record is outside the assigned branch or plant scope.");
    }

    public Task<bool> HasPermission(Guid userId, string permission, CancellationToken cancellationToken) =>
        authorization.HasPermission(userId, permission, cancellationToken);

    public Task<bool> HasTenantWidePermission(
        Guid userId, string permission, CancellationToken cancellationToken) =>
        authorization.CanAccess("M03", userId, permission, null, null, null, cancellationToken);

    public void Audit(M02User user, string action, string entityType, Guid entityId, object? before, object? after,
        string? reason = null)
    {
        db.Add(new M02AuditEvent
        {
            TenantId = tenant.TenantId,
            EventId = Guid.NewGuid(),
            ActorUserId = user.UserId,
            ActorSubject = user.Email ?? user.UserId.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = JsonSerializer.Serialize(before ?? new { }, Json),
            After = JsonSerializer.Serialize(after ?? new { }, Json),
            Reason = reason,
            IpAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Device = http.HttpContext?.Request.Headers.UserAgent.ToString(),
            At = DateTimeOffset.UtcNow
        });
    }

    public static string NormalizeGstin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return SetupRules.Gstin(value);
    }

    public static string? NormalizePan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var pan = value.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
            throw new ValidationException("PAN must use the valid 10-character format.");
        return pan;
    }

    public static string? NormalizeMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var mobile = value.Trim();
        if (!Regex.IsMatch(mobile, @"^\+[1-9][0-9]{7,14}$"))
            throw new ValidationException("Use an international phone number beginning with + and country code.");
        return mobile;
    }

    public static string NormalizeProductAttributes(string category, string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ValidationException("Product category attributes are too large.");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ValidationException("Product category attributes must be a JSON object.");

        category = category.Trim();
        var allowed = category switch
        {
            "RMC" => new[] { "grade", "defaultSlump", "aggregateSize", "cementType", "admixture", "minimumLoadM3" },
            "Steel" => new[] { "brand", "grade", "diameterMm", "lengthM", "kgPerMetre" },
            "Cement" => new[] { "brand", "type", "grade", "bagWeightKg" },
            "Sand" or "Aggregate" => new[] { "materialType", "sizeMm" },
            "Other" => new[] { "specification" },
            _ => throw new ValidationException("Choose a supported product category.")
        };
        var supplied = document.RootElement.EnumerateObject().ToArray();
        if (supplied.Any(property => !allowed.Contains(property.Name, StringComparer.Ordinal)))
            throw new ValidationException($"Attributes for {category} may only include: {string.Join(", ", allowed)}.");
        if (category is "RMC" or "Steel" &&
            (!document.RootElement.TryGetProperty("grade", out var grade) ||
             grade.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(grade.GetString())))
            throw new ValidationException($"{category} products require a grade.");
        if (category == "RMC")
        {
            ValidatePositiveNumber(document.RootElement, "defaultSlump");
            ValidatePositiveNumber(document.RootElement, "aggregateSize");
            ValidatePositiveNumber(document.RootElement, "minimumLoadM3");
        }
        else if (category == "Steel")
        {
            ValidatePositiveNumber(document.RootElement, "diameterMm");
            ValidatePositiveNumber(document.RootElement, "lengthM");
            ValidatePositiveNumber(document.RootElement, "kgPerMetre");
        }
        else if (category == "Cement")
            ValidatePositiveNumber(document.RootElement, "bagWeightKg");
        else if (category is "Sand" or "Aggregate")
            ValidatePositiveNumber(document.RootElement, "sizeMm");
        else if (category == "Other" &&
                 (!document.RootElement.TryGetProperty("specification", out var specification) ||
                  specification.ValueKind != JsonValueKind.String ||
                  string.IsNullOrWhiteSpace(specification.GetString())))
            throw new ValidationException("Other products require a specification.");
        return JsonSerializer.Serialize(document.RootElement, Json);
    }

    private static void ValidatePositiveNumber(JsonElement attributes, string propertyName)
    {
        if (attributes.TryGetProperty(propertyName, out var value) &&
            (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number) || number <= 0))
            throw new ValidationException($"{propertyName} must be a positive decimal.");
    }

    public async Task<M03ResolvedPrice> ResolvePrice(
        Guid customerPartyId,
        Guid productId,
        DateOnly asOf,
        CancellationToken cancellationToken,
        decimal? proposedRate = null,
        string priceBasis = "ExPlant")
    {
        ValidatePriceBasis(priceBasis);
        var customer = await db.Set<M03Party>().SingleOrDefaultAsync(
            x => x.PartyId == customerPartyId && x.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Customer was not found.");
        if (!await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == customerPartyId && x.RoleCode == "Customer", cancellationToken))
            throw new ValidationException("The selected party is not a customer.");

        var lists = await db.Set<M03PriceList>().Where(x =>
                x.IsActive && x.PriceBasis == priceBasis &&
                x.ValidFrom <= asOf && (x.ValidTo == null || x.ValidTo >= asOf))
            .ToListAsync(cancellationToken);
        var selected = lists
            .Where(x => x.CustomerPartyId == customerPartyId || x.PriceListId == customer.DefaultPriceListId)
            .OrderByDescending(x => x.ValidFrom)
            .Select(x => (List: x, Rank: 0))
            .Concat(lists.Where(x => x.CustomerPartyId == null &&
                    x.CustomerGroup == customer.CustomerGroup && x.CustomerGroup != null)
                .OrderByDescending(x => x.ValidFrom).Select(x => (List: x, Rank: 1)))
            .Concat(lists.Where(x => x.IsDefault).OrderByDescending(x => x.ValidFrom)
                .Select(x => (List: x, Rank: 2)))
            .OrderBy(x => x.Rank)
            .ThenByDescending(x => x.List.ValidFrom)
            .FirstOrDefault();

        if (selected.List is null)
            throw new KeyNotFoundException("No valid customer-specific, group, or default price list exists.");

        var line = await db.Set<M03PriceListLine>().SingleOrDefaultAsync(
            x => x.PriceListId == selected.List.PriceListId && x.ProductId == productId,
            cancellationToken);
        if (line is null)
            throw new KeyNotFoundException("No rate exists for this product in the applicable price list.");

        var user = await authorization.CurrentUser(cancellationToken);
        var canViewCost = await authorization.CanAccess(
            "M03", user.UserId, "global.view_cost", null, null, null, cancellationToken);
        return new M03ResolvedPrice(
            selected.List.PriceListId,
            selected.List.Name,
            selected.Rank switch { 0 => "Customer", 1 => "CustomerGroup", _ => "Default" },
            line.Rate,
            canViewCost ? line.MinimumRate : null,
            canViewCost ? line.MinimumMarginPercent : null,
            selected.List.ValidFrom,
            selected.List.ValidTo,
            proposedRate is { } candidate && line.MinimumRate is { } minimum && candidate < minimum);
    }

    public async Task<M03ResolvedTaxRate> ResolveTaxRate(
        Guid productId,
        string? customerCategory,
        DateOnly asOf,
        string? sellerGstin,
        string? buyerGstin,
        string? placeOfSupplyStateCode,
        CancellationToken cancellationToken)
    {
        var seller = NormalizeGstin(sellerGstin);
        var buyer = NormalizeGstin(buyerGstin);
        var placeOfSupply = string.IsNullOrWhiteSpace(placeOfSupplyStateCode)
            ? (string.IsNullOrEmpty(buyer) ? null : buyer[..2])
            : NormalizeStateCode(placeOfSupplyStateCode);
        if (!string.IsNullOrEmpty(buyer) && placeOfSupply is not null && buyer[..2] != placeOfSupply)
            throw new ValidationException("Place of supply must match the buyer GSTIN state prefix.");
        if (seller.Length > 0)
        {
            var setup = OnboardingStore.Decode(await onboarding.Current());
            var registered = setup.Registrations.Concat(setup.Branches.Select(x => x.Gstin))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeGstin).ToHashSet(StringComparer.Ordinal);
            if (!registered.Contains(seller))
                throw new ValidationException("The seller GSTIN is not registered in this tenant's M01 setup.");
        }

        var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
            x => x.ProductId == productId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (!string.IsNullOrWhiteSpace(customerCategory))
        {
            var categoryRate = await db.Set<M03CategoryTaxOverride>().AsNoTracking().SingleOrDefaultAsync(x =>
                x.ProductId == product.ProductId && x.CustomerCategory == customerCategory &&
                x.IsApproved && x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf),
                cancellationToken);
            if (categoryRate is not null)
                return new(product.ProductId, product.HsnCode, categoryRate.RatePercent,
                    "ApprovedCustomerCategoryOverride", categoryRate.EffectiveFrom, categoryRate.EffectiveTo,
                    categoryRate.Reason, seller, string.IsNullOrEmpty(buyer) ? null : buyer, placeOfSupply,
                    categoryRate.OverrideId);
        }
        var rate = await db.Set<M03TaxRate>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.HsnCode == product.HsnCode && x.IsActive && x.IsApproved &&
            x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf), cancellationToken)
            ?? throw new KeyNotFoundException("No effective approved tax rate exists for this HSN.");
        return new(product.ProductId, product.HsnCode, rate.RatePercent, "Hsn",
            rate.EffectiveFrom, rate.EffectiveTo, rate.Reason, seller,
            string.IsNullOrEmpty(buyer) ? null : buyer, placeOfSupply, rate.TaxRateId);
    }

    private static string NormalizeStateCode(string value)
    {
        var code = value.Trim();
        if (!Regex.IsMatch(code, "^[0-9]{2}$"))
            throw new ValidationException("Place of supply must be a two-digit GST state code.");
        return code;
    }

    public async Task<M03RmcRateCard> ResolveRmc(
        Guid productId,
        string grade,
        string slumpBand,
        string admixtureCode,
        string pumpType,
        DateOnly asOf,
        CancellationToken cancellationToken,
        string priceBasis = "ExPlant")
    {
        ValidatePriceBasis(priceBasis);
        var rate = await db.Set<M03RmcRateCard>().SingleOrDefaultAsync(x =>
            x.ProductId == productId && x.Grade == grade && x.SlumpBand == slumpBand &&
            x.AdmixtureCode == admixtureCode && x.PumpType == pumpType && x.PriceBasis == priceBasis && x.IsActive &&
            x.IsApproved && x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf),
            cancellationToken);
        return rate ?? throw new KeyNotFoundException(
            "No approved rate exists for this exact grade, slump band, admixture, and pump type.");
    }

    private static void ValidatePriceBasis(string priceBasis)
    {
        if (priceBasis is not ("ExPlant" or "Delivered" or "Site"))
            throw new ValidationException("Choose ExPlant, Delivered or Site price basis.");
    }
}
