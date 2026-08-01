namespace ShopPro.Hardware
{
    public class SerialWeighingScale
    {
        public bool IsConnected { get; private set; } = false;
        public string ComPort { get; set; } = "COM1";

        public void Connect(string portName = "COM1")
        {
            ComPort = portName;
            IsConnected = true;
        }

        public decimal ReadWeightKg()
        {
            if (!IsConnected) return 0.000m;

            // Simulated NMEA / RS232 Weight packet reader
            return 1.250m; // 1.250 kg reading
        }

        public void Disconnect()
        {
            IsConnected = false;
        }
    }
}
