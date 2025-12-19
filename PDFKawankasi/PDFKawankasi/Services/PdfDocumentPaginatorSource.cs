using System.Windows.Documents;

namespace PDFKawankasi.Services;

/// <summary>
/// Wrapper to provide IDocumentPaginatorSource for DocumentViewer
/// </summary>
public class PdfDocumentPaginatorSource : IDocumentPaginatorSource
{
    public DocumentPaginator DocumentPaginator { get; }

    public PdfDocumentPaginatorSource(DocumentPaginator paginator)
    {
        DocumentPaginator = paginator;
    }
}
