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
        public uint StartDocReturn { get; set; } = 1001; // Job ID 1001
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

        public uint StartDocPrinter(IntPtr hPrinter, int level, WinSpoolPrinter.DOCINFOW di)
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
        public void BuildEscPosByteStream_GeneratesExactEscPosCommandBytes()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-ESC-101",
                CashierName = "Cashier 1",
                Subtotal = 100.00m,
                Discount = 10.00m,
                Tax = 16.20m,
                Total = 106.20m,
                AmountPaid = 200.00m,
                ChangeDue = 93.80m,
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ItemName = "Maggi 280g", Quantity = 2, LineTotal = 96.00m }
                }
            };

            var bytes = printer.BuildEscPosByteStream(receipt, printer.Config);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Command 1: ESC @ (0x1B, 0x40) Initialize Printer
            Assert.Equal(0x1B, bytes[0]);
            Assert.Equal(0x40, bytes[1]);

            // Command 2: ESC a 1 (0x1B, 0x61, 0x01) Center Alignment
            Assert.Equal(0x1B, bytes[2]);
            Assert.Equal(0x61, bytes[3]);
            Assert.Equal(0x01, bytes[4]);

            // Command 3: ESC E 1 (0x1B, 0x45, 0x01) Enable Bold Mode
            Assert.Equal(0x1B, bytes[5]);
            Assert.Equal(0x45, bytes[6]);
            Assert.Equal(0x01, bytes[7]);

            // Command 4: GS V 66 0 (0x1D, 0x56, 0x42, 0x00) Partial Paper Cut at end of stream
            int len = bytes.Length;
            Assert.Equal(0x1D, bytes[len - 4]);
            Assert.Equal(0x56, bytes[len - 3]);
            Assert.Equal(0x42, bytes[len - 2]);
            Assert.Equal(0x00, bytes[len - 1]);
        }

        [Fact]
        public async Task OpenCashDrawerAsync_TransmitsExactEscPosPulseBytes()
        {
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);

            var success = await printer.OpenCashDrawerAsync("EPSON-TM-T88");

            Assert.True(success);
            Assert.Equal("EPSON-TM-T88", fakeTransport.LastPrinterName);
            Assert.NotNull(fakeTransport.LastSentBytes);

            // Verify ESC p m t1 t2 drawer kick pulse bytes: [0x1B, 0x70, 0x00, 0x19, 0xFA]
            var expectedBytes = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
            Assert.Equal(expectedBytes, fakeTransport.LastSentBytes);
        }

        [Fact]
        public void BuildEscPosByteStream_CurrencyEncoding_UsesRsWithoutQuestionMarkSubstitution()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData
            {
                InvoiceNumber = "INV-CURR-001",
                Total = 150.00m,
                AmountPaid = 200.00m,
                ChangeDue = 50.00m,
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

        [Fact]
        public async Task PrintReceiptWithStatus_EmptyPrinterName_ReturnsSuccessFalseWithPreviewMessage()
        {
            var printer = new EscPosPrinterService();
            var receipt = new ReceiptData { InvoiceNumber = "INV-EMPTY-01", Total = 100.00m };

            var result = await printer.PrintReceiptWithStatusAsync(receipt, "");

            Assert.False(result.Success);
            Assert.Contains("No printer configured or selected", result.Message);
            Assert.NotNull(result.OutputPath);
        }

        [Fact]
        public async Task PrintReceiptWithStatus_UnavailablePrinter_ReturnsFailureResult()
        {
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = false };
            var printer = new EscPosPrinterService(fakeTransport);

            var receipt = new ReceiptData { InvoiceNumber = "INV-OFFLINE-01", Total = 500.00m };

            var result = await printer.PrintReceiptWithStatusAsync(receipt, "UNPLUGGED-PRINTER");

            Assert.False(result.Success);
            Assert.Contains("Printer not found — check connection and retry", result.Message);
        }

        [Fact]
        public async Task ReprintLastReceiptAsync_ReconstructsSaleAndTransmitsToTransport()
        {
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var fakeTransport = new FakePrinterTransport { SimulateAvailability = true, SimulateWriteSuccess = true };
            var printer = new EscPosPrinterService(fakeTransport);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 2000.00m);
            Assert.NotNull(sale);

            var reprintResult = await pos.ReprintLastReceiptAsync(sale.Id, printer, "TARGET-THERMAL-PRINTER");

            Assert.True(reprintResult.Success);
            Assert.Equal("TARGET-THERMAL-PRINTER", fakeTransport.LastPrinterName);
            Assert.NotNull(fakeTransport.LastSentBytes);

            var byteText = Encoding.ASCII.GetString(fakeTransport.LastSentBytes);
            Assert.Contains(sale.InvoiceNumber, byteText);
            Assert.Contains("Rs. 1180.00", byteText); // Grand total
            Assert.Contains("Rs. 820.00", byteText);  // Change due
        }

        [Fact]
        public void SerialPrinterTransport_OpenFailure_ReturnsFailureResultDeterministically()
        {
            var fakeSerial = new FakeSerialPortDevice { SimulateOpenSuccess = false };
            var transport = new WindowsSpoolerAndSerialTransport(serialDevice: fakeSerial);

            var result = transport.SendBytes("COM1", new byte[] { 0x1B, 0x40 });

            Assert.False(result.Success);
            Assert.Contains("Serial port error (COM1)", result.Message);
        }

        [Fact]
        public void SerialPrinterTransport_WriteFailure_CleansUpAndReturnsFailureDeterministically()
        {
            var fakeSerial = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            var transport = new WindowsSpoolerAndSerialTransport(serialDevice: fakeSerial);

            var result = transport.SendBytes("COM1", new byte[] { 0x1B, 0x40 });

            Assert.False(result.Success);
            Assert.Contains("Serial port error (COM1)", result.Message);
            Assert.Equal(1, fakeSerial.CloseCount); // Verified port closed in finally block
        }

        [Fact]
        public void WinSpoolPrinter_StartDocFailure_CleansUpAndReturnsFailure()
        {
            var fakeApi = new FakeWin32SpoolerApi { OpenPrinterReturn = true, StartDocReturn = 0 }; // 0 indicates Job ID failure
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("StartDocPrinter failed", result.Message);
            Assert.True(fakeApi.OpenPrinterCalled);
            Assert.True(fakeApi.StartDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
            Assert.False(fakeApi.StartPageCalled);
        }

        [Fact]
        public void WinSpoolPrinter_StartPageFailure_CleansUpDocAndPrinterHandles()
        {
            var fakeApi = new FakeWin32SpoolerApi { OpenPrinterReturn = true, StartDocReturn = 1001, StartPageReturn = false };
            var bytes = new byte[] { 0x1B, 0x40 };

            var result = WinSpoolPrinter.SendBytesToPrinter("POS-80", bytes, fakeApi);

            Assert.False(result.Success);
            Assert.Contains("StartPagePrinter failed", result.Message);
            Assert.True(fakeApi.StartDocCalled);
            Assert.True(fakeApi.StartPageCalled);
            Assert.True(fakeApi.EndDocCalled);
            Assert.True(fakeApi.ClosePrinterCalled);
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
            var fakeApi = new FakeWin32SpoolerApi { SimulateBytesWritten = 1 };
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
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COMPANY_PRINTER"));
            Assert.False(WindowsSpoolerAndSerialTransport.IsComPort("COM"));
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
