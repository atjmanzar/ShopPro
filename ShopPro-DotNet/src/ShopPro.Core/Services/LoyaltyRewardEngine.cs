using ShopPro.Data.Entities;

namespace ShopPro.Core.Services
{
    public class LoyaltyRewardEngine
    {
        public static int CalculatePointsEarned(decimal saleTotal, MembershipTier tier)
        {
            if (saleTotal <= 0) return 0;

            int basePoints = (int)(saleTotal / 100m); // 1 point per ₹100
            double multiplier = tier switch
            {
                MembershipTier.Silver => 1.5,
                MembershipTier.Gold => 2.0,
                MembershipTier.Platinum => 3.0,
                _ => 1.0 // Bronze
            };

            return (int)Math.Round(basePoints * multiplier);
        }

        public static decimal CalculateRedemptionDiscount(int pointsToRedeem, int availablePoints)
        {
            if (pointsToRedeem <= 0 || pointsToRedeem > availablePoints) return 0.00m;

            // 1 Loyalty Point = ₹1.00 Discount
            return (decimal)pointsToRedeem;
        }

        public static MembershipTier EvaluateTierUpgrade(int totalPoints)
        {
            if (totalPoints >= 1000) return MembershipTier.Platinum;
            if (totalPoints >= 500) return MembershipTier.Gold;
            if (totalPoints >= 200) return MembershipTier.Silver;
            return MembershipTier.Bronze;
        }
    }
}
