using System.Runtime.InteropServices;

namespace ShopPro.Hardware
{
    public interface IWin32SpoolerApi
    {
        bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
        bool ClosePrinter(IntPtr hPrinter);
        uint StartDocPrinter(IntPtr hPrinter, int level, WinSpoolPrinter.DOCINFOW di);
        bool EndDocPrinter(IntPtr hPrinter);
        bool StartPagePrinter(IntPtr hPrinter);
        bool EndPagePrinter(IntPtr hPrinter);
        bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
        int GetLastError();
    }

    public class NativeWin32SpoolerApi : IWin32SpoolerApi
    {
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinterW([MarshalAs(UnmanagedType.LPWStr)] string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinterWin32(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint StartDocPrinterW(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] WinSpoolPrinter.DOCINFOW di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinterWin32(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinterWin32(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinterWin32(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinterWin32(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault)
        {
            return OpenPrinterW(pPrinterName, out phPrinter, pDefault);
        }

        public bool ClosePrinter(IntPtr hPrinter)
        {
            return ClosePrinterWin32(hPrinter);
        }

        public uint StartDocPrinter(IntPtr hPrinter, int level, WinSpoolPrinter.DOCINFOW di)
        {
            return StartDocPrinterW(hPrinter, level, di);
        }

        public bool EndDocPrinter(IntPtr hPrinter)
        {
            return EndDocPrinterWin32(hPrinter);
        }

        public bool StartPagePrinter(IntPtr hPrinter)
        {
            return StartPagePrinterWin32(hPrinter);
        }

        public bool EndPagePrinter(IntPtr hPrinter)
        {
            return EndPagePrinterWin32(hPrinter);
        }

        public bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten)
        {
            return WritePrinterWin32(hPrinter, pBytes, dwCount, out dwWritten);
        }

        public int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }
    }
}
