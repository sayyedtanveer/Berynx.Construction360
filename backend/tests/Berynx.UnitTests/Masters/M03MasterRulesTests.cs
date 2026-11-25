using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Application.Masters;
using Berynx.Api.Masters;
using NUnit.Framework;

namespace Berynx.UnitTests.Masters;

[TestFixture]
public sealed class M03MasterRulesTests
{
    [Test]
    public void NormalizeProductAttributesAcceptsCategorySpecificRmcFields()
    {
        var normalized = M03MasterService.NormalizeProductAttributes(
            "RMC", """{"grade":"M30","defaultSlump":100,"aggregateSize":20}""");

        using var document = JsonDocument.Parse(normalized);
        Assert.That(document.RootElement.GetProperty("grade").GetString(), Is.EqualTo("M30"));
        Assert.That(document.RootElement.GetProperty("defaultSlump").GetInt32(), Is.EqualTo(100));
    }

    [Test]
    public void NormalizeProductAttributesRejectsUnknownCategoryFields()
    {
        Assert.Throws<ValidationException>(() =>
            M03MasterService.NormalizeProductAttributes("Steel", """{"grade":"Fe500","unknown":"x"}"""));
    }

    [Test]
    public void NormalizeProductAttributesRequiresRmcGrade()
    {
        Assert.Throws<ValidationException>(() =>
            M03MasterService.NormalizeProductAttributes("RMC", """{"defaultSlump":100}"""));
    }

    [Test]
    public void NormalizeProductAttributesRejectsNonObjectJson()
    {
        Assert.Throws<ValidationException>(() =>
            M03MasterService.NormalizeProductAttributes("Other", """["not","an","object"]"""));
    }

    [Test]
    public void NormalizeProductAttributesAcceptsSteelAndCementSpecifications()
    {
        var steel = M03MasterService.NormalizeProductAttributes(
            "Steel", """{"brand":"Berynx","grade":"Fe500D","diameterMm":16,"lengthM":12,"kgPerMetre":1.58}""");
        var cement = M03MasterService.NormalizeProductAttributes(
            "Cement", """{"brand":"Berynx","type":"OPC","grade":"53","bagWeightKg":50}""");

        Assert.That(JsonDocument.Parse(steel).RootElement.GetProperty("kgPerMetre").GetDecimal(), Is.EqualTo(1.58m));
        Assert.That(JsonDocument.Parse(cement).RootElement.GetProperty("bagWeightKg").GetInt32(), Is.EqualTo(50));
    }

    [Test]
    public void NormalizeGstinUsesExistingOnboardingChecksumValidation()
    {
        Assert.That(M03MasterService.NormalizeGstin("27aapfu0939f1zv"), Is.EqualTo("27AAPFU0939F1ZV"));
        Assert.Throws<ValidationException>(() => M03MasterService.NormalizeGstin("27AAPFU0939F1ZA"));
    }

    [Test]
    public void NormalizeMobileRequiresInternationalFormat()
    {
        Assert.That(M03MasterService.NormalizeMobile("+919876543210"), Is.EqualTo("+919876543210"));
        Assert.Throws<ValidationException>(() => M03MasterService.NormalizeMobile("9876543210"));
    }

    [Test]
    public void NormalizePanCanonicalizesValidValueAndRejectsInvalidFormat()
    {
        Assert.That(M03MasterService.NormalizePan(" aapfu0939f "), Is.EqualTo("AAPFU0939F"));
        Assert.Throws<ValidationException>(() => M03MasterService.NormalizePan("bad"));
    }

    [Test]
    public void CreditControlBlocksHeldCustomersAndExposesPermissionGatedOverride()
    {
        var decision = M03CreditControl.Evaluate(
            new M03CreditProfile(Guid.NewGuid(), 1000m, 30, "Warn", "OnHold"),
            new M03CreditFacts(false, null, null, null),
            100m,
            hasOverridePermission: true);

        Assert.That(decision.Status, Is.EqualTo("Blocked"));
        Assert.That(decision.CanRequestOverride, Is.False);
        Assert.That(decision.Outstanding, Is.Null);
    }

    [Test]
    public void CreditControlDoesNotInventExposureWhenTransactionFactsAreUnavailable()
    {
        var decision = M03CreditControl.Evaluate(
            new M03CreditProfile(Guid.NewGuid(), 1000m, 30, "HardBlock", "Active"),
            new M03CreditFacts(false, null, null, null, "Accounting module unavailable."),
            250m,
            hasOverridePermission: false);

        Assert.That(decision.Status, Is.EqualTo("Blocked"));
        Assert.That(decision.Outstanding, Is.Null);
        Assert.That(decision.ProjectedExposure, Is.Null);
        Assert.That(decision.CanRequestOverride, Is.False);
        Assert.That(decision.Reasons, Does.Contain("Accounting module unavailable."));
    }

