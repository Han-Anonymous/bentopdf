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
    
    /// <summary>
    /// Full-size page image for continuous scroll view
    /// </summary>
    public BitmapSource? PageFullImage { get; set; }

    [ObservableProperty]
    private bool _isCurrentPage;

    [ObservableProperty]
    private bool _isSelected;
}
