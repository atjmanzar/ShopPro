using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public enum UserRole
    {
        Admin,
        Cashier,
        Manager
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Cashier;

        // Granular Permissions
        public bool CanManageProducts { get; set; } = true;
        public bool CanGiveDiscount { get; set; } = true;
        public bool CanViewReports { get; set; } = false;
        public bool CanVoidSale { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
