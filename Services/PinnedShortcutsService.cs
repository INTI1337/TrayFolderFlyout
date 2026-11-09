using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrayFolderFlyout.Native;

namespace TrayFolderFlyout.Services
{
    /// <summary>A shortcut pinned to the taskbar or the Start menu.</summary>
    public class PinnedShortcut
    {
        public string Name { get; init; } = "";
        public string Path { get; init; } = "";
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// Reads pinned shortcuts from "User Pinned" (taskbar and Start menu). Shortcuts created by
    /// this application for its own folders are filtered out, otherwise they would show up as
    /// regular apps that can be added to themselves.
    /// </summary>
    public static class PinnedShortcutsService
    {
        public static string PinnedRoot => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Internet Explorer\Quick Launch\User Pinned");

        public static List<PinnedShortcut> Load()
        {
            var result = new List<PinnedShortcut>();

            string root = PinnedRoot;
            if (!Directory.Exists(root)) return result;

            try
            {
                var files = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(root, "*.url", SearchOption.AllDirectories))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

                foreach (string path in files)
                {
                    if (IsOwnShortcut(path)) continue;

                    result.Add(new PinnedShortcut
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(path),
                        Path = path,
                        Source = DescribeSource(root, path)
                    });
                }
            }
            catch { }

            return result;
        }

        public static bool IsOwnShortcut(string path)
        {
            try
            {
                if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return false;

                var details = ShellShortcutHelper.ResolveShortcut(path);
                if (details == null) return false;

                string target = System.IO.Path.GetFileName(details.TargetPath);
                if (target.Equals("TrayFolderFlyout.exe", StringComparison.OrdinalIgnoreCase) ||
                    target.Equals("TrayFolderFlyout.Launcher.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return details.Arguments.Contains("--flyout", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Turns "...\User Pinned\TaskBar\x.lnk" into "Панель задач".</summary>
        public static string DescribeSource(string root, string path)
        {
            string relative = System.IO.Path.GetRelativePath(root, path);
            string? folder = System.IO.Path.GetDirectoryName(relative);

            if (string.IsNullOrEmpty(folder)) return "Закреплённые";

            return folder.Replace('\\', '/') switch
            {
                "TaskBar" => "Панель задач",
                "StartMenu" => "Меню «Пуск»",
                _ => folder
            };
        }
    }
}
