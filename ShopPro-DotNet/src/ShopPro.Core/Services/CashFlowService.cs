using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class CashBookSummary
    {
        public decimal CashSalesInflow { get; set; }
        public decimal CustomerLedgerInflow { get; set; }
        public decimal VendorPaymentsOutflow { get; set; }
        public decimal ExpensesOutflow { get; set; }
        public decimal NetCashBalance => (CashSalesInflow + CustomerLedgerInflow) - (VendorPaymentsOutflow + ExpensesOutflow);
    }

    public class CashFlowService
    {
        private readonly ShopDbContext _db;

        public CashFlowService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<CashBookSummary> GetCashBookSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var cashSales = await _db.Payments
                .Where(p => p.Method == PaymentMethod.Cash && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);

            var expenses = await _db.Expenses
                .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .SumAsync(e => e.Amount);

            return new CashBookSummary
            {
                CashSalesInflow = cashSales,
                CustomerLedgerInflow = 0.00m,
                VendorPaymentsOutflow = 0.00m,
                ExpensesOutflow = expenses
            };
        }
    }
}
