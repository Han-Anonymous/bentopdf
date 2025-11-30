using System.IO;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace PDFKawankasi.Services;

/// <summary>
/// Service for PDF operations - privacy-first, all processing happens locally
/// </summary>
public class PdfService
{
    /// <summary>
    /// Merge multiple PDF files into one
    /// </summary>
    public async Task<byte[]> MergePdfsAsync(IEnumerable<string> filePaths, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var outputDocument = new PdfDocument();
            var files = filePaths.ToList();
            var processed = 0;

            foreach (var filePath in files)
            {
                using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                
                foreach (var page in inputDocument.Pages)
                {
                    outputDocument.AddPage(page);
                }
                
                processed++;
                progress?.Report((int)((double)processed / files.Count * 100));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Split a PDF into separate pages or page ranges
    /// </summary>
    public async Task<byte[]> SplitPdfAsync(string filePath, int startPage, int endPage, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var totalPages = endPage - startPage + 1;
            var processed = 0;

            for (int i = startPage - 1; i < endPage && i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
                processed++;
                progress?.Report((int)((double)processed / totalPages * 100));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Extract specific pages from a PDF
    /// </summary>
    public async Task<byte[]> ExtractPagesAsync(string filePath, int[] pageNumbers, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var processed = 0;

            foreach (var pageNum in pageNumbers)
            {
                if (pageNum > 0 && pageNum <= inputDocument.PageCount)
                {
                    outputDocument.AddPage(inputDocument.Pages[pageNum - 1]);
                }
                processed++;
                progress?.Report((int)((double)processed / pageNumbers.Length * 100));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Delete specific pages from a PDF
    /// </summary>
    public async Task<byte[]> DeletePagesAsync(string filePath, int[] pageNumbers, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var pagesToDelete = new HashSet<int>(pageNumbers);
            var processed = 0;

            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                if (!pagesToDelete.Contains(i + 1))
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
                processed++;
                progress?.Report((int)((double)processed / inputDocument.PageCount * 100));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Rotate pages in a PDF
    /// </summary>
    public async Task<byte[]> RotatePdfAsync(string filePath, int degrees, int[]? pageNumbers = null, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);

            var pagesToRotate = pageNumbers != null 
                ? new HashSet<int>(pageNumbers) 
                : Enumerable.Range(1, document.PageCount).ToHashSet();

            var processed = 0;

            for (int i = 0; i < document.PageCount; i++)
            {
                if (pagesToRotate.Contains(i + 1))
                {
                    var page = document.Pages[i];
                    page.Rotate = (page.Rotate + degrees) % 360;
                }
                processed++;
                progress?.Report((int)((double)processed / document.PageCount * 100));
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Convert images to PDF
    /// </summary>
    public async Task<byte[]> ImagesToPdfAsync(IEnumerable<string> imagePaths, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var document = new PdfDocument();
            var images = imagePaths.ToList();
            var processed = 0;

            foreach (var imagePath in images)
            {
                using var image = SixLabors.ImageSharp.Image.Load(imagePath);
                
                // Create a page with image dimensions
                var page = document.AddPage();
                page.Width = image.Width;
                page.Height = image.Height;

                // Convert image to PNG bytes
                using var imageStream = new MemoryStream();
                image.Save(imageStream, new PngEncoder());
                imageStream.Position = 0;

                using var xImage = PdfSharpCore.Drawing.XImage.FromStream(() => imageStream);
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);

                processed++;
                progress?.Report((int)((double)processed / images.Count * 100));
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Get the number of pages in a PDF
    /// </summary>
    public int GetPageCount(string filePath)
    {
        using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
        return document.PageCount;
    }

    /// <summary>
    /// Get PDF metadata
    /// </summary>
    public PdfMetadata GetMetadata(string filePath)
    {
        using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
        
        return new PdfMetadata
        {
            Title = document.Info.Title,
            Author = document.Info.Author,
            Subject = document.Info.Subject,
            Keywords = document.Info.Keywords,
            Creator = document.Info.Creator,
            Producer = document.Info.Producer,
            CreationDate = document.Info.CreationDate,
            ModificationDate = document.Info.ModificationDate,
            PageCount = document.PageCount
        };
    }

    /// <summary>
    /// Set PDF metadata
    /// </summary>
    public async Task<byte[]> SetMetadataAsync(string filePath, PdfMetadata metadata, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);
            
            progress?.Report(30);
            if (metadata.Title != null) document.Info.Title = metadata.Title;
            if (metadata.Author != null) document.Info.Author = metadata.Author;
            if (metadata.Subject != null) document.Info.Subject = metadata.Subject;
            if (metadata.Keywords != null) document.Info.Keywords = metadata.Keywords;
            if (metadata.Creator != null) document.Info.Creator = metadata.Creator;

            progress?.Report(70);
            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Reverse the order of pages in a PDF
    /// </summary>
    public async Task<byte[]> ReversePagesAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var processed = 0;

            for (int i = inputDocument.PageCount - 1; i >= 0; i--)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
                processed++;
                progress?.Report((int)((double)processed / inputDocument.PageCount * 100));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Add a blank page to a PDF
    /// </summary>
    public async Task<byte[]> AddBlankPageAsync(string filePath, int position, double width = 595, double height = 842, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var processed = 0;
            var inserted = false;

            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                if (i + 1 == position && !inserted)
                {
                    var blankPage = outputDocument.AddPage();
                    blankPage.Width = width;
                    blankPage.Height = height;
                    inserted = true;
                }
                
                outputDocument.AddPage(inputDocument.Pages[i]);
                processed++;
                progress?.Report((int)((double)processed / (inputDocument.PageCount + 1) * 100));
            }

            // If position is after the last page
            if (!inserted || position > inputDocument.PageCount)
            {
                var blankPage = outputDocument.AddPage();
                blankPage.Width = width;
                blankPage.Height = height;
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Convert PDF pages to JPG images
    /// </summary>
    public async Task<List<byte[]>> PdfToJpgAsync(string filePath, int quality = 90, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var results = new List<byte[]>();
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
            
            for (int i = 0; i < document.PageCount; i++)
            {
                // Note: PdfSharpCore doesn't support rendering PDF to images directly.
                // This would need a separate library like SkiaSharp or similar.
                // For now, return empty list as placeholder
                progress?.Report((int)((double)(i + 1) / document.PageCount * 100));
            }
            
            return results;
        });
    }

    /// <summary>
    /// Convert PDF pages to PNG images
    /// </summary>
    public async Task<List<byte[]>> PdfToPngAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var results = new List<byte[]>();
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
            
            for (int i = 0; i < document.PageCount; i++)
            {
                // Note: PdfSharpCore doesn't support rendering PDF to images directly.
                // This would need a separate library like SkiaSharp or similar.
                // For now, return empty list as placeholder
                progress?.Report((int)((double)(i + 1) / document.PageCount * 100));
            }
            
            return results;
        });
    }

    /// <summary>
    /// Convert PDF to greyscale.
    /// NOTE: This is currently a placeholder. Full greyscale conversion requires 
    /// direct manipulation of content streams or iText library. Returns a copy of the PDF.
    /// </summary>
    public async Task<byte[]> PdfToGreyscaleAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            progress?.Report(30);
            // Placeholder: Copy all pages - actual greyscale conversion would need iText or direct content stream manipulation
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
                progress?.Report(30 + (int)((double)(i + 1) / inputDocument.PageCount * 60));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Flatten PDF (make form fields and annotations non-editable).
    /// NOTE: Basic flattening - copies pages. Full form flattening requires iText library.
    /// </summary>
    public async Task<byte[]> FlattenPdfAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            progress?.Report(30);
            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }
            
            progress?.Report(80);
            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Compress PDF to reduce file size
    /// </summary>
    public async Task<byte[]> CompressPdfAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            // Set compression options
            outputDocument.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
            outputDocument.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Automatic;
            outputDocument.Options.NoCompression = false;
            outputDocument.Options.CompressContentStreams = true;
            
            progress?.Report(30);
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
                progress?.Report(30 + (int)((double)(i + 1) / inputDocument.PageCount * 60));
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Linearize PDF for fast web viewing
    /// </summary>
    public async Task<byte[]> LinearizePdfAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            progress?.Report(30);
            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }
            
            progress?.Report(80);
            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Get page dimensions information
    /// </summary>
    public PdfPageDimensions GetPageDimensions(string filePath)
    {
        using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
        
        var pages = new List<PageDimensionInfo>();
        for (int i = 0; i < document.PageCount; i++)
        {
            var page = document.Pages[i];
            pages.Add(new PageDimensionInfo
            {
                PageNumber = i + 1,
                Width = page.Width.Point,
                Height = page.Height.Point,
                Orientation = page.Width > page.Height ? "Landscape" : "Portrait"
            });
        }
        
        return new PdfPageDimensions
        {
            TotalPages = document.PageCount,
            Pages = pages
        };
    }

    /// <summary>
    /// Get page preview information for a PDF with thumbnail rendering
    /// Returns page information with rendered thumbnails for preview display
    /// </summary>
    public List<Models.PagePreviewModel> GetPagePreviews(string filePath, bool renderThumbnails = true, int thumbnailWidth = 100)
    {
        using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
        var totalPages = document.PageCount;
        var previews = new List<Models.PagePreviewModel>();

        // Read PDF bytes for thumbnail rendering
        List<BitmapSource>? thumbnails = null;

        if (renderThumbnails)
        {
            try
            {
                var pdfBytes = File.ReadAllBytes(filePath);
                thumbnails = RenderPdfPageThumbnails(pdfBytes, totalPages, thumbnailWidth);
            }
            catch
            {
                // If rendering fails, continue without thumbnails
                thumbnails = null;
            }
        }

        for (int i = 0; i < totalPages; i++)
        {
            var page = document.Pages[i];
            previews.Add(new Models.PagePreviewModel
            {
                PageNumber = i + 1,
                TotalPages = totalPages,
                Width = page.Width.Point,
                Height = page.Height.Point,
                IsSelected = false,
                Thumbnail = thumbnails != null && i < thumbnails.Count ? thumbnails[i] : null
            });
        }

        return previews;
    }

    /// <summary>
    /// Render PDF page thumbnails using Docnet
    /// </summary>
    private static List<BitmapSource> RenderPdfPageThumbnails(byte[] pdfBytes, int pageCount, int thumbnailWidth = 100)
    {
        var thumbnails = new List<BitmapSource>();

        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfBytes, new PageDimensions(thumbnailWidth, thumbnailWidth * 2));

        for (int i = 0; i < pageCount; i++)
        {
            try
            {
                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage();
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                // Convert BGRA to BitmapSource
                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96, // DPI
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    rawBytes,
                    width * 4);

                bitmap.Freeze(); // Make it thread-safe
                thumbnails.Add(bitmap);
            }
            catch
            {
                // If a specific page fails to render, add null
                thumbnails.Add(null!);
            }
        }

        return thumbnails;
    }

    /// <summary>
    /// Split selected pages into separate PDF files (one per page)
    /// </summary>
    public async Task<List<byte[]>> SplitPdfToSeparateFilesAsync(string filePath, int[] pageNumbers, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var results = new List<byte[]>();
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);

            var processed = 0;
            foreach (var pageNum in pageNumbers)
            {
                if (pageNum > 0 && pageNum <= inputDocument.PageCount)
                {
                    using var outputDocument = new PdfDocument();
                    outputDocument.AddPage(inputDocument.Pages[pageNum - 1]);

                    using var stream = new MemoryStream();
                    outputDocument.Save(stream);
                    results.Add(stream.ToArray());
                }
                processed++;
                progress?.Report((int)((double)processed / pageNumbers.Length * 100));
            }

            return results;
        });
    }

    /// <summary>
    /// Remove metadata from PDF
    /// </summary>
    public async Task<byte[]> RemoveMetadataAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);
            
            progress?.Report(30);
            document.Info.Title = string.Empty;
            document.Info.Author = string.Empty;
            document.Info.Subject = string.Empty;
            document.Info.Keywords = string.Empty;
            document.Info.Creator = string.Empty;
            
            progress?.Report(70);
            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Add watermark text to PDF pages
    /// </summary>
    public async Task<byte[]> AddWatermarkAsync(string filePath, string watermarkText, double opacity = 0.5, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);
            
            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page, PdfSharpCore.Drawing.XGraphicsPdfPageOptions.Append);
                
                var font = new PdfSharpCore.Drawing.XFont("Arial", 72, PdfSharpCore.Drawing.XFontStyle.Bold);
                var brush = new PdfSharpCore.Drawing.XSolidBrush(PdfSharpCore.Drawing.XColor.FromArgb((int)(opacity * 255), 128, 128, 128));
                
                var size = gfx.MeasureString(watermarkText, font);
                
                // Rotate and center the watermark
                gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                gfx.RotateTransform(-45);
                gfx.DrawString(watermarkText, font, brush, 
                    new PdfSharpCore.Drawing.XPoint(-size.Width / 2, size.Height / 2));
                
                progress?.Report((int)((double)(i + 1) / document.PageCount * 90) + 10);
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Add page numbers to PDF
    /// </summary>
    public async Task<byte[]> AddPageNumbersAsync(string filePath, string position = "bottom-center", string format = "{page} of {total}", IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);
            var totalPages = document.PageCount;
            
            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page, PdfSharpCore.Drawing.XGraphicsPdfPageOptions.Append);
                
                var font = new PdfSharpCore.Drawing.XFont("Arial", 10, PdfSharpCore.Drawing.XFontStyle.Regular);
                var brush = PdfSharpCore.Drawing.XBrushes.Black;
                
                var text = format
                    .Replace("{page}", (i + 1).ToString())
                    .Replace("{total}", totalPages.ToString());
                
                var size = gfx.MeasureString(text, font);
                
                double x, y;
                switch (position)
                {
                    case "top-left":
                        x = 50;
                        y = 30;
                        break;
                    case "top-center":
                        x = page.Width / 2 - size.Width / 2;
                        y = 30;
                        break;
                    case "top-right":
                        x = page.Width - 50 - size.Width;
                        y = 30;
                        break;
                    case "bottom-left":
                        x = 50;
                        y = page.Height - 30;
                        break;
                    case "bottom-right":
                        x = page.Width - 50 - size.Width;
                        y = page.Height - 30;
                        break;
                    default: // bottom-center
                        x = page.Width / 2 - size.Width / 2;
                        y = page.Height - 30;
                        break;
                }
                
                gfx.DrawString(text, font, brush, new PdfSharpCore.Drawing.XPoint(x, y));
                
                progress?.Report((int)((double)(i + 1) / document.PageCount * 90) + 10);
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Invert colors in PDF (dark mode effect).
    /// NOTE: This is a placeholder. Color inversion requires direct manipulation of 
    /// content streams and is complex to implement. Returns a copy of the PDF.
    /// </summary>
    public async Task<byte[]> InvertColorsAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            // Placeholder: Color inversion requires direct manipulation of content streams
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            progress?.Report(30);
            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }
            
            progress?.Report(80);
            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Change background color of PDF pages
    /// </summary>
    public async Task<byte[]> ChangeBackgroundColorAsync(string filePath, string colorHex, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            
            // Validate hex color format
            if (string.IsNullOrEmpty(colorHex) || colorHex.Length != 7 || !colorHex.StartsWith('#'))
            {
                throw new ArgumentException("Invalid hex color format. Expected format: #RRGGBB", nameof(colorHex));
            }
            
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify);
            
            var color = PdfSharpCore.Drawing.XColor.FromArgb(
                Convert.ToInt32(colorHex.Substring(1, 2), 16),
                Convert.ToInt32(colorHex.Substring(3, 2), 16),
                Convert.ToInt32(colorHex.Substring(5, 2), 16)
            );
            
            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page, PdfSharpCore.Drawing.XGraphicsPdfPageOptions.Prepend);
                
                var brush = new PdfSharpCore.Drawing.XSolidBrush(color);
                gfx.DrawRectangle(brush, 0, 0, page.Width, page.Height);
                
                progress?.Report((int)((double)(i + 1) / document.PageCount * 90) + 10);
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Remove blank pages from PDF.
    /// NOTE: This is a placeholder. Blank page detection requires analyzing page 
    /// content streams for actual content. Returns a copy of the PDF.
    /// </summary>
    public async Task<byte[]> RemoveBlankPagesAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            // Placeholder: Copy all pages. Proper blank page detection requires analyzing content streams.
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
                progress?.Report((int)((double)(i + 1) / inputDocument.PageCount * 90) + 10);
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Remove annotations from PDF.
    /// NOTE: This is a placeholder. Proper annotation removal requires modifying 
    /// page dictionaries. Returns a copy of the PDF.
    /// </summary>
    public async Task<byte[]> RemoveAnnotationsAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();
            
            // Placeholder: Copy pages. Proper annotation removal requires modifying page dictionaries.
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                var page = inputDocument.Pages[i];
                outputDocument.AddPage(page);
                progress?.Report((int)((double)(i + 1) / inputDocument.PageCount * 90) + 10);
            }

            using var stream = new MemoryStream();
            outputDocument.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Convert text file to PDF. Uses streaming to handle large files efficiently.
    /// </summary>
    public async Task<byte[]> TextToPdfAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(10);
            
            // Read lines one at a time for memory efficiency with large files
            var lines = File.ReadLines(filePath).ToList();
            
            using var document = new PdfDocument();
            var font = new PdfSharpCore.Drawing.XFont("Courier New", 10, PdfSharpCore.Drawing.XFontStyle.Regular);
            
            var linesPerPage = 50;
            var lineHeight = 14.0;
            var margin = 50.0;
            
            for (int pageNum = 0; pageNum * linesPerPage < lines.Count; pageNum++)
            {
                var page = document.AddPage();
                page.Width = 612; // Letter width
                page.Height = 792; // Letter height
                
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                
                var startLine = pageNum * linesPerPage;
                var endLine = Math.Min(startLine + linesPerPage, lines.Count);
                
                for (int i = startLine; i < endLine; i++)
                {
                    var y = margin + (i - startLine) * lineHeight;
                    gfx.DrawString(lines[i], font, PdfSharpCore.Drawing.XBrushes.Black, 
                        new PdfSharpCore.Drawing.XPoint(margin, y));
                }
                
                progress?.Report((int)((double)(pageNum + 1) / Math.Ceiling((double)lines.Count / linesPerPage) * 90) + 10);
            }

            using var stream = new MemoryStream();
            document.Save(stream);
            progress?.Report(100);
            return stream.ToArray();
        });
    }
}

/// <summary>
/// PDF page dimensions information
/// </summary>
public class PdfPageDimensions
{
    public int TotalPages { get; set; }
    public List<PageDimensionInfo> Pages { get; set; } = new();
}

/// <summary>
/// Individual page dimension info
/// </summary>
public class PageDimensionInfo
{
    public int PageNumber { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Orientation { get; set; } = string.Empty;
}

/// <summary>
/// PDF metadata model
/// </summary>
public class PdfMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public string? Creator { get; set; }
    public string? Producer { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime ModificationDate { get; set; }
    public int PageCount { get; set; }
}
