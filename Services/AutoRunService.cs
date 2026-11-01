using System;
using Microsoft.Win32;

namespace TrayFolderFlyout.Services
{
    public static class AutoRunService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "TrayFolderFlyout";

        public static bool IsAutoRunEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppValueName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoRun(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppValueName, $"\"{exePath}\" --daemon");
                    }
                }
                else
                {
                    if (key.GetValue(AppValueName) != null)
                    {
                        key.DeleteValue(AppValueName, false);
                    }
                }
            }
            catch { }
        }
    }
}
