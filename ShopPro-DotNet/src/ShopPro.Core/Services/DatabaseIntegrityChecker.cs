using Microsoft.Data.Sqlite;

namespace ShopPro.Core.Services
{
    public class IntegrityCheckResult
    {
        public bool IsHealthy { get; set; }
        public string DiagnosticOutput { get; set; } = string.Empty;
    }

    public class DatabaseIntegrityChecker
    {
        public static async Task<IntegrityCheckResult> CheckIntegrityAsync(string dbFilePath)
        {
            if (!File.Exists(dbFilePath))
            {
                return new IntegrityCheckResult { IsHealthy = false, DiagnosticOutput = "Database file does not exist." };
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbFilePath}");
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = (string?)(await cmd.ExecuteScalarAsync());

                bool ok = result != null && result.Equals("ok", StringComparison.OrdinalIgnoreCase);
                return new IntegrityCheckResult
                {
                    IsHealthy = ok,
                    DiagnosticOutput = result ?? "No output returned"
                };
            }
            catch (Exception ex)
            {
                return new IntegrityCheckResult
                {
                    IsHealthy = false,
                    DiagnosticOutput = $"SQLite Error: {ex.Message}"
                };
            }
        }
    }
}
