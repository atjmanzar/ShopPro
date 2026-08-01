using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public enum ShiftStatus
    {
        Open,
        Closed
    }

    public class CashShift
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal OpeningFloat { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalCashSales { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ExpectedCash => OpeningFloat + TotalCashSales;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ClosingCashCount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Variance => ClosingCashCount - ExpectedCash;

        public ShiftStatus Status { get; set; } = ShiftStatus.Open;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
}
