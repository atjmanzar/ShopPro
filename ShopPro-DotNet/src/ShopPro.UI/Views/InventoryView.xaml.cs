using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class InventoryView : Page
    {
        private readonly User _currentUser;
        private readonly ShopDbContext _db;
        private readonly InventoryService _inventoryService;
        private readonly InventoryDashboardService _dashboardService;

        public InventoryView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _db = new ShopDbContext("");
            _inventoryService = new InventoryService(_db);
            _dashboardService = new InventoryDashboardService(_db);

            LoadDashboardAndProducts();
        }

        private async void LoadDashboardAndProducts()
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();
            TxtDashTotalProducts.Text = summary.TotalProducts.ToString();
            TxtDashValuation.Text = $"₹{summary.ValuationAtCost:N0} / ₹{summary.ValuationAtRetail:N0}";
            TxtDashLowStock.Text = summary.LowStockCount.ToString();
            TxtDashOutOfStock.Text = summary.OutOfStockCount.ToString();

            var products = await _inventoryService.GetAllProductsAsync();
            DgProducts.ItemsSource = products;
        }

        private async void BtnFilterLowStock_Click(object sender, RoutedEventArgs e)
        {
            var lowStock = await _inventoryService.GetLowStockProductsAsync();
            DgProducts.ItemsSource = lowStock;
        }

        private async void BtnFilterOutOfStock_Click(object sender, RoutedEventArgs e)
        {
            var outOfStock = await _inventoryService.GetOutOfStockProductsAsync();
            DgProducts.ItemsSource = outOfStock;
        }

        private void BtnShowAll_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardAndProducts();
        }

        private void BtnManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryManagementDialog();
            dialog.ShowDialog();
            LoadDashboardAndProducts();
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadDashboardAndProducts();
            }
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgProducts.SelectedItem as Product;
            if (selected == null)
            {
                MessageBox.Show("Please select a product from the list to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ProductEditDialog(selected);
            if (dialog.ShowDialog() == true)
            {
                LoadDashboardAndProducts();
            }
        }

        private async void BtnAdjustStock_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgProducts.SelectedItem as Product;
            if (selected == null)
            {
                MessageBox.Show("Please select a product from the list to adjust stock.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new StockAdjustmentDialog(selected.Name);
            if (dialog.ShowDialog() == true)
            {
                var success = await _inventoryService.AdjustStockAsync(selected.Id, dialog.QuantityChange, dialog.Reason, _currentUser.Id);
                if (success)
                {
                    MessageBox.Show("Stock adjusted successfully.", "Inventory Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDashboardAndProducts();
                }
            }
        }
    }
}
