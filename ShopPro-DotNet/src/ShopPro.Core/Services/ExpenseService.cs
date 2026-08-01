using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class ExpenseService
    {
        private readonly ShopDbContext _db;

        public ExpenseService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Expense>> GetExpensesAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.Expenses
                .Include(e => e.User)
                .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<Expense> AddExpenseAsync(string category, string title, decimal amount, string description, int userId)
        {
            var expense = new Expense
            {
                Category = category,
                Title = title,
                Amount = amount,
                Description = description,
                UserId = userId,
                ExpenseDate = DateTime.UtcNow
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            return expense;
        }

        public async Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.Expenses
                .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .SumAsync(e => e.Amount);
        }
    }
}
