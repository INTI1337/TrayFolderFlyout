using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TrayFolderFlyout.Services
{
    public class IconPresetItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Glyph { get; set; } = "";
        public System.Drawing.Color ColorStart { get; set; }
        public System.Drawing.Color ColorEnd { get; set; }
        public System.Drawing.Color? GlyphColor { get; set; }
        public string IconPath { get; set; } = "";

        public ImageSource IconSource => IconService.GetIcon(null, IconPath, 48);
    }

    public static class PresetIconsService
    {
        private static readonly List<IconPresetItem> _presets = new();
        private static bool _initialized = false;

        public static IReadOnlyList<IconPresetItem> GetPresets()
        {
            EnsureInitialized();
            return _presets;
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;

            string presetsDir = Path.Combine(ConfigService.Instance.IconsDirectory, "Presets");
            if (!Directory.Exists(presetsDir))
            {
                Directory.CreateDirectory(presetsDir);
            }

            string markerFile = Path.Combine(presetsDir, "v2_bold.marker");
            if (!File.Exists(markerFile))
            {
                try
                {
                    foreach (var oldIco in Directory.GetFiles(presetsDir, "*.ico"))
                    {
                        ConfigService.TryDeleteOldFile(oldIco);
                    }
                    File.WriteAllText(markerFile, "v2_bold");
                }
                catch { }
            }

            var definitions = new[]
            {
                // 1. Games: #6366F1 -> #4338CA, Segoe Fluent \uE7FC (Gamepad)
                new {
                    Key = "games",
                    Name = "Игры",
                    Glyph = "\uE7FC",
                    ColorStart = System.Drawing.Color.FromArgb(99, 102, 241),
                    ColorEnd = System.Drawing.Color.FromArgb(67, 56, 202),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 2. Work: #3B82F6 -> #1D4ED8, Segoe Fluent \uE821 (Briefcase)
                new {
                    Key = "work",
                    Name = "Работа",
                    Glyph = "\uE821",
                    ColorStart = System.Drawing.Color.FromArgb(59, 130, 246),
                    ColorEnd = System.Drawing.Color.FromArgb(29, 78, 216),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 3. System: #06B6D4 -> #0E7490, Segoe Fluent \uE713 (Settings)
                new {
                    Key = "system",
                    Name = "Система",
                    Glyph = "\uE713",
                    ColorStart = System.Drawing.Color.FromArgb(6, 182, 212),
                    ColorEnd = System.Drawing.Color.FromArgb(14, 116, 144),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 4. Media: #F43F5E -> #BE123C, Segoe Fluent \uE768 (Play)
                new {
                    Key = "media",
                    Name = "Медиа",
                    Glyph = "\uE768",
                    ColorStart = System.Drawing.Color.FromArgb(244, 63, 94),
                    ColorEnd = System.Drawing.Color.FromArgb(190, 18, 60),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 5. Code: #27272A -> #18181B (Dark Carbon) with neon green glyph #10B981
                new {
                    Key = "code",
                    Name = "Код",
                    Glyph = "\uE943",
                    ColorStart = System.Drawing.Color.FromArgb(39, 39, 42),
                    ColorEnd = System.Drawing.Color.FromArgb(24, 24, 27),
                    GlyphColor = (System.Drawing.Color?)System.Drawing.Color.FromArgb(16, 185, 129)
                },
                // 6. Web: #0EA5E9 -> #0369A1, Segoe Fluent \uE774 (Globe)
                new {
                    Key = "web",
                    Name = "Интернет",
                    Glyph = "\uE774",
                    ColorStart = System.Drawing.Color.FromArgb(14, 165, 233),
                    ColorEnd = System.Drawing.Color.FromArgb(3, 105, 161),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 7. Music: #D946EF -> #A21CAF, Segoe Fluent \uE8D6 (Music note)
                new {
                    Key = "music",
                    Name = "Музыка",
                    Glyph = "\uE8D6",
                    ColorStart = System.Drawing.Color.FromArgb(217, 70, 239),
                    ColorEnd = System.Drawing.Color.FromArgb(162, 28, 175),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 8. Downloads: #F59E0B -> #B45309, Segoe Fluent \uE896 (Download arrow)
                new {
                    Key = "downloads",
                    Name = "Загрузки",
                    Glyph = "\uE896",
                    ColorStart = System.Drawing.Color.FromArgb(245, 158, 11),
                    ColorEnd = System.Drawing.Color.FromArgb(180, 83, 9),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 9. Messages: #10B981 -> #047857, Segoe Fluent \uE8BD (Chat bubble)
                new {
                    Key = "messages",
                    Name = "Сообщения",
                    Glyph = "\uE8BD",
                    ColorStart = System.Drawing.Color.FromArgb(16, 185, 129),
                    ColorEnd = System.Drawing.Color.FromArgb(4, 120, 87),
                    GlyphColor = (System.Drawing.Color?)null
                },
                // 10. Favorites: #F97316 -> #C2410C, Segoe Fluent \uE735 (Star)
                new {
                    Key = "favorites",
                    Name = "Избранное",
                    Glyph = "\uE735",
                    ColorStart = System.Drawing.Color.FromArgb(249, 115, 22),
                    ColorEnd = System.Drawing.Color.FromArgb(194, 65, 12),
                    GlyphColor = (System.Drawing.Color?)null
                }
            };

            _presets.Clear();

            foreach (var def in definitions)
            {
                string iconPath = Path.Combine(presetsDir, $"{def.Key}.ico");
                if (!File.Exists(iconPath))
                {
                    ConfigService.Instance.GenerateDefaultGroupIcon(
                        iconPath,
                        def.ColorStart,
                        def.ColorEnd,
                        def.Glyph,
                        def.GlyphColor);
                }

                _presets.Add(new IconPresetItem
                {
                    Key = def.Key,
                    Name = def.Name,
                    Glyph = def.Glyph,
                    ColorStart = def.ColorStart,
                    ColorEnd = def.ColorEnd,
                    GlyphColor = def.GlyphColor,
                    IconPath = iconPath
                });
            }

            _initialized = true;
        }
    }
}
