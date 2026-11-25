namespace Berynx.Domain.Quotations;

public sealed record M05LineAmounts(
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TaxAmount,
    decimal Total);

public static class M05QuotationCalculator
{
    public static M05LineAmounts CalculateLine(
        decimal quantity,
        decimal rate,
        decimal discountPercent,
        decimal taxRatePercent,
        bool intraState)
    {
        if (quantity <= 0 || decimal.Round(quantity, 3) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive with up to three decimals.");
        if (rate < 0 || decimal.Round(rate, 4) != rate)
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be non-negative with up to four decimals.");
        if (discountPercent is < 0 or > 100 || decimal.Round(discountPercent, 4) != discountPercent)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be between 0 and 100 with up to four decimals.");
        if (taxRatePercent < 0 || decimal.Round(taxRatePercent, 4) != taxRatePercent)
            throw new ArgumentOutOfRangeException(nameof(taxRatePercent), "Tax rate must be non-negative with up to four decimals.");

        var gross = Currency(quantity * rate);
        var discount = Currency(gross * discountPercent / 100m);
        var taxable = gross - discount;
        var cgst = intraState ? Tax(taxable, taxRatePercent / 2m) : 0m;
        var sgst = cgst;
        var igst = intraState ? 0m : Tax(taxable, taxRatePercent);
        var tax = cgst + sgst + igst;
        return new(gross, discount, taxable, cgst, sgst, igst, tax, taxable + tax);
    }

    private static decimal Tax(decimal taxable, decimal ratePercent) =>
        Currency(taxable * ratePercent / 100m);

    private static decimal Currency(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
