using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class ProductManagementService
    {
        private readonly ShopDbContext _db;

        public ProductManagementService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<(bool Success, string Message)> SaveCategoryAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return (false, "Category Name cannot be empty.");

            var existing = await _db.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower() && c.Id != category.Id);
            if (existing != null)
                return (false, $"Category '{category.Name}' already exists.");

            if (category.Id == 0)
            {
                category.Name = category.Name.Trim();
                _db.Categories.Add(category);
            }
            else
            {
                category.Name = category.Name.Trim();
                _db.Categories.Update(category);
            }

            await _db.SaveChangesAsync();
            return (true, "Category saved successfully.");
        }

        /// <summary>
        /// Category Delete Protection: Prevents deleting categories that have active products assigned.
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId)
        {
            var category = await _db.Categories.FindAsync(categoryId);
            if (category == null) return (false, "Category not found.");

            var productCount = await _db.Products.CountAsync(p => p.CategoryId == categoryId && p.IsActive);
            if (productCount > 0)
            {
                return (false, $"Cannot delete category '{category.Name}' because it has {productCount} active product(s) assigned to it.");
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return (true, $"Category '{category.Name}' deleted successfully.");
        }

        public async Task<(bool Success, string Message)> SaveProductAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name)) return (false, "Product Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(product.Barcode)) return (false, "Barcode cannot be empty.");
            if (string.IsNullOrWhiteSpace(product.Sku)) return (false, "SKU cannot be empty.");
            if (product.Price <= 0) return (false, "Selling Price must be greater than zero (Invalid Price).");
            if (product.Cost < 0) return (false, "Cost Price cannot be negative.");
            if (product.TaxRate < 0) return (false, "Tax Rate percentage cannot be negative.");
            if (product.StockQuantity < 0) return (false, "Stock Quantity cannot be negative.");

            // Check for duplicate barcode
            var existingBarcode = await _db.Products
                .FirstOrDefaultAsync(p => p.Barcode == product.Barcode.Trim() && p.Id != product.Id);
            if (existingBarcode != null)
                return (false, $"Duplicate Barcode: '{product.Barcode}' is already assigned to '{existingBarcode.Name}'.");

            // Check for duplicate SKU
            var existingSku = await _db.Products
                .FirstOrDefaultAsync(p => p.Sku == product.Sku.Trim() && p.Id != product.Id);
            if (existingSku != null)
                return (false, $"Duplicate SKU: '{product.Sku}' is already assigned to '{existingSku.Name}'.");

            product.Name = product.Name.Trim();
            product.Barcode = product.Barcode.Trim();
            product.Sku = product.Sku.Trim();
            product.Brand = product.Brand.Trim();

            if (product.Id == 0)
            {
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                _db.Products.Add(product);
            }
            else
            {
                product.UpdatedAt = DateTime.UtcNow;
                _db.Products.Update(product);
            }

            await _db.SaveChangesAsync();
            return (true, "Product saved successfully.");
        }
    }
}
