using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace PDFKawankasi
{
    /// <summary>
    /// Utility to convert SVG logo to PNG files for app icons
    /// Run this once to generate all required PNG assets
    /// </summary>
    public static class ConvertSvgToPng
    {
        public static void ConvertLogo()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir).Parent.Parent.Parent.FullName;
            var svgPath = Path.Combine(projectRoot, "Assets", "PDF Editor", "Kawankasi", "PDF Kawankasi logo.svg");
            var assetsPath = Path.Combine(projectRoot, "Assets");

            if (!File.Exists(svgPath))
            {
                Console.WriteLine($"SVG file not found: {svgPath}");
                return;
            }

            // Define all required sizes
            var icons = new[]
            {
                new { Name = "Square44x44Logo", Width = 44, Height = 44 },
                new { Name = "Square71x71Logo", Width = 71, Height = 71 },
                new { Name = "Square150x150Logo", Width = 150, Height = 150 },
                new { Name = "Wide310x150Logo", Width = 310, Height = 150 },
                new { Name = "Square310x310Logo", Width = 310, Height = 310 },
                new { Name = "StoreLogo", Width = 50, Height = 50 },
                new { Name = "SplashScreen", Width = 620, Height = 300 },
                new { Name = "icon", Width = 256, Height = 256 }
            };

            Console.WriteLine("Converting SVG logo to PNG files...\n");

            foreach (var icon in icons)
            {
                try
                {
                    var outputPath = Path.Combine(assetsPath, $"{icon.Name}.png");
                    ConvertSvgToPngFile(svgPath, outputPath, icon.Width, icon.Height);
                    Console.WriteLine($"✓ Created: {icon.Name}.png ({icon.Width} x {icon.Height})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to create {icon.Name}.png: {ex.Message}");
                }
            }

            Console.WriteLine("\nConversion complete!");
        }

        private static void ConvertSvgToPngFile(string svgPath, string pngPath, int width, int height)
        {
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = true,
                TextAsGeometry = false
            };

            using (var stream = File.OpenRead(svgPath))
            {
                var converter = new FileSvgReader(settings);
                var drawing = converter.Read(stream);

                if (drawing == null)
                {
                    throw new Exception("Failed to read SVG file");
                }

                // Render at 4x resolution for better quality, then scale down
                int superSampleFactor = 4;
                int renderWidth = width * superSampleFactor;
                int renderHeight = height * superSampleFactor;

                // Create render target bitmap with high DPI for crisp rendering
                var renderTarget = new RenderTargetBitmap(
                    renderWidth, renderHeight, 96 * superSampleFactor, 96 * superSampleFactor, PixelFormats.Pbgra32);

                // Create drawing visual with high quality rendering
                var drawingVisual = new DrawingVisual();
                
                // Enable high quality rendering
                RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(drawingVisual, EdgeMode.Unspecified); // Enables anti-aliasing
                
                using (var context = drawingVisual.RenderOpen())
                {
                    // Scale the drawing to fit the target size
                    var bounds = drawing.Bounds;
                    var scaleX = renderWidth / bounds.Width;
                    var scaleY = renderHeight / bounds.Height;
                    var scale = Math.Min(scaleX, scaleY);

                    // Center the drawing
                    var offsetX = (renderWidth - bounds.Width * scale) / 2 - bounds.Left * scale;
                    var offsetY = (renderHeight - bounds.Height * scale) / 2 - bounds.Top * scale;

                    context.PushTransform(new TranslateTransform(offsetX, offsetY));
                    context.PushTransform(new ScaleTransform(scale, scale));
                    context.DrawDrawing(drawing);
                }

                // Render to high-resolution bitmap
                renderTarget.Render(drawingVisual);

                // Scale down to target size with high quality
                var scaledBitmap = new TransformedBitmap(renderTarget, 
                    new ScaleTransform(1.0 / superSampleFactor, 1.0 / superSampleFactor));

                // Save as PNG with maximum quality
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(scaledBitmap));

                using (var fileStream = File.Create(pngPath))
                {
                    encoder.Save(fileStream);
                }
            }
        }
    }
}
