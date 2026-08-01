using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class DiscountEngineTests
    {
        [Fact]
        public void CalculateDiscount_PercentageDiscount_CalculatesCorrectValue()
        {
            // Hand Calculation:
            // Gross Amount: ₹500.00, Percentage: 10%
            // Discount = 500 * 0.10 = ₹50.00
            var discount = DiscountEngine.CalculateDiscount(500.00m, 10.00m, DiscountType.Percentage);
            Assert.Equal(50.00m, discount);
        }

        [Fact]
        public void CalculateDiscount_PercentageOver100Percent_ClampsAt100Percent()
        {
            // Hand Calculation:
            // Gross Amount: ₹500.00, Percentage: 150% (Exceeds 100%)
            // Clamped Percentage = 100%
            // Discount = 500 * 1.00 = ₹500.00 (Not ₹750.00)
            var discount = DiscountEngine.CalculateDiscount(500.00m, 150.00m, DiscountType.Percentage);
            Assert.Equal(500.00m, discount);
        }

        [Fact]
        public void CalculateDiscount_FixedDiscountExceedingGrossAmount_CapsAtGrossAmount()
        {
            // Hand Calculation:
            // Gross Amount: ₹300.00, Fixed Discount: ₹500.00 (Exceeds Gross Amount)
            // Capped Discount = ₹300.00 (Not ₹500.00)
            var discount = DiscountEngine.CalculateDiscount(300.00m, 500.00m, DiscountType.FixedAmount);
            Assert.Equal(300.00m, discount);
        }
    }
}
