using System.IO;
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
