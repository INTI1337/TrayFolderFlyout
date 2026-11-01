using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TrayFolderFlyout.Services
{
    public static class IcoConverter
    {
        private static readonly int[] StandardSizes = new[] { 16, 32, 48, 64, 256 };

        public static void CreateMultiResIcon(string sourcePath, string outputIcoPath)
        {
            var outputDir = Path.GetDirectoryName(outputIcoPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using Bitmap masterBitmap = LoadMasterBitmap(sourcePath);
            SaveAsMultiResIco(masterBitmap, outputIcoPath);
        }

        private static Bitmap LoadMasterBitmap(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".exe" || ext == ".dll")
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    return icon.ToBitmap();
                }
            }
            else if (ext == ".ico")
            {
                // Try to load largest available icon from memory stream (never lock file)
                byte[] icoBytes = File.ReadAllBytes(path);
                try
                {
                    using var ms = new MemoryStream(icoBytes);
                    using var icon = new Icon(ms, 256, 256);
                    return icon.ToBitmap();
                }
                catch
                {
                    using var ms = new MemoryStream(icoBytes);
                    using var icon = new Icon(ms);
                    return icon.ToBitmap();
                }
            }

            // Normal image files (PNG, JPG, BMP) - read bytes to never lock the file on disk
            byte[] fileBytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(fileBytes);
            using var original = (Bitmap)Image.FromStream(stream);
            return new Bitmap(original);
        }

        public static void SaveAsMultiResIco(Bitmap master, string outputPath)
        {
            var imagesData = new byte[StandardSizes.Length][];

            for (int i = 0; i < StandardSizes.Length; i++)
            {
                int size = StandardSizes[i];
                using var resized = ResizeImage(master, size, size);

                using var ms = new MemoryStream();
                // 256x256 and smaller sizes as PNG inside ICO (modern Windows 10/11 shell standard)
                resized.Save(ms, ImageFormat.Png);
                imagesData[i] = ms.ToArray();
            }

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            // ICONDIR header
            bw.Write((ushort)0); // Reserved
            bw.Write((ushort)1); // Type = 1 (Icon)
            bw.Write((ushort)StandardSizes.Length); // Image count

            // Calculate header offset: 6 bytes + 16 * count
            int currentOffset = 6 + (16 * StandardSizes.Length);

            // Write Directory entries
            for (int i = 0; i < StandardSizes.Length; i++)
            {
                int size = StandardSizes[i];
                byte bWidth = (byte)(size >= 256 ? 0 : size);
                byte bHeight = (byte)(size >= 256 ? 0 : size);

                bw.Write(bWidth);
                bw.Write(bHeight);
                bw.Write((byte)0); // Color count
                bw.Write((byte)0); // Reserved
                bw.Write((ushort)1); // Color planes
                bw.Write((ushort)32); // Bits per pixel
                bw.Write((uint)imagesData[i].Length); // Size of image data
                bw.Write((uint)currentOffset); // Offset

                currentOffset += imagesData[i].Length;
            }

            // Write image data blocks
            for (int i = 0; i < StandardSizes.Length; i++)
            {
                bw.Write(imagesData[i]);
            }
        }

        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            destImage.SetResolution(image.HorizontalResolution > 0 ? image.HorizontalResolution : 96,
                                    image.VerticalResolution > 0 ? image.VerticalResolution : 96);

            using var graphics = Graphics.FromImage(destImage);
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Fit aspect ratio uniformly inside square icon frame (centered with transparent margins)
            float scale = Math.Min((float)width / image.Width, (float)height / image.Height);
            int scaledW = Math.Max(1, (int)Math.Round(image.Width * scale));
            int scaledH = Math.Max(1, (int)Math.Round(image.Height * scale));
            int destX = (width - scaledW) / 2;
            int destY = (height - scaledH) / 2;
            var destRect = new Rectangle(destX, destY, scaledW, scaledH);

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

            return destImage;
        }
    }
}
