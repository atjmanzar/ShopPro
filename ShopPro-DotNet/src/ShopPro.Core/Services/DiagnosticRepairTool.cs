using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ShopPro.Core.Services
{
    public class RepairToolResult
    {
        public bool Success { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class DiagnosticRepairTool
    {
        private readonly ShopDbContext _db;

        public DiagnosticRepairTool(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<RepairToolResult> VacuumDatabaseAsync()
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");
                await _db.Database.ExecuteSqlRawAsync("REINDEX;");

                return new RepairToolResult
                {
                    Success = true,
                    Details = "Database VACUUM and REINDEX completed successfully. Space reclaimed."
                };
            }
            catch (Exception ex)
            {
                return new RepairToolResult
                {
                    Success = false,
                    Details = $"VACUUM failed: {ex.Message}"
                };
            }
        }

        public string ExportSupportDiagnosticPackage(HealthReport health)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("       SHOPPRO TECHNICAL SUPPORT DIAGNOSTIC       ");
            sb.AppendLine("=================================================");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OS Version: {health.OsVersion}");
            sb.AppendLine($"Memory Working Set: {health.MemoryWorkingSetMb} MB");
            sb.AppendLine($"Free Disk Space: {health.FreeDiskSpaceGb} GB");
            sb.AppendLine($"Database File Size: {health.DatabaseFileSizeBytes / (1024 * 1024):N2} MB");
            sb.AppendLine($"Database Ping Latency: {health.DatabasePingLatencyMs:F2} ms");
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine("RECENT CRASH LOGS:");
            sb.AppendLine(ExceptionLogger.GetRecentLogContent());
            sb.AppendLine("=================================================");

            return sb.ToString();
        }
    }
}
