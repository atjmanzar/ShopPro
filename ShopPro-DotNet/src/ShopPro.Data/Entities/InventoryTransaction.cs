using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public enum TransactionType
    {
        StockIn,
        StockOut,
        Adjustment,
        SaleDeduction,
        ReturnRestock
    }

    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int QuantityChange { get; set; }

        public TransactionType Type { get; set; }

        [Required]
        [MaxLength(250)]
        public string Reason { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
