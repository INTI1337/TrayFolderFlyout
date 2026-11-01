using System;
using Microsoft.Win32;

namespace TrayFolderFlyout.Services
{
    public static class ThemeService
    {
        public static event Action? ThemeChanged;

        static ThemeService()
        {
            try
            {
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    ThemeChanged?.Invoke();
                };
            }
            catch { }
        }

        public static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var val = key.GetValue("AppsUseLightTheme");
                    if (val is int intVal)
                    {
                        return intVal == 0;
                    }
                }
            }
            catch { }

            // Default to dark theme for modern Windows 11
            return true;
        }

        public static bool IsDarkTheme(string preference = "System")
        {
            if (preference.Equals("Dark", StringComparison.OrdinalIgnoreCase)) return true;
            if (preference.Equals("Light", StringComparison.OrdinalIgnoreCase)) return false;
            return IsSystemDarkTheme();
        }
    }
}
