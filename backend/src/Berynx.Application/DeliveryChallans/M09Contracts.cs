namespace Berynx.Application.DeliveryChallans;

public sealed record M09ChallanCorrectionRequest(Guid ChallanId, long Version, decimal CorrectedQuantity, string Reason);

public sealed record M09ChallanListRow(
    Guid ChallanId, string Number, DateOnly ChallanDate, string Status,
    string CustomerName, string OrderNumber, string ProductName,
    decimal Quantity, decimal Value, int AgeDays, string EvidenceState);
