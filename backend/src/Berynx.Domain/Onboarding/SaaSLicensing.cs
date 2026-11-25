using System.ComponentModel.DataAnnotations;
using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Onboarding;

public static class SubscriptionLifecycle
{
    public const string Trial = "Trial";
    public const string Active = "Active";
    public const string Grace = "Grace";
    public const string Suspended = "Suspended";
    public const string Cancelled = "Cancelled";

    public static readonly string[] Values = [Trial, Active, Grace, Suspended, Cancelled];

    public static string EffectiveStatus(TenantSubscription subscription, DateTimeOffset now)
    {
        if (subscription.Status is Suspended or Cancelled)
            return subscription.Status;
        if (now < subscription.StartsAt)
            return subscription.Status;
        if (subscription.EndsAt is null || now <= subscription.EndsAt)
            return subscription.Status;
        return subscription.GraceEndsAt is { } graceEnd && now <= graceEnd
            ? Grace
            : Suspended;
    }

    public static void Transition(TenantSubscription subscription, string next)
    {
        if (!Values.Contains(next, StringComparer.Ordinal))
            throw new ValidationException("Choose a supported subscription status.");
        if (subscription.Status == next)
            return;
        if ((subscription.Status, next) is not
            ((Trial, Active or Suspended or Cancelled) or
             (Active, Grace or Suspended or Cancelled) or
             (Grace, Active or Suspended or Cancelled) or
             (Suspended, Active or Cancelled)))
            throw new ValidationException("Invalid subscription lifecycle transition.");
        subscription.Status = next;
    }
}

public sealed class PlatformPlan
{
    public string PlanCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public long MaxUsers { get; set; }
    public long MaxStorageBytes { get; set; }
    public long MaxDocumentsPerMonth { get; set; }
    public long MaxMessagesPerMonth { get; set; }
    public long MaxApiRequestsPerMonth { get; set; }
    public int TrialDays { get; set; }
    public int GracePeriodDays { get; set; }
    public decimal? PriceAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public long Version { get; set; } = 1;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PlanCode) || PlanCode.Length > 48 ||
            PlanCode.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) ||
            string.IsNullOrWhiteSpace(Name) || Name.Length > 120 ||
            Description.Length > 1000 ||
            MaxUsers < 1 || MaxStorageBytes < 1 ||
            MaxDocumentsPerMonth < 1 || MaxMessagesPerMonth < 1 || MaxApiRequestsPerMonth < 1 ||
            TrialDays < 1 || GracePeriodDays < 0 ||
            PriceAmount < 0 || Currency != "INR")
            throw new ValidationException("Check the plan code, name, quotas, trial/grace periods and optional INR price.");
    }
}

public sealed class PlatformModule
{
    public string ModuleCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCore { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class PlatformPlanModule
{
    public string PlanCode { get; set; } = "";
    public string ModuleCode { get; set; } = "";
}

public sealed class TenantSubscription : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string PlanCode { get; set; } = "";
    public long PlanVersion { get; set; }
    public string Status { get; set; } = SubscriptionLifecycle.Trial;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset? GraceEndsAt { get; set; }
    public bool IsCurrent { get; set; } = true;
    public long Version { get; set; } = 1;
    public string TermsSnapshot { get; set; } = "{}";
}

public sealed class TenantModuleEntitlement : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string ModuleCode { get; set; } = "";
    public string Source { get; set; } = "Plan";
    public bool IsEnabled { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveUntil { get; set; }
}

public sealed class SaaSLicenseAuditEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid ActorIdentityId { get; set; }
    public string ActorEmail { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = "";
    public string Before { get; set; } = "{}";
    public string After { get; set; } = "{}";
    public string Origin { get; set; } = "{}";
    public DateTimeOffset At { get; set; }
}

public sealed class TenantApiUsage : ITenantOwned
{
    public Guid TenantId { get; set; }
    public DateOnly MonthStart { get; set; }
    public long RequestCount { get; set; }
}
