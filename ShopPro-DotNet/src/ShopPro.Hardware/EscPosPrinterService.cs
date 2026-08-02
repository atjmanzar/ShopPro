using System.Text;
using System.Drawing.Printing;
using System.IO.Ports;

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
        public string FooterMessage { get; set; } = "Thank you for shopping with ShopPro!\nVisit again soon!";
        public PaperWidth PaperWidth { get; set; } = PaperWidth.mm80;
    }

    public class PrintResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OutputPath { get; set; }
    }

    public class WindowsSpoolerAndSerialTransport : IPrinterTransport
    {
        public (bool Success, string Message) SendBytes(string printerNameOrPort, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort))
                return (false, "Printer name or port is empty.");

            var target = printerNameOrPort.Trim();
            if (target.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var port = new SerialPort(target, 9600, Parity.None, 8, StopBits.One);
                    port.Open();
                    port.Write(bytes, 0, bytes.Length);
                    port.Close();
                    return (true, $"Data transmitted to serial printer at {target}. Physical paper print unverified without attached hardware.");
                }
                catch (Exception ex)
                {
                    return (false, $"Serial port error ({target}): {ex.Message}");
                }
            }
            else
            {
                return WinSpoolPrinter.SendBytesToPrinter(target, bytes);
            }
        }

        public bool CheckAvailability(string printerNameOrPort)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort)) return false;

            var target = printerNameOrPort.Trim();
            if (target.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return SerialPort.GetPortNames().Contains(target.ToUpper());
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                foreach (string installedPrinter in PrinterSettings.InstalledPrinters)
                {
                    if (installedPrinter.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                if (target.Equals("Generic / Text Only", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// ESC/POS Thermal Receipt Printing Engine:
    /// Constructs binary ESC/POS control streams (Initialization, Alignment, Bold Totals, Paper Cut, Cash Drawer Kick)
    /// and transmits them using the injected IPrinterTransport (Win32 Spooler / SerialPort).
    /// 
    /// Currency Encoding:
    /// Uses 'Rs.' to guarantee 100% compatibility across ESC/POS code pages without ASCII '?' substitution.
    /// </summary>
    public class EscPosPrinterService : IPrinterService
    {
        private readonly IPrinterTransport _transport;

        public ReceiptHeaderConfig Config { get; set; } = new();

        public EscPosPrinterService(IPrinterTransport? transport = null)
        {
            _transport = transport ?? new WindowsSpoolerAndSerialTransport();
        }

        public async Task<PrintResult> PrintReceiptWithStatusAsync(ReceiptData receipt, string printerName = "")
        {
            if (receipt == null)
                return new PrintResult { Success = false, Message = "Receipt data cannot be null." };

            var targetPrinter = string.IsNullOrWhiteSpace(printerName) ? "" : printerName.Trim();

            // Build binary ESC/POS command byte stream (includes ESC @, ESC E bold, GS V paper cut)
            byte[] escPosBytes = BuildEscPosByteStream(receipt, Config);
            string formattedText = FormatEscPosText(receipt, Config);

            // Preview file generated for offline development/logging
            var previewPath = Path.Combine(Path.GetTempPath(), $"Receipt_{receipt.InvoiceNumber}.txt");
            await File.WriteAllTextAsync(previewPath, formattedText);

            // If no printer name specified, generate preview receipt cleanly without calling spooler
            if (string.IsNullOrWhiteSpace(targetPrinter))
            {
                return new PrintResult
                {
                    Success = true,
                    Message = $"Preview receipt generated at path: '{previewPath}' (Preview mode / No physical printer selected).",
                    OutputPath = previewPath
                };
            }

            // Check if printer is installed or COM port exists
            bool available = _transport.CheckAvailability(targetPrinter);
            if (!available)
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"Printer not found — check connection and retry (Target: '{targetPrinter}'). Preview saved.",
                    OutputPath = previewPath
                };
            }

            var result = _transport.SendBytes(targetPrinter, escPosBytes);
            return new PrintResult
            {
                Success = result.Success,
                Message = result.Message,
                OutputPath = previewPath
            };
        }

        public async Task<bool> PrintReceiptAsync(ReceiptData receipt)
        {
            var result = await PrintReceiptWithStatusAsync(receipt, "");
            return result.Success;
        }

        public async Task<bool> OpenCashDrawerAsync(string printerNameOrPort = "")
        {
            // ESC/POS Cash Drawer Pulse Command: ESC p m t1 t2 (0x1B, 0x70, 0x00, 0x19, 0xFA)
            // Pin 2 pulse: 25ms ON, 250ms OFF
            byte[] drawerPulseBytes = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };

            var target = string.IsNullOrWhiteSpace(printerNameOrPort) ? "" : printerNameOrPort.Trim();
            if (string.IsNullOrWhiteSpace(target) || !_transport.CheckAvailability(target))
            {
                return false;
            }

            var result = _transport.SendBytes(target, drawerPulseBytes);
            return result.Success;
        }

        public bool CheckPrinterAvailability(string printerNameOrPort)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort)) return false;
            return _transport.CheckAvailability(printerNameOrPort.Trim());
        }

        public byte[] BuildEscPosByteStream(ReceiptData receipt, ReceiptHeaderConfig config)
        {
            var bytes = new List<byte>();

            // ESC @ : Initialize Printer
            bytes.AddRange(new byte[] { 0x1B, 0x40 });

            // ESC a 1 : Center Align Header
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });

            // ESC E 1 : Enable Bold
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(config.StoreName + "\n"));
            // ESC E 0 : Disable Bold
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            bytes.AddRange(Encoding.ASCII.GetBytes(config.AddressLine1 + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(config.AddressLine2 + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"GSTIN: {config.Gstin}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('=', (int)config.PaperWidth) + "\n"));

            // ESC a 0 : Left Align Body
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
            bytes.AddRange(Encoding.ASCII.GetBytes($"Invoice #: {receipt.InvoiceNumber}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Date: {receipt.TransactionDate:yyyy-MM-dd HH:mm}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Cashier: {receipt.CashierName}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes($"Payment Method: {receipt.PaymentMethod}\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('-', (int)config.PaperWidth) + "\n"));

            int cols = (int)config.PaperWidth;
            foreach (var item in receipt.Items)
            {
                var line = $"{item.Quantity}x {item.ItemName}";
                if (line.Length > cols - 12) line = line.Substring(0, cols - 12);
                var priceStr = $"Rs. {item.LineTotal:F2}";
                int pad = cols - line.Length - priceStr.Length;
                if (pad < 1) pad = 1;
                bytes.AddRange(Encoding.ASCII.GetBytes($"{line}{new string(' ', pad)}{priceStr}\n"));
            }

            bytes.AddRange(Encoding.ASCII.GetBytes(new string('-', cols) + "\n"));

            // Totals Section
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Subtotal:", $"Rs. {receipt.Subtotal:F2}", cols) + "\n"));
            if (receipt.Discount > 0)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Discount:", $"Rs. {receipt.Discount:F2}", cols) + "\n"));
            }
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("GST Tax:", $"Rs. {receipt.Tax:F2}", cols) + "\n"));

            // ESC E 1 : Enable Bold for Grand Total
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("GRAND TOTAL:", $"Rs. {receipt.Total:F2}", cols) + "\n"));
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Amount Paid:", $"Rs. {receipt.AmountPaid:F2}", cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Change Due:", $"Rs. {receipt.ChangeDue:F2}", cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(new string('=', cols) + "\n"));

            // ESC a 1 : Center Align Footer
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(config.FooterMessage + "\n"));

            // Feed 3 lines + GS V 66 0 : Partial Paper Cut Command
            bytes.AddRange(Encoding.ASCII.GetBytes("\n\n\n"));
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

            return bytes.ToArray();
        }

        public string FormatEscPosText(ReceiptData receipt, ReceiptHeaderConfig config)
        {
            int cols = (int)config.PaperWidth;
            var sb = new StringBuilder();

            sb.AppendLine(CenterText(config.StoreName, cols));
            sb.AppendLine(CenterText(config.AddressLine1, cols));
            sb.AppendLine(CenterText(config.AddressLine2, cols));
            sb.AppendLine(CenterText($"GSTIN: {config.Gstin}", cols));
            sb.AppendLine(new string('=', cols));

            sb.AppendLine($"Invoice #: {receipt.InvoiceNumber}");
            sb.AppendLine($"Date: {receipt.TransactionDate:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Cashier: {receipt.CashierName}");
            sb.AppendLine($"Payment Method: {receipt.PaymentMethod}");
            sb.AppendLine(new string('-', cols));

            foreach (var item in receipt.Items)
            {
                var line = $"{item.Quantity}x {item.ItemName}";
                if (line.Length > cols - 12) line = line.Substring(0, cols - 12);
                var priceStr = $"Rs. {item.LineTotal:F2}";
                var padding = cols - line.Length - priceStr.Length;
                if (padding < 1) padding = 1;
                sb.AppendLine($"{line}{new string(' ', padding)}{priceStr}");
            }

            sb.AppendLine(new string('-', cols));
            sb.AppendLine(FormatPair("Subtotal:", $"Rs. {receipt.Subtotal:F2}", cols));
            if (receipt.Discount > 0) sb.AppendLine(FormatPair("Discount:", $"Rs. {receipt.Discount:F2}", cols));
            sb.AppendLine(FormatPair("GST Tax:", $"Rs. {receipt.Tax:F2}", cols));
            sb.AppendLine(FormatPair("GRAND TOTAL:", $"Rs. {receipt.Total:F2}", cols));
            sb.AppendLine(FormatPair("Amount Paid:", $"Rs. {receipt.AmountPaid:F2}", cols));
            sb.AppendLine(FormatPair("Change Due:", $"Rs. {receipt.ChangeDue:F2}", cols));
            sb.AppendLine(new string('=', cols));

            sb.AppendLine(CenterText(config.FooterMessage, cols));
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
