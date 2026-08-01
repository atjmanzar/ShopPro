using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public class SaleItem
    {
        [Key]
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal TaxRate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LineTotal { get; set; }
    }
}
