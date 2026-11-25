using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Application.Onboarding;
using Berynx.Domain.Onboarding;
using NUnit.Framework;
namespace Berynx.UnitTests.Onboarding;
public class WorkflowTests
{
    [Test] public void ProvisioningDefaultsHaveSeparateAccountingPermission(){
        var s=new Setup();Assert.That(s.Roles["Accountant"],Does.Not.Contain("onboarding.manage"));
        Assert.That(s.Ledgers.Select(l=>l.Code),Does.Contain("opening-equity"));
    }
    [Test] public void SavedOptionalSkipSurvivesSerialization(){
        var s=new Setup();Workflow.Save(s,6,JsonSerializer.SerializeToElement(new Invite(null,true)));
        var resumed=JsonSerializer.Deserialize<Setup>(JsonSerializer.Serialize(s))!;
        Assert.That(resumed.SkipInvite);Assert.That(resumed.Steps,Does.Contain(6));
    }
    [TestCase("27AAPFU0939F1ZV",true)]
    [TestCase("27AAPFU0939F1ZA",false)]
    [TestCase("invalid",false)]
    public void ValidatesRegistration(string value,bool valid){
        if(valid)Assert.That(SetupRules.Gstin(value.ToLowerInvariant()),Is.EqualTo(value));
        else Assert.Throws<ValidationException>(()=>SetupRules.Gstin(value));
    }
    [Test] public void DuplicateRegistrationRejected(){
        var s=new Setup {Company=new("Legal","Trade","AAPFU0939F","Address",null,null,false,false,false,null)};
        var g="27AAPFU0939F1ZV";
        Assert.Throws<ValidationException>(()=>Workflow.Save(s,2,JsonSerializer.SerializeToElement(new Locations([g,g],[],[]))));
    }
    [Test] public void FiscalYearChangesAtConfiguredMonth(){
        var series=new Series(Guid.NewGuid(),"Invoice","I","B","{PREFIX}/{FY}/{SEQ:4}");
        Assert.That(SetupRules.Number(series,4,new(2026,3,31),2),Is.EqualTo("I/2526/0002"));
        Assert.That(SetupRules.Number(series,4,new(2026,4,1),1),Is.EqualTo("I/2627/0001"));
        Assert.Throws<ValidationException>(()=>SetupRules.Number(series with {Prefix=new string('A',15)},4,new(2026,4,1),1));
    }
    [Test] public void OutstandingRequiresBillAndExactMoney(){
        var item=new OpeningItem{Kind="Customer",Ledger="A",Debit=10};
        Assert.Throws<ValidationException>(item.Validate);
        item.Bill="Opening 1";item.Validate();item.Debit=0.001m;
        Assert.Throws<ValidationException>(item.Validate);
    }
    [Test] public void EmptySetupCannotGoLiveAndDifferenceBlocks(){
        var checks=Workflow.Checklist(new Setup(),0.01m);
        Assert.That(checks.All(c=>!c.Passed));Assert.That(checks.All(c=>c.Route.Length>0));
    }
    [Test] public void CancellationIsTerminalAndPreservesRetentionStart(){
        var t=new Tenant();var now=DateTimeOffset.UtcNow;
        SetupRules.Transition(t,"Suspended",now);SetupRules.Transition(t,"Active",now);
        SetupRules.Transition(t,"Cancelled",now);SetupRules.Transition(t,"Cancelled",now.AddDays(1));
        Assert.That(t.CancelledAt,Is.EqualTo(now));Assert.Throws<ValidationException>(()=>SetupRules.Transition(t,"Active",now));
    }
    [Test] public void UsageThresholdsAreExact(){
        Assert.That(SetupRules.Warning(79,100),Is.False);Assert.That(SetupRules.Warning(80,100));
        SetupRules.Capacity(99,1,100);Assert.Throws<ValidationException>(()=>SetupRules.Capacity(100,1,100));
    }
}
