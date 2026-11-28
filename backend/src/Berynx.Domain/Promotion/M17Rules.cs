namespace Berynx.Domain.Promotion;

public static class M17Rules
{
    public static bool CanSendPromotionalMessage(bool optedIn, DateTimeOffset? optedOutAt) =>
        optedIn && optedOutAt is null;

    public static string RequireChannel(string channel)
    {
        var normalized = channel.Trim().ToLowerInvariant();
        if (normalized is not ("whatsapp" or "sms" or "email"))
            throw new InvalidOperationException("Promotion channel must be WhatsApp, SMS or email.");
        return normalized;
    }
}