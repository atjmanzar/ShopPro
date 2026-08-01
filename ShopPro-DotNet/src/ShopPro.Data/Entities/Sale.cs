using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public enum SaleStatus
    {
        Completed,
        Voided,
        Returned
    }

    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalDiscount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalTax { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal GrandTotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ChangeDue { get; set; } = 0.00m;

        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        public List<SaleItem> Items { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
    }
}
