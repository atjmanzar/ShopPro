using System.Runtime.InteropServices;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Win32 Spooler P/Invoke Helper for Direct RAW Printing:
    /// Transmits raw ESC/POS byte arrays directly to installed Windows printer drivers via winspool.drv.
    /// 
    /// Hardware Verification Note:
    /// This makes P/Invoke system calls to winspool.drv. It can be verified on Windows OS with an installed printer driver.
    /// Spooler acceptance means bytes were handed to the Windows Print Spooler. Physical paper print remains unverified without attached hardware.
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
            if (bytes == null || bytes.Length == 0)
                return (false, "Byte payload is empty.");

            IntPtr hPrinter = IntPtr.Zero;
            IntPtr pUnmanagedBytes = IntPtr.Zero;
            bool docStarted = false;
            bool pageStarted = false;

            try
            {
                if (!OpenPrinter(printerName.Trim(), out hPrinter, IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"Printer '{printerName}' not found or inaccessible (Win32 Error: {err}). Check USB/network connection.");
                }

                var di = new DOCINFOA();
                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"StartDocPrinter failed for '{printerName}' (Win32 Error: {err}).");
                }
                docStarted = true;

                if (!StartPagePrinter(hPrinter))
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"StartPagePrinter failed for '{printerName}' (Win32 Error: {err}).");
                }
                pageStarted = true;

                pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                bool writeOk = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int dwWritten);

                if (!writeOk)
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"WritePrinter failed for '{printerName}' (Win32 Error: {err}).");
                }

                if (dwWritten != bytes.Length)
                {
                    return (false, $"Partial write to spooler for '{printerName}': wrote {dwWritten} of {bytes.Length} bytes.");
                }

                return (true, $"Byte stream successfully handed to Windows Print Spooler for target '{printerName}'. Physical paper print unverified without attached hardware.");
            }
            catch (Exception ex)
            {
                return (false, $"Spooler exception for '{printerName}': {ex.Message}");
            }
            finally
            {
                if (pUnmanagedBytes != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);
                }
                if (pageStarted && hPrinter != IntPtr.Zero)
                {
                    EndPagePrinter(hPrinter);
                }
                if (docStarted && hPrinter != IntPtr.Zero)
                {
                    EndDocPrinter(hPrinter);
                }
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                }
            }
        }
    }
}