    [Test]
    public void CreditControlEvaluatesLimitAndOverduePolicyAgainstProvidedFacts()
    {
        var decision = M03CreditControl.Evaluate(
            new M03CreditProfile(Guid.NewGuid(), 1000m, 30, "SoftBlock", "Active"),
            new M03CreditFacts(true, 900m, 150m, 45)
            {
                UndeliveredConfirmedOrders = 0m,
                UnallocatedAdvances = 0m
            },
            200m,
            hasOverridePermission: true);

        Assert.That(decision.Status, Is.EqualTo("ApprovalRequired"));
        Assert.That(decision.ProjectedExposure, Is.EqualTo(1100m));
        Assert.That(decision.Reasons, Has.Count.EqualTo(2));
        Assert.That(decision.CanRequestOverride, Is.True);
    }

    [Test]
    public void CreditControlTreatsZeroLimitAsZeroAndRequiresAnAuthorizedApproval()
    {
        var decision = M03CreditControl.Evaluate(
            new M03CreditProfile(Guid.NewGuid(), 0m, 30, "HardBlock", "Active"),
            new M03CreditFacts(true, 0m, 0m, null)
            {
                UndeliveredConfirmedOrders = 0m,
                UnallocatedAdvances = 0m
            },
            1m,
            hasOverridePermission: true);

        Assert.That(decision.Status, Is.EqualTo("Blocked"));
        Assert.That(decision.CanRequestOverride, Is.True);
        Assert.That(decision.ProjectedExposure, Is.EqualTo(1m));
    }

    [Test]
    public void CreditControlRejectsIncompleteFactsClaimedAsAvailable()
    {
        Assert.Throws<ArgumentException>(() => M03CreditControl.Evaluate(
            new M03CreditProfile(Guid.NewGuid(), 100m, 30, "Warn", "Active"),
            new M03CreditFacts(true, null, null, null),
            1m,
            hasOverridePermission: false));
    }

    [Test]
    public async Task CreditEvaluationServiceComposesTenantProfileAndTransactionFacts()
    {
        var customerId = Guid.NewGuid();
        var service = new M03CreditEvaluationService(
            new TestCreditProfileProvider(new M03CreditProfile(customerId, 499m, 30, "HardBlock", "Active")),
            new TestCreditFactsProvider(new M03CreditFacts(true, 100m, 0m, null)
            {
                UndeliveredConfirmedOrders = 250m,
                UnallocatedAdvances = 50m
            }));

        var assessment = await service.Evaluate(customerId, 200m, hasOverridePermission: false, default);

        Assert.That(assessment.Decision.Status, Is.EqualTo("Blocked"));
        Assert.That(assessment.Decision.ProjectedExposure, Is.EqualTo(500m));
    }

    [Test]
    public void TaxSnapshotIsImmutableAndRequiresConsistentGstComponents()
    {
        var snapshot = M03TaxSnapshot.Create(
            Guid.NewGuid(), "6810", "27AAPFU0939F1ZV", null, "27",
            new DateOnly(2026, 11, 24), "ApprovedHsnRate", 1000m, 18m,
            9m, 9m, 0m, Guid.NewGuid());

        Assert.That(snapshot.TotalRatePercent, Is.EqualTo(18m));
        Assert.That(snapshot.SellerGstin, Is.EqualTo("27AAPFU0939F1ZV"));
        Assert.Throws<ArgumentException>(() => M03TaxSnapshot.Create(
            Guid.NewGuid(), "6810", "27AAPFU0939F1ZV", null, "27",
            new DateOnly(2026, 11, 24), "ApprovedHsnRate", 1000m, 18m,
            9m, 0m, 9m, null));
        Assert.Throws<ArgumentException>(() => M03TaxSnapshot.Create(
            Guid.NewGuid(), "6810", "27AAPFU0939F1ZV", "29AAPFU0939F1ZR", "27",
            new DateOnly(2026, 11, 24), "ApprovedHsnRate", 1000m, 18m,
            9m, 9m, 0m, null));
        Assert.Throws<ArgumentException>(() => M03TaxSnapshot.Create(
            Guid.NewGuid(), "6810", "27AAPFU0939F1ZV", null, "29",
            new DateOnly(2026, 11, 24), "ApprovedHsnRate", 1000m, 18m,
            9m, 9m, 0m, null));
    }

    private sealed class TestCreditProfileProvider(M03CreditProfile? profile) : IM03CreditProfileProvider
    {
        public Task<M03CreditProfile?> GetProfile(Guid customerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult(profile);
    }

    private sealed class TestCreditFactsProvider(M03CreditFacts facts) : IM03CreditFactsProvider
    {
        public Task<M03CreditFacts> GetFacts(Guid customerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult(facts);
    }
}
