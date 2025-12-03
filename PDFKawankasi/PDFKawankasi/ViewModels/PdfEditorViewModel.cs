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
using PDFKawankasi.Services;
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
    
    // Constants for text box defaults
    private const double DefaultTextBoxWidth = 200;
    private const double DefaultTextBoxHeight = 50;
    private const string DefaultTextBoxPlaceholder = "Type text here...";
    
    private IDocLib? _docLib;
    private readonly PdfService _pdfService;
    private string? _currentFilePath;
    private byte[]? _pdfBytes;

    // Undo/Redo stacks
    private readonly Stack<UndoableAction> _undoStack = new();
    private readonly Stack<UndoableAction> _redoStack = new();

    // Action types for undo/redo
    public abstract class UndoableAction
    {
        public abstract void Undo(PdfEditorViewModel vm);
        public abstract void Redo(PdfEditorViewModel vm);
        public abstract string Description { get; }
    }

    public class StrokeAddedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public Stroke Stroke { get; init; } = null!;
        public override string Description => "Add stroke";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (vm._pageStrokes.TryGetValue(PageNumber, out var strokes))
            {
                strokes.Remove(Stroke);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageStrokes.Remove(Stroke);
                }
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (!vm._pageStrokes.ContainsKey(PageNumber))
            {
                vm._pageStrokes[PageNumber] = new StrokeCollection();
            }
            vm._pageStrokes[PageNumber].Add(Stroke);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageStrokes.Add(Stroke);
            }
        }
    }

    public class StrokeErasedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public Stroke Stroke { get; init; } = null!;
        public override string Description => "Erase stroke";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (!vm._pageStrokes.ContainsKey(PageNumber))
            {
                vm._pageStrokes[PageNumber] = new StrokeCollection();
            }
            vm._pageStrokes[PageNumber].Add(Stroke);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageStrokes.Add(Stroke);
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (vm._pageStrokes.TryGetValue(PageNumber, out var strokes))
            {
                strokes.Remove(Stroke);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageStrokes.Remove(Stroke);
                }
            }
        }
    }

    public class ImageAddedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public ImageAnnotation Image { get; init; } = null!;
        public override string Description => "Add image";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (vm._pageImages.TryGetValue(PageNumber, out var images))
            {
                images.Remove(Image);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageImages.Remove(Image);
                    vm.OnPropertyChanged(nameof(CurrentPageImages));
                }
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (!vm._pageImages.ContainsKey(PageNumber))
            {
                vm._pageImages[PageNumber] = new List<ImageAnnotation>();
            }
            vm._pageImages[PageNumber].Add(Image);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageImages.Add(Image);
                vm.OnPropertyChanged(nameof(CurrentPageImages));
            }
        }
    }

    public class ImageDeletedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public ImageAnnotation Image { get; init; } = null!;
        public override string Description => "Delete image";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (!vm._pageImages.ContainsKey(PageNumber))
            {
                vm._pageImages[PageNumber] = new List<ImageAnnotation>();
            }
            vm._pageImages[PageNumber].Add(Image);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageImages.Add(Image);
                vm.OnPropertyChanged(nameof(CurrentPageImages));
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (vm._pageImages.TryGetValue(PageNumber, out var images))
            {
                images.Remove(Image);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageImages.Remove(Image);
                    vm.OnPropertyChanged(nameof(CurrentPageImages));
                }
            }
        }
    }

    public class TextBoxAddedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public TextBoxAnnotation TextBox { get; init; } = null!;
        public override string Description => "Add text box";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (vm._pageTextBoxes.TryGetValue(PageNumber, out var textBoxes))
            {
                textBoxes.Remove(TextBox);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageTextBoxes.Remove(TextBox);
                    vm.OnPropertyChanged(nameof(CurrentPageTextBoxes));
                }
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (!vm._pageTextBoxes.ContainsKey(PageNumber))
            {
                vm._pageTextBoxes[PageNumber] = new List<TextBoxAnnotation>();
            }
            vm._pageTextBoxes[PageNumber].Add(TextBox);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageTextBoxes.Add(TextBox);
                vm.OnPropertyChanged(nameof(CurrentPageTextBoxes));
            }
        }
    }

    public class TextBoxDeletedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public TextBoxAnnotation TextBox { get; init; } = null!;
        public override string Description => "Delete text box";
        public override void Undo(PdfEditorViewModel vm)
        {
            if (!vm._pageTextBoxes.ContainsKey(PageNumber))
            {
                vm._pageTextBoxes[PageNumber] = new List<TextBoxAnnotation>();
            }
            vm._pageTextBoxes[PageNumber].Add(TextBox);
            if (vm.CurrentPage == PageNumber)
            {
                vm.CurrentPageTextBoxes.Add(TextBox);
                vm.OnPropertyChanged(nameof(CurrentPageTextBoxes));
            }
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            if (vm._pageTextBoxes.TryGetValue(PageNumber, out var textBoxes))
            {
                textBoxes.Remove(TextBox);
                if (vm.CurrentPage == PageNumber)
                {
                    vm.CurrentPageTextBoxes.Remove(TextBox);
                    vm.OnPropertyChanged(nameof(CurrentPageTextBoxes));
                }
            }
        }
    }

    public class PageDeletedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public PageThumbnailModel Thumbnail { get; init; } = null!;
        public StrokeCollection? Strokes { get; init; }
        public List<ImageAnnotation>? Images { get; init; }
        public List<TextBoxAnnotation>? TextBoxes { get; init; }
        public override string Description => $"Delete page {PageNumber}";
        public override void Undo(PdfEditorViewModel vm)
        {
            // Restore the page - insert at original position
            vm.PageThumbnails.Insert(PageNumber - 1, Thumbnail);
            if (Strokes != null) vm._pageStrokes[PageNumber] = Strokes;
            if (Images != null) vm._pageImages[PageNumber] = Images;
            if (TextBoxes != null) vm._pageTextBoxes[PageNumber] = TextBoxes;
            vm.TotalPages++;
            vm.UpdateAllThumbnailPageNumbers();
            vm.UpdatePageNavigation();
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            vm.DeletePageInternal(PageNumber, recordUndo: false, updatePdfFile: false);
        }
    }

    public class PageAddedAction : UndoableAction
    {
        public int PageNumber { get; init; }
        public override string Description => $"Add page at {PageNumber}";
        public override void Undo(PdfEditorViewModel vm)
        {
            vm.DeletePageInternal(PageNumber, recordUndo: false, updatePdfFile: false);
        }
        public override void Redo(PdfEditorViewModel vm)
        {
            _ = vm.AddBlankPageInternal(PageNumber, recordUndo: false, updatePdfFile: false);
        }
    }

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
    private bool _isImageToolActive;

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

    [ObservableProperty]
    private bool _hasPendingChanges;

    // Fullscreen mode toggle
    [ObservableProperty]
    private bool _isFullscreen;

    // Panel visibility toggles
    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    // View mode settings
    [ObservableProperty]
    private bool _isDualPageMode;

    // Continuous scrolling mode
    [ObservableProperty]
    private bool _isContinuousScrollMode;

    // Shift key snap assist for straight lines
    [ObservableProperty]
    private bool _isShiftKeyPressed;

    // Undo/Redo support
    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    // Font settings for text annotations
    [ObservableProperty]
    private string _selectedFontFamily = "Arial";

    [ObservableProperty]
    private double _selectedFontSize = 14.0;

    // Available fonts
    public ObservableCollection<string> AvailableFonts { get; } = new ObservableCollection<string>
    {
        "Arial", "Times New Roman", "Courier New", "Verdana", "Georgia", "Tahoma", "Trebuchet MS", "Impact", "Comic Sans MS"
    };

    // Available font sizes
    public ObservableCollection<double> AvailableFontSizes { get; } = new ObservableCollection<double>
    {
        8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 48, 72
    };

    #endregion

    #region Computed Properties

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    // Computed property for left panel column width
    public double LeftPanelWidth => IsLeftPanelVisible ? 200 : 0;
    
    // Computed property for right panel column width  
    public double RightPanelWidth => IsRightPanelVisible ? 250 : 0;

    #endregion

    public PdfEditorViewModel()
    {
        _docLib = DocLib.Instance;
        _pdfService = new PdfService();
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

    partial void OnIsLeftPanelVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(LeftPanelWidth));
    }

    partial void OnIsRightPanelVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(RightPanelWidth));
    }

    #region Commands

    [RelayCommand]
    private void OpenPdf()
    {
        // Check for pending changes before opening a new file
        if (IsPdfLoaded && HasPendingChanges)
        {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Do you want to save before opening a new file?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Cancel)
            {
                return;
            }
            else if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Save current file first
                SavePdfCommand.Execute(null);
            }
            // If No, discard changes and continue opening new file
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            Title = "Open PDF File"
        };

        if (dialog.ShowDialog() == true)
        {
            // Reset all state before loading new PDF
            ResetEditorState();
            LoadPdf(dialog.FileName);
        }
    }

    private void ResetEditorState()
    {
        // Clear all ink strokes
        _pageStrokes.Clear();
        CurrentPageStrokes.Clear();
        
        // Clear all annotations
        AllAnnotations.Clear();
        CurrentPageAnnotations.Clear();
        
        // Clear page thumbnails
        PageThumbnails.Clear();
        
        // Clear images
        _pageImages.Clear();
        CurrentPageImages.Clear();
        
        // Clear text boxes
        _pageTextBoxes.Clear();
        CurrentPageTextBoxes.Clear();
        
        // Reset tool state
        IsSelectToolActive = true;
        
        // Reset pending changes flag
        HasPendingChanges = false;
        
        // Clear undo/redo stacks
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();
        
        StatusMessage = "Ready - Opening new PDF...";
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    private void PushUndoAction(UndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
        UpdateUndoRedoState();
        HasPendingChanges = true;
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        
        var action = _undoStack.Pop();
        action.Undo(this);
        _redoStack.Push(action);
        UpdateUndoRedoState();
        StatusMessage = $"Undo: {action.Description}";
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        
        var action = _redoStack.Pop();
        action.Redo(this);
        _undoStack.Push(action);
        UpdateUndoRedoState();
        StatusMessage = $"Redo: {action.Description}";
    }

    [RelayCommand]
    private void ToggleContinuousScroll()
    {
        IsContinuousScrollMode = !IsContinuousScrollMode;
        StatusMessage = IsContinuousScrollMode ? "Continuous scrolling enabled" : "Continuous scrolling disabled";
    }

    #region Page Management Commands

    [RelayCommand]
    private void DeleteSelectedPages()
    {
        var selectedPages = PageThumbnails.Where(p => p.IsSelected).OrderByDescending(p => p.PageNumber).ToList();
        if (selectedPages.Count == 0)
        {
            StatusMessage = "No pages selected for deletion";
            return;
        }

        if (selectedPages.Count >= TotalPages)
        {
            System.Windows.MessageBox.Show("Cannot delete all pages. At least one page must remain.", "Delete Pages", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        foreach (var page in selectedPages)
        {
            DeletePageInternal(page.PageNumber, recordUndo: true);
        }
        
        ClearPageSelection();
        StatusMessage = $"Deleted {selectedPages.Count} page(s)";
    }

    [RelayCommand]
    private void DeletePage(int pageNumber)
    {
        if (TotalPages <= 1)
        {
            System.Windows.MessageBox.Show("Cannot delete the only page.", "Delete Page", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        DeletePageInternal(pageNumber, recordUndo: true);
        StatusMessage = $"Deleted page {pageNumber}";
    }

    private void DeletePageInternal(int pageNumber, bool recordUndo, bool updatePdfFile = true)
    {
        if (pageNumber < 1 || pageNumber > TotalPages) return;
        
        var thumbnail = PageThumbnails.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (thumbnail == null) return;

        if (recordUndo)
        {
            var action = new PageDeletedAction
            {
                PageNumber = pageNumber,
                Thumbnail = thumbnail,
                Strokes = _pageStrokes.ContainsKey(pageNumber) ? new StrokeCollection(_pageStrokes[pageNumber]) : null,
                Images = _pageImages.ContainsKey(pageNumber) ? new List<ImageAnnotation>(_pageImages[pageNumber]) : null,
                TextBoxes = _pageTextBoxes.ContainsKey(pageNumber) ? new List<TextBoxAnnotation>(_pageTextBoxes[pageNumber]) : null
            };
            PushUndoAction(action);
        }

        // Remove page data
        PageThumbnails.Remove(thumbnail);
        _pageStrokes.Remove(pageNumber);
        _pageImages.Remove(pageNumber);
        _pageTextBoxes.Remove(pageNumber);
        
        // Shift all subsequent page data down
        for (int i = pageNumber + 1; i <= TotalPages; i++)
        {
            if (_pageStrokes.ContainsKey(i))
            {
                _pageStrokes[i - 1] = _pageStrokes[i];
                _pageStrokes.Remove(i);
            }
            if (_pageImages.ContainsKey(i))
            {
                _pageImages[i - 1] = _pageImages[i];
                _pageImages.Remove(i);
            }
            if (_pageTextBoxes.ContainsKey(i))
            {
                _pageTextBoxes[i - 1] = _pageTextBoxes[i];
                _pageTextBoxes.Remove(i);
            }
        }

        TotalPages--;
        UpdateAllThumbnailPageNumbers();
        
        // Adjust current page if needed
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }
        
        RenderCurrentPage();
        UpdatePageNavigation();
    }

    [RelayCommand]
    private async Task AddPageAbove(int pageNumber)
    {
        await AddBlankPageInternal(pageNumber, recordUndo: true);
        StatusMessage = $"Added blank page above page {pageNumber}";
    }

    [RelayCommand]
    private async Task AddPageBelow(int pageNumber)
    {
        await AddBlankPageInternal(pageNumber + 1, recordUndo: true);
        StatusMessage = $"Added blank page below page {pageNumber}";
    }

    [RelayCommand]
    private async Task InsertPdfPagesAbove(int pageNumber)
    {
        await InsertPdfPagesAtPosition(pageNumber, "above");
    }

    [RelayCommand]
    private async Task InsertPdfPagesBelow(int pageNumber)
    {
        await InsertPdfPagesAtPosition(pageNumber + 1, "below");
    }

    private async Task InsertPdfPagesAtPosition(int insertPosition, string location)
    {
        try
        {
            // Open file dialog to select PDF
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Select PDF to Insert {location}",
                Filter = "PDF Files (*.pdf)|*.pdf",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                StatusMessage = "Loading PDF for page selection...";

                // Create and show page selection window
                var selectionWindow = new Window
                {
                    Title = $"Select Pages to Insert {location}",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Background = new SolidColorBrush(Color.FromRgb(28, 32, 38))
                };

                var viewModel = new PdfPageSelectorViewModel(dialog.FileName);
                selectionWindow.DataContext = viewModel;

                // Create UI for page selection
                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                var itemsControl = new ItemsControl
                {
                    ItemsSource = viewModel.Pages
                };

                // Create item template for page thumbnails with checkboxes
                var dataTemplate = new DataTemplate();
                var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                stackPanelFactory.SetValue(StackPanel.MarginProperty, new Thickness(5));

                var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
                checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected") { Mode = System.Windows.Data.BindingMode.TwoWay });
                checkBoxFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
                checkBoxFactory.SetValue(CheckBox.MarginProperty, new Thickness(0, 0, 10, 0));

                var imageFactory = new FrameworkElementFactory(typeof(Image));
                imageFactory.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Thumbnail"));
                imageFactory.SetValue(Image.WidthProperty, 100.0);
                imageFactory.SetValue(Image.HeightProperty, 140.0);
                imageFactory.SetValue(Image.MarginProperty, new Thickness(0, 0, 10, 0));

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("PageNumber") { StringFormat = "Page {0}" });
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.White));

                stackPanelFactory.AppendChild(checkBoxFactory);
                stackPanelFactory.AppendChild(imageFactory);
                stackPanelFactory.AppendChild(textBlockFactory);
                dataTemplate.VisualTree = stackPanelFactory;

                itemsControl.ItemTemplate = dataTemplate;
                scrollViewer.Content = itemsControl;

                Grid.SetRow(scrollViewer, 0);
                grid.Children.Add(scrollViewer);

                // Add buttons
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var insertButton = new Button
                {
                    Content = "Insert Selected Pages",
                    Padding = new Thickness(20, 8, 20, 8),
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                insertButton.Click += (s, e) => { selectionWindow.DialogResult = true; selectionWindow.Close(); };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Padding = new Thickness(20, 8, 20, 8),
                    Background = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                cancelButton.Click += (s, e) => { selectionWindow.DialogResult = false; selectionWindow.Close(); };

                buttonPanel.Children.Add(insertButton);
                buttonPanel.Children.Add(cancelButton);

                Grid.SetRow(buttonPanel, 1);
                grid.Children.Add(buttonPanel);

                selectionWindow.Content = grid;

                // Show dialog and process selection
                if (selectionWindow.ShowDialog() == true)
                {
                    var selectedPages = viewModel.GetSelectedPageNumbers();
                    if (selectedPages.Any())
                    {
                        StatusMessage = $"Inserting {selectedPages.Count} page(s)...";

                        // Insert pages using PdfService
                        if (_currentFilePath != null && File.Exists(_currentFilePath))
                        {
                            var newPdfBytes = await _pdfService.InsertPdfPagesAsync(
                                _currentFilePath,
                                dialog.FileName,
                                selectedPages.ToArray(),
                                insertPosition);

                            // Update the PDF bytes in memory
                            _pdfBytes = newPdfBytes;

                            // Save the updated PDF back to disk
                            await File.WriteAllBytesAsync(_currentFilePath, newPdfBytes);

                            // Reload the PDF to update thumbnails and page count
                            LoadPdf(_currentFilePath);

                            StatusMessage = $"Inserted {selectedPages.Count} page(s) at position {insertPosition}";
                        }
                    }
                    else
                    {
                        StatusMessage = "No pages selected";
                    }
                }
                else
                {
                    StatusMessage = "Insert cancelled";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error inserting PDF pages: {ex.Message}";
            System.Windows.MessageBox.Show($"Error inserting PDF pages: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddBlankPageInternal(int insertPosition, bool recordUndo, bool updatePdfFile = true)
    {
        if (insertPosition < 1) insertPosition = 1;
        if (insertPosition > TotalPages + 1) insertPosition = TotalPages + 1;

        if (recordUndo)
        {
            PushUndoAction(new PageAddedAction { PageNumber = insertPosition });
        }

        try
        {
            if (updatePdfFile)
            {
                StatusMessage = "Adding blank page...";

                // Add blank page to the actual PDF file
                if (_currentFilePath != null && File.Exists(_currentFilePath))
                {
                    // Get current page dimensions (default to A4 if can't determine)
                    double width = 595; // A4 width in points
                    double height = 842; // A4 height in points

                    if (_pdfBytes != null && _docLib != null)
                    {
                        using var reader = _docLib.GetDocReader(_pdfBytes, new PageDimensions(1));
                        if (reader.GetPageCount() > 0)
                        {
                            using var pageReader = reader.GetPageReader(0);
                            width = pageReader.GetPageWidth();
                            height = pageReader.GetPageHeight();
                        }
                    }

                    // Add blank page using PdfService
                    var newPdfBytes = await _pdfService.AddBlankPageAsync(_currentFilePath, insertPosition, width, height);
                    
                    // Update the PDF bytes in memory
                    _pdfBytes = newPdfBytes;
                    
                    // Save the updated PDF back to disk
                    await File.WriteAllBytesAsync(_currentFilePath, newPdfBytes);
                }
            }

            // Shift all subsequent page data up
            for (int i = TotalPages; i >= insertPosition; i--)
            {
                if (_pageStrokes.ContainsKey(i))
                {
                    _pageStrokes[i + 1] = _pageStrokes[i];
                    _pageStrokes.Remove(i);
                }
                if (_pageImages.ContainsKey(i))
                {
                    _pageImages[i + 1] = _pageImages[i];
                    _pageImages.Remove(i);
                }
                if (_pageTextBoxes.ContainsKey(i))
                {
                    _pageTextBoxes[i + 1] = _pageTextBoxes[i];
                    _pageTextBoxes.Remove(i);
                }
            }

            // Shift annotations to new page numbers
            foreach (var annotation in AllAnnotations.Where(a => a.PageNumber >= insertPosition))
            {
                annotation.PageNumber++;
            }

            // Reload thumbnails from the updated PDF
            TotalPages++;
            
            if (updatePdfFile)
            {
                LoadPageThumbnails();
            }
            else
            {
                // For undo/redo, just create a blank thumbnail
                var blankThumbnail = CreateBlankPageThumbnail(insertPosition);
                PageThumbnails.Insert(insertPosition - 1, blankThumbnail);
                UpdateAllThumbnailPageNumbers();
            }
            
            // Update IsCurrentPage on all thumbnails
            UpdateThumbnailCurrentPageStates();
            
            UpdatePageNavigation();
            
            // Navigate to the newly inserted page
            CurrentPage = insertPosition;
            RenderCurrentPage();
            
            if (updatePdfFile)
            {
                StatusMessage = $"Blank page added at position {insertPosition}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding blank page: {ex.Message}";
        }
    }

    private PageThumbnailModel CreateBlankPageThumbnail(int pageNumber)
    {
        // Create a blank white thumbnail
        var bitmap = new WriteableBitmap(ThumbnailWidth, ThumbnailHeight, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[ThumbnailWidth * ThumbnailHeight * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;     // B
            pixels[i + 1] = 255; // G
            pixels[i + 2] = 255; // R
            pixels[i + 3] = 255; // A
        }
        bitmap.WritePixels(new Int32Rect(0, 0, ThumbnailWidth, ThumbnailHeight), pixels, ThumbnailWidth * 4, 0);
        bitmap.Freeze();

        return new PageThumbnailModel
        {
            PageNumber = pageNumber,
            Thumbnail = bitmap,
            IsCurrentPage = false
        };
    }

    private void UpdateAllThumbnailPageNumbers()
    {
        for (int i = 0; i < PageThumbnails.Count; i++)
        {
            PageThumbnails[i].PageNumber = i + 1;
        }
    }

    private void UpdateThumbnailCurrentPageStates()
    {
        foreach (var thumb in PageThumbnails)
        {
            thumb.IsCurrentPage = thumb.PageNumber == CurrentPage;
        }
    }

    public void TogglePageSelection(int pageNumber, bool isCtrlPressed)
    {
        var thumbnail = PageThumbnails.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (thumbnail == null) return;

        if (isCtrlPressed)
        {
            // Toggle selection for this page
            thumbnail.IsSelected = !thumbnail.IsSelected;
        }
        else
        {
            // Clear other selections and select this one
            ClearPageSelection();
            thumbnail.IsSelected = true;
        }
    }

    public void ClearPageSelection()
    {
        foreach (var thumbnail in PageThumbnails)
        {
            thumbnail.IsSelected = false;
        }
    }

    public List<int> GetSelectedPageNumbers()
    {
        return PageThumbnails.Where(p => p.IsSelected).Select(p => p.PageNumber).ToList();
    }

    #endregion

    // Property for the last saved file path (for hyperlink functionality)
    [ObservableProperty]
    private string? _lastSavedFilePath;

    [ObservableProperty]
    private bool _hasLastSavedFile;

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
                StatusMessage = "Saving PDF with annotations...";
                
                // Save current page strokes before saving
                OnSaveCurrentPageStrokes?.Invoke();
                
                // Count total annotations
                int totalInkStrokes = _pageStrokes.Values.Sum(s => s.Count);
                int totalImages = _pageImages.Values.Sum(i => i.Count);
                int totalTextBoxes = _pageTextBoxes.Values.Sum(t => t.Count);
                
                // Flatten all annotations (ink, images, and text boxes) to PDF
                await FlattenInkToPdf(dialog.FileName);
                
                // Store the saved file path for hyperlink functionality
                LastSavedFilePath = dialog.FileName;
                HasLastSavedFile = true;
                
                // Build status message
                var statusParts = new List<string>();
                if (totalInkStrokes > 0) statusParts.Add($"{totalInkStrokes} ink stroke(s)");
                if (totalImages > 0) statusParts.Add($"{totalImages} image(s)");
                if (totalTextBoxes > 0) statusParts.Add($"{totalTextBoxes} text box(es)");
                
                if (statusParts.Count > 0)
                {
                    StatusMessage = $"Saved with {string.Join(" and ", statusParts)} embedded";
                }
                else
                {
                    StatusMessage = $"Saved successfully";
                }
                
                if (AllAnnotations.Count > 0)
                {
                    StatusMessage += $" (Note: {AllAnnotations.Count} text/shape annotation(s) are session-only)";
                }
                
                HasPendingChanges = false;
            }
            catch (Exception ex)
            {
                // Display the error in a message box so the user can copy it
                var errorMessage = $"Error saving PDF:\n\n{ex.Message}\n\nDetails:\n{ex.ToString()}";
                System.Windows.MessageBox.Show(
                    errorMessage,
                    "Error Saving PDF",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusMessage = $"Error saving: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void OpenSavedFile()
    {
        if (!string.IsNullOrEmpty(LastSavedFilePath) && File.Exists(LastSavedFilePath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LastSavedFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error opening file:\n\n{ex.Message}",
                    "Error Opening File",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    // Event to request saving current page strokes from the view before saving PDF
    public event Action? OnSaveCurrentPageStrokes;

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

    /// <summary>
    /// Apply a specific zoom delta (for mouse wheel zooming)
    /// </summary>
    public void ApplyZoomDelta(double delta)
    {
        var newZoom = ZoomLevel + delta;
        if (newZoom >= 0.1 && newZoom <= 5.0)
        {
            ZoomLevel = newZoom;
            UpdateCanvasSize();
        }
    }

    [RelayCommand]
    private void FitToWidth()
    {
        if (!IsPdfLoaded) return;
        
        // This will be called from the view with the actual container width
        StatusMessage = "Fit to Width - Adjust zoom to fit page width";
        // The actual implementation will be in the View code-behind
    }

    [RelayCommand]
    private void FitToPage()
    {
        if (!IsPdfLoaded) return;
        
        // This will be called from the view with the actual container size
        StatusMessage = "Fit to Page - Adjust zoom to fit entire page";
        // The actual implementation will be in the View code-behind
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        StatusMessage = IsFullscreen ? "Fullscreen mode enabled" : "Fullscreen mode disabled";
    }

    // Event to notify view about fullscreen toggle
    public event Action<bool>? OnFullscreenToggled;

    partial void OnIsFullscreenChanged(bool value)
    {
        OnFullscreenToggled?.Invoke(value);
    }

    [RelayCommand]
    private void ToggleLeftPanel()
    {
        IsLeftPanelVisible = !IsLeftPanelVisible;
        StatusMessage = IsLeftPanelVisible ? "Pages panel shown" : "Pages panel hidden";
    }

    [RelayCommand]
    private void ToggleRightPanel()
    {
        IsRightPanelVisible = !IsRightPanelVisible;
        StatusMessage = IsRightPanelVisible ? "Annotations panel shown" : "Annotations panel hidden";
    }

    [RelayCommand]
    private void ToggleDualPageMode()
    {
        IsDualPageMode = !IsDualPageMode;
        StatusMessage = IsDualPageMode ? "Dual page view enabled" : "Single page view";
        if (IsDualPageMode)
        {
            RenderCurrentPage();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            // Save current page strokes before changing page
            OnSaveCurrentPageStrokes?.Invoke();
            
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
            // Save current page strokes before changing page
            OnSaveCurrentPageStrokes?.Invoke();
            
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

    partial void OnIsImageToolActiveChanged(bool value)
    {
        if (value)
        {
            ClearOtherTools(nameof(IsImageToolActive));
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
        if (exceptTool != nameof(IsImageToolActive)) IsImageToolActive = false;
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
                         IsImageToolActive ? "Image" :
                         IsRedactToolActive ? "Redact" : "None";
    }

    private void UpdateInkAttributesForHighlight()
    {
        InkDrawingAttributes = new DrawingAttributes
        {
            Color = Color.FromArgb(100, SelectedColor.R, SelectedColor.G, SelectedColor.B), // Semi-transparent
            Width = 20,
            Height = 20,
            StylusTip = StylusTip.Rectangle,
            IsHighlighter = true,
            FitToCurve = true,
            IgnorePressure = false
        };
    }

    private void UpdateInkAttributesForDrawing()
    {
        InkDrawingAttributes = new DrawingAttributes
        {
            Color = SelectedColor,
            Width = PenThickness,
            Height = PenThickness,
            StylusTip = StylusTip.Ellipse,
            IsHighlighter = false,
            FitToCurve = true,
            IgnorePressure = false
        };
    }

    private void UpdateInkAttributesForRedaction()
    {
        InkDrawingAttributes = new DrawingAttributes
        {
            Color = Colors.Black,
            Width = 15,
            Height = 15,
            StylusTip = StylusTip.Rectangle,
            IsHighlighter = false,
            FitToCurve = false,
            IgnorePressure = true
        };
    }

    partial void OnSelectedColorChanged(Color value)
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

    partial void OnPenThicknessChanged(double value)
    {
        if (IsDrawToolActive)
        {
            UpdateInkAttributesForDrawing();
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
        if (pageNumber >= 1 && pageNumber <= TotalPages && pageNumber != CurrentPage)
        {
            // Save current page strokes before changing page
            OnSaveCurrentPageStrokes?.Invoke();
            
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
        else if (IsShapeToolActive)
        {
            // Show dummy class path for shape tool
            StatusMessage = "Shape Tool: PDFKawankasi.Models.ShapeAnnotation";
            _currentAnnotation.Description = "Shape annotation - implementation class: PDFKawankasi.Models.ShapeAnnotation";
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
        
        // Load images for this page
        LoadCurrentPageImages();
        
        // Load text boxes for this page
        LoadCurrentPageTextBoxes();
    }

    private Dictionary<int, StrokeCollection> _pageStrokes = new();
    private Dictionary<int, List<ImageAnnotation>> _pageImages = new();
    private Dictionary<int, List<TextBoxAnnotation>> _pageTextBoxes = new();

    // Image annotation model
    public class ImageAnnotation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BitmapSource? Image { get; set; }
        public byte[]? ImageBytes { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsSelected { get; set; }
    }

    // Text box annotation model - movable text on canvas that gets rasterized on save
    public class TextBoxAnnotation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 50;
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 14;
        public Color TextColor { get; set; } = Colors.Black;
        public bool IsSelected { get; set; }
    }

    [ObservableProperty]
    private ObservableCollection<ImageAnnotation> _currentPageImages = new();

    [ObservableProperty]
    private ObservableCollection<TextBoxAnnotation> _currentPageTextBoxes = new();

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
        HasPendingChanges = true;
    }

    public void NotifyStrokeAdded()
    {
        HasPendingChanges = true;
        // Note: Individual stroke undo is handled via NotifyStrokeAddedWithUndo
    }

    public void NotifyStrokeAddedWithUndo(Stroke stroke)
    {
        var action = new StrokeAddedAction
        {
            PageNumber = CurrentPage,
            Stroke = stroke
        };
        PushUndoAction(action);
    }

    public void NotifyStrokeErasedWithUndo(Stroke stroke)
    {
        var action = new StrokeErasedAction
        {
            PageNumber = CurrentPage,
            Stroke = stroke
        };
        PushUndoAction(action);
    }

    public void ClearCurrentPageStrokes()
    {
        CurrentPageStrokes.Clear();
        if (_pageStrokes.ContainsKey(CurrentPage))
        {
            _pageStrokes[CurrentPage].Clear();
        }
    }

    private void LoadCurrentPageImages()
    {
        CurrentPageImages.Clear();
        if (_pageImages.TryGetValue(CurrentPage, out var images))
        {
            foreach (var image in images)
            {
                CurrentPageImages.Add(image);
            }
        }
        OnPropertyChanged(nameof(CurrentPageImages));
    }

    public void SaveCurrentPageImages()
    {
        _pageImages[CurrentPage] = new List<ImageAnnotation>(CurrentPageImages);
    }

    private void LoadCurrentPageTextBoxes()
    {
        CurrentPageTextBoxes.Clear();
        if (_pageTextBoxes.TryGetValue(CurrentPage, out var textBoxes))
        {
            foreach (var textBox in textBoxes)
            {
                CurrentPageTextBoxes.Add(textBox);
            }
        }
        OnPropertyChanged(nameof(CurrentPageTextBoxes));
    }

    public void SaveCurrentPageTextBoxes()
    {
        _pageTextBoxes[CurrentPage] = new List<TextBoxAnnotation>(CurrentPageTextBoxes);
    }

    /// <summary>
    /// Add a new text box annotation to the current page at the specified position
    /// </summary>
    public void AddTextBox(double x, double y)
    {
        // Switch to Select tool so the user can interact with the new text box
        IsSelectToolActive = true;

        var textBox = new TextBoxAnnotation
        {
            X = x,
            Y = y,
            Width = DefaultTextBoxWidth,
            Height = DefaultTextBoxHeight,
            FontFamily = SelectedFontFamily,
            FontSize = SelectedFontSize,
            TextColor = SelectedColor,
            Text = DefaultTextBoxPlaceholder
        };

        CurrentPageTextBoxes.Add(textBox);
        SaveCurrentPageTextBoxes();
        
        var action = new TextBoxAddedAction
        {
            PageNumber = CurrentPage,
            TextBox = textBox
        };
        PushUndoAction(action);
        
        StatusMessage = $"Added text box to page {CurrentPage}. Click to edit, drag to move.";
    }

    public void UpdateTextBoxPosition(string textBoxId, double x, double y)
    {
        var textBox = CurrentPageTextBoxes.FirstOrDefault(t => t.Id == textBoxId);
        if (textBox != null)
        {
            textBox.X = x;
            textBox.Y = y;
            SaveCurrentPageTextBoxes();
            HasPendingChanges = true;
        }
    }

    public void UpdateTextBoxSize(string textBoxId, double width, double height)
    {
        var textBox = CurrentPageTextBoxes.FirstOrDefault(t => t.Id == textBoxId);
        if (textBox != null)
        {
            textBox.Width = width;
            textBox.Height = height;
            SaveCurrentPageTextBoxes();
            HasPendingChanges = true;
        }
    }

    public void UpdateTextBoxContent(string textBoxId, string text)
    {
        var textBox = CurrentPageTextBoxes.FirstOrDefault(t => t.Id == textBoxId);
        if (textBox != null)
        {
            textBox.Text = text;
            SaveCurrentPageTextBoxes();
            HasPendingChanges = true;
        }
    }

    public void DeleteTextBox(string textBoxId)
    {
        var textBox = CurrentPageTextBoxes.FirstOrDefault(t => t.Id == textBoxId);
        if (textBox != null)
        {
            var action = new TextBoxDeletedAction
            {
                PageNumber = CurrentPage,
                TextBox = textBox
            };
            PushUndoAction(action);
            
            CurrentPageTextBoxes.Remove(textBox);
            SaveCurrentPageTextBoxes();
            StatusMessage = "Text box deleted";
        }
    }

    public void AddImage()
    {
        // Switch to Select tool so the user can move/resize the newly added image
        IsSelectToolActive = true;
        
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*",
            Title = "Select Image to Add"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imageBytes = File.ReadAllBytes(dialog.FileName);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.EndInit();
                bitmap.Freeze();

                // Ensure canvas dimensions are valid
                var maxWidth = CanvasWidth > 0 ? CanvasWidth / 2 : 400;
                var maxHeight = CanvasHeight > 0 ? CanvasHeight / 2 : 500;

                var imageAnnotation = new ImageAnnotation
                {
                    Image = bitmap,
                    ImageBytes = imageBytes,
                    X = 100,
                    Y = 100,
                    Width = Math.Min(bitmap.PixelWidth, maxWidth),
                    Height = Math.Min(bitmap.PixelHeight, maxHeight)
                };

                CurrentPageImages.Add(imageAnnotation);
                SaveCurrentPageImages();
                
                var action = new ImageAddedAction
                {
                    PageNumber = CurrentPage,
                    Image = imageAnnotation
                };
                PushUndoAction(action);
                
                StatusMessage = $"Added image to page {CurrentPage}. Use Select tool to move/resize.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding image: {ex.Message}";
            }
        }
    }

    public void UpdateImagePosition(string imageId, double x, double y)
    {
        var image = CurrentPageImages.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.X = x;
            image.Y = y;
            SaveCurrentPageImages();
            HasPendingChanges = true;
        }
    }

    public void UpdateImageSize(string imageId, double width, double height)
    {
        var image = CurrentPageImages.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.Width = width;
            image.Height = height;
            SaveCurrentPageImages();
            HasPendingChanges = true;
        }
    }

    public void DeleteImage(string imageId)
    {
        var image = CurrentPageImages.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            var action = new ImageDeletedAction
            {
                PageNumber = CurrentPage,
                Image = image
            };
            PushUndoAction(action);
            
            CurrentPageImages.Remove(image);
            SaveCurrentPageImages();
            StatusMessage = "Image deleted";
        }
    }

    private void UpdateAnnotationCount()
    {
        AnnotationCount = AllAnnotations.Count;
        HasAnnotations = AnnotationCount > 0;
    }

    private string ShowTextInputDialog(string title)
    {
        // Enhanced input dialog with font and size options for text annotations
        var dialog = new Window
        {
            Title = title,
            Width = 450,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        
        // Font settings row
        var fontSettingsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };
        
        var fontLabel = new TextBlock
        {
            Text = "Font: ",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        
        var fontComboBox = new ComboBox
        {
            Width = 140,
            SelectedItem = SelectedFontFamily,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 16, 0)
        };
        foreach (var font in AvailableFonts)
        {
            fontComboBox.Items.Add(font);
        }
        fontComboBox.SelectedItem = SelectedFontFamily;
        
        var sizeLabel = new TextBlock
        {
            Text = "Size: ",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        
        var sizeComboBox = new ComboBox
        {
            Width = 70,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            Foreground = Brushes.White
        };
        foreach (var size in AvailableFontSizes)
        {
            sizeComboBox.Items.Add(size);
        }
        sizeComboBox.SelectedItem = SelectedFontSize;
        
        fontSettingsPanel.Children.Add(fontLabel);
        fontSettingsPanel.Children.Add(fontComboBox);
        fontSettingsPanel.Children.Add(sizeLabel);
        fontSettingsPanel.Children.Add(sizeComboBox);
        
        var textBox = new TextBox
        {
            Height = 100,
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
        okButton.Click += (s, e) => 
        { 
            result = textBox.Text;
            // Update selected font and size
            if (fontComboBox.SelectedItem != null)
                SelectedFontFamily = fontComboBox.SelectedItem.ToString() ?? "Arial";
            if (sizeComboBox.SelectedItem != null)
                SelectedFontSize = (double)sizeComboBox.SelectedItem;
            dialog.Close(); 
        };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);
        
        panel.Children.Add(fontSettingsPanel);
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        dialog.ShowDialog();
        return result;
    }

    private async Task FlattenInkToPdf(string outputPath)
    {
        // Request the view to render ink strokes and images for all pages
        var renderedPages = new Dictionary<int, byte[]>();
        
        // Signal that we need rendered pages
        OnInkRenderRequested?.Invoke(renderedPages);
        
        // Check for images and text boxes as well
        bool hasImages = _pageImages.Values.Any(images => images.Count > 0);
        bool hasTextBoxes = _pageTextBoxes.Values.Any(textBoxes => textBoxes.Count > 0);
        
        if (renderedPages.Count == 0 && !hasImages && !hasTextBoxes)
        {
            // No rendered pages, images, or text boxes, just save original
            File.WriteAllBytes(outputPath, _pdfBytes!);
            HasPendingChanges = false;
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
            int pageNumber = i + 1;
            
            using var gfx = XGraphics.FromPdfPage(page);
            
            // If this page has ink strokes, overlay them
            if (renderedPages.TryGetValue(pageNumber, out var inkImageBytes))
            {
                using var inkImage = XImage.FromStream(() => new MemoryStream(inkImageBytes));
                
                // Draw ink overlay on top of existing content
                gfx.DrawImage(inkImage, 0, 0, page.Width, page.Height);
            }
            
            // If this page has images, rasterize and draw them
            if (_pageImages.TryGetValue(pageNumber, out var images) && images.Count > 0)
            {
                foreach (var imgAnnotation in images)
                {
                    if (imgAnnotation.ImageBytes != null)
                    {
                        try
                        {
                            using var imgStream = new MemoryStream(imgAnnotation.ImageBytes);
                            using var xImage = XImage.FromStream(() => new MemoryStream(imgAnnotation.ImageBytes));
                            
                            // Convert pixel coordinates to PDF points (assuming 96 DPI)
                            double scaleX = page.Width / CanvasWidth;
                            double scaleY = page.Height / CanvasHeight;
                            
                            double pdfX = imgAnnotation.X * scaleX;
                            double pdfY = imgAnnotation.Y * scaleY;
                            double pdfWidth = imgAnnotation.Width * scaleX;
                            double pdfHeight = imgAnnotation.Height * scaleY;
                            
                            gfx.DrawImage(xImage, pdfX, pdfY, pdfWidth, pdfHeight);
                        }
                        catch (Exception ex)
                        {
                            // Log and skip this image if there's an error loading/drawing it
                            System.Diagnostics.Debug.WriteLine($"Failed to embed image on page {pageNumber}: {ex.Message}");
                        }
                    }
                }
            }
            
            // If this page has text boxes, rasterize and draw them
            if (_pageTextBoxes.TryGetValue(pageNumber, out var textBoxes) && textBoxes.Count > 0)
            {
                foreach (var textBox in textBoxes)
                {
                    try
                    {
                        // Convert pixel coordinates to PDF points
                        double scaleX = page.Width / CanvasWidth;
                        double scaleY = page.Height / CanvasHeight;
                        
                        double pdfX = textBox.X * scaleX;
                        double pdfY = textBox.Y * scaleY;
                        
                        // Create font for the text
                        var fontFamily = textBox.FontFamily;
                        var fontSize = textBox.FontSize * scaleY; // Scale font size proportionally
                        
                        var xFont = new XFont(fontFamily, fontSize, XFontStyle.Regular);
                        var xBrush = new XSolidBrush(XColor.FromArgb(
                            textBox.TextColor.A, 
                            textBox.TextColor.R, 
                            textBox.TextColor.G, 
                            textBox.TextColor.B));
                        
                        // Draw the text
                        if (!string.IsNullOrEmpty(textBox.Text))
                        {
                            // Draw text with word wrapping within the bounding box
                            var pdfWidth = textBox.Width * scaleX;
                            var pdfHeight = textBox.Height * scaleY;
                            var rect = new XRect(pdfX, pdfY, pdfWidth, pdfHeight);
                            
                            var format = new XStringFormat
                            {
                                Alignment = XStringAlignment.Near,
                                LineAlignment = XLineAlignment.Near
                            };
                            
                            gfx.DrawString(textBox.Text, xFont, xBrush, rect, format);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log and skip this text box if there's an error
                        System.Diagnostics.Debug.WriteLine($"Failed to embed text box on page {pageNumber}: {ex.Message}");
                    }
                }
            }
        }

        // Save the modified PDF
        outputDoc.Save(outputPath);
        HasPendingChanges = false;
    }

    // Event to request ink rendering from the view
    public event Action<Dictionary<int, byte[]>>? OnInkRenderRequested;

    #endregion
}
