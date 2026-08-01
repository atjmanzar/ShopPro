using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class AuditLogService
    {
        private readonly ShopDbContext _db;

        public AuditLogService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task LogActivityAsync(string action, string targetEntity, string details, int userId)
        {
            var log = new AuditLog
            {
                Action = action,
                TargetEntity = targetEntity,
                Details = details,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
    }
}
