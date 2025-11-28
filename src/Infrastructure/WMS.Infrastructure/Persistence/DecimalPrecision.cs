namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Centralized configuration for Entity Framework decimal precision.
/// Standards based on financial and inventory management best practices.
/// </summary>
public static class DecimalPrecision
{
    /// <summary>
    /// Standard for Inventory Quantities and Weights.
    /// Precision: 18, Scale: 6
    /// Range: +/- 999,999,999,999.999999
    /// Usage: MaterialInventory.Quantity, PickTransaction.Weight, etc.
    /// Justification: Allows for high precision in unit conversions (e.g. kg to lbs) and small item counts.
    /// </summary>
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 6;

    /// <summary>
    /// Standard for Unit Costs, Rates, and Prices.
    /// Precision: 18, Scale: 6
    /// Range: +/- 999,999,999,999.999999
    /// Usage: InvoiceLine.UnitRate, Product.CostPrice
    /// Justification: Essential for fractional currency (e.g. $0.0045/unit) to prevent rounding errors in large volume calculations.
    /// </summary>
    public const int UnitCostPrecision = 18;
    public const int UnitCostScale = 6;

    /// <summary>
    /// Standard for Final Monetary Amounts (Invoice Totals, Payments).
    /// Precision: 18, Scale: 2
    /// Range: +/- 9,999,999,999,999,999.99
    /// Usage: Invoice.TotalAmount, Payment.Amount
    /// Justification: Final currency values are typically rounded to 2 decimal places (cents).
    /// </summary>
    public const int MoneyPrecision = 18;
    public const int MoneyScale = 2;
    
    /// <summary>
    /// Standard for Percentages (Tax Rates, Discounts).
    /// Precision: 5, Scale: 4
    /// Range: +/- 9.9999 (0% to 100% represented as 0.0000 to 1.0000 usually, or 0-100)
    /// Usage: TaxRate, DiscountPercent
    /// </summary>
    public const int PercentagePrecision = 10; // Increased to 10 to be safe
    public const int PercentageScale = 4;
}
