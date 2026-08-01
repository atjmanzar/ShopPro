using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class CustomerManagementView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly CustomerLedgerService _customerService;

        public CustomerManagementView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _customerService = new CustomerLedgerService(_db);

            LoadCustomers();
        }

        private async void LoadCustomers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            DgCustomers.ItemsSource = customers;
        }

        private void BtnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadCustomers();
            }
        }

        private void BtnEditCustomer_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgCustomers.SelectedItem as Customer;
            if (selected == null)
            {
                MessageBox.Show("Please select a customer from the list to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CustomerEditDialog(selected);
            if (dialog.ShowDialog() == true)
            {
                LoadCustomers();
            }
        }
    }
}
