using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Promotion;
using Berynx.Application.Tenancy;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Promotion;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Promotion;

public static class M17Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM17(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m17")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M17",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/catalogue", Catalogue);
        api.MapPost("/catalogue", SaveCatalogueItem);
        api.MapGet("/consents", Consents);
        api.MapPost("/consents", SaveConsent);

        app.MapGet("/public/m17/{tenantId:guid}/catalogue", PublicCatalogue)
            .AllowAnonymous()
            .RequireRateLimiting("m17-public");
    }

    private static async Task<IResult> Catalogue(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.CurrentUser(ct);
        await authorization.Require("M17", "m17.catalogue.manage", ct);
        var rows = await (from item in db.Set<M17CatalogueItem>().AsNoTracking()
                          join product in db.Set<M03Product>().AsNoTracking()
                              on new { item.TenantId, item.ProductId } equals new { product.TenantId, product.ProductId }
                          orderby product.Name
                          select new
                          {
                              item.CatalogueItemId, item.ProductId, product.Code, product.Name,
                              product.BaseUnitCode, item.Description, item.ImageReference,
                              item.KeySpecs, item.IndicativePrice, item.IsPublished, item.UpdatedAt
                          }).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> SaveCatalogueItem(
        M17CatalogueItemRequest request, TenantAuthorizationService authorization,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M17", "m17.catalogue.manage", ct);
        var description = request.Description?.Trim() ?? "";
        var imageReference = request.ImageReference?.Trim() ?? "";
        var keySpecs = request.KeySpecs?.Trim() ?? "";
        if (request.ProductId == Guid.Empty || description.Length > 2000 ||
            imageReference.Length > 500 || keySpecs.Length > 2000 ||
            request.IndicativePrice is < 0)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Catalogue entry is invalid.");
        if (!await db.Set<M03Product>().AnyAsync(x => x.ProductId == request.ProductId && x.IsActive, ct))
            return Results.NotFound(new { detail = "Active tenant product was not found." });
        var item = await db.Set<M17CatalogueItem>().SingleOrDefaultAsync(x => x.ProductId == request.ProductId, ct);
        if (item is null)
        {
            item = new M17CatalogueItem
            {
                TenantId = tenant.TenantId, CatalogueItemId = Guid.NewGuid(), ProductId = request.ProductId
            };
            db.Add(item);
        }
        item.Description = description;
        item.ImageReference = imageReference;
        item.KeySpecs = keySpecs;
        item.IndicativePrice = request.IndicativePrice;
        item.IsPublished = request.IsPublished;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = actor.UserId;
        db.Add(new M17PromotionEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
            CatalogueItemId = item.CatalogueItemId, EventType = "CatalogueUpdated",
            DetailJson = JsonSerializer.Serialize(new { item.ProductId, item.IsPublished }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { item.CatalogueItemId, item.ProductId, item.IsPublished });
    }

    private static async Task<IResult> Consents(
        Guid? contactId, TenantAuthorizationService authorization,
        Berynx.Infrastructure.Persistence.BerynxDbContext db, CancellationToken ct)
    {
        await authorization.CurrentUser(ct);
        await authorization.Require("M17", "m17.consent.view", ct);
        var query = db.Set<M17ConsentRecord>().AsNoTracking();
        if (contactId.HasValue) query = query.Where(x => x.ContactId == contactId.Value);
        return Results.Ok(await query.OrderByDescending(x => x.CapturedAt)
            .Select(x => new
            {
                x.ConsentId, x.ContactId, x.Channel, x.IsOptedIn, x.CapturedAt,
                x.Source, x.Wording, x.OptedOutAt, x.OptOutSource
            }).ToListAsync(ct));
    }

    private static async Task<IResult> SaveConsent(
        M17ConsentRequest request, TenantAuthorizationService authorization,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M17", "m17.consent.manage", ct);
        var source = request.Source?.Trim() ?? "";
        var wording = request.Wording?.Trim() ?? "";
        if (request.ContactId == Guid.Empty || string.IsNullOrWhiteSpace(source) ||
            string.IsNullOrWhiteSpace(wording) || source.Length > 120 || wording.Length > 1000)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Consent source and exact wording are required.");
        var channel = M17Rules.RequireChannel(request.Channel);
        if (!await db.Set<M03Contact>().AnyAsync(x => x.ContactId == request.ContactId, ct))
            return Results.NotFound(new { detail = "Contact was not found in this tenant." });
        var record = await db.Set<M17ConsentRecord>().SingleOrDefaultAsync(
            x => x.ContactId == request.ContactId && x.Channel == channel, ct);
        var isNew = record is null;
        record ??= new M17ConsentRecord
        {
            TenantId = tenant.TenantId, ConsentId = Guid.NewGuid(),
            ContactId = request.ContactId, Channel = channel
        };
        var before = new { record.IsOptedIn, record.OptedOutAt, record.Source, record.Wording };
        record.IsOptedIn = request.IsOptedIn;
        record.CapturedAt = DateTimeOffset.UtcNow;
        record.Source = source;
        record.Wording = wording;
        if (request.IsOptedIn)
        {
            record.OptedOutAt = null;
            record.OptOutSource = "";
        }
        else
        {
            record.OptedOutAt = DateTimeOffset.UtcNow;
            record.OptOutSource = source;
        }
        if (isNew) db.Add(record);
        db.Add(new M17PromotionEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
            ConsentId = record.ConsentId, EventType = request.IsOptedIn ? "ConsentCaptured" : "ConsentOptedOut",
            DetailJson = JsonSerializer.Serialize(new { before, record.IsOptedIn, record.OptedOutAt, record.Source, record.Wording }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { record.ConsentId, record.ContactId, record.Channel, record.IsOptedIn, record.CapturedAt, record.OptedOutAt });
    }

    private static async Task<IResult> PublicCatalogue(
        Guid tenantId, BerynxDbContext db, CancellationToken ct)
    {
        var tenant = await db.Set<Tenant>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (tenant is null || tenant.CancelledAt is { } cancelled && cancelled.AddDays(30) <= DateTimeOffset.UtcNow)
            return Results.NotFound();
        var now = DateTimeOffset.UtcNow;
        var subscription = tenant.CurrentSubscriptionId is { } subscriptionId
            ? await db.Set<TenantSubscription>().IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.SubscriptionId == subscriptionId, ct)
            : null;
        var subscriptionStatus = subscription is null
            ? tenant.Status
            : SubscriptionLifecycle.EffectiveStatus(subscription, now);
        if (subscriptionStatus is SubscriptionLifecycle.Suspended or SubscriptionLifecycle.Cancelled)
            return Results.NotFound();
        var entitled = await db.Set<TenantModuleEntitlement>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.ModuleCode == "M17" && x.IsEnabled &&
                           x.EffectiveFrom <= now && (x.EffectiveUntil == null || x.EffectiveUntil > now), ct);
        if (!entitled) return Results.NotFound();
        var items = await (from item in db.Set<M17CatalogueItem>().IgnoreQueryFilters()
                           join product in db.Set<M03Product>().IgnoreQueryFilters()
                               on new { item.TenantId, item.ProductId } equals new { product.TenantId, product.ProductId }
                           where item.TenantId == tenantId && product.TenantId == tenantId &&
                                 item.IsPublished && product.IsActive
                           orderby product.Name
                           select new
                           {
                               product.Code, product.Name, product.Category, product.BaseUnitCode,
                               product.HsnCode, item.Description, item.KeySpecs
                           }).ToListAsync(ct);
        return Results.Ok(new { tenant = tenant.Name, items });
    }
}