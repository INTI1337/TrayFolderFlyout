using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using TrayFolderFlyout.Models;
using TrayFolderFlyout.Native;

namespace TrayFolderFlyout.Services
{
    public class ConfigService
    {
        private static ConfigService? _instance;
        public static ConfigService Instance => _instance ??= new ConfigService();

        public string BaseDirectory { get; }
        public string IconsDirectory { get; }
        public string ShortcutsDirectory { get; }
        public string SavedLinksDirectory { get; }
        public string ConfigFilePath { get; }
        public bool IsPortableMode { get; }

        private bool _repairDone = false;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public ConfigService()
        {
            BaseDirectory = ResolveBaseDirectory(out bool isPortable);
            IsPortableMode = isPortable;
            IconsDirectory = Path.Combine(BaseDirectory, "Icons");
            ShortcutsDirectory = Path.Combine(BaseDirectory, "Shortcuts");
            SavedLinksDirectory = Path.Combine(BaseDirectory, "SavedLinks");
            ConfigFilePath = Path.Combine(BaseDirectory, "config.json");

            EnsureDirectories();
        }

        private static string ResolveBaseDirectory(out bool isPortable)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localUserData = Path.Combine(appDir, "UserData");
            string localConfigJson = Path.Combine(appDir, "config.json");
            string portableMarker = Path.Combine(appDir, ".portable");

            string processName = "";
            try
            {
                processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            }
            catch { }

            bool isPortableExe = processName.Contains("Portable", StringComparison.OrdinalIgnoreCase);

            bool shouldBePortable = Directory.Exists(localUserData) ||
                                    File.Exists(localConfigJson) ||
                                    File.Exists(portableMarker) ||
                                    isPortableExe;

            if (shouldBePortable)
            {
                string targetDir = File.Exists(localConfigJson) ? appDir : localUserData;
                try
                {
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    string test = Path.Combine(targetDir, ".perm_test");
                    File.WriteAllText(test, "ok");
                    File.Delete(test);
                    isPortable = true;
                    return targetDir;
                }
                catch
                {
                    // Fall through to %APPDATA% if writing to local directory fails (e.g. Program Files)
                }
            }

            try
            {
                string roaming = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TrayFolderFlyout");

                if (!Directory.Exists(roaming))
                {
                    Directory.CreateDirectory(roaming);
                }

                string test = Path.Combine(roaming, ".perm_test");
                File.WriteAllText(test, "ok");
                File.Delete(test);
                isPortable = false;
                return roaming;
            }
            catch
            {
                // Fallback to local UserData
                try
                {
                    if (!Directory.Exists(localUserData))
                    {
                        Directory.CreateDirectory(localUserData);
                    }
                }
                catch { }
                isPortable = true;
                return localUserData;
            }
        }

        private void EnsureDirectories()
        {
            try
            {
                if (!Directory.Exists(BaseDirectory)) Directory.CreateDirectory(BaseDirectory);
                if (!Directory.Exists(IconsDirectory)) Directory.CreateDirectory(IconsDirectory);
                if (!Directory.Exists(ShortcutsDirectory)) Directory.CreateDirectory(ShortcutsDirectory);
                if (!Directory.Exists(SavedLinksDirectory)) Directory.CreateDirectory(SavedLinksDirectory);
            }
            catch { }
        }

        public string GenerateUniqueIconPath(string id)
        {
            EnsureDirectories();
            return Path.Combine(IconsDirectory, $"{id}_{DateTime.Now.Ticks}.ico");
        }

        public string GetIconPathForGroup(string groupId)
        {
            return GenerateUniqueIconPath(groupId);
        }

