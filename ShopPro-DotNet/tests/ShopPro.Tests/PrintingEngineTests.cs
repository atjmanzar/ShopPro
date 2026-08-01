using ShopPro.Hardware;
using Xunit;

namespace ShopPro.Tests
{
    public class PrintingEngineTests
    {
        [Fact]
        public void FormatEscPosText_80mmPaperWidth_Formats48Columns()
        {
            // Arrange
            var printerService = new EscPosPrinterService
            {
                Config = new ReceiptHeaderConfig { PaperWidth = PaperWidth.mm80 }
            };

            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-20260802-101",
                CashierName = "Admin",
                Subtotal = 100.00m,
                Discount = 10.00m,
                Tax = 16.20m,
                Total = 106.20m,
                AmountPaid = 200.00m,
                ChangeDue = 93.80m,
                PaymentMethod = "Cash",
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Maggi Noodles", Quantity = 2, UnitPrice = 48.00m, LineTotal = 96.00m }
                }
            };

            // Act
            var text = printerService.FormatEscPosText(receipt, printerService.Config);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("INV-20260802-101", text);
            Assert.Contains("GRAND TOTAL:", text);
            Assert.Contains("₹106.20", text);
        }

        [Fact]
        public void FormatEscPosText_58mmPaperWidth_Formats32Columns()
        {
            // Arrange
            var printerService = new EscPosPrinterService
            {
                Config = new ReceiptHeaderConfig { PaperWidth = PaperWidth.mm58 }
            };

            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-58MM-001",
                CashierName = "Cashier",
                Subtotal = 48.00m,
                Total = 48.00m,
                AmountPaid = 50.00m,
                ChangeDue = 2.00m,
                PaymentMethod = "Cash"
            };

            // Act
            var text = printerService.FormatEscPosText(receipt, printerService.Config);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("INV-58MM-001", text);
        }
    }
}
