using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrayFolderFlyout.Models;
using TrayFolderFlyout.Native;

namespace TrayFolderFlyout.Services
{
    public static class IconService
    {
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new();

        public static void ClearCache()
        {
            _iconCache.Clear();
        }

        public static void InvalidateIcon(string? pathOrId)
        {
            if (string.IsNullOrEmpty(pathOrId)) return;
            foreach (var key in _iconCache.Keys)
            {
                if (key.Contains(pathOrId, StringComparison.OrdinalIgnoreCase))
                {
                    _iconCache.TryRemove(key, out _);
                }
            }
        }

        public static string ConvertAndAssignCustomIcon(string sourceFilePath, string targetEntityId, string? oldIconPath)
        {
            string newIcoPath = ConfigService.Instance.GenerateUniqueIconPath(targetEntityId);
            IcoConverter.CreateMultiResIcon(sourceFilePath, newIcoPath);
            if (!string.IsNullOrEmpty(oldIconPath))
            {
                InvalidateIcon(oldIconPath);
                ConfigService.TryDeleteOldFile(oldIconPath);
            }
            InvalidateIcon(sourceFilePath);
            InvalidateIcon(targetEntityId);
            InvalidateIcon(newIcoPath);
            return newIcoPath;
        }

        public static T RunInSta<T>(Func<T> action)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return action();
            }

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                return Application.Current.Dispatcher.Invoke(action);
            }

            T result = default!;
            var thread = new Thread(() =>
            {
                result = action();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }

        public static ImageSource GetIcon(string? targetPath, string? customIconPath = null, int preferredSize = 48)
        {
            string cacheKey = $"{customIconPath}|{targetPath}|{preferredSize}";
            if (_iconCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            ImageSource? icon = null;

            // 1. If custom icon path provided and exists
            if (!string.IsNullOrEmpty(customIconPath) && File.Exists(customIconPath))
            {
                icon = LoadFromImageFile(customIconPath, preferredSize);
            }

            // 2. If target is .lnk file
            if (icon == null && !string.IsNullOrEmpty(targetPath) && targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
            {
                icon = ExtractIconFromLnk(targetPath, preferredSize);
            }

            // 3. If target is .url file
            if (icon == null && !string.IsNullOrEmpty(targetPath) && targetPath.EndsWith(".url", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
            {
                icon = ExtractIconFromUrl(targetPath, preferredSize);
            }

            // 4. If target is web URL
            if (icon == null && !string.IsNullOrEmpty(targetPath) &&
                (targetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 targetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                icon = GetDefaultWebIcon(preferredSize);
            }

            // 5. If target is regular file or executable
            if (icon == null && !string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
            {
                icon = ExtractIconFromFile(targetPath, 0, preferredSize);
            }

            // 6. Generic folder fallback
            if (icon == null && !string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath))
            {
                icon = GetDefaultFolderIcon(preferredSize);
            }

            // 7. Generic document fallback
            icon ??= GetDefaultDocumentIcon(preferredSize);

            _iconCache[cacheKey] = icon;
            return icon;
        }

        public static BitmapSource? ExtractIconFromLnk(string lnkPath, int preferredSize = 48)
        {
            if (!File.Exists(lnkPath)) return null;

            try
            {
                // Step 1: Resolve shortcut details in STA
                var details = RunInSta(() => ShellShortcutHelper.ResolveShortcut(lnkPath));

                if (details != null)
                {
                    // If shortcut has explicit custom IconLocation
                    if (!string.IsNullOrEmpty(details.IconLocation))
                    {
                        string expandedIcon = Environment.ExpandEnvironmentVariables(details.IconLocation);
                        if (File.Exists(expandedIcon))
                        {
                            var icon = ExtractIconFromFile(expandedIcon, details.IconIndex, preferredSize);
                            if (icon != null) return icon;
                        }
                    }

                    // If shortcut has resolved TargetPath (.exe, file)
                    if (!string.IsNullOrEmpty(details.TargetPath) && File.Exists(details.TargetPath))
                    {
                        var icon = ExtractIconFromFile(details.TargetPath, 0, preferredSize);
                        if (icon != null) return icon;
                    }
                }

                // Step 2: Try IShellItemImageFactory on the .lnk itself in STA
                var shellImg = RunInSta(() => GetShellItemImage(lnkPath, preferredSize));
                if (shellImg is BitmapSource bsShell) return bsShell;

                // Step 3: Try SHGetFileInfo on .lnk in STA
                var shImg = RunInSta(() => GetShellFileInfoIcon(lnkPath, false));
                if (shImg is BitmapSource bsSh) return bsSh;
            }
            catch { }

            return null;
        }

        public static BitmapSource? ExtractIconFromUrl(string urlPath, int preferredSize = 48)
        {
            if (!File.Exists(urlPath)) return null;

            try
            {
                var details = ShellShortcutHelper.ParseUrlFile(urlPath);
                if (details != null && !string.IsNullOrEmpty(details.IconFile))
                {
                    string iconFile = details.IconFile;
                    if (File.Exists(iconFile))
                    {
                        var icon = ExtractIconFromFile(iconFile, details.IconIndex, preferredSize);
                        if (icon != null) return icon;
                    }

                    // Fallback for Steam games if path has changed or relative
                    if (details.Url.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                    {
                        string fn = Path.GetFileName(iconFile);
                        string[] candidateSteamDirs =
                        {
                            @"C:\Steam\steam\games",
                            @"C:\Program Files (x86)\Steam\steam\games",
                            @"D:\Steam\steam\games",
                            @"E:\Steam\steam\games"
                        };

                        foreach (var dir in candidateSteamDirs)
                        {
                            string candidate = Path.Combine(dir, fn);
                            if (File.Exists(candidate))
                            {
                                var icon = ExtractIconFromFile(candidate, 0, preferredSize);
                                if (icon != null) return icon;
                            }
                        }
                    }
                }

                // Step 2: Try IShellItemImageFactory on the .url file in STA
                var shellImg = RunInSta(() => GetShellItemImage(urlPath, preferredSize));
                if (shellImg is BitmapSource bsShell) return bsShell;
            }
            catch { }

            return null;
        }

        public static BitmapSource? ExtractIconFromFile(string filePath, int iconIndex = 0, int preferredSize = 48)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                // 1. Direct ICO file
                if (ext == ".ico")
                {
                    var src = LoadFromImageFile(filePath, preferredSize) as BitmapSource;
                    if (src != null) return src;
                }

                // 2. Direct PNG, JPG, BMP
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    var src = LoadFromImageFile(filePath, preferredSize) as BitmapSource;
                    if (src != null) return src;
                }

                // 3. Try ExtractIconEx for .exe or .dll with iconIndex
                if (ext == ".exe" || ext == ".dll")
                {
                    try
                    {
                        var hLarge = new IntPtr[1];
                        var hSmall = new IntPtr[1];
                        uint count = Win32.ExtractIconEx(filePath, iconIndex, hLarge, hSmall, 1);
                        if (count > 0)
                        {
                            IntPtr hIcon = hLarge[0] != IntPtr.Zero ? hLarge[0] : hSmall[0];
                            if (hIcon != IntPtr.Zero)
                            {
                                using var icon = Icon.FromHandle(hIcon);
                                var bs = ToBitmapSource(icon);
                                Win32.DestroyIcon(hIcon);
                                if (hSmall[0] != IntPtr.Zero && hSmall[0] != hLarge[0])
                                    Win32.DestroyIcon(hSmall[0]);
                                return bs;
                            }
                        }
                    }
                    catch { }
                }

                // 4. Try IShellItemImageFactory in STA
                var shellImg = RunInSta(() => GetShellItemImage(filePath, preferredSize));
                if (shellImg is BitmapSource bsShell) return bsShell;

                // 5. Fallback: Icon.ExtractAssociatedIcon
                try
                {
                    using var assoc = Icon.ExtractAssociatedIcon(filePath);
                    if (assoc != null)
                    {
                        return ToBitmapSource(assoc);
                    }
                }
                catch { }

                // 6. Fallback: SHGetFileInfo in STA
                var shImg = RunInSta(() => GetShellFileInfoIcon(filePath, Directory.Exists(filePath)));
                if (shImg is BitmapSource bsSh) return bsSh;
            }
            catch { }

            return null;
        }

        public static void SaveBitmapSourceToPng(BitmapSource bitmapSource, string outputPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                encoder.Save(fs);
            }
            catch { }
        }

        public static string? CacheItemIcon(FolderItem item, string sourceOrTargetPath)
        {
            try
            {
                string iconsDir = ConfigService.Instance.IconsDirectory;
                string cachedIconPath = Path.Combine(iconsDir, $"Item_{item.Id}.png");

                // If already cached and valid, return it
                if (File.Exists(cachedIconPath) && new FileInfo(cachedIconPath).Length > 0)
                {
                    return cachedIconPath;
                }

                string ext = Path.GetExtension(sourceOrTargetPath).ToLowerInvariant();
                BitmapSource? icon = null;

                if (ext == ".lnk")
                {
                    icon = ExtractIconFromLnk(sourceOrTargetPath, 64);
                }
                else if (ext == ".url")
                {
                    icon = ExtractIconFromUrl(sourceOrTargetPath, 64);
                }
                else if (File.Exists(sourceOrTargetPath))
                {
                    icon = ExtractIconFromFile(sourceOrTargetPath, 0, 64);
                }

                if (icon != null)
                {
                    SaveBitmapSourceToPng(icon, cachedIconPath);
                    if (File.Exists(cachedIconPath))
                    {
                        InvalidateIcon(cachedIconPath);
                        return cachedIconPath;
                    }
                }
            }
            catch { }

            return null;
        }

        private static ImageSource? GetShellItemImage(string path, int size)
        {
            try
            {
                Guid iidImageFactory = new Guid("bcc18b79-ba16-442f-80c4-8a59c07c463b");
                int hr = Win32.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidImageFactory, out object factoryObj);
                if (hr == 0 && factoryObj is IShellItemImageFactory factory)
                {
                    IntPtr hBitmap;
                    var sz = new Native.SIZE(size, size);
                    hr = factory.GetImage(sz, SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_ICONONLY, out hBitmap);
                    if (hr == 0 && hBitmap != IntPtr.Zero)
                    {
                        var bmp = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        Win32.DeleteObject(hBitmap);
                        bmp.Freeze();
                        return bmp;
                    }
                }
            }
            catch { }

            return null;
        }

        public static ImageSource? LoadFromImageFile(string path, int size)
        {
            try
            {
                if (!File.Exists(path)) return null;

                byte[] bytes = File.ReadAllBytes(path);

                if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var ms = new MemoryStream(bytes);
                        using var icon = new Icon(ms, size > 0 ? size : 48, size > 0 ? size : 48);
                        return ToBitmapSource(icon);
                    }
                    catch
                    {
                        using var ms = new MemoryStream(bytes);
                        using var icon = new Icon(ms);
                        return ToBitmapSource(icon);
                    }
                }

                // PNG, JPG, BMP: load directly into memory without file locking or WPF image cache
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.StreamSource = new MemoryStream(bytes);
                if (size > 0) bmp.DecodePixelWidth = size;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private static ImageSource? GetShellFileInfoIcon(string path, bool isDirectory)
        {
            try
            {
                var shinfo = new Win32.SHFILEINFO();
                uint flags = Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON;
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    flags |= Win32.SHGFI_USEFILEATTRIBUTES;
                }

                uint attributes = isDirectory ? 0x00000010u : 0x00000080u;
                IntPtr res = Win32.SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(shinfo.hIcon);
                    var src = ToBitmapSource(icon);
                    Win32.DestroyIcon(shinfo.hIcon);
                    return src;
                }
            }
            catch { }

            return null;
        }

        public static BitmapSource ToBitmapSource(Icon icon)
        {
            var wpfBitmap = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            wpfBitmap.Freeze();
            return wpfBitmap;
        }

        public static ImageSource GetDefaultFolderIcon(int size = 48)
        {
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var icon = RunInSta(() => GetShellItemImage(systemFolder, size)) ?? RunInSta(() => GetShellFileInfoIcon(systemFolder, true));
            return icon ?? CreateFallbackIcon(System.Windows.Media.Color.FromRgb(255, 185, 0), size);
        }

        public static ImageSource GetDefaultWebIcon(int size = 48)
        {
            return CreateFallbackIcon(System.Windows.Media.Color.FromRgb(0, 120, 215), size);
        }

        public static ImageSource GetDefaultDocumentIcon(int size = 48)
        {
            return CreateFallbackIcon(System.Windows.Media.Color.FromRgb(128, 128, 128), size);
        }

        private static ImageSource CreateFallbackIcon(System.Windows.Media.Color color, int size)
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var brush = new SolidColorBrush(color);
                dc.DrawRoundedRectangle(brush, null, new Rect(4, 4, size - 8, size - 8), 6, 6);
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }
    }
}