        public static void TryDeleteOldFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { /* Ignore if locked */ }
        }

        public void DeleteOldShortcutsForGroup(string groupId)
        {
            try
            {
                EnsureDirectories();
                if (!Directory.Exists(ShortcutsDirectory)) return;

                foreach (var file in Directory.GetFiles(ShortcutsDirectory, "*.lnk"))
                {
                    try
                    {
                        var args = ShellShortcutHelper.GetShortcutArguments(file);
                        if (args != null && args.Contains(groupId, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(file);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public string GetShortcutPathForGroup(FolderGroup group, List<FolderGroup>? allGroups = null)
        {
            EnsureDirectories();

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                int index = 0;
                if (allGroups != null)
                {
                    var nameless = allGroups.FindAll(g => string.IsNullOrWhiteSpace(g.Name));
                    index = nameless.IndexOf(group);
                    if (index < 0) index = 0;
                }
                string brailleName = new string('\u2800', Math.Max(1, index + 1));
                return Path.Combine(ShortcutsDirectory, $"{brailleName}.lnk");
            }

            string safeName = string.Join("_", group.Name.Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = new string('\u2800', 1);
            return Path.Combine(ShortcutsDirectory, $"{safeName}.lnk");
        }

        public AppConfig LoadConfig()
        {
            App.Log($"LoadConfig entered. ConfigFilePath: {ConfigFilePath}");
            EnsureDirectories();

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (config != null && config.Groups != null && config.Groups.Count > 0)
                    {
                        App.Log($"LoadConfig successfully loaded existing config with {config.Groups.Count} groups.");
                        RepairMovedShortcuts(config);
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    App.Log("Exception reading existing config: " + ex);
                }
            }

            App.Log("Config file not found or empty, creating default config...");
            // Create initial default configuration with useful examples
            var initialConfig = CreateDefaultConfig();
            App.Log("Default config created, saving...");
            SaveConfig(initialConfig);
            App.Log("Default config saved.");
            return initialConfig;
        }

        public void SaveConfig(AppConfig config)
        {
            EnsureDirectories();
            string json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }

        private AppConfig CreateDefaultConfig()
        {
            var config = new AppConfig
            {
                Theme = "System",
                DefaultColumns = 4,
                Groups = new List<FolderGroup>()
            };

            // Starter Group: Virtual "Система"
            var systemGroup = new FolderGroup
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Система",
                Type = FolderType.Virtual,
                Columns = 4,
                ShowLabels = true,
                Items = new List<FolderItem>()
            };

            // Standard Windows utilities
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            AddDefaultItemIfExists(systemGroup.Items, "Проводник", Path.Combine(windir, "explorer.exe"));
            AddDefaultItemIfExists(systemGroup.Items, "Диспетчер задач", Path.Combine(sys32, "Taskmgr.exe"));
            AddDefaultItemIfExists(systemGroup.Items, "Калькулятор", Path.Combine(sys32, "calc.exe"));
            AddDefaultItemIfExists(systemGroup.Items, "Блокнот", Path.Combine(sys32, "notepad.exe"));

            // Preset icon for "Система": Cyan gradient with settings gear \uE713
            string iconPath = GetIconPathForGroup(systemGroup.Id);
            GenerateDefaultGroupIcon(
                iconPath,
                System.Drawing.Color.FromArgb(6, 182, 212),
                System.Drawing.Color.FromArgb(14, 116, 144),
                "\uE713");
            systemGroup.CustomIconPath = iconPath;

            config.Groups.Add(systemGroup);

            return config;
        }

        private static void AddDefaultItemIfExists(List<FolderItem> list, string name, string path)
        {
            if (path.StartsWith("ms-") || File.Exists(path) || Directory.Exists(path))
            {
                var item = new FolderItem
                {
                    Name = name,
                    TargetPath = path
                };
                try
                {
                    string? cached = IconService.CacheItemIcon(item, path);
                    if (!string.IsNullOrEmpty(cached) && File.Exists(cached))
                    {
                        item.CustomIconPath = cached;
                    }
                }
                catch { }
                list.Add(item);
            }
        }

        public void UpdateTaskbarPinnedShortcuts(string groupId, string newIconPath)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(newIconPath) || !File.Exists(newIconPath))
            {
                return;
            }

            try
            {
                string taskbarPinnedDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

                if (!Directory.Exists(taskbarPinnedDir)) return;

                foreach (var lnk in Directory.GetFiles(taskbarPinnedDir, "*.lnk"))
                {
                    try
                    {
                        string? args = ShellShortcutHelper.GetShortcutArguments(lnk);
                        if (args != null && args.Contains(groupId, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ShellShortcutHelper.UpdateShortcutIcon(lnk, newIconPath))
                            {
                                File.SetLastWriteTimeUtc(lnk, DateTime.UtcNow);
                                Win32.NotifyShellIconChange(lnk);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void GenerateDefaultGroupIcon(string outputPath, System.Drawing.Color bgColor, string glyph)
        {
            System.Drawing.Color colorEnd = System.Drawing.Color.FromArgb(
                bgColor.A,
                Math.Max(0, bgColor.R - 35),
                Math.Max(0, bgColor.G - 35),
                Math.Max(0, bgColor.B - 35));
            GenerateDefaultGroupIcon(outputPath, bgColor, colorEnd, glyph);
        }

        public void GenerateDefaultGroupIcon(
            string outputPath,
            System.Drawing.Color colorStart,
            System.Drawing.Color colorEnd,
            string glyph,
            System.Drawing.Color? glyphColor = null,
            string? preferredFont = null)
        {
            try
            {
                using var bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Squircle 224x224 (radius 52) centered at (16, 16)
                    var bounds = new Rectangle(16, 16, 224, 224);
                    using var squirclePath = GetRoundedRect(bounds, 52);

                    // Deep diagonal gradient
                    using var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Point(16, 16),
                        new Point(240, 240),
                        colorStart,
                        colorEnd);
                    g.FillPath(gradBrush, squirclePath);

                    // 2px inner highlight pen #35FFFFFF
                    using var borderPen = new Pen(System.Drawing.Color.FromArgb(0x35, 255, 255, 255), 2f);
                    g.DrawPath(borderPen, squirclePath);

                    // Large & bold glyph
                    Font font = ResolveFont(preferredFont, glyph, 120);

                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // Subtle drop shadow 2px down
                    using (var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(70, 0, 0, 0)))
                    {
                        g.DrawString(glyph, font, shadowBrush, new RectangleF(0, 2, 256, 256), sf);
                    }

                    // Foreground glyph
                    var fgColor = glyphColor ?? System.Drawing.Color.White;
                    using (var fgBrush = new SolidBrush(fgColor))
                    {
                        g.DrawString(glyph, font, fgBrush, new RectangleF(0, 0, 256, 256), sf);
                    }

                    font.Dispose();
                }

                IcoConverter.SaveAsMultiResIco(bmp, outputPath);
            }
            catch (Exception ex)
            {
                App.Log("GenerateDefaultGroupIcon exception: " + ex);
            }
        }

        private static Font ResolveFont(string? preferredFont, string glyph, float sizeInPixels)
        {
            string[] candidateFamilies = string.IsNullOrEmpty(preferredFont)
                ? new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Emoji", "Segoe UI Symbol", "Segoe UI Variable Text", "Segoe UI" }
                : new[] { preferredFont, "Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Emoji", "Segoe UI Symbol", "Segoe UI" };

            foreach (var fam in candidateFamilies)
            {
                try
                {
                    var font = new Font(fam, sizeInPixels, FontStyle.Regular, GraphicsUnit.Pixel);
                    return font;
                }
                catch { }
            }

            return new Font(FontFamily.GenericSansSerif, sizeInPixels, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Top left arc
            path.AddArc(arc, 180, 90);
            // Top right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            // Bottom right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            // Bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public string CreateTaskbarShortcut(FolderGroup group, List<FolderGroup>? allGroups = null, bool copyToDesktop = false)
        {
            string processExe = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(processExe))
            {
                processExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            }

            // Remove any old shortcuts pointing to this group ID (e.g. if name or Braille index changed)
            DeleteOldShortcutsForGroup(group.Id);

            string shortcutPath = GetShortcutPathForGroup(group, allGroups);

            // Ensure icon exists
            string iconPath = group.CustomIconPath ?? "";
            if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
            {
                iconPath = GenerateUniqueIconPath(group.Id);
                GenerateDefaultGroupIcon(iconPath, System.Drawing.Color.FromArgb(99, 102, 241), System.Drawing.Color.FromArgb(67, 56, 202), "\uE8B7");
                group.CustomIconPath = iconPath;
            }

            string appUserModelId = $"TrayFolderFlyout.Group.{group.Id}";
            string args = $"--flyout \"{group.Id}\"";

            ShellShortcutHelper.CreateOrUpdateShortcut(
                shortcutPath: shortcutPath,
                targetExePath: processExe,
                arguments: args,
                iconPath: iconPath,
                appUserModelId: appUserModelId,
                description: group.Name ?? "");

            // Asynchronously update pinned taskbar shortcuts & notify Windows shell without blocking UI
            string finalIconPath = iconPath;
            string grpId = group.Id;
            System.Threading.Tasks.Task.Run(() =>
            {
                UpdateTaskbarPinnedShortcuts(grpId, finalIconPath);
                Win32.NotifyShellIconChange(shortcutPath);
            });

            if (copyToDesktop)
            {
                try
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string desktopShortcut = Path.Combine(desktop, Path.GetFileName(shortcutPath));
                    File.Copy(shortcutPath, desktopShortcut, true);
                }
                catch { }
            }

            return shortcutPath;
        }

        public string SaveShortcutAutonomously(string sourcePath, string itemId)
        {
            try
            {
                EnsureDirectories();
                if (!File.Exists(sourcePath)) return sourcePath;

                // If already inside SavedLinksDirectory, keep as is
                if (sourcePath.StartsWith(SavedLinksDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return sourcePath;
                }

                string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (ext != ".lnk" && ext != ".url")
                {
                    return sourcePath;
                }

                string fileName = Path.GetFileName(sourcePath);
                string cleanFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars())).Trim();
                if (string.IsNullOrEmpty(cleanFileName)) cleanFileName = $"item{ext}";

                string targetPath = Path.Combine(SavedLinksDirectory, $"{itemId}_{cleanFileName}");
                File.Copy(sourcePath, targetPath, overwrite: true);
                return targetPath;
            }
            catch (Exception ex)
            {
                App.Log($"SaveShortcutAutonomously error for '{sourcePath}': {ex.Message}");
                return sourcePath;
            }
        }

        public FolderItem CreateFolderItem(string sourcePath, string? customName = null)
        {
            EnsureDirectories();

            bool isWebUrl = sourcePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            sourcePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            string name = customName ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                if (isWebUrl)
                {
                    name = sourcePath.Replace("https://", "").Replace("http://", "").TrimEnd('/');
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(sourcePath);
                    if (string.IsNullOrEmpty(name)) name = Path.GetFileName(sourcePath);
                }
            }

            var item = new FolderItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                TargetPath = sourcePath
            };

            if (isWebUrl)
            {
                return item;
            }

            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext == ".lnk" || ext == ".url")
            {
                string autonomousPath = SaveShortcutAutonomously(sourcePath, item.Id);
                item.TargetPath = autonomousPath;

                if (ext == ".lnk")
                {
                    try
                    {
                        var details = IconService.RunInSta(() => ShellShortcutHelper.ResolveShortcut(sourcePath));
                        if (details != null && !string.IsNullOrEmpty(details.Arguments))
                        {
                            item.Arguments = details.Arguments;
                        }
                    }
                    catch { }
                }
            }

            // Immediately extract and cache icon so it is immune to source file relocation or deletion
            try
            {
                string? cachedIcon = IconService.CacheItemIcon(item, item.TargetPath);
                if (!string.IsNullOrEmpty(cachedIcon) && File.Exists(cachedIcon))
                {
                    item.CustomIconPath = cachedIcon;
                }
            }
            catch { }

            return item;
        }

        public void RepairMovedShortcuts(AppConfig config, bool force = false)
        {
            if (_repairDone && !force) return;
            _repairDone = true;

            if (config?.Groups == null || config.Groups.Count == 0) return;

            try
            {
                EnsureDirectories();
                bool modified = false;

                // 1. Check if any items are missing on disk
                var missingItems = new List<FolderItem>();
                foreach (var group in config.Groups)
                {
                    if (group.Items == null) continue;
                    foreach (var item in group.Items)
                    {
                        if (string.IsNullOrEmpty(item.TargetPath)) continue;

                        bool isWebOrProtocol = item.TargetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                              item.TargetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                              item.TargetPath.StartsWith("ms-", StringComparison.OrdinalIgnoreCase);
                        if (isWebOrProtocol) continue;

                        if (!File.Exists(item.TargetPath) && !Directory.Exists(item.TargetPath))
                        {
                            missingItems.Add(item);
                        }
                    }
                }

                // 2. Pre-index search roots if any files are missing
                Dictionary<string, string>? fileIndex = null;
                if (missingItems.Count > 0)
                {
                    fileIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var searchRoots = GetShortcutSearchRoots();
                    foreach (var root in searchRoots)
                    {
                        IndexDirectory(root, fileIndex, maxDepth: 4);
                    }
                    App.Log($"[Auto-Heal] Indexed {fileIndex.Count} files across search roots for {missingItems.Count} missing items.");
                }

                // 3. Process each item across all groups
                foreach (var group in config.Groups)
                {
                    if (group.Items == null) continue;
                    foreach (var item in group.Items)
                    {
                        if (string.IsNullOrEmpty(item.TargetPath)) continue;

                        bool isWebOrProtocol = item.TargetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                              item.TargetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                              item.TargetPath.StartsWith("ms-", StringComparison.OrdinalIgnoreCase);
                        if (isWebOrProtocol) continue;

                        // Case A: Missing file on disk -> Heal from index
                        if (!File.Exists(item.TargetPath) && !Directory.Exists(item.TargetPath) && fileIndex != null)
                        {
                            string? foundPath = null;
                            string originalFn = Path.GetFileName(item.TargetPath);

                            if (!string.IsNullOrEmpty(originalFn) && fileIndex.TryGetValue(originalFn, out var m1))
                            {
                                foundPath = m1;
                            }
                            else if (!string.IsNullOrEmpty(item.Name) && fileIndex.TryGetValue(item.Name + ".lnk", out var m2))
                            {
                                foundPath = m2;
                            }
                            else if (!string.IsNullOrEmpty(item.Name) && fileIndex.TryGetValue(item.Name + ".url", out var m3))
                            {
                                foundPath = m3;
                            }
                            else if (!string.IsNullOrEmpty(item.Name) && fileIndex.TryGetValue(item.Name, out var m4))
                            {
                                foundPath = m4;
                            }

                            if (!string.IsNullOrEmpty(foundPath) && File.Exists(foundPath))
                            {
                                string autonomous = SaveShortcutAutonomously(foundPath, item.Id);
                                item.TargetPath = autonomous;
                                modified = true;

                                if (autonomous.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(item.Arguments))
                                {
                                    try
                                    {
                                        var details = IconService.RunInSta(() => ShellShortcutHelper.ResolveShortcut(autonomous));
                                        if (details != null && !string.IsNullOrEmpty(details.Arguments))
                                        {
                                            item.Arguments = details.Arguments;
                                        }
                                    }
                                    catch { }
                                }

                                App.Log($"[Auto-Heal] Successfully restored '{item.Name}' -> {autonomous}");
                            }
                            else
                            {
                                App.Log($"[Auto-Heal] Could not locate file for '{item.Name}' (was: {item.TargetPath})");
                            }
                        }

                        // Case B: Shortcut exists but not yet stored autonomously in SavedLinks
                        if (File.Exists(item.TargetPath))
                        {
                            string ext = Path.GetExtension(item.TargetPath).ToLowerInvariant();
                            if ((ext == ".lnk" || ext == ".url") && !item.TargetPath.StartsWith(SavedLinksDirectory, StringComparison.OrdinalIgnoreCase))
                            {
                                string autonomous = SaveShortcutAutonomously(item.TargetPath, item.Id);
                                if (!autonomous.Equals(item.TargetPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    item.TargetPath = autonomous;
                                    modified = true;
                                    App.Log($"[SavedLinks] Migrated '{item.Name}' to autonomous copy -> {autonomous}");
                                }
                            }
                        }

                        // Case C: Cache icon to Icons/Item_<Id>.png if not already set or missing
                        if (File.Exists(item.TargetPath))
                        {
                            if (string.IsNullOrEmpty(item.CustomIconPath) || !File.Exists(item.CustomIconPath))
                            {
                                string? cachedIcon = IconService.CacheItemIcon(item, item.TargetPath);
                                if (!string.IsNullOrEmpty(cachedIcon) && File.Exists(cachedIcon))
                                {
                                    item.CustomIconPath = cachedIcon;
                                    modified = true;
                                    App.Log($"[Auto-Heal] Cached icon for '{item.Name}' -> {cachedIcon}");
                                }
                            }
                        }
                    }
                }

                if (modified)
                {
                    SaveConfig(config);
                    App.Log("[Auto-Heal] Config updated and saved successfully.");
                }
            }
            catch (Exception ex)
            {
                App.Log("[Auto-Heal] Exception in RepairMovedShortcuts: " + ex);
            }
        }

        private static List<string> GetShortcutSearchRoots()
        {
            var roots = new List<string>();

            void TryAdd(string? path)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !roots.Any(r => r.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    roots.Add(path);
                }
            }

            // Desktop folders
            TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                TryAdd(Path.Combine(userProfile, "Desktop"));
                TryAdd(Path.Combine(userProfile, @"OneDrive\Рабочий стол"));
                TryAdd(Path.Combine(userProfile, @"OneDrive\Desktop"));
                TryAdd(Path.Combine(userProfile, "Downloads"));
            }

            // Start Menu programs
            TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
            TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms));

            return roots;
        }

        private static void IndexDirectory(string dir, Dictionary<string, string> index, int maxDepth)
        {
            if (maxDepth < 0 || !Directory.Exists(dir)) return;

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    string name = Path.GetFileName(file);
                    index.TryAdd(name, file);
                }

                if (maxDepth > 0)
                {
                    foreach (var subDir in Directory.EnumerateDirectories(dir))
                    {
                        try
                        {
                            var di = new DirectoryInfo(subDir);
                            if ((di.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                                continue;
                            if (di.Name.Equals("AppData", StringComparison.OrdinalIgnoreCase) ||
                                di.Name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                                di.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                                continue;

                            IndexDirectory(subDir, index, maxDepth - 1);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
