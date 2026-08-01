using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ShopPro.Core.Services
{
    public class HealthReport
    {
        public string OsVersion { get; set; } = string.Empty;
        public long MemoryWorkingSetMb { get; set; }
        public long FreeDiskSpaceGb { get; set; }
        public long DatabaseFileSizeBytes { get; set; }
        public bool IsDatabaseConnected { get; set; }
        public double DatabasePingLatencyMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SystemHealthMonitor
    {
        private readonly ShopDbContext _db;

        public SystemHealthMonitor(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<HealthReport> GetHealthReportAsync()
        {
            var report = new HealthReport
            {
                OsVersion = Environment.OSVersion.ToString(),
                MemoryWorkingSetMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024),
                FreeDiskSpaceGb = GetAvailableFreeSpaceGb()
            };

            var dbPath = GetDatabasePath();
            if (File.Exists(dbPath))
            {
                report.DatabaseFileSizeBytes = new FileInfo(dbPath).Length;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var count = await _db.Products.CountAsync();
                sw.Stop();
                report.IsDatabaseConnected = true;
                report.DatabasePingLatencyMs = sw.Elapsed.TotalMilliseconds;
            }
            catch
            {
                sw.Stop();
                report.IsDatabaseConnected = false;
                report.DatabasePingLatencyMs = -1;
            }

            return report;
        }

        private long GetAvailableFreeSpaceGb()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
                return drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            }
            catch
            {
                return -1;
            }
        }

        private string GetDatabasePath()
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localData, "ShopPro", "shoppro.db");
        }
    }
}
