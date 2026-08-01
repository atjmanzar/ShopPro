namespace ShopPro.Core.Services
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public class DiscountEngine
    {
        public static decimal CalculateDiscount(decimal grossAmount, decimal discountValue, DiscountType type)
        {
            if (grossAmount <= 0 || discountValue <= 0) return 0.0m;

            if (type == DiscountType.Percentage)
            {
                var clampedPct = Math.Clamp(discountValue, 0m, 100m);
                return Math.Round(grossAmount * (clampedPct / 100m), 2);
            }
            else
            {
                return Math.Min(discountValue, grossAmount);
            }
        }
    }
}
