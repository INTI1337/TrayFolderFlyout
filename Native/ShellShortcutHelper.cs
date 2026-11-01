using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TrayFolderFlyout.Native
{
    public static class ShellShortcutHelper
    {
        public static void CreateOrUpdateShortcut(
            string shortcutPath,
            string targetExePath,
            string arguments,
            string iconPath,
            string appUserModelId,
            string description = "")
        {
            var dir = Path.GetDirectoryName(shortcutPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var link = (IShellLinkW)new ShellLink();

            link.SetPath(targetExePath);
            link.SetArguments(arguments);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetExePath) ?? "");
            link.SetDescription(description);

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                link.SetIconLocation(iconPath, 0);
            }

            // Set PKEY_AppUserModel_ID via IPropertyStore
            if (!string.IsNullOrEmpty(appUserModelId) && link is IPropertyStore propStore)
            {
                var key = PROPERTYKEY.AppUserModel_ID;
                var pv = new PROPVARIANT
                {
                    vt = PROPVARIANT.VT_LPWSTR,
                    pwszVal = Marshal.StringToCoTaskMemUni(appUserModelId)
                };

                try
                {
                    propStore.SetValue(ref key, ref pv);
                    propStore.Commit();
                }
                finally
                {
                    Win32.PropVariantClear(ref pv);
                }
            }

            var persistFile = (IPersistFile)link;
            persistFile.Save(shortcutPath, true);
        }

        public static string? GetShortcutArguments(string shortcutPath)
        {
            try
            {
                if (!File.Exists(shortcutPath)) return null;
                var link = (IShellLinkW)new ShellLink();
                var persistFile = (IPersistFile)link;
                persistFile.Load(shortcutPath, 0);
                var sb = new System.Text.StringBuilder(512);
                link.GetArguments(sb, sb.Capacity);
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static bool UpdateShortcutIcon(string shortcutPath, string newIconPath)
        {
            try
            {
                if (!File.Exists(shortcutPath) || !File.Exists(newIconPath)) return false;
                var link = (IShellLinkW)new ShellLink();
                var persistFile = (IPersistFile)link;
                persistFile.Load(shortcutPath, 0);
                link.SetIconLocation(newIconPath, 0);
                persistFile.Save(shortcutPath, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class ShortcutDetails
        {
            public string TargetPath { get; set; } = "";
            public string Arguments { get; set; } = "";
            public string WorkingDirectory { get; set; } = "";
            public string IconLocation { get; set; } = "";
            public int IconIndex { get; set; }
            public string Description { get; set; } = "";
        }

        public static ShortcutDetails? ResolveShortcut(string shortcutPath)
        {
            try
            {
                if (!File.Exists(shortcutPath)) return null;
                var link = (IShellLinkW)new ShellLink();
                var persistFile = (IPersistFile)link;
                persistFile.Load(shortcutPath, 0);

                var sbPath = new System.Text.StringBuilder(1024);
                link.GetPath(sbPath, sbPath.Capacity, IntPtr.Zero, 0);

                var sbArgs = new System.Text.StringBuilder(1024);
                link.GetArguments(sbArgs, sbArgs.Capacity);

                var sbDir = new System.Text.StringBuilder(1024);
                link.GetWorkingDirectory(sbDir, sbDir.Capacity);

                var sbIcon = new System.Text.StringBuilder(1024);
                link.GetIconLocation(sbIcon, sbIcon.Capacity, out int iconIndex);

                var sbDesc = new System.Text.StringBuilder(1024);
                link.GetDescription(sbDesc, sbDesc.Capacity);

                return new ShortcutDetails
                {
                    TargetPath = sbPath.ToString(),
                    Arguments = sbArgs.ToString(),
                    WorkingDirectory = sbDir.ToString(),
                    IconLocation = sbIcon.ToString(),
                    IconIndex = iconIndex,
                    Description = sbDesc.ToString()
                };
            }
            catch
            {
                return null;
            }
        }

        public class UrlShortcutDetails
        {
            public string Url { get; set; } = "";
            public string IconFile { get; set; } = "";
            public int IconIndex { get; set; }
        }

        public static UrlShortcutDetails? ParseUrlFile(string urlFilePath)
        {
            try
            {
                if (!File.Exists(urlFilePath)) return null;
                var details = new UrlShortcutDetails();
                foreach (var line in File.ReadAllLines(urlFilePath))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Url = trimmed.Substring(4).Trim();
                    }
                    else if (trimmed.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        string iconPath = trimmed.Substring(9).Trim().Trim('"', '\'');
                        details.IconFile = Environment.ExpandEnvironmentVariables(iconPath);
                    }
                    else if (trimmed.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(trimmed.Substring(10).Trim(), out int idx))
                        {
                            details.IconIndex = idx;
                        }
                    }
                }
                return details;
            }
            catch
            {
                return null;
            }
        }
    }
}
