using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public class HardwareSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        public string SettingValue { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
