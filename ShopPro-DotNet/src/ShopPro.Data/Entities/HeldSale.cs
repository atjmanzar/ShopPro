using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public class HeldSale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string HoldReference { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public string CustomerName { get; set; } = "Walk-in Customer";

        /// <summary>
        /// JSON Serialized Cart Items string
        /// </summary>
        [Required]
        public string CartJson { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }

        public DateTime HeldAt { get; set; } = DateTime.UtcNow;
    }
}
