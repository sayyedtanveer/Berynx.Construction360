using System.ComponentModel.DataAnnotations;

namespace Berynx.Domain.IdentityAccess;

public static class M02Rules
{
    public static readonly string[] Actions =
        ["view", "create", "edit", "approve", "cancel", "export", "print", "view_cost"];

    public static void ValidatePermission(string permissionCode)
    {
        var parts = permissionCode.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) ||
            !Actions.Contains(parts[1], StringComparer.Ordinal))
            throw new ValidationException("Choose a permission from the server catalog.");
    }

    public static void ValidateScope(string scopeType, Guid? scopeId)
    {
        if (scopeType is not ("All" or "Branch" or "Plant" or "Own") ||
            (scopeType is "Branch" or "Plant") != scopeId.HasValue)
            throw new ValidationException("Choose All, Branch, Plant or Own scope with a matching scope ID.");
    }

    public static void ValidateInvitation(string? email, string? mobile, string[] roleKeys)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
            throw new ValidationException("Provide an email address or mobile number.");
        if (email is not null &&
            (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw new ValidationException("Enter a valid email address.");
        if (mobile is not null && !System.Text.RegularExpressions.Regex.IsMatch(mobile, @"^\+[1-9][0-9]{7,14}$"))
            throw new ValidationException("Enter a mobile number in international +country-code format.");
        if (roleKeys.Length == 0 || roleKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != roleKeys.Length)
            throw new ValidationException("Assign at least one distinct role.");
    }

    public static bool Matches(M02ApprovalRule rule, decimal amount, decimal? discount,
        decimal? margin, string? creditStatus)
    {
        var configured = rule.AmountGreaterThan.HasValue ||
                         rule.DiscountPercentGreaterThan.HasValue ||
                         rule.MarginPercentBelow.HasValue ||
                         rule.CreditStatus is not null;
        return configured &&
               (!rule.AmountGreaterThan.HasValue || amount > rule.AmountGreaterThan.Value) &&
               (!rule.DiscountPercentGreaterThan.HasValue ||
                    discount > rule.DiscountPercentGreaterThan.Value) &&
               (!rule.MarginPercentBelow.HasValue || margin < rule.MarginPercentBelow.Value) &&
               (rule.CreditStatus is null ||
                    string.Equals(creditStatus, rule.CreditStatus, StringComparison.OrdinalIgnoreCase));
    }

    public static void ValidateDecision(Guid requester, Guid actor, string decision, string? comment)
    {
        if (requester == actor)
            throw new UnauthorizedAccessException("A requester cannot approve or reject their own request.");
        if (decision is not ("Approve" or "Reject"))
            throw new ValidationException("Choose Approve or Reject.");
        if (decision == "Reject" && string.IsNullOrWhiteSpace(comment))
            throw new ValidationException("Provide a reason when rejecting an approval.");
    }

    public static void ValidateDelegatedApproval(bool delegateHasApprovalPermission, bool withinDataScope)
    {
        if (!delegateHasApprovalPermission || !withinDataScope)
            throw new UnauthorizedAccessException(
                "Delegation cannot grant approval permission or data scope the delegate does not already hold.");
    }

    public static bool IsDelegationActive(M02Delegation delegation, DateTimeOffset now) =>
        delegation.RevokedAt is null && delegation.StartsAt <= now && now < delegation.EndsAt;

    public static DateTimeOffset InvitationExpiry(DateTimeOffset sentAt) => sentAt.AddHours(72);
}
