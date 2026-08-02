using ShopPro.Hardware;
using ShopPro.Core.Services;
using ShopPro.Core.Models;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Text;
using System.Runtime.InteropServices;

namespace ShopPro.Tests
{
    public class FakeWin32SpoolerApi : IWin32SpoolerApi
    {
        public bool OpenPrinterReturn { get; set; } = true;
        public uint StartDocReturn { get; set; } = 1001;
        public bool StartPageReturn { get; set; } = true;
        public bool WritePrinterReturn { get; set; } = true;
        public int SimulateBytesWritten { get; set; } = -1;

        public bool OpenPrinterCalled { get; private set; }
        public bool ClosePrinterCalled { get; private set; }
        public bool StartDocCalled { get; private set; }
        public bool EndDocCalled { get; private set; }
        public bool StartPageCalled { get; private set; }
        public bool EndPageCalled { get; private set; }
        public bool WritePrinterCalled { get; private set; }

        public bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault)
        {
            OpenPrinterCalled = true;
            if (!OpenPrinterReturn) { phPrinter = IntPtr.Zero; return false; }
            phPrinter = new IntPtr(12345);
            return true;
        }

        public bool ClosePrinter(IntPtr hPrinter) { ClosePrinterCalled = true; return true; }

        public uint StartDocPrinter(IntPtr hPrinter, int level, WinSpoolPrinter.DOCINFOW di)
        {
            StartDocCalled = true;
            return StartDocReturn;
        }

        public bool EndDocPrinter(IntPtr hPrinter) { EndDocCalled = true; return true; }
        public bool StartPagePrinter(IntPtr hPrinter) { StartPageCalled = true; return StartPageReturn; }
        public bool EndPagePrinter(IntPtr hPrinter) { EndPageCalled = true; return true; }

