namespace Berynx.Application.Promotion;

public sealed record M17CatalogueItemRequest(
    Guid ProductId, string Description, string ImageReference, string KeySpecs,
    decimal? IndicativePrice, bool IsPublished);

public sealed record M17ConsentRequest(
    Guid ContactId, string Channel, bool IsOptedIn, string Source, string Wording);