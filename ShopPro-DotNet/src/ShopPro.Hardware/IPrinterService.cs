namespace ShopPro.Hardware
{
    public class ReceiptData
    {
        public string StoreName { get; set; } = "ShopPro Retail Store";
        public string AddressLine1 { get; set; } = "123 Main Commercial Street";
        public string AddressLine2 { get; set; } = "City Center, State - 400001";
        public string Gstin { get; set; } = "27AAAAA0000A1Z5";
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public List<ReceiptLineItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }

        // Stage 3 GST Tax Model Breakdown
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal Tax => CgstAmount + SgstAmount + IgstAmount;

        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeDue { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public bool IsRefundOrCredit { get; set; } = false;
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
        /// <summary>
        /// Print a receipt to the specified printer.
        /// printerName must be a non-empty, validated target (spooler name or COM port).
        /// Empty/null printerName returns Success=false (preview-only).
        /// </summary>
        Task<PrintResult> PrintReceiptWithStatusAsync(ReceiptData receipt, string printerName);

        /// <summary>
        /// Convenience wrapper — requires a non-empty printer name.
        /// Returns true only when transport accepted the bytes.
        /// </summary>
        Task<bool> PrintReceiptAsync(ReceiptData receipt, string printerName);

        Task<bool> OpenCashDrawerAsync(string printerNameOrPort);
        bool CheckPrinterAvailability(string printerNameOrPort);
    }
}
