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
}
