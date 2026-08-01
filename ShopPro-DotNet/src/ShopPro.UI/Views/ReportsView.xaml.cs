using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class ReportsView : Page
    {
        private readonly User _currentUser;
        private readonly ShopDbContext _db;
        private readonly ReportService _reportService;
        private readonly AdvancedAnalyticsService _analyticsService;

        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today.AddDays(1);

        public ReportsView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _db = new ShopDbContext("");
            _reportService = new ReportService(_db);
            _analyticsService = new AdvancedAnalyticsService(_db);

            LoadAnalytics();
        }

        private async void LoadAnalytics()
        {
            var pl = await _analyticsService.GetProfitLossSummaryAsync(_startDate, _endDate);
            TxtGrossRevenue.Text = $"₹{pl.GrossRevenue:N2}";
            TxtCogs.Text = $"₹{pl.TotalCogs:N2}";
            TxtExpenses.Text = $"₹{pl.OperatingExpenses:N2}";
            TxtNetProfit.Text = $"₹{pl.NetProfit:N2} ({pl.NetProfitMarginPercent}%)";

            var summary = await _reportService.GetSalesReportAsync(_startDate, _endDate);
            DgSales.ItemsSource = summary.Sales;

            var velocities = await _analyticsService.GetProductVelocitiesAsync(_startDate, _endDate);
            DgProductVelocity.ItemsSource = velocities;
        }

        private void BtnFilterToday_Click(object sender, RoutedEventArgs e)
        {
            _startDate = DateTime.Today;
            _endDate = DateTime.Today.AddDays(1);
            LoadAnalytics();
        }

        private void BtnFilterMonth_Click(object sender, RoutedEventArgs e)
        {
            _startDate = DateTime.Today.AddDays(-30);
            _endDate = DateTime.Today.AddDays(1);
            LoadAnalytics();
        }

        private void BtnManageExpenses_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExpenseManagementDialog(_currentUser);
            dialog.ShowDialog();
            LoadAnalytics();
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var pl = await _analyticsService.GetProfitLossSummaryAsync(_startDate, _endDate);
            var gst = await _analyticsService.GetGstTaxLiabilityAsync(_startDate, _endDate);

            var pdfText = PdfExcelExporter.ExportToPdfText(pl, gst, _startDate, _endDate);
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"Financial_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            File.WriteAllText(filePath, pdfText);
            MessageBox.Show($"Financial PDF Summary exported to Desktop:\n{filePath}", "PDF Report Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var velocities = await _analyticsService.GetProductVelocitiesAsync(_startDate, _endDate);
            var xml = PdfExcelExporter.ExportToExcelXml(velocities);

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"Product_Velocity_{DateTime.Now:yyyyMMdd_HHmmss}.xml");

            File.WriteAllText(filePath, xml);
            MessageBox.Show($"Excel XML Product Velocity Report exported to Desktop:\n{filePath}", "Excel Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
