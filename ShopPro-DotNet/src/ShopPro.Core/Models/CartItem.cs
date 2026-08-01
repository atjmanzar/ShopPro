using ShopPro.Data.Entities;

namespace ShopPro.Core.Models
{
    /// <summary>
    /// Line Item Money Calculations:
    /// - EffectivePrice: PriceOverride if present, else UnitPrice.
    /// - RawSubtotal: EffectivePrice * Quantity.
    /// - DiscountAmount: Capped at RawSubtotal. Percentage > 100% is clamped to 100%.
    /// - NetSubtotal: RawSubtotal - DiscountAmount (floored at 0.00).
    /// - TaxAmount: NetSubtotal * (TaxRate / 100) rounded to 2 decimal places per line.
    /// - LineTotal: NetSubtotal + TaxAmount.
    /// </summary>
    public class CartItem
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal? PriceOverride { get; set; }
        public string? LineNote { get; set; }
        public decimal DiscountPercentage { get; set; } = 0.0m;
        public decimal FixedDiscount { get; set; } = 0.0m;
        public decimal TaxRate { get; set; } = 18.00m;

        public decimal EffectivePrice => PriceOverride.HasValue && PriceOverride.Value >= 0 ? PriceOverride.Value : UnitPrice;
        public decimal RawSubtotal => EffectivePrice * Quantity;

        public decimal DiscountAmount
        {
            get
            {
                if (DiscountPercentage > 0)
                {
                    var clampedPct = Math.Clamp(DiscountPercentage, 0m, 100m);
                    return Math.Round(RawSubtotal * (clampedPct / 100m), 2, MidpointRounding.AwayFromZero);
                }
                return Math.Min(Math.Max(0m, FixedDiscount), RawSubtotal);
            }
        }

        public decimal NetSubtotal => Math.Max(0m, RawSubtotal - DiscountAmount);
        public decimal TaxAmount => Math.Round(NetSubtotal * (TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
        public decimal LineTotal => NetSubtotal + TaxAmount;
    }
}
