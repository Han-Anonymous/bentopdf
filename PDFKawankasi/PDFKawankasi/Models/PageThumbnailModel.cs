using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFKawankasi.Models;

/// <summary>
/// Represents a page thumbnail in the PDF Editor
/// </summary>
public partial class PageThumbnailModel : ObservableObject
{
    public int PageNumber { get; set; }
    public BitmapSource? Thumbnail { get; set; }

    [ObservableProperty]
    private bool _isCurrentPage;

    [ObservableProperty]
    private bool _isSelected;
}