        public bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten)
        {
            WritePrinterCalled = true;
            if (!WritePrinterReturn) { dwWritten = 0; return false; }
            dwWritten = SimulateBytesWritten >= 0 ? SimulateBytesWritten : dwCount;
            return true;
        }

        public int GetLastError() => 0;
    }

    public class EscPosPrinterServiceTests
    {
        private ShopDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var db = new ShopDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();
            DbInitializer.Initialize(db);
            return db;
        }

        // --- Receipt Sanitizer Tests ---

        [Fact]
        public void ReceiptSanitizer_StripsEscPosControlBytesAndLineBreaks()
        {
            string untrusted = "Super\x1bMart\x00 Store\r\nName\x1d";
            string sanitized = ReceiptSanitizer.SanitizeLineText(untrusted, 48);

            Assert.Equal("SuperMart  Store Name", sanitized);
            Assert.DoesNotContain("\x1b", sanitized);
            Assert.DoesNotContain("\x1d", sanitized);
            Assert.DoesNotContain("\x00", sanitized);
            Assert.DoesNotContain("\n", sanitized);
        }

        [Fact]
        public void ReceiptSanitizer_TruncatesToMaxLength()
        {
            string long_text = new string('A', 100);
            string result = ReceiptSanitizer.SanitizeLineText(long_text, 20);
            Assert.Equal(20, result.Length);
        }

        // --- Financial Validation ---

        [Fact]
        public async Task PrintReceiptWithStatus_NegativeAmountsNonRefund_ReturnsFailure()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-NEG-01",
                Subtotal = -100.00m,
                Total = -100.00m,
                AmountPaid = -100.00m,
                IsRefundOrCredit = false
            };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "POS-80");
            Assert.False(result.Success);
            Assert.Contains("Invalid negative amounts", result.Message);
        }

        // --- ESC/POS Byte Stream Tests ---

        [Fact]
        public void BuildEscPosByteStream_GeneratesCorrectControlBytes()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-ESC-101",
                CashierName = "Cashier 1",
                Subtotal = 100.00m, Discount = 10.00m,
                CgstAmount = 8.10m, SgstAmount = 8.10m,
                Total = 106.20m, AmountPaid = 200.00m, ChangeDue = 93.80m,
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Maggi 280g\nPack", Quantity = 2, LineTotal = 96.00m }
                }
            };

            var bytes = printer.BuildEscPosByteStream(receipt, printer.Config);
            Assert.True(bytes.Length > 0);

            // ESC @ init
            Assert.Equal(0x1B, bytes[0]);
            Assert.Equal(0x40, bytes[1]);

            // ESC a 1 center
            Assert.Equal(0x1B, bytes[2]);
            Assert.Equal(0x61, bytes[3]);
            Assert.Equal(0x01, bytes[4]);

            // ESC E 1 bold on
            Assert.Equal(0x1B, bytes[5]);
            Assert.Equal(0x45, bytes[6]);
            Assert.Equal(0x01, bytes[7]);

            // GS V 66 0 partial cut at end
            int len = bytes.Length;
            Assert.Equal(0x1D, bytes[len - 4]);
            Assert.Equal(0x56, bytes[len - 3]);
            Assert.Equal(0x42, bytes[len - 2]);
            Assert.Equal(0x00, bytes[len - 1]);

            // GST breakdown present in byte stream text
            var text = Encoding.ASCII.GetString(bytes);
            Assert.Contains("CGST Tax:", text);
            Assert.Contains("SGST Tax:", text);
            Assert.Contains("Rs. 106.20", text); // Grand total
            Assert.Contains("Rs. 200.00", text); // Amount paid
            Assert.Contains("Rs. 93.80", text);  // Change due

            // Item line-break was sanitised
            Assert.DoesNotContain("Maggi 280g\nPack", text);
            Assert.Contains("Maggi 280gPack", text);
        }

        // --- Currency Encoding ---

        [Fact]
        public void BuildEscPosByteStream_CurrencyUsesRsNotQuestionMark()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-CURR-001",
                Total = 150.00m, AmountPaid = 200.00m, ChangeDue = 50.00m,
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Test Item", Quantity = 1, LineTotal = 150.00m }
                }
            };
            var bytes = printer.BuildEscPosByteStream(receipt, printer.Config);
            var textStr = Encoding.ASCII.GetString(bytes);
            Assert.Contains("Rs. 150.00", textStr);
            Assert.DoesNotContain("?150.00", textStr);
        }

        // --- Cash Drawer ---

        [Fact]
        public async Task OpenCashDrawerAsync_TransmitsExactPulseBytes()
        {
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);

            var success = await printer.OpenCashDrawerAsync("EPSON-TM-T88");

            Assert.True(success);
            Assert.Equal("EPSON-TM-T88", fakeTransport.LastPrinterName);
            var expectedBytes = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
            Assert.Equal(expectedBytes, fakeTransport.LastSentBytes);
        }

        // --- Printer Target Resolution ---

        [Fact]
        public async Task PrintReceiptWithStatus_EmptyPrinterName_ReturnsFailureWithPreview()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-EMPTY-01", Total = 100.00m };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "");
            Assert.False(result.Success);
            Assert.Contains("No printer configured or selected", result.Message);
        }

        [Fact]
        public async Task PrintReceiptWithStatus_UnavailablePrinter_ReturnsFailure()
        {
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = false };
            var printer = new EscPosPrinterService(fakeTransport);
            var receipt = new ReceiptData { InvoiceNumber = "INV-OFF-01", Total = 500.00m };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "UNPLUGGED-PRINTER");
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task PrintReceiptAsync_RequiresExplicitPrinterName()
        {
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);
            var receipt = new ReceiptData { InvoiceNumber = "INV-EXPLICIT", Total = 100.00m };

            // Empty name → false
            bool emptyResult = await printer.PrintReceiptAsync(receipt, "");
            Assert.False(emptyResult);

            // Valid name → true
            bool validResult = await printer.PrintReceiptAsync(receipt, "MY-PRINTER");
            Assert.True(validResult);
            Assert.Equal("MY-PRINTER", fakeTransport.LastPrinterName);
        }

        // --- Reprint from SQLite ---

        [Fact]
        public async Task ReprintLastReceiptAsync_ReconstructsFromDbAndTransmits()
        {
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 2000.00m);
            Assert.NotNull(sale);

            var reprintResult = await pos.ReprintLastReceiptAsync(sale.Id, printer, "TARGET-PRINTER");
            Assert.True(reprintResult.Success);
            Assert.Equal("TARGET-PRINTER", fakeTransport.LastPrinterName);
            Assert.NotNull(fakeTransport.LastSentBytes);

            var byteText = Encoding.ASCII.GetString(fakeTransport.LastSentBytes);
            Assert.Contains(sale.InvoiceNumber, byteText);
        }

        // --- Audit Log ---

        [Fact]
        public async Task TryPrintCheckoutReceiptAsync_CreatesAuditLogEntry()
        {
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 1, UnitPrice = 100.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 200.00m);
            Assert.NotNull(sale);

            await pos.TryPrintCheckoutReceiptAsync(sale, printer, "AUDIT-PRINTER");

            var auditLogs = await db.AuditLogs.Where(a => a.Action == "PRINT_RECEIPT_ATTEMPT").ToListAsync();
            Assert.True(auditLogs.Count > 0);
            Assert.Contains(sale.InvoiceNumber, auditLogs[0].Details);
            Assert.Contains("AUDIT-PRINTER", auditLogs[0].Details);
        }

        // --- Serial Printer Transport ---

        [Fact]
        public void SerialTransport_OpenFailure_ReturnsFailure()
        {
            var fakeSerial = new FakeSerialPortDevice { SimulateOpenSuccess = false };
            var transport = new WindowsSpoolerAndSerialTransport(serialDevice: fakeSerial);
            var result = transport.SendBytes("COM1", new byte[] { 0x1B, 0x40 });
            Assert.False(result.Success);
            Assert.Contains("Serial port error (COM1)", result.Message);
        }

        [Fact]
        public void SerialTransport_WriteFailure_ClosesPortInFinally()
        {
            var fakeSerial = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            var transport = new WindowsSpoolerAndSerialTransport(serialDevice: fakeSerial);
            var result = transport.SendBytes("COM1", new byte[] { 0x1B, 0x40 });
            Assert.False(result.Success);
            Assert.Equal(1, fakeSerial.CloseCount);
        }

        // --- Win32 Spooler ---

        [Fact]
        public void WinSpoolPrinter_StartDocFailure_CleansUp()
        {
            var fakeApi = new FakeWin32SpoolerApi { OpenPrinterReturn = true, StartDocReturn = 0 };
            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", new byte[] { 0x1B, 0x40 }, fakeApi);
            Assert.False(result.Success);
            Assert.Contains("StartDocPrinter failed", result.Message);
            Assert.True(fakeApi.ClosePrinterCalled);
            Assert.False(fakeApi.StartPageCalled);
        }

        [Fact]
        public void WinSpoolPrinter_StartPageFailure_CleansUpDocAndPrinter()
        {
            var fakeApi = new FakeWin32SpoolerApi { StartDocReturn = 1001, StartPageReturn = false };
            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", new byte[] { 0x1B, 0x40 }, fakeApi);
            Assert.False(result.Success);
            Assert.Contains("StartPagePrinter failed", result.Message);
            Assert.True(fakeApi.EndDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
        }

        [Fact]
        public void WinSpoolPrinter_WriteFailure_CleansUpAll()
        {
            var fakeApi = new FakeWin32SpoolerApi { WritePrinterReturn = false };
            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", new byte[] { 0x1B, 0x40 }, fakeApi);
            Assert.False(result.Success);
            Assert.Contains("WritePrinter failed", result.Message);
            Assert.True(fakeApi.EndPageCalled);
            Assert.True(fakeApi.EndDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
        }

        [Fact]
        public void WinSpoolPrinter_PartialWrite_ReturnsFailure()
        {
            var fakeApi = new FakeWin32SpoolerApi { SimulateBytesWritten = 1 };
            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", new byte[] { 0x1B, 0x40 }, fakeApi);
            Assert.False(result.Success);
            Assert.Contains("Partial write", result.Message);
        }

        [Fact]
        public void IsComPort_ValidatesStrictIdentifiers()
        {
            Assert.True(WindowsSpoolerAndSerialTransport.IsComPort("COM1"));
            Assert.True(WindowsSpoolerAndSerialTransport.IsComPort("com99"));
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COMPANY_PRINTER"));
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COM"));
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COM100"));
        }

        // --- Skipped Physical Hardware Tests ---

        [Fact(Skip = "Requires physical ESC/POS thermal printer via USB/COM")]
        public async Task Physical_PrintReceipt()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-HW-TEST", Total = 100.00m };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "POS-80");
            Assert.True(result.Success);
        }

        [Fact(Skip = "Requires physical RJ11 cash drawer attached to thermal printer")]
        public async Task Physical_OpenCashDrawer()
        {
            var printer = new EscPosPrinterService();
            var success = await printer.OpenCashDrawerAsync("POS-80");
            Assert.True(success);
        }
    }
}
