# ShopPro Retail POS — Production Deployment Guide

This guide details the procedure for installing and deploying **ShopPro Retail POS** on clean Windows 10 and Windows 11 commercial desktop PCs.

---

## 💻 1. System Requirements

- **Operating System**: Windows 10 (64-bit) or Windows 11 (64-bit)
- **Processor**: Intel Core i3 / AMD Ryzen 3 or higher
- **RAM**: 4 GB RAM minimum (8 GB recommended)
- **Disk Space**: 500 MB free storage for application & database
- **Framework**: .NET 8 Desktop Runtime (x64)

---

## 🚀 2. Installation Steps

### Step 1: Download & Run Installer
1. Download the official installer: `ShopPro_Setup_v1.0.0.exe`.
2. Double-click to launch the installer wizard.
3. Select installation directory (Default: `C:\Program Files\ShopPro Retail POS\`).
4. Click **Install**. The setup wizard will place desktop and start menu shortcuts.

### Step 2: Database Initialization
- ShopPro uses an embedded **SQLite 3** database located at `%LOCALAPPDATA%\ShopPro\shoppro.db`.
- On initial launch, ShopPro automatically creates the database file, executes migrations, and seeds default accounts and categories.

### Step 3: Initial Login Credentials
- **Admin Username**: `admin`
- **Admin Password**: `admin123`
- *Note: You will be prompted to change the default password upon initial login.*

---

## 🔒 3. Commercial License Activation

1. Launch ShopPro and navigate to **Settings -> Licensing Console**.
2. Note your unique **Machine Hardware Fingerprint** (`HW-XXXX-XXXX-XXXX`).
3. Click **Enter License Key** and input your commercial license key (`PRO-XXXX-XXXX`).
4. Click **Activate License**.

---

## 🛠️ 4. Maintenance & Backups

- Scheduled automatic database backups are stored daily in `%LOCALAPPDATA%\ShopPro\Backups\`.
- Use the **Diagnostics & Support** tool to execute SQLite `VACUUM` and `REINDEX` maintenance monthly.
