using ShopPro.Hardware;

namespace ShopPro.Tests
{
    /// <summary>
    /// Test-only fake serial port. Lives in test project, not production.
    /// </summary>
    public class FakeSerialPortDevice : ISerialPortDevice
    {
        public bool SimulateOpenSuccess { get; set; } = true;
        public bool SimulateWriteSuccess { get; set; } = true;
        public bool SimulateTimeout { get; set; } = false;
        public string? SimulatedReadResponse { get; set; }

        public bool IsOpen { get; private set; }
        public int CloseCount { get; private set; }
        public string? LastWrittenText { get; private set; }
        public SerialPortConfig? LastOpenConfig { get; private set; }
        public string? LastOpenPortName { get; private set; }
        public int? LastOpenBaudRate { get; private set; }

        public void Open(string portName, int baudRate = 9600)
        {
            LastOpenPortName = portName;
            LastOpenBaudRate = baudRate;
            if (!SimulateOpenSuccess)
                throw new System.IO.IOException($"Simulated open failure for {portName}");
            IsOpen = true;
        }

        public void Open(SerialPortConfig config)
        {
            LastOpenConfig = config;
            LastOpenPortName = config.PortName;
            LastOpenBaudRate = config.BaudRate;
            if (!SimulateOpenSuccess)
                throw new System.IO.IOException($"Simulated open failure for {config.PortName}");
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            CloseCount++;
        }

        public void Write(string text)
        {
            if (!SimulateWriteSuccess)
                throw new System.IO.IOException("Serial write operation failed.");
            LastWrittenText = text;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!SimulateWriteSuccess)
                throw new System.IO.IOException("Serial write operation failed.");
        }

        public string ReadExisting()
        {
            if (SimulateTimeout)
                throw new TimeoutException("Simulated serial read timeout.");
            return SimulatedReadResponse ?? string.Empty;
        }

        public string ReadLine()
        {
            if (SimulateTimeout)
                throw new TimeoutException("Simulated serial read timeout.");
            return SimulatedReadResponse ?? string.Empty;
        }

        public void Dispose()
        {
            if (IsOpen) Close();
        }
    }

    /// <summary>
    /// Test-only fake printer transport. Lives in test project, not production.
    /// </summary>
    public class FakePrinterTransport : IPrinterTransport
    {
        public bool SimulateAvailability { get; set; } = true;
        public bool SimulateWriteSuccess { get; set; } = true;
        public string? LastPrinterName { get; private set; }
        public byte[]? LastSentBytes { get; private set; }

        public (bool Success, string Message) SendBytes(string printerNameOrPort, byte[] bytes)
        {
            LastPrinterName = printerNameOrPort;
            LastSentBytes = bytes;
            if (!SimulateWriteSuccess)
                return (false, $"Fake transport write failure for '{printerNameOrPort}'.");
            return (true, $"Fake transport accepted {bytes.Length} bytes for '{printerNameOrPort}'.");
        }

        public bool CheckAvailability(string printerNameOrPort)
        {
            return SimulateAvailability;
        }
    }
}
