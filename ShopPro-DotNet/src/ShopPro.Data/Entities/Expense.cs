using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "General Store Expense";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    }
}
