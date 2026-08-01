namespace ShopPro.Core.Services
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    /// <summary>
    /// Discount Order of Operations & Rules:
    /// 1. Line Item Discount: Applied to pre-tax gross amount (UnitPrice * Quantity).
    ///    - Percentage discount > 100% is clamped to 100%.
    ///    - Fixed discount > Gross Amount is capped at Gross Amount.
    ///    - Net Line Subtotal is floored at 0.00 (never negative).
    /// 2. Tax: Calculated per-line-item on Net Line Subtotal.
    /// 3. Invoice-level Discount: Applied to pre-tax sum of Net Line Subtotals.
    ///    - Percentage > 100% clamped to 100%.
    ///    - Fixed discount > Invoice Subtotal capped at Invoice Subtotal.
    ///    - Net Invoice Subtotal is floored at 0.00 (never negative).
    /// </summary>
    public class DiscountEngine
    {
        public static decimal CalculateDiscount(decimal grossAmount, decimal discountValue, DiscountType type)
        {
            if (grossAmount <= 0 || discountValue <= 0) return 0.0m;

            if (type == DiscountType.Percentage)
            {
                var clampedPct = Math.Clamp(discountValue, 0m, 100m); // Cap percentage at 100%
                return Math.Round(grossAmount * (clampedPct / 100m), 2);
            }
            else
            {
                return Math.Min(discountValue, grossAmount); // Cap fixed discount at gross amount
            }
        }
    }
}
