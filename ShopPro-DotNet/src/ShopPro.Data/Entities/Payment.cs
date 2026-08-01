using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPro.Data.Entities
{
    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        UPI,
        StoreCredit
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public PaymentMethod Method { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceCode { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}
