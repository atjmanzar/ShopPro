using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class CategoryManagementDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly ProductManagementService _productService;
        private Category? _selectedCategory;

        public CategoryManagementDialog()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _productService = new ProductManagementService(_db);

            LoadCategories();
        }

        private async void LoadCategories()
        {
            var categories = await _productService.GetCategoriesAsync();
            DgCategories.ItemsSource = categories;
        }

        private void DgCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCategory = DgCategories.SelectedItem as Category;
            if (_selectedCategory != null)
            {
                TxtTitle.Text = "Edit Category";
                TxtCatName.Text = _selectedCategory.Name;
                TxtCatDesc.Text = _selectedCategory.Description;
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedCategory = null;
            TxtTitle.Text = "Create Category";
            TxtCatName.Clear();
            TxtCatDesc.Clear();
            DgCategories.UnselectAll();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var cat = _selectedCategory ?? new Category();
            cat.Name = TxtCatName.Text.Trim();
            cat.Description = TxtCatDesc.Text.Trim();

            var result = await _productService.SaveCategoryAsync(cat);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Category Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories();
                BtnNew_Click(sender, e);
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null)
            {
                MessageBox.Show("Please select a category to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = await _productService.DeleteCategoryAsync(_selectedCategory.Id);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Category Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories();
                BtnNew_Click(sender, e);
            }
            else
            {
                MessageBox.Show(result.Message, "Delete Protection Enforced", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}
