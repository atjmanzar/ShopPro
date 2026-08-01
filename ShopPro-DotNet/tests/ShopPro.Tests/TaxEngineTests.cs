using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class TaxEngineTests
    {
        [Fact]
        public void CalculateTax_IntraState_SplitsCgstAndSgstEqually()
        {
            // Arrange & Act (Net Amount ₹1000 @ 18% GST)
            var result = TaxEngine.CalculateTax(1000.00m, 18.00m, isInterState: false);

            // Assert
            Assert.Equal(180.00m, result.TotalTaxAmount);
            Assert.Equal(90.00m, result.CgstAmount); // 9% CGST
            Assert.Equal(90.00m, result.SgstAmount); // 9% SGST
            Assert.Equal(0.00m, result.IgstAmount);
            Assert.False(result.IsInterState);
        }

        [Fact]
        public void CalculateTax_InterState_Allocates100PercentToIgst()
        {
            // Arrange & Act (Net Amount ₹500 @ 12% GST Inter-state)
            var result = TaxEngine.CalculateTax(500.00m, 12.00m, isInterState: true);

            // Assert
            Assert.Equal(60.00m, result.TotalTaxAmount);
            Assert.Equal(0.00m, result.CgstAmount);
            Assert.Equal(0.00m, result.SgstAmount);
            Assert.Equal(60.00m, result.IgstAmount); // 12% IGST
            Assert.True(result.IsInterState);
        }
    }
}
