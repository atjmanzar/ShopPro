using System.Text.RegularExpressions;
using System.Drawing.Printing;
using System.IO.Ports;

namespace ShopPro.Hardware
{
    public interface IPrinterTransport
    {
        (bool Success, string Message) SendBytes(string printerNameOrPort, byte[] bytes);
        bool CheckAvailability(string printerNameOrPort);
    }

    public class FakePrinterTransport : IPrinterTransport
    {
        public bool SimulateAvailability { get; set; } = true;
        public bool SimulateWriteSuccess { get; set; } = true;
        public string LastPrinterName { get; private set; } = string.Empty;
        public byte[]? LastSentBytes { get; private set; }
        public string FailureMessage { get; set; } = "Simulated transport error.";

        public (bool Success, string Message) SendBytes(string printerNameOrPort, byte[] bytes)
        {
            LastPrinterName = printerNameOrPort;
            LastSentBytes = bytes;

            if (!SimulateWriteSuccess)
            {
                return (false, FailureMessage);
            }

            return (true, $"Simulated transport transmitted {bytes.Length} bytes to '{printerNameOrPort}'.");
        }

        public bool CheckAvailability(string printerNameOrPort)
        {
            LastPrinterName = printerNameOrPort;
            return SimulateAvailability;
        }
    }

    public class WindowsSpoolerAndSerialTransport : IPrinterTransport
    {
        private static readonly Regex ComPortRegex = new Regex(@"^COM[1-9][0-9]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IWin32SpoolerApi _spoolerApi;

        public WindowsSpoolerAndSerialTransport(IWin32SpoolerApi? spoolerApi = null)
        {
            _spoolerApi = spoolerApi ?? new NativeWin32SpoolerApi();
        }

        public (bool Success, string Message) SendBytes(string printerNameOrPort, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort))
                return (false, "Printer name or port is empty.");

            var target = printerNameOrPort.Trim();
            if (IsComPort(target))
            {
                try
                {
                    using var port = new SerialPort(target.ToUpper(), 9600, Parity.None, 8, StopBits.One);
                    port.Open();
                    port.Write(bytes, 0, bytes.Length);
                    port.Close();
                    return (true, $"Data transmitted to serial printer at {target}. Physical paper print unverified without attached hardware.");
                }
                catch (Exception ex)
                {
                    return (false, $"Serial port error ({target}): {ex.Message}");
                }
            }
            else
            {
                return WinSpoolPrinter.SendBytesToPrinter(target, bytes, _spoolerApi);
            }
        }

        public bool CheckAvailability(string printerNameOrPort)
        {
            if (string.IsNullOrWhiteSpace(printerNameOrPort)) return false;

            var target = printerNameOrPort.Trim();
            if (IsComPort(target))
            {
                try
                {
                    return SerialPort.GetPortNames().Select(p => p.ToUpper()).Contains(target.ToUpper());
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                foreach (string installedPrinter in PrinterSettings.InstalledPrinters)
                {
                    if (installedPrinter.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                if (target.Equals("Generic / Text Only", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsComPort(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return ComPortRegex.IsMatch(input.Trim());
        }
    }
}
