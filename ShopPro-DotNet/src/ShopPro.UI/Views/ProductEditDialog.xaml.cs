using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class ProductEditDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly ProductManagementService _productService;
        public Product Product { get; private set; }

        public ProductEditDialog(Product? productToEdit = null)
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _productService = new ProductManagementService(_db);

            Product = productToEdit ?? new Product();
            LoadCategoriesAndPopulate();
        }

        private async void LoadCategoriesAndPopulate()
        {
            var categories = await _productService.GetCategoriesAsync();
            CmbCategory.ItemsSource = categories;

            if (Product.Id > 0)
            {
                TxtDialogTitle.Text = "Edit Product Master";
                TxtSku.Text = Product.Sku;
                TxtBarcode.Text = Product.Barcode;
                TxtName.Text = Product.Name;
                TxtPrice.Text = Product.Price.ToString("F2");
                TxtCost.Text = Product.Cost.ToString("F2");
                TxtTaxRate.Text = Product.TaxRate.ToString("F2");
                TxtMinStock.Text = Product.MinStockAlert.ToString();
                CmbCategory.SelectedValue = Product.CategoryId;
            }
            else
            {
                TxtDialogTitle.Text = "Create New Product";
                if (categories.Any()) CmbCategory.SelectedIndex = 0;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtPrice.Text, out var price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid selling price greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal.TryParse(TxtCost.Text, out var cost);
            decimal.TryParse(TxtTaxRate.Text, out var taxRate);
            int.TryParse(TxtMinStock.Text, out var minStock);

            Product.Sku = TxtSku.Text.Trim();
            Product.Barcode = TxtBarcode.Text.Trim();
            Product.Name = TxtName.Text.Trim();
            Product.Price = price;
            Product.Cost = cost;
            Product.TaxRate = taxRate;
            Product.MinStockAlert = minStock;

            if (CmbCategory.SelectedValue is int catId)
            {
                Product.CategoryId = catId;
            }

            var result = await _productService.SaveProductAsync(Product);
            if (result.Success)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(result.Message, "Product Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
