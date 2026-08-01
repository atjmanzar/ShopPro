using ShopPro.Core.Services;
using ShopPro.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class DiagnosticsView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly SystemHealthMonitor _healthMonitor;
        private readonly DiagnosticRepairTool _repairTool;

        public DiagnosticsView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _healthMonitor = new SystemHealthMonitor(_db);
            _repairTool = new DiagnosticRepairTool(_db);

            LoadDiagnostics();
        }

        private async void LoadDiagnostics()
        {
            var health = await _healthMonitor.GetHealthReportAsync();
            TxtRamUsage.Text = $"RAM Working Set: {health.MemoryWorkingSetMb} MB";
            TxtDiskSpace.Text = $"Free Disk Space: {health.FreeDiskSpaceGb} GB";
            TxtDbLatency.Text = $"Database Ping Latency: {health.DatabasePingLatencyMs:F2} ms";

            TxtLogs.Text = ExceptionLogger.GetRecentLogContent();
        }

        private void BtnRefreshHealth_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagnostics();
        }

        private async void BtnVacuum_Click(object sender, RoutedEventArgs e)
        {
            var result = await _repairTool.VacuumDatabaseAsync();
            if (result.Success)
            {
                MessageBox.Show(result.Details, "Database Repair Passed", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDiagnostics();
            }
            else
            {
                MessageBox.Show(result.Details, "Repair Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExportSupport_Click(object sender, RoutedEventArgs e)
        {
            var health = await _healthMonitor.GetHealthReportAsync();
            var report = _repairTool.ExportSupportDiagnosticPackage(health);

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"ShopPro_DiagnosticPackage_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            File.WriteAllText(filePath, report);
            MessageBox.Show($"Technical Support Diagnostic Package saved to Desktop:\n{filePath}", "Package Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
