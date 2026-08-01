using ShopPro.Core.Models;
using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class HeldSalesDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly HeldSaleService _heldSaleService;

        public List<CartItem>? ResumedCart { get; private set; }

        public HeldSalesDialog()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _heldSaleService = new HeldSaleService(_db);

            LoadHeldSales();
        }

        private async void LoadHeldSales()
        {
            var sales = await _heldSaleService.GetActiveHeldSalesAsync();
            DgHeldSales.ItemsSource = sales;
        }

        private async void BtnResume_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgHeldSales.SelectedItem as HeldSale;
            if (selected == null)
            {
                MessageBox.Show("Please select a held sale cart to resume.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ResumedCart = await _heldSaleService.ResumeHeldSaleAsync(selected.Id);
            if (ResumedCart != null)
            {
                DialogResult = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
