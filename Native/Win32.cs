using System;
using System.Runtime.InteropServices;

namespace TrayFolderFlyout.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    public static class Win32
    {
        // AppUserModelID
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        // DWM Attributes
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public const int DWMWCP_DEFAULT = 0;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;

        public const int DWMSBT_AUTO = 0;
        public const int DWMSBT_NONE = 1;
        public const int DWMSBT_MAINWINDOW = 2;       // Mica
        public const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
        public const int DWMSBT_TABBEDWINDOW = 4;     // Tabbed Mica

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Window Foreground & Focus
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // Cursor & Display
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        public const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        public static RECT GetWorkAreaForPoint(POINT pt)
        {
            GetMonitorAndWorkArea(pt, out _, out RECT rcWork);
            return rcWork;
        }

        public static bool GetMonitorAndWorkArea(POINT pt, out RECT rcMonitor, out RECT rcWork)
        {
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref mi))
            {
                rcMonitor = mi.rcMonitor;
                rcWork = mi.rcWork;
                return true;
            }

            // Fallback: Primary monitor work area via SystemParameters
            rcMonitor = new RECT
            {
                Left = 0,
                Top = 0,
                Right = (int)System.Windows.SystemParameters.PrimaryScreenWidth,
                Bottom = (int)System.Windows.SystemParameters.PrimaryScreenHeight
            };
            rcWork = new RECT
            {
                Left = (int)System.Windows.SystemParameters.WorkArea.Left,
                Top = (int)System.Windows.SystemParameters.WorkArea.Top,
                Right = (int)System.Windows.SystemParameters.WorkArea.Right,
                Bottom = (int)System.Windows.SystemParameters.WorkArea.Bottom
            };
            return false;
        }

        // Shell Item Creation
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);

        // GDI Object cleanup
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(
            string szFileName,
            int nIconIndex,
            IntPtr[]? phiconLarge,
            IntPtr[]? phiconSmall,
            uint nIcons);

        // SHGetFileInfo fallback
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint SHGFI_SMALLICON = 0x000000001;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("ole32.dll")]
        public static extern int PropVariantClear(ref PROPVARIANT pvar);

        // Windows Shell Icon Cache Notification
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public const uint SHCNE_UPDATEITEM = 0x00002000;
        public const uint SHCNE_ASSOCCHANGED = 0x08000000;
        public const uint SHCNF_IDLIST = 0x0000;
        public const uint SHCNF_PATHW = 0x0005;
        public const uint SHCNF_FLUSH = 0x1000;
        public const uint SHCNF_FLUSHNOWAIT = 0x3000;

        public static void NotifyShellIconChange(string? specificItemPath = null)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(specificItemPath) && System.IO.File.Exists(specificItemPath))
                    {
                        IntPtr pItem = Marshal.StringToHGlobalUni(specificItemPath);
                        try
                        {
                            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, pItem, IntPtr.Zero);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pItem);
                        }
                    }

                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }
            });
        }

        // Apply Windows 11 fluent styling (Corner rounding, Backdrop Acrylic / Mica, Dark mode)
        public static void ApplyWin11Style(IntPtr hwnd, bool isDark, bool useAcrylic = true)
        {
            try
            {
                // Dark mode
                int darkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                // Rounded corners
                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                // Backdrop (Acrylic for popups/flyouts, Mica for main manager)
                int backdrop = useAcrylic ? DWMSBT_TRANSIENTWINDOW : DWMSBT_MAINWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
            catch
            {
                // Ignore if running on Windows version without DWM backdrop support
            }
        }
    }
}
