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

    /// <summary>
    /// ESC/POS Thermal Receipt Printing Engine:
    /// Constructs real binary ESC/POS control streams (Initialization, Alignment, Bold Totals, Paper Cut) and transmits them via:
    /// 1. Win32 Raw Print Spooler (winspool.drv P/Invoke) for USB / Windows-installed Printers.
    /// 2. System.IO.Ports.SerialPort for COM-connected thermal printers.
    /// 
    /// Note on Verification:
    /// This code builds real binary ESC/POS command sequences and invokes winspool.drv / SerialPort OS APIs.
    /// Spooler API invocation can be verified on Windows OS. Physical paper feeding, motor cutting, and RJ11 drawer kick can only be verified when attached to a physical ESC/POS thermal printer.
    /// </summary>
    public class EscPosPrinterService : IPrinterService
    {
        public ReceiptHeaderConfig Config { get; set; } = new();

        public async Task<PrintResult> PrintReceiptWithStatusAsync(ReceiptData receipt, string printerName = "")
        {
            if (receipt == null)
                return new PrintResult { Success = false, Message = "Receipt data cannot be null." };

            var targetPrinter = string.IsNullOrWhiteSpace(printerName) ? "Generic / Text Only" : printerName.Trim();

            // Build binary ESC/POS command byte stream (includes ESC @, ESC E bold, GS V paper cut)
            byte[] escPosBytes = BuildEscPosByteStream(receipt, Config);
            string formattedText = FormatEscPosText(receipt, Config);

            // Preview file generated for offline development/logging
            var previewPath = Path.Combine(Path.GetTempPath(), $"Receipt_{receipt.InvoiceNumber}.txt");
            await File.WriteAllTextAsync(previewPath, formattedText);

            // Check if printer is installed or COM port exists
            bool exists = CheckPrinterExists(targetPrinter);
            if (!exists)
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"Printer not found — check connection and retry (Target: '{targetPrinter}'). Saved to preview file.",
                    OutputPath = previewPath
                };
            }

            // Route to COM Serial Port or Windows Print Spooler
            if (targetPrinter.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                return await SendToSerialPortAsync(targetPrinter, escPosBytes, previewPath);
            }
            else
            {
                return await Task.Run(() =>
                {
                    var result = WinSpoolPrinter.SendBytesToPrinter(targetPrinter, escPosBytes);
                    return new PrintResult
                    {
                        Success = result.Success,
                        Message = result.Message,
                        OutputPath = previewPath
                    };
                });
            }
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

            var targetPrinter = string.IsNullOrWhiteSpace(printerNameOrPort) ? "Generic / Text Only" : printerNameOrPort.Trim();

            if (!CheckPrinterExists(targetPrinter))
                return false;

            if (targetPrinter.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                var res = await SendToSerialPortAsync(targetPrinter, drawerPulseBytes, null);
                return res.Success;
            }
            else
            {
                var res = WinSpoolPrinter.SendBytesToPrinter(targetPrinter, drawerPulseBytes);
                return res.Success;
            }
        }

        public async Task<bool> TestPrinterConnectionAsync(string printerNameOrPort)
        {
            await Task.CompletedTask;
            return CheckPrinterExists(printerNameOrPort);
        }

        public bool CheckPrinterExists(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;

            if (printerName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var availablePorts = SerialPort.GetPortNames();
                    return availablePorts.Contains(printerName.ToUpper().Trim());
                }
                catch
                {
                    return false;
                }
            }

            // Check installed Windows Printers via System.Drawing.Printing.PrinterSettings
            try
            {
                foreach (string installedPrinter in PrinterSettings.InstalledPrinters)
                {
                    if (installedPrinter.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback for non-Windows environments
                if (printerName.Equals("Generic / Text Only", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
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
                if (line.Length > cols - 10) line = line.Substring(0, cols - 10);
                var priceStr = $"₹{item.LineTotal:F2}";
                int pad = cols - line.Length - priceStr.Length;
                if (pad < 1) pad = 1;
                bytes.GetBytes($"{line}{new string(' ', pad)}{priceStr}\n", 0, line.Length + pad + priceStr.Length + 1, bytes.ToArray(), 0); // Format line
            }

            bytes.AddRange(Encoding.ASCII.GetBytes(new string('-', cols) + "\n"));

            // ESC E 1 : Enable Bold for Totals
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("GRAND TOTAL:", $"₹{receipt.Total:F2}", cols) + "\n"));
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Amount Paid:", $"₹{receipt.AmountPaid:F2}", cols) + "\n"));
            bytes.AddRange(Encoding.ASCII.GetBytes(FormatPair("Change Due:", $"₹{receipt.ChangeDue:F2}", cols) + "\n"));
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
                if (line.Length > cols - 10) line = line.Substring(0, cols - 10);
                var priceStr = $"₹{item.LineTotal:F2}";
                var padding = cols - line.Length - priceStr.Length;
                if (padding < 1) padding = 1;
                sb.AppendLine($"{line}{new string(' ', padding)}{priceStr}");
            }

            sb.AppendLine(new string('-', cols));
            sb.AppendLine(FormatPair("Subtotal:", $"₹{receipt.Subtotal:F2}", cols));
            if (receipt.Discount > 0) sb.AppendLine(FormatPair("Discount:", $"₹{receipt.Discount:F2}", cols));
            sb.AppendLine(FormatPair("GST Tax:", $"₹{receipt.Tax:F2}", cols));
            sb.AppendLine(FormatPair("GRAND TOTAL:", $"₹{receipt.Total:F2}", cols));
            sb.AppendLine(FormatPair("Amount Paid:", $"₹{receipt.AmountPaid:F2}", cols));
            sb.AppendLine(FormatPair("Change Due:", $"₹{receipt.ChangeDue:F2}", cols));
            sb.AppendLine(new string('=', cols));

            sb.AppendLine(CenterText(config.FooterMessage, cols));
            return sb.ToString();
        }

        private async Task<PrintResult> SendToSerialPortAsync(string portName, byte[] bytes, string? previewPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                    port.Open();
                    port.Write(bytes, 0, bytes.Length);
                    port.Close();
                    return new PrintResult { Success = true, Message = $"Data transmitted to serial printer at {portName}.", OutputPath = previewPath };
                }
                catch (Exception ex)
                {
                    return new PrintResult { Success = false, Message = $"Serial port error ({portName}): {ex.Message}", OutputPath = previewPath };
                }
            });
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
