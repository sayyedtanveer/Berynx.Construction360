using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Masters;

public sealed class M03Party : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PartyId { get; set; }
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public string PartyType { get; set; } = "Business";
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string? CustomerCategory { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? BillingAddress { get; set; }
    public string? CustomerGroup { get; set; }
    public Guid? SalespersonUserId { get; set; }
    public string TagsJson { get; set; } = "[]";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public Guid? PaymentTermId { get; set; }
    public string CreditPolicy { get; set; } = "Warn";
    public string CreditStatus { get; set; } = "Active";
    public Guid? DefaultPriceListId { get; set; }
    public bool IsMsme { get; set; }
    public string? UdyamNumber { get; set; }
    public string? TdsCategory { get; set; }
    public long Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class M03PartyRole : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PartyId { get; set; }
    public string RoleCode { get; set; } = "";
}

public sealed class M03PartyBankAccount : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid PartyId { get; set; }
    public string AccountHolder { get; set; } = "";
    public string AccountNumberProtected { get; set; } = "";
    public string Ifsc { get; set; } = "";
    public string? BankName { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03PartyMerge : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid MergeId { get; set; }
    public Guid SourcePartyId { get; set; }
    public Guid SurvivorPartyId { get; set; }
    public string Reason { get; set; } = "";
    public string MergedBy { get; set; } = "";
    public DateTimeOffset MergedAt { get; set; }
}

public sealed class M03Site : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SiteId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string StateCode { get; set; } = "";
    public string? Gstin { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? DefaultPlantId { get; set; }
    public string AccessNotes { get; set; } = "";
    public string Status { get; set; } = "Active";
    public long Version { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class M03Contact : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ContactId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? SiteId { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public bool EmailConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool WhatsAppConsent { get; set; }
    public string? ConsentSource { get; set; }
    public DateTimeOffset? ConsentUpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03Plant : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public Guid SupplierPartyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string StateCode { get; set; } = "";
    public decimal? CapacityPerHour { get; set; }
    public string GradesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03Product : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string HsnCode { get; set; } = "";
    public string BaseUnitCode { get; set; } = "";
    public string CategoryAttributesJson { get; set; } = "{}";
    public string QualityReference { get; set; } = "";
    public bool Traceable { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class M03TaxRate : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid TaxRateId { get; set; }
    public string HsnCode { get; set; } = "";
    public decimal RatePercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Reason { get; set; } = "";
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M03CategoryTaxOverride : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid OverrideId { get; set; }
    public Guid ProductId { get; set; }
    public string CustomerCategory { get; set; } = "";
    public decimal RatePercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Reason { get; set; } = "";
    public bool IsApproved { get; set; }
    public string ApprovedBy { get; set; } = "";
    public DateTimeOffset? ApprovedAt { get; set; }
}

public sealed class M03Unit : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string UnitCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03UnitConversion : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ConversionId { get; set; }
    public string FromUnitCode { get; set; } = "";
    public string ToUnitCode { get; set; } = "";
    public decimal ToUnitsPerFromUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03PriceList : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PriceListId { get; set; }
    public string Name { get; set; } = "";
    public Guid? CustomerPartyId { get; set; }
    public string? CustomerGroup { get; set; }
    public bool IsDefault { get; set; }
    public string PriceBasis { get; set; } = "ExPlant";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03PriceListLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PriceListLineId { get; set; }
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Rate { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MinimumMarginPercent { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M03RmcRateCard : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RateCardId { get; set; }
    public Guid ProductId { get; set; }
    public string Grade { get; set; } = "";
    public string SlumpBand { get; set; } = "";
    public string AdmixtureCode { get; set; } = "";
    public string PumpType { get; set; } = "";
    public decimal Rate { get; set; }
    public string PriceBasis { get; set; } = "ExPlant";
    public Guid? AdmixtureChargeTypeId { get; set; }
    public Guid? PumpChargeTypeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03Vehicle : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid VehicleId { get; set; }
    public string RegistrationNumber { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Capacity { get; set; }
    public string CapacityUnitCode { get; set; } = "";
    public string Ownership { get; set; } = "Own";
    public Guid? TransporterPartyId { get; set; }
    public DateOnly? InsuranceExpiresOn { get; set; }
    public DateOnly? PucExpiresOn { get; set; }
    public DateOnly? FitnessExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03Driver : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DriverId { get; set; }
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string LicenceNumber { get; set; } = "";
    public DateOnly LicenceExpiresOn { get; set; }
    public Guid? VehicleId { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03Equipment : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EquipmentId { get; set; }
    public string Name { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string PumpType { get; set; } = "";
    public decimal? Capacity { get; set; }
    public string? CapacityUnitCode { get; set; }
    public Guid? OwnerPartyId { get; set; }
    public DateOnly? InsuranceExpiresOn { get; set; }
    public DateOnly? FitnessExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03PaymentTerm : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PaymentTermId { get; set; }
    public string Name { get; set; } = "";
    public int DueDays { get; set; }
    public string? LedgerCode { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03ChargeType : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChargeTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? LedgerCode { get; set; }
    public string? HsnCode { get; set; }
    public decimal? DefaultSurchargeRate { get; set; }
    public decimal? DefaultTaxRatePercent { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M03ImportBatch : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid BatchId { get; set; }
    public string EntityType { get; set; } = "";
    public string Status { get; set; } = "Preview";
    public string SnapshotJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? UndoneAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M03ImportRowError : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RowErrorId { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string Message { get; set; } = "";
    public string RowJson { get; set; } = "{}";
}
