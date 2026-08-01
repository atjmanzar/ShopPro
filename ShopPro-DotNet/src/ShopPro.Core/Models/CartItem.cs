using ShopPro.Data.Entities;

namespace ShopPro.Core.Models
{
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

        public decimal EffectivePrice => PriceOverride ?? UnitPrice;
        public decimal RawSubtotal => EffectivePrice * Quantity;

        public decimal DiscountAmount
        {
            get
            {
                if (DiscountPercentage > 0)
                    return Math.Round(RawSubtotal * (DiscountPercentage / 100m), 2);
                return Math.Min(FixedDiscount, RawSubtotal);
            }
        }

        public decimal NetSubtotal => Math.Max(0m, RawSubtotal - DiscountAmount);
        public decimal TaxAmount => Math.Round(NetSubtotal * (TaxRate / 100m), 2);
        public decimal LineTotal => NetSubtotal + TaxAmount;
    }
}
