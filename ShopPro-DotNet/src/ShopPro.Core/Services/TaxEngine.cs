namespace ShopPro.Core.Services
{
    public class TaxBreakdown
    {
        public decimal TotalTaxAmount { get; set; }
        public decimal CgstAmount { get; set; } // Central GST (50% of Total Tax)
        public decimal SgstAmount { get; set; } // State GST (50% of Total Tax)
        public decimal IgstAmount { get; set; } // Integrated GST (100% of Total Tax for Inter-state)
        public bool IsInterState { get; set; }
    }

    public class TaxEngine
    {
        public static TaxBreakdown CalculateTax(decimal netAmount, decimal taxRatePercent, bool isInterState = false)
        {
            if (netAmount <= 0 || taxRatePercent <= 0)
            {
                return new TaxBreakdown { TotalTaxAmount = 0, CgstAmount = 0, SgstAmount = 0, IgstAmount = 0, IsInterState = isInterState };
            }

            var totalTax = Math.Round(netAmount * (taxRatePercent / 100m), 2);

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
                var split = Math.Round(totalTax / 2m, 2);
                return new TaxBreakdown
                {
                    TotalTaxAmount = totalTax,
                    CgstAmount = split,
                    SgstAmount = totalTax - split, // Ensure exact penny rounding
                    IgstAmount = 0,
                    IsInterState = false
                };
            }
        }
    }
}
