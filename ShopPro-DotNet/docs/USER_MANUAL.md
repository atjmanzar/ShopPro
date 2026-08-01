# ShopPro Retail POS — Commercial User Manual & Operations Guide

Welcome to **ShopPro Retail POS**, a native Windows-first offline retail management and Point-of-Sale desktop application.

---

## 🛒 1. Point of Sale (POS) Checkout Operations

### 1.1 Barcode Scanning & Item Addition
- Connect any USB keyboard-wedge barcode scanner to your workstation.
- On the **POS Checkout View**, scan the product barcode. The item will automatically be added to the active cart with tax and pricing pre-calculated.
- Alternatively, type the SKU or Barcode manually into the search box and press `Enter`.

### 1.2 Quantity Override & Line Item Notes
- Double-click any item quantity in the cart or use the `+` / `-` buttons to adjust item count.
- Click **Add Note** on a line item to attach special instructions or warranty terms.

### 1.3 Discounts & Split GST Tax Calculation
- **Line Discounts**: Apply a percentage (%) or fixed amount (₹) discount per line item.
- **Invoice Discounts**: Apply a global invoice discount to the total bill.
- **Split GST**: ShopPro automatically calculates Split GST (**CGST 50%**, **SGST 50%**, or **IGST 100%**) based on customer tax location.

### 1.4 Sale Hold & Resume
- Click **Hold Sale (F6)** to pause an active cart (e.g., customer forgot an item).
- Click **Held Sales (F7)** to view held carts and resume checkout instantly.

### 1.5 Split Payments & Checkout
- ShopPro supports **Cash**, **Card**, **UPI QR Code**, and **Store Credit** payment methods.
- Click **Process Checkout (F10)** to complete the transaction, print the thermal receipt, and kick the cash drawer.

---

## 📦 2. Inventory & Stock Management

### 2.1 Product Master & Stock Movements
- Navigate to **Inventory Management**.
- Add or Edit products with SKU, Barcode, Retail Price, Cost Price, GST Tax Rate, and Low Stock Alert threshold.
- Record **Stock In** (restock) or **Stock Out** (damaged / expired items) with audit log remarks.

### 2.2 Category Management & Deletion Safety
- Organize products into categories (e.g., Grocery, Beverages, Apparel).
- Categories linked to active products are protected from accidental deletion.

---

## 👥 3. Customer Accounts & Loyalty Rewards

### 3.1 Customer Accounts & Credit Ledger
- Register customers with Name, Phone, Email, Address, and **GSTIN Tax Number**.
- Track customer credit balances and accept bill payments via the **Customer Ledger**.

### 3.2 Tiered Loyalty Points Earning & Redemption
- Customers earn loyalty points on every purchase based on their membership tier (**Bronze 1.0x**, **Silver 1.5x**, **Gold 2.0x**, **Platinum 3.0x**).
- Redeem accumulated loyalty points at checkout for direct cash discounts (1 Point = ₹1.00 Discount).

---

## 📊 4. Reports & Business Analytics

- Navigate to **Sales & Financial Reports**.
- View **Net Profit & Loss**: `Gross Revenue - COGS - Store Expenses = Net Profit`.
- Export reports to **PDF Document** or **Excel Spreadsheet** with one click.

---

## 🛡️ 5. Data Protection & Diagnostics

- **Database Backups**: Click **Create Backup Now** to create timestamped database archives in `%LOCALAPPDATA%\ShopPro\Backups\`.
- **Integrity Check**: Run **PRAGMA Integrity Check** to verify SQLite database health.
- **System Health**: View RAM footprint, free disk space, and query ping latency under **Diagnostics & Support**.
