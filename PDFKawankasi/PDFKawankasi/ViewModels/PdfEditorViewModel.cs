using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docnet.Core;
using Docnet.Core.Models;
using PDFKawankasi.Models;

namespace PDFKawankasi.ViewModels;

/// <summary>
/// ViewModel for the PDF Editor - an all-in-one PDF workspace
/// </summary>
public partial class PdfEditorViewModel : ObservableObject
{
    // Constants for default page dimensions
    private const int DefaultPageWidth = 800;
    private const int DefaultPageHeight = 1000;
    private const int ThumbnailWidth = 100;
    private const int ThumbnailHeight = 140;
    
    private IDocLib? _docLib;
    private string? _currentFilePath;
    private byte[]? _pdfBytes;

    #region Observable Properties

    [ObservableProperty]
    private bool _isPdfLoaded;

    [ObservableProperty]
    private string _statusMessage = "Ready - Open a PDF to start editing";

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _canvasWidth = 800;

    [ObservableProperty]
    private double _canvasHeight = 1000;

    [ObservableProperty]
    private BitmapSource? _currentPageImage;

    [ObservableProperty]
    private ObservableCollection<PageThumbnailModel> _pageThumbnails = new();

    [ObservableProperty]
    private ObservableCollection<AnnotationModel> _allAnnotations = new();

    [ObservableProperty]
    private ObservableCollection<AnnotationModel> _currentPageAnnotations = new();

    [ObservableProperty]
    private int _annotationCount;

    [ObservableProperty]
    private bool _hasAnnotations;

    // Tool selection
    [ObservableProperty]
    private bool _isSelectToolActive = true;

    [ObservableProperty]
    private bool _isHighlightToolActive;

    [ObservableProperty]
    private bool _isDrawToolActive;

    [ObservableProperty]
    private bool _isTextToolActive;

    [ObservableProperty]
    private bool _isShapeToolActive;

    [ObservableProperty]
    private bool _isCommentToolActive;

    [ObservableProperty]
    private bool _isRedactToolActive;

    [ObservableProperty]
    private Color _selectedColor = Colors.Yellow;

    [ObservableProperty]
    private string _currentToolName = "Select";

    #endregion

    #region Computed Properties

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    #endregion

    public PdfEditorViewModel()
    {
        _docLib = DocLib.Instance;
    }

    #region Commands

