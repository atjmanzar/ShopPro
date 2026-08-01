using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class ShiftManagementService
    {
        private readonly ShopDbContext _db;

        public ShiftManagementService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<CashShift?> GetActiveShiftAsync(int userId)
        {
            return await _db.CashShifts
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open);
        }

        public async Task<CashShift> OpenShiftAsync(int userId, decimal openingFloat)
        {
            var active = await GetActiveShiftAsync(userId);
            if (active != null) return active;

            var shift = new CashShift
            {
                UserId = userId,
                OpeningFloat = openingFloat,
                Status = ShiftStatus.Open,
                OpenedAt = DateTime.UtcNow
            };

            _db.CashShifts.Add(shift);
            await _db.SaveChangesAsync();
            return shift;
        }

        public async Task<CashShift?> CloseShiftAsync(int shiftId, decimal actualCashCount)
        {
            var shift = await _db.CashShifts.FindAsync(shiftId);
            if (shift == null || shift.Status == ShiftStatus.Closed) return null;

            // Calculate total cash sales during shift
            var cashSales = await _db.Payments
                .Include(p => p.Sale)
                .Where(p => p.Method == PaymentMethod.Cash && p.PaymentDate >= shift.OpenedAt)
                .SumAsync(p => p.Amount);

            shift.TotalCashSales = cashSales;
            shift.ClosingCashCount = actualCashCount;
            shift.Status = ShiftStatus.Closed;
            shift.ClosedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return shift;
        }
    }
}
