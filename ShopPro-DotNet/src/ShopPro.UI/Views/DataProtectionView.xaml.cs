using ShopPro.Core.Services;
using ShopPro.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class DataProtectionView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly BackupRecoveryService _backupService;
        private readonly CatalogArchiveExporter _catalogExporter;

        public DataProtectionView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _backupService = new BackupRecoveryService(_db);
            _catalogExporter = new CatalogArchiveExporter(_db);

            LoadBackups();
        }

        private void LoadBackups()
        {
            var backups = _backupService.GetAvailableBackups();
            DgBackups.ItemsSource = backups;
        }

        private async void BtnCreateBackup_Click(object sender, RoutedEventArgs e)
        {
            var backup = await _backupService.CreateBackupAsync("manual");
            MessageBox.Show($"Database backup created successfully.\nFile: {backup.FileName}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadBackups();
        }

        private async void BtnCheckIntegrity_Click(object sender, RoutedEventArgs e)
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(localData, "ShopPro", "shoppro.db");

            var result = await DatabaseIntegrityChecker.CheckIntegrityAsync(dbPath);
            if (result.IsHealthy)
            {
                TxtIntegrityStatus.Text = "Status: HEALTHY (PRAGMA integrity_check = ok)";
                MessageBox.Show("Database file integrity is 100% healthy.", "Integrity Diagnostic Passed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                TxtIntegrityStatus.Text = $"Status: CORRUPTED ({result.DiagnosticOutput})";
                MessageBox.Show($"Database integrity issue detected:\n{result.DiagnosticOutput}", "Integrity Check Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgBackups.SelectedItem as BackupFileInfo;
            if (selected == null)
            {
                MessageBox.Show("Please select a backup file from the list to restore.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to restore database from backup '{selected.FileName}'?\nCurrent unsaved changes will be overwritten.", "Confirm Database Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var success = await _backupService.RestoreBackupAsync(selected.FilePath);
            if (success)
            {
                MessageBox.Show("Database restored successfully from backup archive.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to restore database. Integrity check failed on backup archive.", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExportCatalog_Click(object sender, RoutedEventArgs e)
        {
            var json = await _catalogExporter.ExportCatalogToJsonAsync();
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"StoreCatalog_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            File.WriteAllText(filePath, json);
            MessageBox.Show($"Complete Store Catalog Package exported to Desktop:\n{filePath}", "Catalog Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnImportCatalog_Click(object sender, RoutedEventArgs e)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var jsonFiles = Directory.GetFiles(desktop, "StoreCatalog_Export_*.json");

            if (!jsonFiles.Any())
            {
                MessageBox.Show("No 'StoreCatalog_Export_*.json' package files found on Desktop to import.", "No Package Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var latestPackage = jsonFiles.OrderByDescending(f => File.GetCreationTime(f)).First();
            var json = File.ReadAllText(latestPackage);

            var importedCount = await _catalogExporter.ImportCatalogFromJsonAsync(json);
            MessageBox.Show($"Catalog Package Imported Successfully!\nImported {importedCount} new products into database.", "Catalog Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
