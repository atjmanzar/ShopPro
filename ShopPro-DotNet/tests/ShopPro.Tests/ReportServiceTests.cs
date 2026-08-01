using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class ReportServiceTests
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
        public async Task GetSalesReport_ReturnsCorrectAggregatesAndCsvExport()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            await pos.AddProductByBarcodeAsync("8901234567890"); // ₹48.00 item
            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);

            var reportService = new ReportService(db);

            // Act
            var summary = await reportService.GetSalesReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            var csv = reportService.ExportSalesReportToCsv(summary);

            // Assert
            Assert.NotNull(summary);
            Assert.Equal(1, summary.TotalTransactions);
            Assert.Equal(48.00m, summary.GrossRevenue);
            Assert.Contains("Invoice Number,Date,Cashier,Subtotal", csv);
            Assert.Contains(sale!.InvoiceNumber, csv);
        }
    }
}
