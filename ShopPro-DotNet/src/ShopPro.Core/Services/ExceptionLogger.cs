using System.Text;

namespace ShopPro.Core.Services
{
    public class ExceptionLogger
    {
        private static readonly string _logFolder;

        static ExceptionLogger()
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logFolder = Path.Combine(localData, "ShopPro", "Logs");
            if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);
        }

        public static void LogException(Exception ex, string contextMessage = "")
        {
            try
            {
                var fileName = $"crash_{DateTime.Now:yyyyMMdd}.log";
                var filePath = Path.Combine(_logFolder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine($"[CRASH LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                if (!string.IsNullOrEmpty(contextMessage)) sb.AppendLine($"Context: {contextMessage}");
                sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Stack Trace:\n{ex.StackTrace}");
                sb.AppendLine("--------------------------------------------------------------------------------\n");

                File.AppendAllText(filePath, sb.ToString());
            }
            catch
            {
                // Silent fallback to avoid unhandled crash during logging
            }
        }

        public static string GetRecentLogContent()
        {
            if (!Directory.Exists(_logFolder)) return "No logs recorded.";

            var logFiles = Directory.GetFiles(_logFolder, "crash_*.log");
            if (!logFiles.Any()) return "No crash logs recorded.";

            var latest = logFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
            return File.ReadAllText(latest);
        }
    }
}
