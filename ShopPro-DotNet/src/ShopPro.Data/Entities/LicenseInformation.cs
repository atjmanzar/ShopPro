using System.ComponentModel.DataAnnotations;

namespace ShopPro.Data.Entities
{
    public enum LicenseType
    {
        Trial,
        CommercialStandard,
        CommercialEnterprise
    }

    public class LicenseInformation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string RegisteredCustomerName { get; set; } = "Trial Workstation";

        [Required]
        [MaxLength(128)]
        public string HardwareFingerprint { get; set; } = string.Empty;

        public LicenseType Type { get; set; } = LicenseType.Trial;

        public bool IsActivated { get; set; } = false;

        public DateTime TrialStartDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
