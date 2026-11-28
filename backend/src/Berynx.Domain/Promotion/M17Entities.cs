using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Promotion;

public sealed class M17CatalogueItem : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid CatalogueItemId { get; set; }
    public Guid ProductId { get; set; }
    public string Description { get; set; } = "";
    public string ImageReference { get; set; } = "";
    public string KeySpecs { get; set; } = "";
    public decimal? IndicativePrice { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedByUserId { get; set; }
}

public sealed class M17ConsentRecord : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ConsentId { get; set; }
    public Guid ContactId { get; set; }
    public string Channel { get; set; } = "";
    public bool IsOptedIn { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public string Source { get; set; } = "";
    public string Wording { get; set; } = "";
    public DateTimeOffset? OptedOutAt { get; set; }
    public string OptOutSource { get; set; } = "";
}

public sealed class M17PromotionEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? ConsentId { get; set; }
    public Guid? CatalogueItemId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
