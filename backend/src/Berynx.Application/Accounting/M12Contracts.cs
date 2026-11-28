using Berynx.Domain.Accounting;

namespace Berynx.Application.Accounting;

public sealed record M12PostVoucherRequest(
    string VoucherType, Guid BranchId, DateOnly VoucherDate, string Narration,
    string IdempotencyKey, IReadOnlyList<M12LineDraft> Lines);

public sealed record M12PostCommand(
    Guid TenantId, Guid ActorUserId, Guid BranchId, DateOnly VoucherDate,
    string VoucherType, string SourceModule, Guid? SourceEntityId,
    string IdempotencyKey, string Narration, IReadOnlyList<M12LineDraft> Lines,
    bool IsManual, Guid? ReversalOfVoucherId = null);

public sealed record M12PostedVoucher(Guid VoucherId, string Number, decimal TotalDebit, decimal TotalCredit);

public interface IM12PostingEngine
{
    Task<M12PostedVoucher> PostAsync(M12PostCommand command, CancellationToken cancellationToken);
}
