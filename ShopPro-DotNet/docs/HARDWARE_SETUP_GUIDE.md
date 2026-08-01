# ShopPro Retail POS — Hardware Setup & Configuration Guide

This manual details how to connect, configure, and troubleshoot retail POS peripherals with **ShopPro Retail POS**.

---

## 🖨️ 1. ESC/POS Thermal Receipt Printers & Cash Drawers

### 1.1 Supported Thermal Printers
- **80mm Thermal Receipt Printers** (Epson TM-T82, TVS RP-3160, Xprinter 80mm)
- **58mm Thermal Receipt Printers** (58mm USB / POS-58 Printers)

### 1.2 Thermal Printer Configuration
1. Connect printer via USB and install Windows vendor driver.
2. In ShopPro, navigate to **Settings -> Hardware Settings**.
3. Select your installed thermal printer from the dropdown menu.
4. Select paper width (**80mm** or **58mm**).
5. Click **Test Print** to verify logo, split GST formatting, and UPI QR code rendering.

### 1.3 RJ12 Cash Drawer Pulse Kick
- Connect the cash drawer RJ12 cable directly to the thermal printer's `DK` (Drawer Kick) port.
- ShopPro automatically transmits the ESC/POS pulse sequence (`DLE DC4 1 1 1`) to trigger cash drawer opening upon cash checkout.

---

## 📟 2. Customer VFD Pole Displays

- Connect 20x2 VFD customer pole display via USB or RS-232 COM port.
- Select COM port (e.g. `COM3`, Baud Rate `9600`) in **Hardware Settings**.
- ShopPro updates customer pole display in real-time with line item names and total due.

---

## ⚖️ 3. RS-232 Serial Weighing Scales

- Connect RS-232 serial scale to PC COM port.
- Configure NMEA serial protocol in **Hardware Settings** (`COM1`, `9600` baud rate, 8 data bits).
- Weighing scale reading automatically populates cart line item weights during checkout.
