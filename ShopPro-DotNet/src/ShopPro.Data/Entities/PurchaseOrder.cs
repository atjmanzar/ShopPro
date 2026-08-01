using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public enum PurchaseOrderStatus
    {
        Draft,
        Sent,
        Received, // GRN completed
        Canceled
    }

    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string PoNumber { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReceivedDate { get; set; }

        public List<PurchaseOrderItem> Items { get; set; } = new();
    }

    public class PurchaseOrderItem
    {
        [Key]
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LineTotal => QuantityOrdered * UnitCost;
    }
}
