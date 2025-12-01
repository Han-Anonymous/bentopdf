using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docnet.Core;
using Docnet.Core.Models;
using PDFKawankasi.Models;
using PdfSharpCore.Drawing;

// Use aliases to avoid conflicts between PdfSharpCore and iText7
using PdfSharpDocument = PdfSharpCore.Pdf.PdfDocument;
using PdfSharpReader = PdfSharpCore.Pdf.IO.PdfReader;
using PdfSharpOpenMode = PdfSharpCore.Pdf.IO.PdfDocumentOpenMode;

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

    // Ink Canvas properties for Windows Inking API
    [ObservableProperty]
    private InkCanvasEditingMode _inkEditingMode = InkCanvasEditingMode.None;

    [ObservableProperty]
    private DrawingAttributes _inkDrawingAttributes = null!;

    [ObservableProperty]
    private StrokeCollection _currentPageStrokes = new();

    [ObservableProperty]
    private double _penThickness = 3.0;

    #endregion

    #region Computed Properties

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    #endregion

    public PdfEditorViewModel()
    {
        _docLib = DocLib.Instance;
        InitializeInkDrawingAttributes();
    }

    private void InitializeInkDrawingAttributes()
    {
        InkDrawingAttributes = new DrawingAttributes
        {
            Color = SelectedColor,
            Height = 3,
            Width = 3,
            FitToCurve = true,
            StylusTip = StylusTip.Ellipse,
            IgnorePressure = false // Enable pressure sensitivity for stylus/pen
        };

        // Add extended properties for richer ink experience
        InkDrawingAttributes.AddPropertyData(
            DrawingAttributeIds.StylusTipTransform,
            Matrix.Identity);
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
    private async Task SavePdf()
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
                StatusMessage = "Saving PDF with ink annotations...";
                
                // Flatten ink strokes to PDF if any exist
                int totalInkStrokes = _pageStrokes.Values.Sum(s => s.Count);
                if (totalInkStrokes > 0)
                {
                    await FlattenInkToPdf(dialog.FileName);
                    StatusMessage = $"Saved: {dialog.FileName} with {totalInkStrokes} ink stroke(s) embedded";
                }
                else
                {
                    File.WriteAllBytes(dialog.FileName, _pdfBytes);
                    StatusMessage = $"Saved: {dialog.FileName}";
                }
                
                if (AllAnnotations.Count > 0)
                {
                    StatusMessage += $" (Note: {AllAnnotations.Count} text/shape annotation(s) are session-only)";
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

    [RelayCommand]
    private void ClearCurrentPageInk()
    {
        ClearCurrentPageStrokes();
        StatusMessage = $"Cleared all ink strokes from page {CurrentPage}";
    }

    [RelayCommand]
    private void ClearAllInk()
    {
        _pageStrokes.Clear();
        CurrentPageStrokes.Clear();
        StatusMessage = "Cleared all ink strokes from all pages";
    }

    [RelayCommand]
    private void ExportInkStrokes()
    {
        if (_pageStrokes.Count == 0)
        {
            StatusMessage = "No ink strokes to export";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Ink Serialized Format (*.isf)|*.isf|All Files (*.*)|*.*",
            DefaultExt = ".isf",
            FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "_ink"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Combine all strokes from all pages
                var allStrokes = new StrokeCollection();
                foreach (var pageStroke in _pageStrokes.Values)
                {
                    allStrokes.Add(pageStroke);
                }

                // Save to ISF format (Windows Ink native format)
                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                allStrokes.Save(stream);

                StatusMessage = $"Exported {allStrokes.Count} ink strokes to: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting ink: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ImportInkStrokes()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Ink Serialized Format (*.isf)|*.isf|All Files (*.*)|*.*",
            Title = "Import Ink Strokes"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var stream = new FileStream(dialog.FileName, FileMode.Open);
                var importedStrokes = new StrokeCollection(stream);

                // Add to current page
                foreach (var stroke in importedStrokes)
                {
                    CurrentPageStrokes.Add(stroke);
                }

                StatusMessage = $"Imported {importedStrokes.Count} ink strokes to page {CurrentPage}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing ink: {ex.Message}";
            }
        }
    }

    #endregion

    #region Tool Selection

    partial void OnIsSelectToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsSelectToolActive));
            InkEditingMode = InkCanvasEditingMode.None;
        }
        UpdateCurrentToolName();
    }

    partial void OnIsHighlightToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsHighlightToolActive));
            InkEditingMode = InkCanvasEditingMode.Ink;
            UpdateInkAttributesForHighlight();
        }
        UpdateCurrentToolName();
    }

    partial void OnIsDrawToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsDrawToolActive));
            InkEditingMode = InkCanvasEditingMode.Ink;
            UpdateInkAttributesForDrawing();
        }
        UpdateCurrentToolName();
    }

    partial void OnIsTextToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsTextToolActive));
            InkEditingMode = InkCanvasEditingMode.None;
        }
        UpdateCurrentToolName();
    }

    partial void OnIsShapeToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsShapeToolActive));
            InkEditingMode = InkCanvasEditingMode.None;
        }
        UpdateCurrentToolName();
    }

    partial void OnIsCommentToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsCommentToolActive));
            InkEditingMode = InkCanvasEditingMode.None;
        }
        UpdateCurrentToolName();
    }

    partial void OnIsRedactToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsRedactToolActive));
            InkEditingMode = InkCanvasEditingMode.Ink;
            UpdateInkAttributesForRedaction();
        }
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

    private void UpdateInkAttributesForHighlight()
    {
        InkDrawingAttributes.Color = Color.FromArgb(100, SelectedColor.R, SelectedColor.G, SelectedColor.B); // Semi-transparent
        InkDrawingAttributes.Width = 20;
        InkDrawingAttributes.Height = 20;
        InkDrawingAttributes.StylusTip = StylusTip.Rectangle;
        InkDrawingAttributes.IsHighlighter = true;
    }

    private void UpdateInkAttributesForDrawing()
    {
        InkDrawingAttributes.Color = SelectedColor;
        InkDrawingAttributes.Width = PenThickness;
        InkDrawingAttributes.Height = PenThickness;
        InkDrawingAttributes.StylusTip = StylusTip.Ellipse;
        InkDrawingAttributes.IsHighlighter = false;
        InkDrawingAttributes.FitToCurve = true;
    }

    private void UpdateInkAttributesForRedaction()
    {
        InkDrawingAttributes.Color = Colors.Black;
        InkDrawingAttributes.Width = 15;
        InkDrawingAttributes.Height = 15;
        InkDrawingAttributes.StylusTip = StylusTip.Rectangle;
        InkDrawingAttributes.IsHighlighter = false;
    }

    partial void OnSelectedColorChanged(Color value)
    {
        if (InkDrawingAttributes != null)
        {
            if (IsHighlightToolActive)
            {
                UpdateInkAttributesForHighlight();
            }
            else if (IsDrawToolActive)
            {
                UpdateInkAttributesForDrawing();
            }
        }
    }

    partial void OnPenThicknessChanged(double value)
    {
        if (InkDrawingAttributes != null && !IsHighlightToolActive)
        {
            InkDrawingAttributes.Width = value;
            InkDrawingAttributes.Height = value;
        }
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

        // Load ink strokes for this page
        LoadCurrentPageStrokes();
    }

    private Dictionary<int, StrokeCollection> _pageStrokes = new();

    private void LoadCurrentPageStrokes()
    {
        if (_pageStrokes.TryGetValue(CurrentPage, out var strokes))
        {
            CurrentPageStrokes = new StrokeCollection(strokes);
        }
        else
        {
            CurrentPageStrokes = new StrokeCollection();
            _pageStrokes[CurrentPage] = CurrentPageStrokes;
        }
        
        OnPropertyChanged(nameof(CurrentPageStrokes));
    }

    public void SaveCurrentPageStrokes(StrokeCollection strokes)
    {
        _pageStrokes[CurrentPage] = new StrokeCollection(strokes);
        StatusMessage = $"Ink strokes saved for page {CurrentPage}";
    }

    public void ClearCurrentPageStrokes()
    {
        CurrentPageStrokes.Clear();
        if (_pageStrokes.ContainsKey(CurrentPage))
        {
            _pageStrokes[CurrentPage].Clear();
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

    private async Task FlattenInkToPdf(string outputPath)
    {
        // Request the view to render ink strokes for all pages
        var renderedPages = new Dictionary<int, byte[]>();
        
        // Signal that we need rendered pages
        OnInkRenderRequested?.Invoke(renderedPages);
        
        if (renderedPages.Count == 0)
        {
            // No rendered pages, just save original
            File.WriteAllBytes(outputPath, _pdfBytes!);
            return;
        }

        // Create a new PDF with ink overlays using PdfSharpCore
        using var inputStream = new MemoryStream(_pdfBytes!);
        using var inputDoc = PdfSharpReader.Open(inputStream, PdfSharpOpenMode.Import);
        using var outputDoc = new PdfSharpDocument();

        // Process each page
        for (int i = 0; i < inputDoc.PageCount; i++)
        {
            var page = outputDoc.AddPage(inputDoc.Pages[i]);
            
            // If this page has ink strokes, overlay them
            if (renderedPages.TryGetValue(i + 1, out var inkImageBytes))
            {
                using var gfx = XGraphics.FromPdfPage(page);
                using var inkImage = XImage.FromStream(() => new MemoryStream(inkImageBytes));
                
                // Draw ink overlay on top of existing content
                gfx.DrawImage(inkImage, 0, 0, page.Width, page.Height);
            }
        }

        // Save the modified PDF
        outputDoc.Save(outputPath);
    }

    // Event to request ink rendering from the view
    public event Action<Dictionary<int, byte[]>>? OnInkRenderRequested;

    #endregion
}
