using ShopPro.Core.Services;
using ShopPro.Data.Entities;
using Xunit;

namespace ShopPro.Tests
{
    public class LoyaltyEngineTests
    {
        [Theory]
        [InlineData(1000.00, MembershipTier.Bronze, 10)]   // 1x multiplier
        [InlineData(1000.00, MembershipTier.Silver, 15)]   // 1.5x multiplier
        [InlineData(1000.00, MembershipTier.Gold, 20)]     // 2.0x multiplier
        [InlineData(1000.00, MembershipTier.Platinum, 30)] // 3.0x multiplier
        public void CalculatePointsEarned_AppliesTierMultipliers(decimal saleTotal, MembershipTier tier, int expectedPoints)
        {
            // Act
            int points = LoyaltyRewardEngine.CalculatePointsEarned(saleTotal, tier);

            // Assert
            Assert.Equal(expectedPoints, points);
        }

        [Fact]
        public void CalculateRedemptionDiscount_RedeemsPoints1To1ForCash()
        {
            // Act
            decimal discount = LoyaltyRewardEngine.CalculateRedemptionDiscount(50, 100);

            // Assert
            Assert.Equal(50.00m, discount);
        }
    }
}
