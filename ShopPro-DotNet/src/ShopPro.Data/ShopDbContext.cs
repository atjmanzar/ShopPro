using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Data
{
    public class ShopDbContext : DbContext
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<HeldSale> HeldSales => Set<HeldSale>();
        public DbSet<HardwareSetting> HardwareSettings => Set<HardwareSetting>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<CashShift> CashShifts => Set<CashShift>();
        public DbSet<LicenseInformation> Licenses => Set<LicenseInformation>();

        public string DbPath { get; }

        public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
        {
            DbPath = string.Empty;
        }

        public ShopDbContext(string dbPath)
        {
            DbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                var folder = Environment.SpecialFolder.LocalApplicationData;
                var path = Environment.GetFolderPath(folder);
                var dir = Path.Combine(path, "ShopPro");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                var sqlitePath = string.IsNullOrEmpty(DbPath) ? Path.Combine(dir, "shoppro.db") : DbPath;
                options.UseSqlite($"Data Source={sqlitePath}");
            }
        }

        public void EnableWalMode()
        {
            try
            {
                Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
                Database.ExecuteSqlRaw("PRAGMA cache_size=10000;");
            }
            catch
            {
                // Fallback for in-memory SQLite
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<HardwareSetting>()
                .HasIndex(h => h.SettingKey)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(p => p.PoNumber)
                .IsUnique();
        }
    }
}
