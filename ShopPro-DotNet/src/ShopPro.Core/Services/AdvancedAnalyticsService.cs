using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class ProfitLossSummary
    {
        public decimal GrossRevenue { get; set; }
        public decimal TotalCogs { get; set; } // Cost of Goods Sold
        public decimal GrossProfit => GrossRevenue - TotalCogs;
        public decimal OperatingExpenses { get; set; }
        public decimal NetProfit => GrossProfit - OperatingExpenses;
        public decimal NetProfitMarginPercent => GrossRevenue > 0 ? Math.Round((NetProfit / GrossRevenue) * 100m, 2) : 0m;
    }

    public class ProductVelocity
    {
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal RevenueGenerated { get; set; }
        public bool IsFastMoving => UnitsSold >= 10;
        public bool IsDeadStock => UnitsSold == 0;
    }

    public class GstTaxLiabilitySummary
    {
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalCgstCollected { get; set; }
        public decimal TotalSgstCollected { get; set; }
        public decimal TotalIgstCollected { get; set; }
    }

    public class AdvancedAnalyticsService
    {
        private readonly ShopDbContext _db;

        public AdvancedAnalyticsService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<ProfitLossSummary> GetProfitLossSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var sales = await _db.Sales
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .ToListAsync();

            decimal grossRevenue = sales.Sum(s => s.GrandTotal);
            decimal totalCogs = sales.SelectMany(s => s.Items).Sum(i => (decimal)i.Quantity * (i.Product?.Cost ?? 0m));

            decimal operatingExpenses = await _db.Expenses
                .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .SumAsync(e => e.Amount);

            return new ProfitLossSummary
            {
                GrossRevenue = grossRevenue,
                TotalCogs = totalCogs,
                OperatingExpenses = operatingExpenses
            };
        }

        public async Task<List<ProductVelocity>> GetProductVelocitiesAsync(DateTime startDate, DateTime endDate)
        {
            var saleItems = await _db.SaleItems
                .Include(i => i.Product)
                .Include(i => i.Sale)
                .Where(i => i.Sale != null && i.Sale.SaleDate >= startDate && i.Sale.SaleDate <= endDate)
                .ToListAsync();

            var products = await _db.Products.Where(p => p.IsActive).ToListAsync();

            var velocities = new List<ProductVelocity>();
            foreach (var p in products)
            {
                var itemsForP = saleItems.Where(i => i.ProductId == p.Id).ToList();
                int unitsSold = itemsForP.Sum(i => i.Quantity);
                decimal revenue = itemsForP.Sum(i => i.LineTotal);

                velocities.Add(new ProductVelocity
                {
                    Sku = p.Sku,
                    ProductName = p.Name,
                    UnitsSold = unitsSold,
                    RevenueGenerated = revenue
                });
            }

            return velocities.OrderByDescending(v => v.UnitsSold).ToList();
        }

        public async Task<GstTaxLiabilitySummary> GetGstTaxLiabilityAsync(DateTime startDate, DateTime endDate)
        {
            var totalTax = await _db.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .SumAsync(s => s.TotalTax);

            var half = Math.Round(totalTax / 2m, 2);

            return new GstTaxLiabilitySummary
            {
                TotalTaxCollected = totalTax,
                TotalCgstCollected = half,
                TotalSgstCollected = totalTax - half,
                TotalIgstCollected = 0.00m
            };
        }
    }
}
