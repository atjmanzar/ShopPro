using System.IO.Ports;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Typed serial port configuration.
    /// All values default to common Toledo/NCI scale settings but are configurable per device.
    /// </summary>
    public class SerialPortConfig
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public int ReadTimeoutMs { get; set; } = 1000;
        public int WriteTimeoutMs { get; set; } = 1000;
    }
}
