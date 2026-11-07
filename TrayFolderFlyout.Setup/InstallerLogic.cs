using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace TrayFolderFlyout.Setup
{
    public static class InstallerLogic
    {
        public const string AppVersion = "v1.1.0";
        public const string DisplayVersion = "1.1.0";

        public static string InstallDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\TrayFolderFlyout");

        public static string TargetExePath => Path.Combine(InstallDirectory, "TrayFolderFlyout.exe");
        public static string LauncherExePath => Path.Combine(InstallDirectory, "TrayFolderFlyout.Launcher.exe");
        public static string UninstallExePath => Path.Combine(InstallDirectory, "Uninstall.exe");

        public static string StartMenuShortcutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            "TrayFolderFlyout.lnk");

        public static string DesktopShortcutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "TrayFolderFlyout.lnk");

        private const string RegistryRunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryUninstallPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\TrayFolderFlyout";

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static bool IsInstalled()
        {
            try
            {
                using var uninstallKey = Registry.CurrentUser.OpenSubKey(RegistryUninstallPath);
                if (uninstallKey != null) return true;
            }
            catch { }

            return File.Exists(TargetExePath);
        }

        public static void KillRunningProcesses()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("TrayFolderFlyout"))
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1500);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static async Task InstallAsync(bool createDesktopShortcut, bool enableAutoRun, IProgress<(int percent, string status)> progress)
        {
            await Task.Run(() =>
            {
                bool isUpdate = IsInstalled();

                // 1. Terminate running instances so binaries are not locked
                progress.Report((10, "Завершение работающих процессов..."));
                KillRunningProcesses();

                // 2. Prepare directory
                progress.Report((25, isUpdate ? "Подготовка каталога обновления..." : "Создание каталога установки..."));
                if (!Directory.Exists(InstallDirectory))
                {
                    Directory.CreateDirectory(InstallDirectory);
                }

                // 3. Extract embedded TrayFolderFlyout.exe (updating binaries only, preserving all user profiles)
                progress.Report((50, isUpdate ? "Обновление исполняемого файла..." : "Распаковка исполняемого файла..."));
                ExtractEmbeddedFile(TargetExePath);

                // The tiny NativeAOT launcher turns a taskbar click into an IPC message in ~12 ms.
                // It is optional: without it the app falls back to the slower in-process launch path.
                try
                {
                    ExtractEmbeddedFile(LauncherExePath);
                }
                catch { }

                // 4. Copy self as Uninstall.exe
                progress.Report((70, "Настройка модуля удаления..."));
                try
                {
                    string currentProcess = Environment.ProcessPath ?? "";
                    if (!string.IsNullOrEmpty(currentProcess) && File.Exists(currentProcess))
                    {
                        File.Copy(currentProcess, UninstallExePath, true);
                    }
                }
                catch { }

                // 5. Create shortcuts
                progress.Report((85, "Создание ярлыков..."));
                ShortcutHelper.CreateShortcut(
                    StartMenuShortcutPath,
                    TargetExePath,
                    description: "TrayFolderFlyout — папки панели задач Windows 11");

                if (createDesktopShortcut)
                {
                    ShortcutHelper.CreateShortcut(
                        DesktopShortcutPath,
                        TargetExePath,
                        description: "TrayFolderFlyout — папки панели задач Windows 11");
                }

                // 6. Autorun & Registry registration
                progress.Report((95, "Регистрация приложения в Windows..."));
                try
                {
                    using (var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true))
                    {
                        if (runKey != null)
                        {
                            if (enableAutoRun)
                            {
                                runKey.SetValue("TrayFolderFlyout", $"\"{TargetExePath}\" --daemon");
                            }
                            else if (!isUpdate)
                            {
                                runKey.DeleteValue("TrayFolderFlyout", false);
                            }
                        }
                    }

                    using (var uninstallKey = Registry.CurrentUser.CreateSubKey(RegistryUninstallPath))
                    {
                        if (uninstallKey != null)
                        {
                            uninstallKey.SetValue("DisplayName", "TrayFolderFlyout");
                            uninstallKey.SetValue("DisplayIcon", $"{TargetExePath},0");
                            uninstallKey.SetValue("DisplayVersion", DisplayVersion);
                            uninstallKey.SetValue("Publisher", "TrayFolderFlyout");
                            uninstallKey.SetValue("InstallLocation", InstallDirectory);
                            uninstallKey.SetValue("UninstallString", $"\"{UninstallExePath}\" --uninstall");
                            uninstallKey.SetValue("QuietUninstallString", $"\"{UninstallExePath}\" --uninstall --silent");
                            uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            uninstallKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                            try
                            {
                                long sizeInKb = new FileInfo(TargetExePath).Length / 1024;
                                uninstallKey.SetValue("EstimatedSize", (int)sizeInKb, RegistryValueKind.DWord);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // 7. Notify shell
                try
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                progress.Report((100, isUpdate ? "Обновление успешно завершено!" : "Установка успешно завершена!"));
            });
        }

        private static void ExtractEmbeddedFile(string destinationPath)
        {
            string fileName = Path.GetFileName(destinationPath);
            var asm = Assembly.GetExecutingAssembly();
            string? resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new InvalidOperationException($"Встроенный файл {fileName} не найден в установщике.");
            }

            using var srcStream = asm.GetManifestResourceStream(resourceName);
            if (srcStream == null)
            {
                throw new InvalidOperationException("Не удалось открыть поток ресурса приложения.");
            }

            string tempFile = destinationPath + ".tmp";
            using (var dstStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                srcStream.CopyTo(dstStream);
            }

            if (File.Exists(destinationPath))
            {
                try
                {
                    File.Delete(destinationPath);
                }
                catch
                {
                    KillRunningProcesses();
                    File.Delete(destinationPath);
                }
            }

            File.Move(tempFile, destinationPath);
        }

        public static void Uninstall(bool silent = false)
        {
            if (!silent)
            {
                var result = MessageBox.Show(
                    "Вы действительно хотите удалить TrayFolderFlyout со своего компьютера?",
                    "Удаление TrayFolderFlyout",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                // 1. Terminate running processes
                KillRunningProcesses();

                // 2. Remove shortcuts
                TryDeleteFile(StartMenuShortcutPath);
                TryDeleteFile(DesktopShortcutPath);

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile))
                {
                    TryDeleteFile(Path.Combine(userProfile, @"OneDrive\Рабочий стол\TrayFolderFlyout.lnk"));
                    TryDeleteFile(Path.Combine(userProfile, @"OneDrive\Desktop\TrayFolderFlyout.lnk"));
                }

                // 3. Remove Registry entries
                try
                {
                    using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
                    runKey?.DeleteValue("TrayFolderFlyout", false);
                }
                catch { }

                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(RegistryUninstallPath, false);
                }
                catch { }

                // 4. Delete target executable
                TryDeleteFile(TargetExePath);

                // 5. Schedule directory deletion after this uninstaller process exits
                string dir = InstallDirectory;
                if (Directory.Exists(dir))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c timeout /t 1 /nobreak >nul & rmdir /s /q \"{dir}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process.Start(psi);
                    }
                    catch { }
                }

                // 6. Notify shell
                try
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                if (!silent)
                {
                    MessageBox.Show(
                        "TrayFolderFlyout был успешно удалён.",
                        "Удаление завершено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void TryDeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { }
        }
    }
}
