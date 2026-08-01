namespace ShopPro.Core.Services
{
    public class TaxBreakdown
    {
        public decimal TotalTaxAmount { get; set; }
        public decimal CgstAmount { get; set; } // Central GST (50% of Total Tax for Intra-state)
        public decimal SgstAmount { get; set; } // State GST (50% of Total Tax for Intra-state)
        public decimal IgstAmount { get; set; } // Integrated GST (100% of Total Tax for Inter-state)
        public bool IsInterState { get; set; }
    }

    /// <summary>
    /// Indian GST Tax Calculation Engine:
    /// - Tax is calculated per line item on the pre-tax net subtotal (EffectivePrice * Quantity - LineDiscount).
    /// - Tax amounts are rounded to 2 decimal places per line item (AwayFromZero).
    /// - Intra-state sale (isInterState = false): Tax is split 50/50 into CGST and SGST.
    /// - Inter-state sale (isInterState = true): Tax is 100% allocated to IGST.
    /// </summary>
    public class TaxEngine
    {
        public static TaxBreakdown CalculateTax(decimal netAmount, decimal taxRatePercent, bool isInterState = false)
        {
            if (netAmount <= 0 || taxRatePercent <= 0)
            {
                return new TaxBreakdown { TotalTaxAmount = 0, CgstAmount = 0, SgstAmount = 0, IgstAmount = 0, IsInterState = isInterState };
            }

            var totalTax = Math.Round(netAmount * (taxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);

            if (isInterState)
            {
                return new TaxBreakdown
                {
                    TotalTaxAmount = totalTax,
                    CgstAmount = 0,
                    SgstAmount = 0,
                    IgstAmount = totalTax,
                    IsInterState = true
                };
            }
            else
            {
                var cgst = Math.Round(totalTax / 2m, 2, MidpointRounding.AwayFromZero);
                var sgst = totalTax - cgst; // Ensure exact penny match: CGST + SGST == TotalTax

                return new TaxBreakdown
                {
                    TotalTaxAmount = totalTax,
                    CgstAmount = cgst,
                    SgstAmount = sgst,
                    IgstAmount = 0,
                    IsInterState = false
                };
            }
        }
    }
}
