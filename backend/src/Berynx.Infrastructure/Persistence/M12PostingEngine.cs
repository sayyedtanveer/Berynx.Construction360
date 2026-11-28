using System.Data;
using System.Text.Json;
using Berynx.Application.Accounting;
using Berynx.Application.Tenancy;
using Berynx.Domain.Accounting;
using Berynx.Domain.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;

public sealed class M12PostingEngine(BerynxDbContext db) : IM12PostingEngine
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<M12PostedVoucher> PostAsync(
        M12PostCommand command, CancellationToken cancellationToken)
    {
        if (command.TenantId == Guid.Empty || command.ActorUserId == Guid.Empty ||
            command.BranchId == Guid.Empty || string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length > 100 || command.Lines.Count > 500)
            throw new InvalidOperationException("Voucher identity, branch, idempotency key or line count is invalid.");
        if (command.IsManual && command.VoucherType is not ("Journal" or "Contra"))
            throw new InvalidOperationException("Manual vouchers are limited to Journal and Contra.");
        var (debit, credit) = M12Rules.ValidateBalancedVoucher(command.Lines);
        var existing = await db.Set<M12Voucher>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var existingLines = await db.Set<M12VoucherLine>().AsNoTracking()
                .Where(x => x.VoucherId == existing.VoucherId)
                .Select(x => new
                {
                    x.LedgerCode, x.PartyId, x.BranchId, x.SiteId, x.SalesOrderId,
                    x.TripId, x.Narration, x.Debit, x.Credit
                }).ToListAsync(cancellationToken);
            var requestedLines = command.Lines.Select(x => new
                {
                    LedgerCode = x.LedgerCode.Trim(), x.PartyId,
                    BranchId = (Guid?)(x.BranchId ?? command.BranchId),
                    x.SiteId, x.SalesOrderId, x.TripId, Narration = x.Narration.Trim(), x.Debit, x.Credit
                }).OrderBy(x => x.LedgerCode).ThenBy(x => x.Debit).ThenBy(x => x.Credit)
                .ThenBy(x => x.PartyId).ToArray();
            var priorLines = existingLines.OrderBy(x => x.LedgerCode)
                .ThenBy(x => x.Debit).ThenBy(x => x.Credit).ThenBy(x => x.PartyId).ToArray();
            if (existing.SourceModule != command.SourceModule ||
                existing.SourceEntityId != command.SourceEntityId ||
                existing.BranchId != command.BranchId ||
                existing.VoucherDate != command.VoucherDate ||
                existing.VoucherType != command.VoucherType ||
                existing.ReversalOfVoucherId != command.ReversalOfVoucherId ||
                existing.TotalDebit != debit || existing.TotalCredit != credit ||
                !priorLines.SequenceEqual(requestedLines))
                throw new InvalidOperationException("Idempotency key was already used by a different voucher command.");
            return new M12PostedVoucher(existing.VoucherId, existing.Number, existing.TotalDebit, existing.TotalCredit);
        }

        if (command.ReversalOfVoucherId is { } reversalOfVoucherId)
        {
            var original = await db.Set<M12Voucher>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.VoucherId == reversalOfVoucherId &&
                                           x.Status == "Posted" && x.ReversalOfVoucherId == null,
                    cancellationToken)
                ?? throw new InvalidOperationException("A reversal must reference an unreversed posted voucher.");
            if (original.BranchId != command.BranchId)
                throw new InvalidOperationException("A reversal must use the original voucher branch.");
            if (await db.Set<M12Voucher>().AsNoTracking()
                    .AnyAsync(x => x.ReversalOfVoucherId == reversalOfVoucherId, cancellationToken))
                throw new InvalidOperationException("The original voucher already has a reversal.");
            var originalLines = await db.Set<M12VoucherLine>().AsNoTracking()
                .Where(x => x.VoucherId == reversalOfVoucherId)
                .OrderBy(x => x.VoucherLineId)
                .ToListAsync(cancellationToken);
            var expected = M12Rules.ReversalLines(originalLines)
                .OrderBy(x => x.LedgerCode).ThenBy(x => x.Debit).ThenBy(x => x.Credit)
                .ThenBy(x => x.PartyId).ThenBy(x => x.Narration).ToArray();
            var reversalTotals = M12Rules.ValidateBalancedVoucher(expected);
            var supplied = command.Lines
                .Select(x => x with { LedgerCode = x.LedgerCode.Trim(), Narration = x.Narration.Trim() })
                .OrderBy(x => x.LedgerCode).ThenBy(x => x.Debit).ThenBy(x => x.Credit)
                .ThenBy(x => x.PartyId).ThenBy(x => x.Narration).ToArray();
            if (reversalTotals.Debit != original.TotalDebit ||
                reversalTotals.Credit != original.TotalCredit ||
                !expected.SequenceEqual(supplied))
                throw new InvalidOperationException("A reversal must exactly mirror the original voucher lines.");
        }

        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var tenant = await db.Set<Tenant>()
            .SingleAsync(x => x.TenantId == command.TenantId, cancellationToken);
        var setup = OnboardingStore.Decode(tenant);
        if (!setup.Branches.Any(x => x.Id == command.BranchId))
            throw new InvalidOperationException("Voucher branch is not configured for this tenant.");
        var periodLocked = await db.Set<M12FinancialPeriod>().AsNoTracking()
            .AnyAsync(x => x.IsLocked && x.PeriodStart <= command.VoucherDate &&
                          x.PeriodEnd >= command.VoucherDate, cancellationToken);
        if (periodLocked)
            throw new InvalidOperationException("Posting is blocked because the financial period is locked.");

        var ledgers = setup.Ledgers ?? [];
        foreach (var line in command.Lines)
        {
            var ledger = ledgers.SingleOrDefault(x =>
                x.IsActive && x.Code.Equals(line.LedgerCode, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Ledger code '{line.LedgerCode}' is not configured.");
            if (command.IsManual) M12Rules.RequireManualAccountAllowed(ledger.Group, line.PartyId);
            if (line.PartyId is { } partyId &&
                !await db.Set<Berynx.Domain.Masters.M03Party>().AnyAsync(x => x.PartyId == partyId, cancellationToken))
                throw new InvalidOperationException("Voucher party dimension is outside this tenant.");
            if (line.BranchId is { } lineBranch && !setup.Branches.Any(x => x.Id == lineBranch))
                throw new InvalidOperationException("Voucher line branch dimension is outside this tenant.");
        }

        var voucherId = Guid.NewGuid();
        var year = command.VoucherDate.Year;
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m12_voucher_number_counter
                ("TenantId","BranchId","VoucherType","CalendarYear","NextNumber")
            VALUES ({command.TenantId},{command.BranchId},{command.VoucherType},{year},2)
            ON CONFLICT ("TenantId","BranchId","VoucherType","CalendarYear")
            DO UPDATE SET "NextNumber" = m12_voucher_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(cancellationToken);
        var prefix = command.VoucherType == "Journal" ? "JV" :
            command.VoucherType == "Contra" ? "CV" :
            command.VoucherType == "Expense" ? "EX" : "SV";
        var voucher = new M12Voucher
        {
            TenantId = command.TenantId,
            VoucherId = voucherId,
            BranchId = command.BranchId,
            Number = $"{prefix}-{year}-{sequence:D6}",
            VoucherType = command.VoucherType,
            Status = "Posted",
            VoucherDate = command.VoucherDate,
            SourceModule = command.SourceModule,
            SourceEntityId = command.SourceEntityId,
            IdempotencyKey = command.IdempotencyKey,
            Narration = command.Narration.Trim(),
            TotalDebit = debit,
            TotalCredit = credit,
            ReversalOfVoucherId = command.ReversalOfVoucherId,
            CreatedByUserId = command.ActorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            PostedAt = DateTimeOffset.UtcNow
        };
        db.Add(voucher);
        foreach (var line in command.Lines)
            db.Add(new M12VoucherLine
            {
                TenantId = command.TenantId,
                VoucherLineId = Guid.NewGuid(),
                VoucherId = voucherId,
                LedgerCode = line.LedgerCode.Trim(),
                PartyId = line.PartyId,
                BranchId = line.BranchId ?? command.BranchId,
                SiteId = line.SiteId,
                SalesOrderId = line.SalesOrderId,
                TripId = line.TripId,
                Narration = line.Narration.Trim(),
                Debit = line.Debit,
                Credit = line.Credit
            });
        db.Add(new M12VoucherEvent
        {
            TenantId = command.TenantId,
            EventId = Guid.NewGuid(),
            VoucherId = voucherId,
            ActorUserId = command.ActorUserId,
            EventType = "Posted",
            DetailJson = JsonSerializer.Serialize(new
            {
                command.VoucherType, command.SourceModule, command.SourceEntityId,
                command.IdempotencyKey, command.ReversalOfVoucherId, debit, credit
            }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return new M12PostedVoucher(voucherId, voucher.Number, debit, credit);
    }
}
