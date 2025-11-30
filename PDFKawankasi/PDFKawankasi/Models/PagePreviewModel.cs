using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFKawankasi.Models;

/// <summary>
/// Model representing a PDF page preview with selection state
/// </summary>
public partial class PagePreviewModel : ObservableObject
{
    /// <summary>
    /// The page number (1-based)
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// The total number of pages in the PDF
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Width of the page in points
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Height of the page in points
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Whether this page is currently selected for splitting
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Display name for the page (e.g., "Page 1 of 10")
    /// </summary>
    public string DisplayName => $"Page {PageNumber} of {TotalPages}";

    /// <summary>
    /// Orientation of the page
    /// </summary>
    public string Orientation => Width > Height ? "Landscape" : "Portrait";
}

/// <summary>
/// Mode for how pages should be split
/// </summary>
public enum SplitMode
{
    /// <summary>
    /// Split selected pages into a single PDF file
    /// </summary>
    SingleFile,

    /// <summary>
    /// Split selected pages into separate PDF files (one per page)
    /// </summary>
    SeparateFiles
}
