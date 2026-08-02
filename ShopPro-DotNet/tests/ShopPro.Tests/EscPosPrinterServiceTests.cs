using ShopPro.Hardware;
using ShopPro.Core.Services;
using ShopPro.Core.Models;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Text;

namespace ShopPro.Tests
{
    public class FakeWin32SpoolerApi : IWin32SpoolerApi
    {
        public bool OpenPrinterReturn { get; set; } = true;
        public bool StartDocReturn { get; set; } = true;
        public bool StartPageReturn { get; set; } = true;
        public bool WritePrinterReturn { get; set; } = true;
        public int SimulateBytesWritten { get; set; } = -1; // -1 means write full bytes

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
            if (!OpenPrinterReturn)
            {
                phPrinter = IntPtr.Zero;
                return false;
            }
            phPrinter = new IntPtr(12345);
            return true;
        }

        public bool ClosePrinter(IntPtr hPrinter)
        {
            ClosePrinterCalled = true;
            return true;
        }

        public bool StartDocPrinter(IntPtr hPrinter, int level, WinSpoolPrinter.DOCINFOW di)
        {
            StartDocCalled = true;
            return StartDocReturn;
        }

        public bool EndDocPrinter(IntPtr hPrinter)
        {
            EndDocCalled = true;
            return true;
        }

        public bool StartPagePrinter(IntPtr hPrinter)
        {
            StartPageCalled = true;
            return StartPageReturn;
        }

        public bool EndPagePrinter(IntPtr hPrinter)
        {
            EndPageCalled = true;
            return true;
        }

        public bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten)
        {
            WritePrinterCalled = true;
            if (!WritePrinterReturn)
            {
                dwWritten = 0;
                return false;
            }
            dwWritten = SimulateBytesWritten >= 0 ? SimulateBytesWritten : dwCount;
            return true;
        }

        public int GetLastError()
        {
            return 0;
        }
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

        [Fact]
        public async Task PrintReceiptWithStatus_EmptyPrinterName_ReturnsSuccessFalseWithPreviewMessage()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-EMPTY-01", Total = 100.00m };

            // Act: Empty printer name passed
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "");

            // Assert: Must return Success = false (Not a successful physical print)
            Assert.False(result.Success);
            Assert.Contains("No printer configured or selected", result.Message);
            Assert.NotNull(result.OutputPath);
        }

        [Fact]
        public void WinSpoolPrinter_StartDocFailure_CleansUpAndReturnsFailure()
        {
            var fakeApi = new FakeWin32SpoolerApi { OpenPrinterReturn = true, StartDocReturn = false };
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("StartDocPrinter failed", result.Message);
            Assert.True(fakeApi.OpenPrinterCalled);
            Assert.True(fakeApi.StartDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled); // Cleaned up
            Assert.False(fakeApi.StartPageCalled);
        }

        [Fact]
        public void WinSpoolPrinter_StartPageFailure_CleansUpDocAndPrinterHandles()
        {
            var fakeApi = new FakeWin32SpoolerApi { OpenPrinterReturn = true, StartDocReturn = true, StartPageReturn = false };
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("StartPagePrinter failed", result.Message);
            Assert.True(fakeApi.StartDocCalled);
            Assert.True(fakeApi.StartPageCalled);
            Assert.True(fakeApi.EndDocCalled); // Cleaned up
            Assert.True(fakeApi.ClosePrinterCalled); // Cleaned up
        }

        [Fact]
        public void WinSpoolPrinter_WritePrinterFailure_FreesMemoryAndCleansUpAllHandles()
        {
            var fakeApi = new FakeWin32SpoolerApi { WritePrinterReturn = false };
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("WritePrinter failed", result.Message);
            Assert.True(fakeApi.EndPageCalled);
            Assert.True(fakeApi.EndDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
        }

        [Fact]
        public void WinSpoolPrinter_PartialWrite_ReturnsFailureMessage()
        {
            var fakeApi = new FakeWin32SpoolerApi { SimulateBytesWritten = 1 }; // Sent 2 bytes, wrote 1
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("Partial write to spooler", result.Message);
            Assert.True(fakeApi.EndPageCalled);
            Assert.True(fakeApi.EndDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
        }

        [Fact]
        public void IsComPort_ValidatesStrictComPortIdentifiers()
        {
            Assert.True(WindowsSpoolerAndSerialTransport.IsComPort("COM1"));
            Assert.True(WindowsSpoolerAndSerialTransport.IsComPort("com99"));
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COMPANY_PRINTER")); // Not routed to serial
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COM"));
        }

        [Fact]
        public void SendBytes_UnavailableSerialPort_ReturnsFailureMessage()
        {
            var transport = new WindowsSpoolerAndSerialTransport();
            var result = transport.SendBytes("COM99", new byte[] { 0x1B, 0x40 });

            Assert.False(result.Success);
            Assert.Contains("Serial port error", result.Message);
        }

        [Fact(Skip = "Hardware-only verification: requires physical ESC/POS thermal printer attached via USB/COM port")]
        public async Task PrintReceipt_PhysicalHardware_PrintsOnRealThermalPrinter()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-HARDWARE-TEST", Total = 100.00m };
            var result = await printer.PrintReceiptWithStatusAsync(receipt, "POS-80");
            Assert.True(result.Success);
        }

        [Fact(Skip = "Hardware-only verification: requires physical RJ11 cash drawer attached to thermal printer")]
        public async Task OpenCashDrawer_PhysicalHardware_KicksPhysicalDrawer()
        {
            var printer = new EscPosPrinterService();
            var success = await printer.OpenCashDrawerAsync("POS-80");
            Assert.True(success);
        }
    }
}
