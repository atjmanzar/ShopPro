using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class TaxEngineTests
    {
        [Fact]
        public void CalculateTax_IntraState_SplitsCgstAndSgst5050()
        {
            // Hand Calculation:
            // Net Amount: ₹1000.00, Tax Rate: 18%
            // Total Tax: 1000 * 0.18 = ₹180.00
            // CGST (50%): ₹90.00, SGST (50%): ₹90.00, IGST: ₹0.00
            var breakdown = TaxEngine.CalculateTax(1000.00m, 18.00m, isInterState: false);

            Assert.Equal(180.00m, breakdown.TotalTaxAmount);
            Assert.Equal(90.00m, breakdown.CgstAmount);
            Assert.Equal(90.00m, breakdown.SgstAmount);
            Assert.Equal(0.00m, breakdown.IgstAmount);
            Assert.False(breakdown.IsInterState);
        }

        [Fact]
        public void CalculateTax_InterState_Allocates100PercentToIgst()
        {
            // Hand Calculation:
            // Net Amount: ₹1000.00, Tax Rate: 18%
            // Total Tax: 1000 * 0.18 = ₹180.00
            // CGST: ₹0.00, SGST: ₹0.00, IGST (100%): ₹180.00
            var breakdown = TaxEngine.CalculateTax(1000.00m, 18.00m, isInterState: true);

            Assert.Equal(180.00m, breakdown.TotalTaxAmount);
            Assert.Equal(0.00m, breakdown.CgstAmount);
            Assert.Equal(0.00m, breakdown.SgstAmount);
            Assert.Equal(180.00m, breakdown.IgstAmount);
            Assert.True(breakdown.IsInterState);
        }

        [Fact]
        public void CalculateTax_PennyRounding_EnsuresCgstAndSgstSumToTotalTax()
        {
            // Hand Calculation:
            // Net Amount: ₹99.99, Tax Rate: 5%
            // Total Tax: 99.99 * 0.05 = 4.9995 => Rounded to ₹5.00
            // CGST: 5.00 / 2 = ₹2.50, SGST: 5.00 - 2.50 = ₹2.50
            var breakdown = TaxEngine.CalculateTax(99.99m, 5.00m, isInterState: false);

            Assert.Equal(5.00m, breakdown.TotalTaxAmount);
            Assert.Equal(breakdown.TotalTaxAmount, breakdown.CgstAmount + breakdown.SgstAmount);
        }
    }
}
