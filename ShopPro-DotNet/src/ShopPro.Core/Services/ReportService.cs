using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ShopPro.Core.Services
{
    public class ReportSummary
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal NetRevenue { get; set; }
        public List<Sale> SalesList { get; set; } = new();
    }

    public class ReportService
    {
        private readonly ShopDbContext _db;

        public ReportService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<ReportSummary> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var startUtc = startDate.Date;
            var endUtc = endDate.Date.AddDays(1).AddTicks(-1);

            var sales = await _db.Sales
                .Include(s => s.User)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .Where(s => s.SaleDate >= startUtc && s.SaleDate <= endUtc)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return new ReportSummary
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalTransactions = sales.Count,
                GrossRevenue = sales.Sum(s => s.Subtotal),
                TotalDiscounts = sales.Sum(s => s.TotalDiscount),
                TotalTaxCollected = sales.Sum(s => s.TotalTax),
                NetRevenue = sales.Sum(s => s.GrandTotal),
                SalesList = sales
            };
        }

        public string ExportSalesReportToCsv(ReportSummary summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Invoice Number,Date,Cashier,Subtotal,Discount,Tax,Grand Total");

            foreach (var sale in summary.SalesList)
            {
                sb.AppendLine($"\"{sale.InvoiceNumber}\",\"{sale.SaleDate:yyyy-MM-dd HH:mm:ss}\",\"{sale.User?.FullName}\",{sale.Subtotal:F2},{sale.TotalDiscount:F2},{sale.TotalTax:F2},{sale.GrandTotal:F2}");
            }

            return sb.ToString();
        }
    }
}
