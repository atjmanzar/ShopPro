namespace ShopPro.Hardware
{
    public class ReceiptData
    {
        public string StoreName { get; set; } = "ShopPro Retail Store";
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public List<ReceiptLineItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeDue { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class ReceiptLineItem
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public interface IPrinterService
    {
        Task<bool> PrintReceiptAsync(ReceiptData receipt);
        Task<bool> OpenCashDrawerAsync();
        Task<bool> TestPrinterConnectionAsync(string printerNameOrPort);
    }
}
