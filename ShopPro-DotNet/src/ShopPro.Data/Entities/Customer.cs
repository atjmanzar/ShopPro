using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public enum MembershipTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Gstin { get; set; } = string.Empty;

        public DateTime? Birthday { get; set; }

        public MembershipTier Tier { get; set; } = MembershipTier.Bronze;

        public int LoyaltyPoints { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CreditBalance { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
