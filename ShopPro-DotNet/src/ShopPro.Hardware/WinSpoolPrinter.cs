using System.Runtime.InteropServices;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Win32 Spooler P/Invoke Helper for Direct RAW Printing:
    /// Transmits raw ESC/POS byte arrays directly to installed Windows printer drivers via winspool.drv.
    /// 
    /// Hardware Verification Note:
    /// This makes P/Invoke system calls to winspool.drv. It can be verified on Windows OS with an installed printer driver.
    /// Physical printing requires a physical USB/Network thermal printer attached to the machine.
    /// </summary>
    public static class WinSpoolPrinter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = "ShopPro Receipt";
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static (bool Success, string Message) SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                return (false, "Printer name is empty.");

            IntPtr hPrinter = IntPtr.Zero;
            var di = new DOCINFOA();
            bool success = false;
            string errorMsg = string.Empty;

            try
            {
                if (!OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"Printer '{printerName}' not found or inaccessible (Win32 Error: {err}). Check USB/network connection.");
                }

                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                        Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                        success = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int dwWritten);
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);

                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }

                ClosePrinter(hPrinter);

                if (success)
                    return (true, "Raw bytes transmitted to Windows Print Spooler.");
                else
                    return (false, $"Failed to write bytes to printer '{printerName}'.");
            }
            catch (Exception ex)
            {
                if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter);
                return (false, $"Spooler exception: {ex.Message}");
            }
        }
    }
}