    [RelayCommand]
    private void OpenPdf()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            Title = "Open PDF File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadPdf(dialog.FileName);
        }
    }

    [RelayCommand]
    private void SavePdf()
    {
        if (!IsPdfLoaded || _pdfBytes == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "_edited"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Note: Current implementation saves the original PDF
                // Full annotation embedding will be implemented in a future update
                File.WriteAllBytes(dialog.FileName, _pdfBytes);
                StatusMessage = $"Saved: {dialog.FileName}";
                
                if (AllAnnotations.Count > 0)
                {
                    StatusMessage += $" (Note: {AllAnnotations.Count} annotation(s) are session-only and not embedded in PDF yet)";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void TakeScreenshot()
    {
        if (!IsPdfLoaded || CurrentPageImage == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
            DefaultExt = ".png",
            FileName = $"page_{CurrentPage}_screenshot"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                BitmapEncoder encoder = dialog.FilterIndex == 2
                    ? new JpegBitmapEncoder { QualityLevel = 95 }
                    : new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(CurrentPageImage));

                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(stream);

                StatusMessage = $"Screenshot saved: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving screenshot: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < 3.0)
        {
            ZoomLevel += 0.25;
            UpdateCanvasSize();
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > 0.25)
        {
            ZoomLevel -= 0.25;
            UpdateCanvasSize();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
            RenderCurrentPage();
            UpdatePageNavigation();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            RenderCurrentPage();
            UpdatePageNavigation();
        }
    }

    [RelayCommand]
    private void DeleteAnnotation(AnnotationModel annotation)
    {
        AllAnnotations.Remove(annotation);
        if (annotation.PageNumber == CurrentPage)
        {
            CurrentPageAnnotations.Remove(annotation);
        }
        UpdateAnnotationCount();
        StatusMessage = "Annotation deleted";
    }

    #endregion

    #region Tool Selection

    partial void OnIsSelectToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsSelectToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsHighlightToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsHighlightToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsDrawToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsDrawToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsTextToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsTextToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsShapeToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsShapeToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsCommentToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsCommentToolActive));
        UpdateCurrentToolName();
    }

    partial void OnIsRedactToolActiveChanged(bool value)
    {
        if (value) ClearOtherTools(nameof(IsRedactToolActive));
        UpdateCurrentToolName();
    }

    private void ClearOtherTools(string exceptTool)
    {
        if (exceptTool != nameof(IsSelectToolActive)) IsSelectToolActive = false;
        if (exceptTool != nameof(IsHighlightToolActive)) IsHighlightToolActive = false;
        if (exceptTool != nameof(IsDrawToolActive)) IsDrawToolActive = false;
        if (exceptTool != nameof(IsTextToolActive)) IsTextToolActive = false;
        if (exceptTool != nameof(IsShapeToolActive)) IsShapeToolActive = false;
        if (exceptTool != nameof(IsCommentToolActive)) IsCommentToolActive = false;
        if (exceptTool != nameof(IsRedactToolActive)) IsRedactToolActive = false;
    }

    private void UpdateCurrentToolName()
    {
        CurrentToolName = IsSelectToolActive ? "Select" :
                         IsHighlightToolActive ? "Highlight" :
                         IsDrawToolActive ? "Draw" :
                         IsTextToolActive ? "Text" :
                         IsShapeToolActive ? "Shape" :
                         IsCommentToolActive ? "Comment" :
                         IsRedactToolActive ? "Redact" : "None";
    }

    #endregion

    #region PDF Loading and Rendering

    private void LoadPdf(string filePath)
    {
        try
        {
            StatusMessage = "Loading PDF...";
            _currentFilePath = filePath;
            _pdfBytes = File.ReadAllBytes(filePath);

            using var reader = _docLib!.GetDocReader(_pdfBytes, new PageDimensions(1));
            TotalPages = reader.GetPageCount();
            CurrentPage = 1;

            LoadPageThumbnails();
            RenderCurrentPage();
            UpdatePageNavigation();

            IsPdfLoaded = true;
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)} ({TotalPages} pages)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading PDF: {ex.Message}";
            IsPdfLoaded = false;
        }
    }

    private void LoadPageThumbnails()
    {
        PageThumbnails.Clear();

        if (_pdfBytes == null) return;

        try
        {
            using var reader = _docLib!.GetDocReader(_pdfBytes, new PageDimensions(ThumbnailWidth, ThumbnailHeight));

            for (int i = 0; i < TotalPages; i++)
            {
                using var pageReader = reader.GetPageReader(i);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage();

                var thumbnail = CreateBitmapFromRawBytes(rawBytes, width, height);

                PageThumbnails.Add(new PageThumbnailModel
                {
                    PageNumber = i + 1,
                    Thumbnail = thumbnail,
                    IsCurrentPage = i == 0
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading thumbnails: {ex.Message}";
        }
    }

    private void RenderCurrentPage()
    {
        if (_pdfBytes == null || CurrentPage < 1 || CurrentPage > TotalPages) return;

        try
        {
            var scaledWidth = (int)(DefaultPageWidth * ZoomLevel);
            var scaledHeight = (int)(DefaultPageHeight * ZoomLevel);

            using var reader = _docLib!.GetDocReader(_pdfBytes, new PageDimensions(scaledWidth, scaledHeight));
            using var pageReader = reader.GetPageReader(CurrentPage - 1);

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage();

            CurrentPageImage = CreateBitmapFromRawBytes(rawBytes, width, height);
            CanvasWidth = width;
            CanvasHeight = height;

            // Update thumbnails to show current page
            foreach (var thumb in PageThumbnails)
            {
                thumb.IsCurrentPage = thumb.PageNumber == CurrentPage;
            }

            // Load annotations for current page
            LoadCurrentPageAnnotations();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error rendering page: {ex.Message}";
        }
    }

    private static BitmapSource CreateBitmapFromRawBytes(byte[] rawBytes, int width, int height)
    {
        // Docnet returns BGRA format
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
        return bitmap;
    }

    private void UpdateCanvasSize()
    {
        RenderCurrentPage();
    }

    private void UpdatePageNavigation()
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public void GoToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPages)
        {
            CurrentPage = pageNumber;
            RenderCurrentPage();
            UpdatePageNavigation();
        }
    }

    #endregion

    #region Annotation Handling

    private AnnotationModel? _currentAnnotation;
    private Point _annotationStartPoint;

    public void StartAnnotation(Point point)
    {
        if (IsSelectToolActive) return;

        _annotationStartPoint = point;

        _currentAnnotation = new AnnotationModel
        {
            Id = Guid.NewGuid().ToString(),
            PageNumber = CurrentPage,
            X = point.X,
            Y = point.Y,
            Color = SelectedColor,
            Type = GetCurrentAnnotationType(),
            CreatedAt = DateTime.Now
        };

        if (IsTextToolActive || IsCommentToolActive)
        {
            // For text and comments, we'll show an input dialog
            var result = ShowTextInputDialog(IsCommentToolActive ? "Add Comment" : "Add Text");
            if (!string.IsNullOrEmpty(result))
            {
                _currentAnnotation.Text = result;
                _currentAnnotation.Description = result.Length > 50 ? result[..50] + "..." : result;
                FinishAnnotation(point);
            }
            else
            {
                _currentAnnotation = null;
            }
        }
    }

    public void UpdateAnnotation(Point point)
    {
        if (_currentAnnotation == null || IsSelectToolActive) return;

        _currentAnnotation.Width = Math.Abs(point.X - _annotationStartPoint.X);
        _currentAnnotation.Height = Math.Abs(point.Y - _annotationStartPoint.Y);

        if (IsDrawToolActive)
        {
            // Add point to path for freehand drawing
            _currentAnnotation.Points ??= new List<Point>();
            _currentAnnotation.Points.Add(point);
        }
    }

    public void FinishAnnotation(Point point)
    {
        if (_currentAnnotation == null) return;

        _currentAnnotation.Width = Math.Abs(point.X - _annotationStartPoint.X);
        _currentAnnotation.Height = Math.Abs(point.Y - _annotationStartPoint.Y);

        // Set minimum size for clickable annotations
        if (_currentAnnotation.Width < 10) _currentAnnotation.Width = 100;
        if (_currentAnnotation.Height < 10) _currentAnnotation.Height = 30;

        _currentAnnotation.UpdateTypeInfo();
        
        AllAnnotations.Add(_currentAnnotation);
        CurrentPageAnnotations.Add(_currentAnnotation);
        UpdateAnnotationCount();

        StatusMessage = $"Added {_currentAnnotation.TypeName} annotation";
        _currentAnnotation = null;
    }

    public void CancelAnnotation()
    {
        _currentAnnotation = null;
    }

    private AnnotationType GetCurrentAnnotationType()
    {
        if (IsHighlightToolActive) return AnnotationType.Highlight;
        if (IsDrawToolActive) return AnnotationType.Drawing;
        if (IsTextToolActive) return AnnotationType.Text;
        if (IsShapeToolActive) return AnnotationType.Shape;
        if (IsCommentToolActive) return AnnotationType.Comment;
        if (IsRedactToolActive) return AnnotationType.Redaction;
        return AnnotationType.None;
    }

    private void LoadCurrentPageAnnotations()
    {
        CurrentPageAnnotations.Clear();
        foreach (var annotation in AllAnnotations.Where(a => a.PageNumber == CurrentPage))
        {
            CurrentPageAnnotations.Add(annotation);
        }
    }

    private void UpdateAnnotationCount()
    {
        AnnotationCount = AllAnnotations.Count;
        HasAnnotations = AnnotationCount > 0;
    }

    private string ShowTextInputDialog(string title)
    {
        // Simple input dialog - in a full implementation, use a proper dialog
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        var textBox = new TextBox
        {
            Height = 80,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Padding = new Thickness(8)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var okButton = new Button
        {
            Content = "Add",
            Width = 80,
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        string result = string.Empty;
        okButton.Click += (s, e) => { result = textBox.Text; dialog.Close(); };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        dialog.ShowDialog();
        return result;
    }

    #endregion
}
