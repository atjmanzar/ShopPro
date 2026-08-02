using System.IO.Ports;

namespace ShopPro.Hardware
{
    public interface ISerialPortDevice : IDisposable
    {
        bool IsOpen { get; }
        void Open(string portName, int baudRate = 9600);
        void Close();
        void Write(string text);
        void Write(byte[] buffer, int offset, int count);
        string ReadExisting();
        string ReadLine();
    }

    public class NativeSerialPortDevice : ISerialPortDevice
    {
        private SerialPort? _port;

        public bool IsOpen => _port != null && _port.IsOpen;

        public void Open(string portName, int baudRate = 9600)
        {
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            _port.Open();
        }

        public void Close()
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }
            _port?.Dispose();
            _port = null;
        }

        public void Write(string text)
        {
            if (_port == null || !_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            _port.Write(text);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_port == null || !_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            _port.Write(buffer, offset, count);
        }

        public string ReadExisting()
        {
            if (_port == null || !_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            return _port.ReadExisting();
        }

        public string ReadLine()
        {
            if (_port == null || !_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            return _port.ReadLine();
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class FakeSerialPortDevice : ISerialPortDevice
    {
        public bool SimulateOpenSuccess { get; set; } = true;
        public bool SimulateWriteSuccess { get; set; } = true;
        public string SimulatedReadResponse { get; set; } = string.Empty;
        public bool SimulateTimeout { get; set; } = false;
        public string LastPortName { get; private set; } = string.Empty;
        public string LastWrittenText { get; private set; } = string.Empty;
        public byte[]? LastWrittenBytes { get; private set; }
        public int CloseCount { get; private set; } = 0;

        public bool IsOpen { get; private set; } = false;

        public void Open(string portName, int baudRate = 9600)
        {
            LastPortName = portName;
            if (!SimulateOpenSuccess)
            {
                IsOpen = false;
                throw new InvalidOperationException($"Failed to open serial port '{portName}'.");
            }
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            CloseCount++;
        }

        public void Write(string text)
        {
            if (!IsOpen) throw new InvalidOperationException("Port is closed.");
            if (!SimulateWriteSuccess) throw new InvalidOperationException("Serial write operation failed.");
            LastWrittenText = text;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen) throw new InvalidOperationException("Port is closed.");
            if (!SimulateWriteSuccess) throw new InvalidOperationException("Serial write operation failed.");
            LastWrittenBytes = new byte[count];
            Array.Copy(buffer, offset, LastWrittenBytes, 0, count);
        }

        public string ReadExisting()
        {
            if (!IsOpen) throw new InvalidOperationException("Port is closed.");
            if (SimulateTimeout) throw new TimeoutException("Serial read timeout.");
            return SimulatedReadResponse;
        }

        public string ReadLine()
        {
            if (!IsOpen) throw new InvalidOperationException("Port is closed.");
            if (SimulateTimeout) throw new TimeoutException("Serial read timeout.");
            return SimulatedReadResponse;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
