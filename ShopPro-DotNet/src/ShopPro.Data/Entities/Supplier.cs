using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Gstin { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PayableBalance { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
