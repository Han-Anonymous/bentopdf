using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docnet.Core;
using Docnet.Core.Models;

namespace PDFKawankasi.ViewModels;

public partial class PrintWindowViewModel : ObservableObject
{
    private readonly byte[] _pdfBytes;
    private readonly IDocLib _docLib;

    [ObservableProperty]
    private ObservableCollection<string> _availablePrinters = new();

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private bool _printAllPages = true;

    [ObservableProperty]
    private bool _printCurrentPage;

    [ObservableProperty]
    private bool _printCustomRange;

    [ObservableProperty]
    private string _customPageRange = "";

    [ObservableProperty]
    private string _statusMessage = "Ready to print";

    [ObservableProperty]
    private bool _isPrinting;

    private List<ImageSource> _allPreviewPages = new();

    [ObservableProperty]
    private ObservableCollection<ImageSource> _previewPages = new();

    [ObservableProperty]
    private bool _isLoadingPreview;

    public int TotalPages { get; }
    public int CurrentPage { get; }

    // Action to close the window with a result
    public Action<bool>? CloseAction { get; set; }

    public PrintWindowViewModel(byte[] pdfBytes, IDocLib docLib, int totalPages, int currentPage)
    {
        _pdfBytes = pdfBytes;
        _docLib = docLib;
        TotalPages = totalPages;
        CurrentPage = currentPage;
        LoadPrinters();
        GeneratePreviews();
    }

    partial void OnPrintAllPagesChanged(bool value) => UpdatePreviewPages();
    partial void OnPrintCurrentPageChanged(bool value) => UpdatePreviewPages();
    partial void OnPrintCustomRangeChanged(bool value) => UpdatePreviewPages();
    partial void OnCustomPageRangeChanged(string value) => UpdatePreviewPages();

    private void LoadPrinters()
    {
        try
        {
            var server = new LocalPrintServer();
            var queues = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });

            AvailablePrinters.Clear();
            foreach (var queue in queues)
            {
                AvailablePrinters.Add(queue.Name);
            }

            // Select default printer
            var defaultQueue = LocalPrintServer.GetDefaultPrintQueue();
            if (defaultQueue != null && AvailablePrinters.Contains(defaultQueue.Name))
            {
                SelectedPrinter = defaultQueue.Name;
            }
            else if (AvailablePrinters.Count > 0)
            {
                SelectedPrinter = AvailablePrinters[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading printers: {ex.Message}";
        }
    }

    private async void GeneratePreviews()
    {
        IsLoadingPreview = true;
        _allPreviewPages.Clear();
        PreviewPages.Clear();

        try
        {
            await Task.Run(() =>
            {
                // Use a reasonable resolution for preview (e.g. fit to width of ~800px)
                using var reader = _docLib.GetDocReader(_pdfBytes, new PageDimensions(800, 1132)); 
                var count = reader.GetPageCount();

                for (int i = 0; i < count; i++)
                {
                    using var pageReader = reader.GetPageReader(i);
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    var rawBytes = pageReader.GetImage();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmap = BitmapSource.Create(
                            width,
                            height,
                            96,
                            96,
                            PixelFormats.Bgra32,
                            null,
                            rawBytes,
                            width * 4);
                        bitmap.Freeze();
                        _allPreviewPages.Add(bitmap);
                    });
                }
            });

            UpdatePreviewPages();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating preview: {ex.Message}";
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    private void UpdatePreviewPages()
    {
        if (_allPreviewPages.Count == 0) return;

        PreviewPages.Clear();

        if (PrintAllPages)
        {
            foreach (var page in _allPreviewPages)
            {
                PreviewPages.Add(page);
            }
        }
        else if (PrintCurrentPage)
        {
            if (CurrentPage >= 1 && CurrentPage <= _allPreviewPages.Count)
            {
                PreviewPages.Add(_allPreviewPages[CurrentPage - 1]);
            }
        }
        else if (PrintCustomRange)
        {
            if (ParsePageRange(CustomPageRange, out int start, out int end))
            {
                // Ensure range is within bounds
                start = Math.Max(1, start);
                end = Math.Min(_allPreviewPages.Count, end);

                for (int i = start; i <= end; i++)
                {
                    PreviewPages.Add(_allPreviewPages[i - 1]);
                }
            }
        }
    }

    private bool ParsePageRange(string rangeText, out int start, out int end)
    {
        start = 1;
        end = TotalPages;
        
        if (string.IsNullOrWhiteSpace(rangeText)) return false;
        
        try 
        {
            var parts = rangeText.Split('-');
            if (parts.Length == 1)
            {
                if (int.TryParse(parts[0].Trim(), out int p))
                {
                    start = p;
                    end = p;
                    return true;
                }
            }
            else if (parts.Length == 2)
            {
                bool sOk = int.TryParse(parts[0].Trim(), out int s);
                bool eOk = int.TryParse(parts[1].Trim(), out int e);
                
                if (sOk && eOk)
                {
                    start = s;
                    end = e;
                    return true;
                }
            }
        }
        catch { }
        
        return false;
    }

    [RelayCommand]
    private void Print()
    {
        if (string.IsNullOrEmpty(SelectedPrinter))
        {
            StatusMessage = "Please select a printer";
            return;
        }

        if (PrintCustomRange && string.IsNullOrWhiteSpace(CustomPageRange))
        {
            StatusMessage = "Please enter a page range";
            return;
        }

        CloseAction?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke(false);
    }
}
