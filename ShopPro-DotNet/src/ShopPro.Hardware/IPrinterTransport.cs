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
}
