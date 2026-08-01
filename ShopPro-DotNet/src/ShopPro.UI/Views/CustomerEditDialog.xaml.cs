using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class CustomerEditDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly CustomerLedgerService _customerService;
        public Customer Customer { get; private set; }

        public CustomerEditDialog(Customer? customerToEdit = null)
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _customerService = new CustomerLedgerService(_db);

            Customer = customerToEdit ?? new Customer();
            if (Customer.Id > 0)
            {
                TxtTitle.Text = "Edit Customer Account";
                TxtName.Text = Customer.Name;
                TxtPhone.Text = Customer.Phone;
                TxtEmail.Text = Customer.Email;
                TxtAddress.Text = Customer.Address;
                TxtGstin.Text = Customer.Gstin;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Customer.Name = TxtName.Text.Trim();
            Customer.Phone = TxtPhone.Text.Trim();
            Customer.Email = TxtEmail.Text.Trim();
            Customer.Address = TxtAddress.Text.Trim();
            Customer.Gstin = TxtGstin.Text.Trim();

            var result = await _customerService.SaveCustomerAsync(Customer);
            if (result.Success)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
