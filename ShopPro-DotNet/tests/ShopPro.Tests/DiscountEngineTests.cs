using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class DiscountEngineTests
    {
        [Fact]
        public void CalculateDiscount_Percentage_CalculatesCorrectDiscount()
        {
            // Arrange & Act (15% discount on ₹200)
            var discount = DiscountEngine.CalculateDiscount(200.00m, 15.00m, DiscountType.Percentage);

            // Assert
            Assert.Equal(30.00m, discount);
        }

        [Fact]
        public void CalculateDiscount_FixedAmount_CapsAtGrossAmount()
        {
            // Arrange & Act (₹500 fixed discount on ₹300 gross)
            var discount = DiscountEngine.CalculateDiscount(300.00m, 500.00m, DiscountType.FixedAmount);

            // Assert
            Assert.Equal(300.00m, discount); // Capped at gross
        }
    }
}
