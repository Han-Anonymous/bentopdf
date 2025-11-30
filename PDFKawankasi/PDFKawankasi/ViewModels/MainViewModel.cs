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
        StatusMessage = $"Selected: {tool.Name}";
    }

    [RelayCommand]
    private void BackToGrid()
    {
        IsToolViewVisible = false;
        SelectedTool = null;
        SelectedFiles.Clear();
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void AddFiles(string[] filePaths)
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

    [RelayCommand]
    private void RemoveFile(string filePath)
    {
        SelectedFiles.Remove(filePath);
        StatusMessage = $"{SelectedFiles.Count} file(s) selected";
    }

    [RelayCommand]
    private void ClearFiles()
    {
        SelectedFiles.Clear();
        StatusMessage = "Files cleared";
    }

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

            switch (SelectedTool.ToolType)
            {
                case ToolType.Merge:
                    result = await _pdfService.MergePdfsAsync(SelectedFiles, progress);
                    break;

                case ToolType.ReversePages:
                    if (SelectedFiles.Count > 0)
                    {
                        result = await _pdfService.ReversePagesAsync(SelectedFiles[0], progress);
                    }
                    break;

                case ToolType.ImageToPdf:
                case ToolType.JpgToPdf:
                case ToolType.PngToPdf:
                    result = await _pdfService.ImagesToPdfAsync(SelectedFiles, progress);
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
}
