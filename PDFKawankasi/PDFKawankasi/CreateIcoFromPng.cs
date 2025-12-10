using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace PDFKawankasi
{
    /// <summary>
    /// Creates a proper Windows ICO file from PNG
    /// </summary>
    public static class CreateIcoFromPng
    {
        public static void CreateIco()
        {
            var projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            if (projectRoot == null) return;

            var pngPath = Path.Combine(projectRoot, "Assets", "icon.png");
            var icoPath = Path.Combine(projectRoot, "Assets", "app-icon.ico");

            if (!File.Exists(pngPath))
            {
                Console.WriteLine($"PNG file not found: {pngPath}");
                return;
            }

            Console.WriteLine("Creating ICO file from PNG...");

            try
            {
                // Load the PNG image
                using var pngStream = File.OpenRead(pngPath);
                using var originalImage = System.Drawing.Image.FromStream(pngStream);

                // Create ICO with multiple sizes
                int[] sizes = { 16, 32, 48, 64, 128, 256 };
                
                using var icoStream = File.Create(icoPath);
                using var writer = new BinaryWriter(icoStream);

                // ICO header
                writer.Write((short)0); // Reserved
                writer.Write((short)1); // Type (1 = ICO)
                writer.Write((short)sizes.Length); // Number of images

                long dataOffset = 6 + (16 * sizes.Length); // Header + directory entries

                // Write directory entries
                var imageData = new System.Collections.Generic.List<byte[]>();
                foreach (var size in sizes)
                {
                    // Create bitmap with transparency support
                    using var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                    using var graphics = Graphics.FromImage(resized);
                    graphics.Clear(Color.Transparent);
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, size, size);
                    
                    using var ms = new MemoryStream();
                    resized.Save(ms, ImageFormat.Png);
                    var data = ms.ToArray();
                    imageData.Add(data);

                    writer.Write((byte)size); // Width (0 means 256)
                    writer.Write((byte)size); // Height (0 means 256)
                    writer.Write((byte)0); // Color palette
                    writer.Write((byte)0); // Reserved
                    writer.Write((short)1); // Color planes
                    writer.Write((short)32); // Bits per pixel
                    writer.Write((int)data.Length); // Image data size
                    writer.Write((int)dataOffset); // Offset to image data
                    
                    dataOffset += data.Length;
                }

                // Write image data
                foreach (var data in imageData)
                {
                    writer.Write(data);
                }

                Console.WriteLine($"✓ Created: app-icon.ico");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to create ICO: {ex.Message}");
            }
        }
    }
}
