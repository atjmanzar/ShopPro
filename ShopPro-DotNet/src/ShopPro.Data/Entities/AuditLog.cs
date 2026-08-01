using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string TargetEntity { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
