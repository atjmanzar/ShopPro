using System.IO.Ports;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Injectable serial port abstraction.
    /// Shared by printer, scale, and VFD paths.
    /// Production code uses NativeSerialPortDevice; tests use FakeSerialPortDevice (defined in test project).
    /// </summary>
    public interface ISerialPortDevice : IDisposable
    {
        bool IsOpen { get; }

        /// <summary>
        /// Opens a serial port using default parity/databits/stopbits/timeouts.
        /// Use Open(SerialPortConfig) for full control.
        /// </summary>
        void Open(string portName, int baudRate = 9600);

        /// <summary>
        /// Opens a serial port using a typed configuration object.
        /// Parity, data bits, stop bits, and timeouts are taken from config.
        /// </summary>
        void Open(SerialPortConfig config);

        void Close();
        void Write(string text);
        void Write(byte[] buffer, int offset, int count);
        string ReadExisting();
        string ReadLine();
    }

    /// <summary>
    /// Native .NET System.IO.Ports.SerialPort implementation.
    /// All serial parameters come from the caller; nothing is hard-coded.
    /// </summary>
    public class NativeSerialPortDevice : ISerialPortDevice
    {
        private SerialPort? _port;

        public bool IsOpen => _port != null && _port.IsOpen;

        public void Open(string portName, int baudRate = 9600)
        {
            Open(new SerialPortConfig
            {
                PortName = portName,
                BaudRate = baudRate
            });
        }

        public void Open(SerialPortConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _port = new SerialPort(
                config.PortName,
                config.BaudRate,
                config.Parity,
                config.DataBits,
                config.StopBits)
            {
                ReadTimeout = config.ReadTimeoutMs,
                WriteTimeout = config.WriteTimeoutMs
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
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            _port.Write(text);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            _port.Write(buffer, offset, count);
        }

        public string ReadExisting()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            return _port.ReadExisting();
        }

        public string ReadLine()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            return _port.ReadLine();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
