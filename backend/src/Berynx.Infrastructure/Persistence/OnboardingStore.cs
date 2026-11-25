using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Application.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;
public sealed class OnboardingStore(BerynxDbContext db, ICurrentTenantContext scope)
{
    public string Origin { private get; set; } = "{}";

    public static string Encode(object data) => JsonSerializer.Serialize(data, Workflow.Json);
    public static Setup Decode(Tenant tenant) => JsonSerializer.Deserialize<Setup>(tenant.Configuration, Workflow.Json)!;
    public async Task<Tenant> Current() => await db.Set<Tenant>().SingleOrDefaultAsync(t => t.TenantId == scope.TenantId) ?? throw new UnauthorizedAccessException();
    private static long Bytes(object value) => System.Text.Encoding.UTF8.GetByteCount(Encode(value));
    private static long OpeningBytes(OpeningItem row) => Bytes(new { row.Kind, row.Ledger, row.Bill, row.Debit, row.Credit });
    private async Task<long> OpeningStorage() => (await db.Set<OpeningItem>().AsNoTracking().ToListAsync()).Sum(OpeningBytes);
    public async Task<long> Storage(Setup setup) => Bytes(setup) + await OpeningStorage();
    public async Task<object> View()
    {
        var t = await Current();
        var s = Decode(t);
        var rows = await db.Set<OpeningItem>().AsNoTracking().OrderBy(x => x.Ledger).ThenBy(x => x.Id).ToListAsync();
        var difference = rows.Sum(x => x.Debit - x.Credit);
        return new
        {
            t.Name,
            t.Plan,
            t.Status,
            t.Version,
            t.LiveAt,
            setup = s,
            rows,
            difference,
            progress = (s.Steps.Count + (t.LiveAt == null ? 0 : 1)) * 100 / 8,
            registrations = s.Registrations.Select(g => new { gstin = g, state = g[..2], pan = g.Substring(2, 10) }),
            checks = Workflow.Checklist(s, difference)
        };
    }

    public async Task Mutate(long version, string action, string actor, Action<Tenant, Setup> change, bool platform = false, long? openingBytes = null)
    {
        var t = await Current();
        if (!platform && t.Status is "Cancelled" or "Suspended")
            throw new UnauthorizedAccessException();
        if (t.Version != version)
            throw new DbUpdateConcurrencyException();
        var before = Encode(new { t.Status, t.LiveAt, setup = Decode(t) });
        var setup = Decode(t);
        change(t, setup);
        if (t.LiveAt != null && !platform && Workflow.Checklist(setup, 0).Any(check => !check.Passed))
            throw new ValidationException("Live companies must keep all mandatory setup in place.");
        var configuration = Encode(setup);
        if (!platform)
        {
            var bytes = Bytes(setup) + (openingBytes ?? await OpeningStorage());
            SetupRules.Capacity(0, bytes, setup.Limits.Storage);
        }

        t.Configuration = configuration;
        t.Version++;
        Audit(t, action, actor, before, Encode(new { t.Status, t.LiveAt, setup }));
        await db.SaveChangesAsync();
    }

    public void Audit(Tenant t, string action, string actor, string before, string after) => db.Add(new SetupEvent { TenantId = t.TenantId, Id = Guid.NewGuid(), Actor = actor, Action = action, Origin = Origin, Before = before, After = after, At = DateTimeOffset.UtcNow });
    public async Task Create(Provision r, string actor, Setup defaults)
    {
        Workflow.Require(r.RequestId != Guid.Empty && !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.Plan) && r.Modules.Length > 0 && r.Limits.Users >= 1 && r.Limits.Storage >= 0 && r.Limits.Messages >= 1 && r.Limits.Documents >= 0 && System.Net.Mail.MailAddress.TryCreate(r.AdminEmail, out var email) && email.Address == r.AdminEmail, "Check tenant name, admin email, plan, limits and enabled modules.");
        defaults.Limits = r.Limits;
        defaults.Modules = r.Modules;
        defaults.Invitations = [new(r.AdminEmail, "TenantAdmin", DateTimeOffset.UtcNow)];
        var t = new Tenant
        {
            TenantId = scope.TenantId,
            Name = r.Name,
            Plan = r.Plan,
            Version = 1,
            Configuration = Encode(defaults)
        };
        SetupRules.Capacity(0, System.Text.Encoding.UTF8.GetByteCount(t.Configuration), r.Limits.Storage);
        db.Add(t);
        Audit(t, "Provision", actor, "{}", Encode(r));
        await db.SaveChangesAsync();
    }

    public async Task SaveOpenings(long version, OpeningItem[] rows, string actor)
    {
        foreach (var row in rows)
        {
            row.Validate();
            row.TenantId = scope.TenantId;
            row.Id = Guid.NewGuid();
        }

        var existing = await db.Set<OpeningItem>().ToListAsync();
        await Mutate(version, "Opening balances", actor, (t, s) =>
        {
            if (t.LiveAt != null)
                throw new ValidationException("Opening balances are locked. An authorized adjustment voucher is required.");
            Audit(t, "Opening detail", actor, Encode(existing), Encode(rows));
            db.RemoveRange(existing);
            db.AddRange(rows);
            s.Steps.Add(7);
        }, openingBytes: rows.Sum(OpeningBytes));
    }

    public async Task GoLive(long version, string actor)
    {
        var t = await Current();
        if (t.LiveAt != null)
            return;
        var difference = await db.Set<OpeningItem>().SumAsync(x => x.Debit - x.Credit);
        try
        {
            await Mutate(version, "Go live", actor, (tenant, setup) =>
            {
                if (Workflow.Checklist(setup, difference).Any(c => !c.Passed))
                    throw new ValidationException("Complete all mandatory checklist items.");
                tenant.LiveAt = DateTimeOffset.UtcNow;
                setup.Steps.UnionWith(Enumerable.Range(1, 7));
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            if ((await Current()).LiveAt == null)
                throw;
        }
    }
}
