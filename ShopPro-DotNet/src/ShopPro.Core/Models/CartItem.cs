using ShopPro.Data.Entities;

namespace ShopPro.Core.Models
{
    /// <summary>
    /// Line Item Money Calculations & Order of Operations:
    /// 1. EffectivePrice: PriceOverride if specified (>= 0), else UnitPrice.
    /// 2. RawSubtotal: EffectivePrice * Quantity.
    /// 3. Line DiscountAmount: Applied to RawSubtotal. Percentage > 100% is clamped to 100%, fixed discount is capped at RawSubtotal.
    /// 4. NetSubtotal (Pre-Invoice Discount): RawSubtotal - Line DiscountAmount (floored at 0.00).
    /// 5. AllocatedInvoiceDiscount: Proportionally allocated share of invoice-level discount based on LineSubtotal ratio.
    /// 6. FinalTaxableAmount: NetSubtotal - AllocatedInvoiceDiscount (floored at 0.00).
    /// 7. TaxAmount: FinalTaxableAmount * (TaxRate / 100) rounded to 2 decimal places (AwayFromZero). Tax is calculated strictly on the post-discount taxable amount paid by customer under GST.
    /// 8. LineTotal: FinalTaxableAmount + TaxAmount.
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

        /// <summary>
        /// Share of invoice-level discount allocated to this line item
        /// </summary>
        public decimal AllocatedInvoiceDiscount { get; set; } = 0.0m;

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

        public decimal FinalTaxableAmount => Math.Max(0m, NetSubtotal - AllocatedInvoiceDiscount);

        public decimal TaxAmount => Math.Round(FinalTaxableAmount * (TaxRate / 100m), 2, MidpointRounding.AwayFromZero);

        public decimal LineTotal => FinalTaxableAmount + TaxAmount;
    }
}
