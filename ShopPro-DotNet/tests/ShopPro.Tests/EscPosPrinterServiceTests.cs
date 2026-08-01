using ShopPro.Hardware;
using Xunit;

namespace ShopPro.Tests
{
    public class EscPosPrinterServiceTests
    {
        [Fact]
        public void FormatEscPosText_80mmPaper_FormatsStoreInfoAndLineItemsCorrectly()
        {
            var printer = new EscPosPrinterService
            {
                Config = new ReceiptHeaderConfig
                {
                    StoreName = "SuperMart Retail",
                    Gstin = "27ABCDE1234F1Z5",
                    PaperWidth = PaperWidth.mm80
                }
            };

            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-20260802-001",
                CashierName = "John Doe",
                PaymentMethod = "Cash",
                Subtotal = 1000.00m,
                Discount = 100.00m,
                Tax = 162.00m,
                Total = 1062.00m,
                AmountPaid = 2000.00m,
                ChangeDue = 938.00m,
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Maggi 280g", Quantity = 2, UnitPrice = 48.00m, LineTotal = 96.00m }
                }
            };

            var text = printer.FormatEscPosText(receipt, printer.Config);

            Assert.Contains("SuperMart Retail", text);
            Assert.Contains("GSTIN: 27ABCDE1234F1Z5", text);
            Assert.Contains("Invoice #: INV-20260802-001", text);
            Assert.Contains("2x Maggi 280g", text);
            Assert.Contains("GRAND TOTAL:", text);
            Assert.Contains("₹1062.00", text);
            Assert.Contains("Change Due:", text);
            Assert.Contains("₹938.00", text);
        }

        [Fact]
        public async Task PrintReceiptWithStatus_PrinterUnavailable_ReturnsGracefulErrorResult()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-999",
                CashierName = "Admin",
                Total = 500.00m
            };

            // Act: Request print to non-existent printer "POS-PRINTER-MISSING"
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "POS-PRINTER-MISSING");

            // Assert: Fails gracefully without throwing an exception or crashing
            Assert.False(result.Success);
            Assert.Contains("Printer not found — check connection and retry", result.Message);
        }

        [Fact(Skip = "Requires physical ESC/POS thermal printer attached via USB/COM port")]
        public async Task PrintReceipt_PhysicalHardware_PrintsReceiptOnThermalPrinter()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-HARDWARE-TEST", Total = 100.00m };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "COM1");
            Assert.True(result.Success);
        }
    }
}
