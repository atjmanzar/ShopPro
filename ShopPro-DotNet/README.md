# ShopPro Native C# / .NET 8 WPF Retail POS Solution

> **Architecture**: Clean 4-Layer Architecture + xUnit Test Suite  
> **Target Framework**: .NET 8.0 (WPF / C# 12)  
> **Database Engine**: Embedded Local SQLite (`Microsoft.EntityFrameworkCore.Sqlite`)  
> **UI Theme**: Modern Slate Dark Mode (`AppTheme.xaml`)  

---

## 📂 Solution Structure
```text
ShopPro-DotNet/
├── ShopPro.sln
├── src/
│   ├── ShopPro.Data/        (EF Core Models: Product, Category, InventoryTransaction, Sale, SaleItem, Payment, User, Customer)
│   ├── ShopPro.Core/        (PosEngine, InventoryService, AuthService, ReportService, ZXing BarcodeService)
│   ├── ShopPro.Hardware/    (KeyboardWedgeScanner Buffer, IPrinterService & EscPosPrinterService Stubs)
│   └── ShopPro.UI/          (WPF Desktop App, Slate Dark Theme, POS Checkout, Inventory, Barcodes, Reports)
└── tests/
    └── ShopPro.Tests/       (xUnit Tests for Cart Pricing, Item Discounts, Tax Rates, Stock Deduction, Auth)
```

---

## ⚡ Building & Running on Windows PC

1. Open `ShopPro.sln` in **Visual Studio 2022 (v17.9+)** or **VS Code**.
2. Restore NuGet Packages:
   ```cmd
   dotnet restore
   ```
3. Run Unit Tests:
   ```cmd
   dotnet test
   ```
4. Launch Desktop Application:
   ```cmd
   dotnet run --project src/ShopPro.UI/ShopPro.UI.csproj
   ```

---

## 🔐 Default Credentials
- **Admin Role**: `admin` / `admin123` (Access to Checkout, Inventory, Barcode Generator, Reports)
- **Cashier Role**: `cashier` / `cashier123` (Access to POS Checkout only)
