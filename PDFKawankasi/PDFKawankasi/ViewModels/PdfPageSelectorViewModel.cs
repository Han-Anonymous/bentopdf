using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Docnet.Core;
using Docnet.Core.Models;
using PDFKawankasi.Models;

namespace PDFKawankasi.ViewModels;

/// <summary>
/// ViewModel for PDF page selection dialog
/// </summary>
public partial class PdfPageSelectorViewModel : ObservableObject
{
    private const int ThumbnailWidth = 100;
    private const int ThumbnailHeight = 140;

    [ObservableProperty]
    private ObservableCollection<PageThumbnailModel> _pages = new();

    public PdfPageSelectorViewModel(string pdfFilePath)
    {
        LoadPdfPages(pdfFilePath);
    }

    private void LoadPdfPages(string filePath)
    {
        try
        {
            var pdfBytes = File.ReadAllBytes(filePath);
            // Create a new DocLib instance to avoid conflicts with the main editor's instance
            using var docLib = DocLib.Instance;
            using var reader = docLib.GetDocReader(pdfBytes, new PageDimensions(ThumbnailWidth, ThumbnailHeight));

            var pageCount = reader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = reader.GetPageReader(i);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage();

                var thumbnail = CreateBitmapFromRawBytes(rawBytes, width, height);

                Pages.Add(new PageThumbnailModel
                {
                    PageNumber = i + 1,
                    Thumbnail = thumbnail,
                    IsSelected = false
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading PDF pages: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static BitmapSource CreateBitmapFromRawBytes(byte[] rawBytes, int width, int height)
    {
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            rawBytes,
            width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public List<int> GetSelectedPageNumbers()
    {
        return Pages.Where(p => p.IsSelected).Select(p => p.PageNumber).ToList();
    }

    public void SelectAll()
    {
        foreach (var page in Pages)
        {
            page.IsSelected = true;
        }
    }

    public void DeselectAll()
    {
        foreach (var page in Pages)
        {
            page.IsSelected = false;
        }
    }
}
