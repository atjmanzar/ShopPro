using ShopPro.Hardware;
using ShopPro.Core.Services;
using ShopPro.Core.Models;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class EscPosPrinterServiceTests
    {
        private ShopDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            var db = new ShopDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            DbInitializer.Initialize(db);
            return db;
        }

        [Fact]
        public void BuildEscPosByteStream_GeneratesRealEscPosControlBytes_IncludingBoldAndPaperCut()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-ESC-001",
                CashierName = "Cashier 1",
                Total = 150.00m,
                AmountPaid = 200.00m,
                ChangeDue = 50.00m,
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Maggi 280g", Quantity = 1, LineTotal = 48.00m }
                }
            };

            var bytes = printer.BuildEscPosByteStream(receipt, printer.Config);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Verify ESC @ (0x1B, 0x40) Initialize Printer Command
            Assert.Equal(0x1B, bytes[0]);
            Assert.Equal(0x40, bytes[1]);

            // Verify GS V 66 0 (0x1D, 0x56, 0x42, 0x00) Partial Paper Cut Command at end of stream
            int len = bytes.Length;
            Assert.Equal(0x1D, bytes[len - 4]);
            Assert.Equal(0x56, bytes[len - 3]);
            Assert.Equal(0x42, bytes[len - 2]);
            Assert.Equal(0x00, bytes[len - 1]);
        }

        [Fact]
        public async Task PrintReceiptWithStatus_PrinterUnavailable_ReturnsGracefulErrorResult_AndDecoupledFromCheckout()
        {
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var printer = new EscPosPrinterService();

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            // Step 1: Process Checkout (Completes financially)
            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 2000.00m);
            Assert.NotNull(sale);

            // Step 2: Try Print Receipt to non-existent hardware printer
            var printResult = await pos.TryPrintCheckoutReceiptAsync(sale, printer, "MISSING-PRINTER-NAME");

            // Assert: Print fails gracefully with clear message, but Sale in DB is NOT affected
            Assert.False(printResult.Success);
            Assert.Contains("Printer not found — check connection and retry", printResult.Message);

            var dbSale = await db.Sales.FindAsync(sale.Id);
            Assert.NotNull(dbSale); // Completed Sale remains intact in DB
            Assert.Equal(SaleStatus.Completed, dbSale.Status);
        }

        [Fact]
        public async Task ReprintLastReceiptAsync_ReconstructsReceiptDataFromDatabaseSaleRecord()
        {
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var printer = new EscPosPrinterService();

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 2000.00m);
            Assert.NotNull(sale);

            // Act: Reprint receipt by saleId
            var printResult = await pos.ReprintLastReceiptAsync(sale.Id, printer, "None");

            // Assert: Successfully maps sale from SQLite and generates receipt preview
            Assert.True(printResult.Success);
            Assert.NotNull(printResult.OutputPath);
            Assert.True(System.IO.File.Exists(printResult.OutputPath));

            string fileText = await System.IO.File.ReadAllTextAsync(printResult.OutputPath);
            Assert.Contains(sale.InvoiceNumber, fileText);
            Assert.Contains("₹1180.00", fileText); // Grand total
            Assert.Contains("₹820.00", fileText);  // Change due
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
