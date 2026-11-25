using System.ComponentModel.DataAnnotations;
using Berynx.Application.Tenancy;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;

namespace Berynx.IntegrationTests;

public sealed class OnboardingPersistenceTests
{
    private sealed record Scope(Guid TenantId):ICurrentTenantContext;
    private sealed record BranchScope(Guid Allowed):IBranchAccess
    {
        public void RequireBranch(Guid id){if(id!=Allowed)throw new UnauthorizedAccessException();}
    }
    private readonly Scope first=new(Guid.NewGuid());
    private readonly Scope other=new(Guid.NewGuid());
    private readonly Guid branch=Guid.NewGuid();
    private readonly Guid otherBranch=Guid.NewGuid();
    private readonly string schema="m01_test_"+Guid.NewGuid().ToString("N");
    private DbContextOptions<BerynxDbContext> options=null!;
    private string? connection;
    private BerynxDbContext Open(Scope scope)=>new(options,scope);

    [OneTimeSetUp] public async Task Prepare()
    {
        var value=Environment.GetEnvironmentVariable("BERYNX_TEST_POSTGRES");
        if(string.IsNullOrWhiteSpace(value))Assert.Ignore("Set BERYNX_TEST_POSTGRES to a disposable local integration database.");
        var config=new NpgsqlConnectionStringBuilder(value);
        if(config.Host is not ("localhost" or "127.0.0.1" or "::1") || config.Database?.EndsWith("_test")!=true)
            throw new InvalidOperationException("Integration fixture requires a localhost database whose name ends in _test.");
        await using var conn=new NpgsqlConnection(value);await conn.OpenAsync();
        await using var create=new NpgsqlCommand("CREATE SCHEMA "+schema,conn);await create.ExecuteNonQueryAsync();
        connection=value;config.SearchPath=schema;
        options=new DbContextOptionsBuilder<BerynxDbContext>().UseNpgsql(config.ConnectionString).Options;
        await using var db=Open(first);await db.Database.MigrateAsync();
        var setup=new Setup {StartMonth=4,SkipInvite=true,Limits=new(10,50000,100,100),
            Company=new("Test","","AAPFU0939F","Test",null,null,false,false,false,null),
            Registrations=["27AAPFU0939F1ZV"],Branches=[new(branch,"Site","Test","27AAPFU0939F1ZV"),new(otherBranch,"Other site","Test","27AAPFU0939F1ZV")],
            Accounts=[new("Till","Cash",null)],Taxes=[new("Test",0,"unit")],
            Series=SetupRules.Documents.SelectMany(d=>new[]{branch,otherBranch}.Select(id=>new Series(id,d,"","","{FY}/{SEQ:4}"))).ToList()};
        db.Add(new Tenant {TenantId=first.TenantId,Name="Test",Configuration=OnboardingStore.Encode(setup)});
        await db.SaveChangesAsync();
    }
    [OneTimeTearDown] public async Task Cleanup()
    {
        if(connection==null)return;
        await using var conn=new NpgsqlConnection(connection);await conn.OpenAsync();
        await using var drop=new NpgsqlCommand("DROP SCHEMA "+schema+" CASCADE",conn);await drop.ExecuteNonQueryAsync();
    }
    [Test] public async Task ProvisionReservesFirstSeatAndSuspensionStopsChanges(){
        var scope=new Scope(Guid.NewGuid());
        await using var db=Open(scope);
        var store=new OnboardingStore(db,scope);
        var limits=new Limits(2,50000,100,2);
        await store.Create(new(scope.TenantId,"Pilot","Selected","first@example.invalid",["P1-M01"],limits),"platform",new Setup());
        var t=await store.Current();
        Assert.That(OnboardingStore.Decode(t).Invitations.Single().Role,Is.EqualTo("TenantAdmin"));
        var queued=await db.Database.SqlQuery<long>($"SELECT count(*) AS \"Value\" FROM onboarding_invitation WHERE tenant_id={scope.TenantId}").SingleAsync();
        Assert.That(queued,Is.EqualTo(1));
        await store.Mutate(1,"Suspend","platform",(tenant,_)=>SetupRules.Transition(tenant,"Suspended",DateTimeOffset.UtcNow),true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>store.Mutate(2,"Edit","admin",(tenant,setup)=>setup.SkipInvite=true));
        await store.Mutate(2,"Recover","platform",(tenant,_)=>SetupRules.Transition(tenant,"Active",DateTimeOffset.UtcNow),true);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>store.Mutate(1,"Stale","admin",(tenant,setup)=>setup.SkipInvite=true));
        Assert.That(await db.Set<SetupEvent>().CountAsync(),Is.EqualTo(3));
    }
    [Test] public async Task ForeignTenantCannotReadOrWrite(){
        await using var db=Open(other);
        Assert.That(await db.Set<Tenant>().CountAsync(),Is.Zero);
        Assert.That(await db.Set<OpeningItem>().CountAsync(),Is.Zero);
        Assert.That(await db.Set<SetupEvent>().CountAsync(),Is.Zero);
        db.Add(new OpeningItem{TenantId=first.TenantId,Id=Guid.NewGuid(),Kind="Cash",Ledger="Till",Debit=1});
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async()=>await db.SaveChangesAsync());
    }
    [Test] public async Task GoLiveLocksOpeningsAndPostingNumbersSurviveRollbackAndRetries()
    {
        await using var db=Open(first);var store=new OnboardingStore(db,first);
        OpeningItem Cash()=>new(){Kind="Cash",Ledger="Till",Debit=1};
        await store.SaveOpenings(0,[Cash()],"test");
        await Assert.ThrowsAsync<ValidationException>(()=>store.GoLive(1,"test"));
        await store.SaveOpenings(1,[Cash(),new(){Kind="Ledger",Ledger="Opening equity",Credit=1}],"test");
        await store.GoLive(2,"test");await store.GoLive(2,"test");
        await Assert.ThrowsAsync<ValidationException>(()=>store.SaveOpenings(3,[],"test"));
        async Task<string> Post(Guid id,bool commit,Guid? postingBranch=null){
            await using var context=Open(first);await using var tx=await context.Database.BeginTransactionAsync();
            var targetBranch=postingBranch??branch;
            var number=await new PostingNumbers(context,first,new BranchScope(targetBranch)).Assign(id,targetBranch,"Invoice",new(2026,4,1),default);
            if(commit)await tx.CommitAsync();else await tx.RollbackAsync();
            return number.Number;
        }
        var retry=Guid.NewGuid();var rolledBack=await Post(retry,false);
        Assert.That(await Post(retry,true),Is.EqualTo(rolledBack));
        var numbers=await Task.WhenAll(Post(retry,true),Post(Guid.NewGuid(),true),Post(Guid.NewGuid(),true));
        Assert.That(numbers.Order(),Is.EqualTo(new[]{"2627/0001","2627/0002","2627/0003"}));
        Assert.That(await Post(Guid.NewGuid(),true,otherBranch),Is.EqualTo("2627/0001"));
    }
}