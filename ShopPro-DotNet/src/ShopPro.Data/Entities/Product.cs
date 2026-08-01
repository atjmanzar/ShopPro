using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Cost { get; set; }

        /// <summary>
        /// Tax rate percentage (e.g. 18.00 for 18% GST/VAT)
        /// </summary>
        [Column(TypeName = "decimal(5, 2)")]
        public decimal TaxRate { get; set; } = 18.00m;

        public int StockQuantity { get; set; }

        public int MinStockAlert { get; set; } = 5;

        [NotMapped]
        public int ReorderLevel
        {
            get => MinStockAlert;
            set => MinStockAlert = value;
        }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
