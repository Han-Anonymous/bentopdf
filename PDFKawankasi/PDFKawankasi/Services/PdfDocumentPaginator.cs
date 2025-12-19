using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace PDFKawankasi.Services;

/// <summary>
/// A custom DocumentPaginator implementation for printing PDF documents
/// using the Windows PrintDialog. This renders PDF pages as high-quality images
/// for printing with proper scaling.
/// </summary>
public class PdfDocumentPaginator : DocumentPaginator
{
    private readonly byte[] _pdfBytes;
    private readonly IDocLib _docLib;
    private readonly int _pageCount;
    private Size _pageSize;
    private readonly int _startPage;
    private readonly int _endPage;
    private readonly int _printablePageCount;
    private readonly double _renderScale;

    /// <summary>
    /// Creates a paginator for all pages in the PDF
    /// </summary>
    public PdfDocumentPaginator(byte[] pdfBytes, IDocLib docLib, Size pageSize, double renderScale = 3.0)
        : this(pdfBytes, docLib, pageSize, 1, -1, renderScale)
    {
    }

    /// <summary>
    /// Creates a paginator for a specific page range
    /// </summary>
    /// <param name="pdfBytes">PDF document bytes</param>
    /// <param name="docLib">Docnet library instance</param>
    /// <param name="pageSize">Target print page size</param>
    /// <param name="startPage">1-based start page number</param>
    /// <param name="endPage">1-based end page number (-1 for all remaining pages)</param>
    /// <param name="renderScale">Scale factor for rendering (higher = better quality, slower)</param>
    public PdfDocumentPaginator(byte[] pdfBytes, IDocLib docLib, Size pageSize, int startPage, int endPage, double renderScale = 3.0)
    {
        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        _docLib = docLib ?? throw new ArgumentNullException(nameof(docLib));
        _pageSize = pageSize;
        _renderScale = renderScale;

        // Get total page count
        using var reader = _docLib.GetDocReader(_pdfBytes, new PageDimensions(100, 100));
        _pageCount = reader.GetPageCount();

        // Validate and set page range
        _startPage = Math.Max(1, startPage);
        _endPage = endPage < 1 ? _pageCount : Math.Min(endPage, _pageCount);
        _printablePageCount = Math.Max(0, _endPage - _startPage + 1);
    }

    public override bool IsPageCountValid => true;

    public override int PageCount => _printablePageCount;

    public override Size PageSize
    {
        get => _pageSize;
        set => _pageSize = value;
    }

    public override IDocumentPaginatorSource? Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        // Convert from 0-based paginator page to actual PDF page
        int actualPage = _startPage + pageNumber - 1;
        
        if (actualPage < 1 || actualPage > _pageCount)
        {
            return DocumentPage.Missing;
        }

        try
        {
            // Render at high DPI for quality printing (300 DPI equivalent)
            // We calculate render dimensions based on page size and target quality
            int renderWidth = (int)(_pageSize.Width * _renderScale);
            int renderHeight = (int)(_pageSize.Height * _renderScale);

            using var reader = _docLib.GetDocReader(_pdfBytes, new PageDimensions(renderWidth, renderHeight));
            using var pageReader = reader.GetPageReader(actualPage - 1); // 0-based index

            var pageWidth = pageReader.GetPageWidth();
            var pageHeight = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage();

            // Create a high-quality bitmap from the PDF page
            var bitmap = BitmapSource.Create(
                pageWidth,
                pageHeight,
                96 * _renderScale, // DPI
                96 * _renderScale,
                PixelFormats.Bgra32,
                null,
                rawBytes,
                pageWidth * 4);
            bitmap.Freeze();

            // Calculate scaling to fit the page while maintaining aspect ratio
            double scaleX = _pageSize.Width / pageWidth;
            double scaleY = _pageSize.Height / pageHeight;
            double scale = Math.Min(scaleX, scaleY);

            double scaledWidth = pageWidth * scale;
            double scaledHeight = pageHeight * scale;

            // Center the image on the page
            double offsetX = (_pageSize.Width - scaledWidth) / 2;
            double offsetY = (_pageSize.Height - scaledHeight) / 2;

            // Create a DrawingVisual to render the page
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Draw the PDF page image scaled to fit
                context.DrawImage(bitmap, new Rect(offsetX, offsetY, scaledWidth, scaledHeight));
            }

            return new DocumentPage(visual, _pageSize, new Rect(_pageSize), new Rect(_pageSize));
        }
        catch (Exception)
        {
            return DocumentPage.Missing;
        }
    }
}
