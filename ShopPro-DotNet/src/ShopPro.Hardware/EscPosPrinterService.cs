using System.Text;

namespace ShopPro.Hardware
{
    public enum PaperWidth
    {
        mm80 = 48, // 48 Columns for 80mm thermal paper
        mm58 = 32  // 32 Columns for 58mm thermal paper
    }

    public class ReceiptHeaderConfig
    {
        public string StoreName { get; set; } = "ShopPro Retail Store";
        public string AddressLine1 { get; set; } = "123 Main Commercial Street";
        public string AddressLine2 { get; set; } = "City Center, State - 400001";
        public string Gstin { get; set; } = "27AAAAA0000A1Z5";
        public string FooterMessage { get; set; } = "Thank you for shopping with ShopPro!";
        public PaperWidth PaperWidth { get; set; } = PaperWidth.mm80;
    }

    public class PrintResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OutputPath { get; set; }
    }

    /// <summary>
    /// ESC/POS Thermal Receipt Printing Engine.
    ///
    /// Constructs binary ESC/POS control streams and transmits them via IPrinterTransport.
    /// All text fields are sanitised through ReceiptSanitizer before encoding.
    /// Currency uses "Rs." (pure ASCII) to avoid code-page substitution.
    /// Tax breakdown prints CGST/SGST or IGST matching Stage 3 GST model.
    ///
    /// Transport dispatch uses Task.Run to avoid blocking the WPF UI thread.
    /// Spooler acceptance means bytes were handed to Windows; physical paper is unverified.
    /// </summary>
    public class EscPosPrinterService : IPrinterService
    {
        private readonly IPrinterTransport _transport;

        public ReceiptHeaderConfig Config { get; set; } = new();

        public EscPosPrinterService(IPrinterTransport? transport = null)
        {
            _transport = transport ?? new WindowsSpoolerAndSerialTransport();
        }

        public async Task<PrintResult> PrintReceiptWithStatusAsync(ReceiptData receipt, string printerName)
        {
            if (receipt == null)
                return new PrintResult { Success = false, Message = "Receipt data cannot be null." };

            // Financial validation: reject negative amounts on standard sale receipts
            if (!receipt.IsRefundOrCredit && (receipt.Subtotal < 0 || receipt.Total < 0 || receipt.AmountPaid < 0))
            {
                return new PrintResult { Success = false, Message = "Invalid negative amounts on standard sale receipt." };
            }

            var targetPrinter = string.IsNullOrWhiteSpace(printerName) ? "" : printerName.Trim();

            // Build binary ESC/POS byte stream and text preview
            byte[] escPosBytes = BuildEscPosByteStream(receipt, Config);
            string formattedText = FormatEscPosText(receipt, Config);

            // Save preview file (always attempted, independent of physical print)
            string? previewPath = null;
            try
            {
                string safeInvoice = ReceiptSanitizer.SanitizeFilename(receipt.InvoiceNumber);
                previewPath = Path.Combine(Path.GetTempPath(), $"Receipt_{safeInvoice}.txt");
                await File.WriteAllTextAsync(previewPath, formattedText);
            }
            catch
            {
                previewPath = null;
            }

            // No printer configured → explicit failure with preview path
            if (string.IsNullOrWhiteSpace(targetPrinter))
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"No printer configured or selected — preview saved to file: '{previewPath}'.",
                    OutputPath = previewPath
                };
            }

            // Check printer availability
            bool available = _transport.CheckAvailability(targetPrinter);
            if (!available)
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"Printer '{targetPrinter}' not found — check connection and retry. Preview saved.",
                    OutputPath = previewPath
                };
            }

            // Dispatch blocking spooler/serial I/O off WPF UI thread
            return await Task.Run(() =>
            {
                var result = _transport.SendBytes(targetPrinter, escPosBytes);
                return new PrintResult
                {
                    Success = result.Success,
                    Message = result.Message,
                    OutputPath = previewPath
                };
            });
        }

        /// <summary>
        /// Convenience wrapper. Requires a non-empty, validated printer target.
        /// Returns true only when transport accepted bytes.
        /// </summary>
        public async Task<bool> PrintReceiptAsync(ReceiptData receipt, string printerName)
        {
            var result = await PrintReceiptWithStatusAsync(receipt, printerName);
            return result.Success;
        }

        public async Task<bool> OpenCashDrawerAsync(string printerNameOrPort)
        {
            // ESC p m t1 t2: Pin 2 RJ11 pulse, 25ms ON / 250ms OFF
            byte[] drawerPulseBytes = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };

            var target = string.IsNullOrWhiteSpace(printerNameOrPort) ? "" : printerNameOrPort.Trim();
            if (string.IsNullOrWhiteSpace(target) || !_transport.CheckAvailability(target))
            {
                return false;
            }

            return await Task.Run(() =>
            {
                var result = _transport.SendBytes(target, drawerPulseBytes);
                return result.Success;
            });
        }

        public bool CheckPrinterAvailability(string printerNameOrPort)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort)) return false;
            return _transport.CheckAvailability(printerNameOrPort.Trim());
        }

        public byte[] BuildEscPosByteStream(ReceiptData receipt, ReceiptHeaderConfig config)
        {
            int cols = (int)config.PaperWidth;
            var bytes = new List<byte>();

            // ESC @ : Initialize Printer
            bytes.AddRange(new byte[] { 0x1B, 0x40 });

            // ESC a 1 : Center Align Header
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });

            // ESC E 1 : Enable Bold
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(ReceiptSanitizer.SanitizeLineText(config.StoreName, cols) + "\n"));
            // ESC E 0 : Disable Bold
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            bytes.AddRange(Encoding.ASCII.GetBytes(ReceiptSanitizer.SanitizeLineText(config.AddressLine1, cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(ReceiptSanitizer.SanitizeLineText(config.AddressLine2, cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"GSTIN: {ReceiptSanitizer.SanitizeLineText(config.Gstin, cols)}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('=', cols) + "\n"));

            // ESC a 0 : Left Align Body
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
            bytes.AddRange(Encoding.ASCII.GetBytes($"Invoice #: {ReceiptSanitizer.SanitizeLineText(receipt.InvoiceNumber, cols)}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Date: {receipt.TransactionDate:yyyy-MM-dd HH:mm}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Cashier: {ReceiptSanitizer.SanitizeLineText(receipt.CashierName, cols)}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Payment: {ReceiptSanitizer.SanitizeLineText(receipt.PaymentMethod, cols)}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('-', cols) + "\n"));

            foreach (var item in receipt.Items)
            {
                var cleanName = ReceiptSanitizer.SanitizeLineText(item.ItemName, cols - 12);
                var line = $"{item.Quantity}x {cleanName}";
                var priceStr = $"Rs. {item.LineTotal:F2}";
                int pad = cols - line.Length - priceStr.Length;
                if (pad < 1) pad = 1;
                bytes.AddRange(Encoding.ASCII.GetBytes($"{line}{new string(' ', pad)}{priceStr}\n"));
            }

            bytes.AddRange(Encoding.ASCII.GetBytes(new string('-', cols) + "\n"));

            // Totals with Stage 3 GST breakdown
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Subtotal:", $"Rs. {receipt.Subtotal:F2}", cols) + "\n"));
            if (receipt.Discount > 0)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Discount:", $"Rs. {receipt.Discount:F2}", cols) + "\n"));
            }

            if (receipt.IgstAmount > 0)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("IGST Tax:", $"Rs. {receipt.IgstAmount:F2}", cols) + "\n"));
            }
            else
            {
                if (receipt.CgstAmount > 0)
                    bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("CGST Tax:", $"Rs. {receipt.CgstAmount:F2}", cols) + "\n"));
                if (receipt.SgstAmount > 0)
                    bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("SGST Tax:", $"Rs. {receipt.SgstAmount:F2}", cols) + "\n"));
            }

            // ESC E 1 : Bold grand total
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("GRAND TOTAL:", $"Rs. {receipt.Total:F2}", cols) + "\n"));
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Amount Paid:", $"Rs. {receipt.AmountPaid:F2}", cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Change Due:", $"Rs. {receipt.ChangeDue:F2}", cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('=', cols) + "\n"));

            // ESC a 1 : Center footer
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(ReceiptSanitizer.SanitizeLineText(config.FooterMessage, cols) + "\n"));

            // Feed 3 lines + GS V 66 0 : Partial Paper Cut
            bytes.AddRange(Encoding.ASCII.GetBytes("\n\n\n"));
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

            return bytes.ToArray();
        }

        public string FormatEscPosText(ReceiptData receipt, ReceiptHeaderConfig config)
        {
            int cols = (int)config.PaperWidth;
            var sb = new StringBuilder();

            sb.AppendLine(CenterText(ReceiptSanitizer.SanitizeLineText(config.StoreName, cols), cols));
            sb.AppendLine(CenterText(ReceiptSanitizer.SanitizeLineText(config.AddressLine1, cols), cols));
            sb.AppendLine(CenterText(ReceiptSanitizer.SanitizeLineText(config.AddressLine2, cols), cols));
            sb.AppendLine(CenterText($"GSTIN: {ReceiptSanitizer.SanitizeLineText(config.Gstin, cols)}", cols));
            sb.AppendLine(new string('=', cols));

            sb.AppendLine($"Invoice #: {ReceiptSanitizer.SanitizeLineText(receipt.InvoiceNumber, cols)}");
            sb.AppendLine($"Date: {receipt.TransactionDate:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Cashier: {ReceiptSanitizer.SanitizeLineText(receipt.CashierName, cols)}");
            sb.AppendLine($"Payment: {ReceiptSanitizer.SanitizeLineText(receipt.PaymentMethod, cols)}");
            sb.AppendLine(new string('-', cols));

            foreach (var item in receipt.Items)
            {
                var cleanName = ReceiptSanitizer.SanitizeLineText(item.ItemName, cols - 12);
                var line = $"{item.Quantity}x {cleanName}";
                var priceStr = $"Rs. {item.LineTotal:F2}";
                var padding = cols - line.Length - priceStr.Length;
                if (padding < 1) padding = 1;
                sb.AppendLine($"{line}{new string(' ', padding)}{priceStr}");
            }

            sb.AppendLine(new string('-', cols));
            sb.AppendLine(FormatPair("Subtotal:", $"Rs. {receipt.Subtotal:F2}", cols));
            if (receipt.Discount > 0) sb.AppendLine(FormatPair("Discount:", $"Rs. {receipt.Discount:F2}", cols));
            if (receipt.IgstAmount > 0)
            {
                sb.AppendLine(FormatPair("IGST Tax:", $"Rs. {receipt.IgstAmount:F2}", cols));
            }
            else
            {
                if (receipt.CgstAmount > 0) sb.AppendLine(FormatPair("CGST Tax:", $"Rs. {receipt.CgstAmount:F2}", cols));
                if (receipt.SgstAmount > 0) sb.AppendLine(FormatPair("SGST Tax:", $"Rs. {receipt.SgstAmount:F2}", cols));
            }
            sb.AppendLine(FormatPair("GRAND TOTAL:", $"Rs. {receipt.Total:F2}", cols));
            sb.AppendLine(FormatPair("Amount Paid:", $"Rs. {receipt.AmountPaid:F2}", cols));
            sb.AppendLine(FormatPair("Change Due:", $"Rs. {receipt.ChangeDue:F2}", cols));
            sb.AppendLine(new string('=', cols));

            sb.AppendLine(CenterText(ReceiptSanitizer.SanitizeLineText(config.FooterMessage, cols), cols));
            return sb.ToString();
        }

        private string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length >= width) return text.Substring(0, width);
            int leftPad = (width - text.Length) / 2;
            return new string(' ', leftPad) + text;
        }

        private string FormatPair(string label, string val, int width)
        {
            int pad = width - label.Length - val.Length;
            if (pad < 1) pad = 1;
            return $"{label}{new string(' ', pad)}{val}";
        }
    }
}
