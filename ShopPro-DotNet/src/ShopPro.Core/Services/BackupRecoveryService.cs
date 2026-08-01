using ShopPro.Data;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class BackupFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BackupRecoveryService
    {
        private readonly ShopDbContext _db;
        private readonly string _backupFolder;

        public BackupRecoveryService(ShopDbContext db)
        {
            _db = db;
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _backupFolder = Path.Combine(localData, "ShopPro", "Backups");
            if (!Directory.Exists(_backupFolder)) Directory.CreateDirectory(_backupFolder);
        }

        public async Task<BackupFileInfo> CreateBackupAsync(string prefix = "manual")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"shoppro_{prefix}_backup_{timestamp}.db";
            var destPath = Path.Combine(_backupFolder, fileName);

            // Execute SQLite Vacuum Into or file copy
            var dbPath = GetDatabasePath();
            await Task.Run(() =>
            {
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, destPath, overwrite: true);
                }
            });

            var fileInfo = new FileInfo(destPath);
            return new BackupFileInfo
            {
                FileName = fileName,
                FilePath = destPath,
                FileSizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTime
            };
        }

        public List<BackupFileInfo> GetAvailableBackups()
        {
            if (!Directory.Exists(_backupFolder)) return new();

            return Directory.GetFiles(_backupFolder, "*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileInfo
                {
                    FileName = f.Name,
                    FilePath = f.FullName,
                    FileSizeBytes = f.Length,
                    CreatedAt = f.CreationTime
                }).ToList();
        }

        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath)) return false;

            // Integrity Check before restore
            var integrity = await DatabaseIntegrityChecker.CheckIntegrityAsync(backupFilePath);
            if (!integrity.IsHealthy) return false;

            var activeDbPath = GetDatabasePath();
            await Task.Run(() =>
            {
                File.Copy(backupFilePath, activeDbPath, overwrite: true);
            });

            return true;
        }

        private string GetDatabasePath()
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localData, "ShopPro", "shoppro.db");
        }
    }
}
