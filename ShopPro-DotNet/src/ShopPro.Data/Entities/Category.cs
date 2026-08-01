using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
