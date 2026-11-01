using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TrayFolderFlyout.Services
{
    public static class AppIconService
    {
        public static void EnsureAppIcon(string outputPath)
        {
            if (File.Exists(outputPath)) return;

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                var sri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
                if (sri?.Stream != null)
                {
                    using var fs = File.Create(outputPath);
                    sri.Stream.CopyTo(fs);
                    return;
                }
            }
            catch { }

            using var bmp = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Squircle 224x224 (radius 52) centered with 16px margins
                var mainRect = new Rectangle(16, 16, 224, 224);
                using var mainPath = GetRoundedRect(mainRect, 52);

                // Gradient #4F46E5 -> #7C3AED
                var c1 = Color.FromArgb(79, 70, 229);
                var c2 = Color.FromArgb(124, 58, 237);
                using var gradBrush = new LinearGradientBrush(
                    new Point(16, 16),
                    new Point(240, 240),
                    c1,
                    c2);
                g.FillPath(gradBrush, mainPath);

                // 1.5px glowing border #60FFFFFF
                using var borderPen = new Pen(Color.FromArgb(96, 255, 255, 255), 1.5f);
                g.DrawPath(borderPen, mainPath);

                // Bento Grid: 4 rounded tiles
                int tileW = 62;
                int tileH = 62;
                int gap = 14;
                int startX = (int)(128 - (tileW * 2 + gap) / 2.0); // 59
                int startY = (int)(128 - (tileH * 2 + gap) / 2.0); // 59

                using var tileBrush = new SolidBrush(Color.FromArgb(250, 255, 255, 255));
                using var tileShadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));

                Point[] coords =
                {
                    new(startX, startY),
                    new(startX + tileW + gap, startY),
                    new(startX, startY + tileH + gap),
                    new(startX + tileW + gap, startY + tileH + gap)
                };

                foreach (var pt in coords)
                {
                    // Drop shadow offset 2px down
                    var sRect = new Rectangle(pt.X, pt.Y + 2, tileW, tileH);
                    using var sPath = GetRoundedRect(sRect, 16);
                    g.FillPath(tileShadow, sPath);

                    // Tile
                    var tRect = new Rectangle(pt.X, pt.Y, tileW, tileH);
                    using var tPath = GetRoundedRect(tRect, 16);
                    g.FillPath(tileBrush, tPath);
                }
            }

            IcoConverter.SaveAsMultiResIco(bmp, outputPath);
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);
            var path = new GraphicsPath();

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
