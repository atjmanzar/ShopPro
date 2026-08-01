using ShopPro.Core.Services;
using ShopPro.Data;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class SupplierManagementView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly SupplierService _supplierService;

        public SupplierManagementView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _supplierService = new SupplierService(_db);

            LoadSuppliers();
        }

        private async void LoadSuppliers()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            DgSuppliers.ItemsSource = suppliers;
        }

        private void BtnCreatePo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Purchase Order Creation Modal Launched.", "Purchase Orders", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
