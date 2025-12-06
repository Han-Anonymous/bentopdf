using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDFKawankasi.Models;
using PDFKawankasi.Services;
using System.Collections.ObjectModel;

namespace PDFKawankasi.ViewModels;

/// <summary>
/// Main ViewModel for the PDF Kawankasi application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly PdfService _pdfService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ToolCategoryModel> _categories;

    [ObservableProperty]
    private ObservableCollection<ToolCategoryModel> _filteredCategories;

    [ObservableProperty]
    private PdfTool? _selectedTool;

    [ObservableProperty]
    private bool _isToolViewVisible;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<string> _selectedFiles = new();

    public MainViewModel()
    {
        _pdfService = new PdfService();
        var categories = ToolsService.GetCategories();
        _categories = new ObservableCollection<ToolCategoryModel>(categories);
        _filteredCategories = new ObservableCollection<ToolCategoryModel>(categories);
        
        // Auto-select PDF Editor as default tool
        var pdfEditorTool = ToolsService.GetAllTools().FirstOrDefault(t => t.ToolType == ToolType.PdfEditor);
        if (pdfEditorTool != null)
        {
            SelectTool(pdfEditorTool);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterTools();
    }

    private void FilterTools()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredCategories = new ObservableCollection<ToolCategoryModel>(Categories);
            return;
        }

        var lowerQuery = SearchQuery.ToLowerInvariant();
        var filtered = Categories
            .Select(category => new ToolCategoryModel
            {
                Name = category.Name,
                Tools = category.Tools
                    .Where(t => t.Name.ToLowerInvariant().Contains(lowerQuery) ||
                               (t.Subtitle?.ToLowerInvariant().Contains(lowerQuery) ?? false))
                    .ToList()
            })
            .Where(c => c.Tools.Count > 0)
            .ToList();

        FilteredCategories = new ObservableCollection<ToolCategoryModel>(filtered);
    }

    [RelayCommand]
    private void SelectTool(PdfTool tool)
    {
        SelectedTool = tool;
        IsToolViewVisible = true;
        SelectedFiles.Clear();
        PagePreviews.Clear();
        IsSplitPreviewVisible = false;
        StatusMessage = $"Selected: {tool.Name}";
    }

    [RelayCommand]
    private void BackToGrid()
    {
        IsToolViewVisible = false;
        SelectedTool = null;
        SelectedFiles.Clear();
        PagePreviews.Clear();
        IsSplitPreviewVisible = false;
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void AddFiles(string[] filePaths)
    {
        // For Split PDF, only allow one file at a time and show preview
        if (SelectedTool?.ToolType == ToolType.Split)
        {
            if (filePaths.Length > 0)
            {
                var filePath = filePaths[0];
                SelectedFiles.Clear();
                SelectedFiles.Add(filePath);
                LoadPagePreviews(filePath);
            }
        }
        else
        {
            foreach (var path in filePaths)
            {
                if (!SelectedFiles.Contains(path))
                {
                    SelectedFiles.Add(path);
                }
            }
            StatusMessage = $"{SelectedFiles.Count} file(s) selected";
        }
    }

    private void LoadPagePreviews(string filePath)
    {
        try
        {
            var previews = _pdfService.GetPagePreviews(filePath);
            PagePreviews.Clear();
            foreach (var preview in previews)
            {
                PagePreviews.Add(preview);
            }
            IsSplitPreviewVisible = true;
            SplitMode = SplitMode.SingleFile;
            StatusMessage = $"Loaded {PagePreviews.Count} pages. Select pages to split.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading PDF: {ex.Message}";
            IsSplitPreviewVisible = false;
        }
    }

    [RelayCommand]
    private void RemoveFile(string filePath)
    {
        SelectedFiles.Remove(filePath);
        if (SelectedTool?.ToolType == ToolType.Split)
        {
            PagePreviews.Clear();
            IsSplitPreviewVisible = false;
        }
        StatusMessage = $"{SelectedFiles.Count} file(s) selected";
    }

    [RelayCommand]
    private void ClearFiles()
    {
        SelectedFiles.Clear();
        PagePreviews.Clear();
        IsSplitPreviewVisible = false;
        StatusMessage = "Files cleared";
    }

    // Split PDF specific properties
    [ObservableProperty]
    private ObservableCollection<PagePreviewModel> _pagePreviews = new();

    [ObservableProperty]
    private bool _isSplitPreviewVisible;

    [ObservableProperty]
    private SplitMode _splitMode = SplitMode.SingleFile;

    [RelayCommand]
    private void SelectAllPages()
    {
        foreach (var page in PagePreviews)
        {
            page.IsSelected = true;
        }
        UpdateSelectedPageCount();
        StatusMessage = $"All {PagePreviews.Count} pages selected";
    }

    [RelayCommand]
    private void DeselectAllPages()
    {
        foreach (var page in PagePreviews)
        {
            page.IsSelected = false;
        }
        UpdateSelectedPageCount();
        StatusMessage = "All pages deselected";
    }

    [RelayCommand]
    private void TogglePageSelection(PagePreviewModel page)
    {
        page.IsSelected = !page.IsSelected;
        UpdateSelectedPageCount();
        StatusMessage = $"{SelectedPageCount} page(s) selected";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPages))]
    private int _selectedPageCount;

    public bool HasSelectedPages => SelectedPageCount > 0;

    private void UpdateSelectedPageCount()
    {
        SelectedPageCount = PagePreviews.Count(p => p.IsSelected);
    }

    // Additional properties for tool-specific inputs
    [ObservableProperty]
    private int _startPage = 1;

    [ObservableProperty]
    private int _endPage = 1;

    [ObservableProperty]
    private string _pageNumbers = string.Empty;

    [ObservableProperty]
    private int _rotationDegrees = 90;

    [ObservableProperty]
    private string _watermarkText = "CONFIDENTIAL";

    [ObservableProperty]
    private double _watermarkOpacity = 0.5;

    [ObservableProperty]
    private string _pageNumberPosition = "bottom-center";

    [ObservableProperty]
    private string _pageNumberFormat = "{page} of {total}";

    [ObservableProperty]
    private string _backgroundColor = "#FFFFFF";

    [ObservableProperty]
    private int _blankPagePosition = 1;

    [RelayCommand]
    private async Task ProcessFilesAsync()
    {
        if (SelectedTool == null || SelectedFiles.Count == 0)
        {
            StatusMessage = "Please select files first";
            return;
        }

        IsProcessing = true;
        ProgressValue = 0;
        StatusMessage = "Processing...";

        try
        {
            var progress = new Progress<int>(value => ProgressValue = value);
            byte[]? result = null;
            string? metadataResult = null;

            switch (SelectedTool.ToolType)
            {
                case ToolType.Merge:
                    result = await _pdfService.MergePdfsAsync(SelectedFiles, progress);
                    break;

                case ToolType.Split:
                    if (SelectedFiles.Count > 0)
                    {
                        var selectedPageNumbers = PagePreviews
                            .Where(p => p.IsSelected)
                            .Select(p => p.PageNumber)
                            .OrderBy(p => p)
                            .ToArray();

                        if (selectedPageNumbers.Length == 0)
                        {
                            StatusMessage = "Please select at least one page to split";
                            IsProcessing = false;
                            return;
                        }

                        if (SplitMode == SplitMode.SeparateFiles)
                        {
                            // Split into separate files
                            var separateResults = await _pdfService.SplitPdfToSeparateFilesAsync(SelectedFiles[0], selectedPageNumbers, progress);
                            
                            // Save each file separately using CommonOpenFileDialog for folder selection
                            var baseName = Path.GetFileNameWithoutExtension(SelectedFiles[0]);
                            
                            using var folderDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
                            {
                                Title = "Select folder to save split PDFs",
                                IsFolderPicker = true,
                                InitialDirectory = Path.GetDirectoryName(SelectedFiles[0])
                            };

                            if (folderDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                            {
                                var folderPath = folderDialog.FileName;
                                for (int i = 0; i < separateResults.Count; i++)
                                {
                                    var fileName = Path.Combine(folderPath, $"{baseName}_page_{selectedPageNumbers[i]}.pdf");
                                    await File.WriteAllBytesAsync(fileName, separateResults[i]);
                                }
                                StatusMessage = $"Saved {separateResults.Count} PDF files to {folderPath}";
                            }
                            else
                            {
                                StatusMessage = "Save cancelled";
                            }
                        }
                        else
                        {
                            // Split into single file with selected pages
                            result = await _pdfService.ExtractPagesAsync(SelectedFiles[0], selectedPageNumbers, progress);
                        }
                    }
                    break;

                case ToolType.ExtractPages:
                    if (SelectedFiles.Count > 0)
                    {
                        var pages = ParsePageNumbers(PageNumbers);
                        result = await _pdfService.ExtractPagesAsync(SelectedFiles[0], pages, progress);
                    }
                    break;

                case ToolType.DeletePages:
                    if (SelectedFiles.Count > 0)
                    {
                        var pagesToDelete = ParsePageNumbers(PageNumbers);
                        result = await _pdfService.DeletePagesAsync(SelectedFiles[0], pagesToDelete, progress);
                    }
                    break;

                case ToolType.Rotate:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.RotatePdfAsync(SelectedFiles[0], RotationDegrees, null, progress);
                    }
                    break;

                case ToolType.ReversePages:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.ReversePagesAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.AddBlankPage:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.AddBlankPageAsync(SelectedFiles[0], BlankPagePosition, 595, 842, progress);
                    }
                    break;

                case ToolType.ImageToPdf:
                case ToolType.JpgToPdf:
                case ToolType.PngToPdf:
                    result = await _pdfService.ImagesToPdfAsync(SelectedFiles, progress);
                    break;

                case ToolType.TextToPdf:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.TextToPdfAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.Compress:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.CompressPdfAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.Flatten:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.FlattenPdfAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.Linearize:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.LinearizePdfAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.AddWatermark:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.AddWatermarkAsync(SelectedFiles[0], WatermarkText, WatermarkOpacity, progress);
                    }
                    break;

                case ToolType.AddPageNumbers:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.AddPageNumbersAsync(SelectedFiles[0], PageNumberPosition, PageNumberFormat, progress);
                    }
                    break;

                case ToolType.InvertColors:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.InvertColorsAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.BackgroundColor:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.ChangeBackgroundColorAsync(SelectedFiles[0], BackgroundColor, progress);
                    }
                    break;

                case ToolType.PdfToGreyscale:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.PdfToGreyscaleAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.RemoveAnnotations:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.RemoveAnnotationsAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.RemoveBlankPages:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.RemoveBlankPagesAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.RemoveMetadata:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.RemoveMetadataAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.ViewMetadata:
                    if (SelectedFiles.Count > 0)
                    {
                        var metadata = _pdfService.GetMetadata(SelectedFiles[0]);
                        metadataResult = FormatMetadata(metadata);
                        StatusMessage = metadataResult;
                    }
                    break;

                case ToolType.EditMetadata:
                    if (SelectedFiles.Count > 0)
                    {
                        var newMetadata = new PdfMetadata
                        {
                            Title = "Updated Title",
                            Author = "PDF Kawankasi",
                            Subject = "Processed Document"
                        };
                        result = await _pdfService.SetMetadataAsync(SelectedFiles[0], newMetadata, progress);
                    }
                    break;

                case ToolType.PageDimensions:
                    if (SelectedFiles.Count > 0)
                    {
                        var dimensions = _pdfService.GetPageDimensions(SelectedFiles[0]);
                        metadataResult = FormatPageDimensions(dimensions);
                        StatusMessage = metadataResult;
                    }
                    break;

                case ToolType.Compare:
                    if (SelectedFiles.Count >= 2)
                    {
                        // For comparison, just show file info comparison
                        var metadata1 = _pdfService.GetMetadata(SelectedFiles[0]);
                        var metadata2 = _pdfService.GetMetadata(SelectedFiles[1]);
                        metadataResult = $"PDF 1: {metadata1.PageCount} pages | PDF 2: {metadata2.PageCount} pages";
                        StatusMessage = metadataResult;
                    }
                    else
                    {
                        StatusMessage = "Please select at least 2 PDF files to compare";
                    }
                    break;

                case ToolType.PdfToJpg:
                    if (SelectedFiles.Count > 0)
                    {
                        StatusMessage = "PDF to JPG conversion requires additional rendering library. Feature coming soon.";
                    }
                    break;

                case ToolType.PdfToPng:
                    if (SelectedFiles.Count > 0)
                    {
                        StatusMessage = "PDF to PNG conversion requires additional rendering library. Feature coming soon.";
                    }
                    break;

                case ToolType.Encrypt:
                    StatusMessage = "PDF encryption requires password input. Feature coming soon with dialog support.";
                    break;

                case ToolType.Decrypt:
                    StatusMessage = "PDF decryption requires password input. Feature coming soon with dialog support.";
                    break;

                default:
                    StatusMessage = "This tool is not yet implemented";
                    break;
            }

            if (result != null)
            {
                // Save the result
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    FileName = $"{SelectedTool.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, result);
                    StatusMessage = $"Saved successfully: {dialog.FileName}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            ProgressValue = 100;
        }
    }

    private static int[] ParsePageNumbers(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<int>();

        var pages = new List<int>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Contains('-'))
            {
                var range = trimmed.Split('-');
                if (range.Length == 2 && 
                    int.TryParse(range[0].Trim(), out int start) && 
                    int.TryParse(range[1].Trim(), out int end) &&
                    start <= end && start > 0)
                {
                    for (int i = start; i <= end; i++)
                    {
                        pages.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out int page) && page > 0)
            {
                pages.Add(page);
            }
        }
        
        return pages.ToArray();
    }

    private static string FormatMetadata(PdfMetadata metadata)
    {
        return $"Title: {metadata.Title ?? "N/A"} | Author: {metadata.Author ?? "N/A"} | Pages: {metadata.PageCount} | Created: {metadata.CreationDate:g}";
    }

    private static string FormatPageDimensions(PdfPageDimensions dimensions)
    {
        if (dimensions.Pages.Count == 0)
            return "No pages found";
            
        var first = dimensions.Pages[0];
        return $"Total Pages: {dimensions.TotalPages} | First Page: {first.Width:F0}x{first.Height:F0} pts ({first.Orientation})";
    }
}
