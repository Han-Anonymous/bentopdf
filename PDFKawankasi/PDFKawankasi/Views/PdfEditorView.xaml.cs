using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PDFKawankasi.Models;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

/// <summary>
/// Interaction logic for PdfEditorView.xaml with Windows Inking API integration
/// </summary>
public partial class PdfEditorView : UserControl
{
    // Constants for default page dimensions
    private const double DefaultPageWidth = 800.0;
    private const double DefaultPageHeight = 1000.0;
    
    // Constants for text box UI layout
    private const double TextBoxBorderPadding = 4.0;
    private const double TextBoxDragHandleHeight = 24.0;
    private const double MinTextBoxWidth = 100.0;
    private const double MinTextBoxHeight = 50.0;
    
    // Constants for snap assist angle thresholds (in degrees)
    private const double SnapHorizontalThreshold = 22.5;
    private const double SnapVerticalThreshold = 67.5;
    
    private PdfEditorViewModel ViewModel => (PdfEditorViewModel)DataContext;
    private bool _isDrawing;
    private Point _startPoint;

    public PdfEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Bind InkCanvas strokes to ViewModel
        if (PdfInkCanvas != null && ViewModel != null)
        {
            PdfInkCanvas.Strokes = ViewModel.CurrentPageStrokes;
            
            // Enable gesture recognition for scratch-out (erasing) and other gestures
            PdfInkCanvas.Gesture += OnInkCanvasGesture;
            
            // Subscribe to property changes to sync strokes when page changes
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // Subscribe to ink render requests for PDF saving
            ViewModel.OnInkRenderRequested += OnInkRenderRequested;
            
            // Subscribe to save current page strokes request
            ViewModel.OnSaveCurrentPageStrokes += OnSaveCurrentPageStrokes;
            
            // Subscribe to fullscreen toggle
            ViewModel.OnFullscreenToggled += OnFullscreenToggled;
            
            // Focus the control to enable keyboard shortcuts
            // Done after event subscriptions to ensure proper initialization
            Dispatcher.BeginInvoke(new Action(() => this.Focus()), 
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (PdfInkCanvas != null)
        {
            PdfInkCanvas.Gesture -= OnInkCanvasGesture;
        }

        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.OnInkRenderRequested -= OnInkRenderRequested;
            ViewModel.OnSaveCurrentPageStrokes -= OnSaveCurrentPageStrokes;
            ViewModel.OnFullscreenToggled -= OnFullscreenToggled;
        }
    }

    #region Keyboard and Mouse Wheel Handlers

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Track Shift key for snap assist
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            ViewModel.IsShiftKeyPressed = true;
        }

        // F11 for fullscreen toggle
        if (e.Key == Key.F11)
        {
            ViewModel.ToggleFullscreenCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Handle Ctrl+Plus and Ctrl+Minus for zooming, Ctrl+Z for undo, Ctrl+Y for redo
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                ViewModel.ZoomInCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                ViewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                // Ctrl+0 to reset zoom to 100%
                if (ViewModel.IsPdfLoaded)
                {
                    ViewModel.ApplyZoomDelta(1.0 - ViewModel.ZoomLevel);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Z)
            {
                // Ctrl+Z for undo
                if (ViewModel.CanUndo)
                {
                    ViewModel.UndoCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Y)
            {
                // Ctrl+Y for redo
                if (ViewModel.CanRedo)
                {
                    ViewModel.RedoCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        // Track Shift key release for snap assist
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            ViewModel.IsShiftKeyPressed = false;
        }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl+Mouse Wheel for zooming
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (ViewModel.IsPdfLoaded)
            {
                double delta = e.Delta > 0 ? 0.1 : -0.1;
                ViewModel.ApplyZoomDelta(delta);
            }
            e.Handled = true;
        }
    }

    // Store original window properties for restoring after fullscreen
    private WindowState _originalWindowState;
    private WindowStyle _originalWindowStyle;
    private ResizeMode _originalResizeMode;
    private double _originalLeft;
    private double _originalTop;
    private double _originalWidth;
    private double _originalHeight;
    private bool _originalTopmost;

    private void OnFullscreenToggled(bool isFullscreen)
    {
        // Find parent window and toggle true OS-level fullscreen
        var window = Window.GetWindow(this);
        if (window != null)
        {
            if (isFullscreen)
            {
                // Store original window properties
                _originalWindowState = window.WindowState;
                _originalWindowStyle = window.WindowStyle;
                _originalResizeMode = window.ResizeMode;
                _originalTopmost = window.Topmost;
                
                // Only store bounds if window is in normal state
                if (window.WindowState == WindowState.Normal)
                {
                    _originalLeft = window.Left;
                    _originalTop = window.Top;
                    _originalWidth = window.Width;
                    _originalHeight = window.Height;
                }

                // Get the full screen dimensions (including taskbar area) from primary screen
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                // Enter true fullscreen mode
                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;
                window.Topmost = true;
                
                // Must set to Normal first, then manually set bounds to cover entire screen including taskbar
                window.WindowState = WindowState.Normal;
                window.Left = 0;
                window.Top = 0;
                window.Width = screenWidth;
                window.Height = screenHeight;
            }
            else
            {
                // Exit fullscreen - restore original properties
                window.Topmost = _originalTopmost;
                window.ResizeMode = _originalResizeMode;
                window.WindowStyle = _originalWindowStyle;
                
                // Restore window bounds if we stored them
                if (_originalWidth > 0 && _originalHeight > 0)
                {
                    window.WindowState = WindowState.Normal;
                    window.Left = _originalLeft;
                    window.Top = _originalTop;
                    window.Width = _originalWidth;
                    window.Height = _originalHeight;
                }
                
                // Restore original window state
                window.WindowState = _originalWindowState;
            }
        }
    }

    private void OnFitToWidthClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsPdfLoaded || PdfScrollViewer == null) return;

        // Calculate zoom to fit width
        var availableWidth = PdfScrollViewer.ActualWidth - 20; // Subtract scrollbar width
        if (availableWidth > 0 && ViewModel.CanvasWidth > 0)
        {
            var desiredZoom = availableWidth / DefaultPageWidth;
            ViewModel.ApplyZoomDelta(desiredZoom - ViewModel.ZoomLevel);
        }
    }

    private void OnFitToPageClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsPdfLoaded || PdfScrollViewer == null) return;

        // Calculate zoom to fit entire page
        var availableWidth = PdfScrollViewer.ActualWidth - 20;
        var availableHeight = PdfScrollViewer.ActualHeight - 20;
        
        if (availableWidth > 0 && availableHeight > 0)
        {
            var zoomW = availableWidth / DefaultPageWidth;
            var zoomH = availableHeight / DefaultPageHeight;
            var desiredZoom = Math.Min(zoomW, zoomH);
            ViewModel.ApplyZoomDelta(desiredZoom - ViewModel.ZoomLevel);
        }
    }

    private void OnAddImageClick(object sender, RoutedEventArgs e)
    {
        // Immediately open file picker for adding image
        if (ViewModel.IsPdfLoaded)
        {
            ViewModel.AddImage();
            RefreshImagesDisplay();
        }
    }

    #endregion

    private void OnSaveCurrentPageStrokes()
    {
        if (PdfInkCanvas != null)
        {
            ViewModel.SaveCurrentPageStrokes(PdfInkCanvas.Strokes);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfEditorViewModel.CurrentPageStrokes) && PdfInkCanvas != null)
        {
            // Sync InkCanvas strokes with ViewModel
            PdfInkCanvas.Strokes = ViewModel.CurrentPageStrokes;
        }
        else if (e.PropertyName == nameof(PdfEditorViewModel.CurrentPageImages))
        {
            // Refresh images display
            RefreshImagesDisplay();
        }
        else if (e.PropertyName == nameof(PdfEditorViewModel.CurrentPageTextBoxes))
        {
            // Refresh text boxes display
            RefreshTextBoxesDisplay();
        }
    }

    private void OnInkCanvasGesture(object sender, InkCanvasGestureEventArgs e)
    {
        // Handle recognized gestures
        var gestures = e.GetGestureRecognitionResults();
        
        foreach (var gesture in gestures)
        {
            if (gesture.ApplicationGesture == ApplicationGesture.ScratchOut)
            {
                // Scratch-out gesture detected - could be used for quick erase
                ViewModel.StatusMessage = "Scratch-out gesture detected";
            }
            else if (gesture.ApplicationGesture == ApplicationGesture.Check)
            {
                ViewModel.StatusMessage = "Check mark gesture detected";
            }
            else if (gesture.ApplicationGesture == ApplicationGesture.Circle)
            {
                ViewModel.StatusMessage = "Circle gesture detected";
            }
        }
    }

    private void OnColorPickerClick(object sender, MouseButtonEventArgs e)
    {
        // Show a simple color selection popup with common colors
        var popup = new System.Windows.Controls.Primitives.Popup
        {
            PlacementTarget = sender as UIElement,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            IsOpen = true
        };

        var colorPanel = new WrapPanel { Width = 160, Background = new SolidColorBrush(Color.FromRgb(45, 55, 72)) };
        
        Color[] colors = {
            Colors.Yellow, Colors.Orange, Colors.Red, Colors.Pink,
            Colors.Green, Colors.LightGreen, Colors.Blue, Colors.LightBlue,
            Colors.Purple, Colors.Magenta, Colors.White, Colors.Black,
            Colors.Gray, Colors.DarkGray, Colors.Brown, Colors.Cyan
        };

        foreach (var color in colors)
        {
            var colorBtn = new Button
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(4),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(color == ViewModel.SelectedColor ? 3 : 1),
                BorderBrush = color == ViewModel.SelectedColor 
                    ? new SolidColorBrush(Colors.White) 
                    : new SolidColorBrush(Colors.Gray)
            };
            
            colorBtn.Click += (s, args) =>
            {
                ViewModel.SelectedColor = color;
                popup.IsOpen = false;
            };
            
            colorPanel.Children.Add(colorBtn);
        }

        popup.Child = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = colorPanel
        };

        popup.StaysOpen = false;
    }

    private void OnThumbnailClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is PageThumbnailModel thumbnail)
        {
            // Check for Ctrl+Click for multi-select
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                ViewModel.TogglePageSelection(thumbnail.PageNumber, true);
                return;
            }

            // Save current page strokes, images, and text boxes before switching
            if (PdfInkCanvas != null)
            {
                ViewModel.SaveCurrentPageStrokes(PdfInkCanvas.Strokes);
                ViewModel.SaveCurrentPageImages();
                ViewModel.SaveCurrentPageTextBoxes();
            }

            ViewModel.GoToPage(thumbnail.PageNumber);

            // Load new page strokes
            if (PdfInkCanvas != null)
            {
                PdfInkCanvas.Strokes = ViewModel.CurrentPageStrokes;
            }
            
            // Refresh images and text boxes display
            RefreshImagesDisplay();
            RefreshTextBoxesDisplay();
        }
    }

    private void OnThumbnailRightClick(object sender, MouseButtonEventArgs e)
    {
        // Right-click shows context menu - handled by XAML context menu
        if (sender is Border border && border.DataContext is PageThumbnailModel thumbnail)
        {
            // Store the page number for context menu commands
            border.Tag = thumbnail.PageNumber;
        }
    }

    /// <summary>
    /// Helper method to get thumbnail from context menu event sender
    /// </summary>
    private PageThumbnailModel? GetThumbnailFromContextMenuEvent(object sender)
    {
        if (sender is MenuItem menuItem 
            && menuItem.Parent is ContextMenu contextMenu 
            && contextMenu.PlacementTarget is Border border 
            && border.DataContext is PageThumbnailModel thumbnail)
        {
            return thumbnail;
        }
        return null;
    }

    private void OnAddPageAboveClick(object sender, RoutedEventArgs e)
    {
        var thumbnail = GetThumbnailFromContextMenuEvent(sender);
        if (thumbnail != null)
        {
            ViewModel.AddPageAboveCommand.Execute(thumbnail.PageNumber);
        }
    }

    private void OnAddPageBelowClick(object sender, RoutedEventArgs e)
    {
        var thumbnail = GetThumbnailFromContextMenuEvent(sender);
        if (thumbnail != null)
        {
            ViewModel.AddPageBelowCommand.Execute(thumbnail.PageNumber);
        }
    }

    private void OnInsertPdfPagesAboveClick(object sender, RoutedEventArgs e)
    {
        var thumbnail = GetThumbnailFromContextMenuEvent(sender);
        if (thumbnail != null)
        {
            ViewModel.InsertPdfPagesAboveCommand.Execute(thumbnail.PageNumber);
        }
    }

    private void OnInsertPdfPagesBelowClick(object sender, RoutedEventArgs e)
    {
        var thumbnail = GetThumbnailFromContextMenuEvent(sender);
        if (thumbnail != null)
        {
            ViewModel.InsertPdfPagesBelowCommand.Execute(thumbnail.PageNumber);
        }
    }

    private void OnDeletePageClick(object sender, RoutedEventArgs e)
    {
        var thumbnail = GetThumbnailFromContextMenuEvent(sender);
        if (thumbnail != null)
        {
            ViewModel.DeletePageCommand.Execute(thumbnail.PageNumber);
        }
    }

    private void OnCommentCalloutClick(object sender, MouseButtonEventArgs e)
    {
        // Toggle the expanded/collapsed state of the comment callout (Adobe-style)
        if (sender is Border border && border.DataContext is AnnotationModel annotation)
        {
            annotation.ToggleExpanded();
            e.Handled = true;
        }
    }

    #region InkCanvas Event Handlers

    private void OnEraseToolClick(object sender, RoutedEventArgs e)
    {
        if (PdfInkCanvas != null && sender is ToggleButton btn && btn.IsChecked == true)
        {
            // Switch to eraser mode
            PdfInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            ViewModel.StatusMessage = "Erase mode - Click on strokes to remove them";
            
            // Deselect other tools
            ViewModel.IsSelectToolActive = false;
            ViewModel.IsHighlightToolActive = false;
            ViewModel.IsDrawToolActive = false;
            ViewModel.IsTextToolActive = false;
            ViewModel.IsShapeToolActive = false;
            ViewModel.IsCommentToolActive = false;
            ViewModel.IsRedactToolActive = false;
        }
        else if (PdfInkCanvas != null)
        {
            // Switch back to select mode
            PdfInkCanvas.EditingMode = InkCanvasEditingMode.None;
            ViewModel.IsSelectToolActive = true;
        }
    }

    private void OnInkStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        // Stroke has been added to InkCanvas
        var stroke = e.Stroke;
        
        // Apply snap assist if Shift is held (straight line)
        if (ViewModel.IsShiftKeyPressed && stroke.StylusPoints.Count >= 2)
        {
            var startPoint = stroke.StylusPoints[0];
            var endPoint = stroke.StylusPoints[stroke.StylusPoints.Count - 1];
            
            // Calculate angle to determine horizontal, vertical, or 45° snap
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            double angle = Math.Atan2(Math.Abs(dy), Math.Abs(dx)) * 180 / Math.PI;
            
            StylusPoint newEndPoint;
            if (angle < SnapHorizontalThreshold)
            {
                // Snap to horizontal
                newEndPoint = new StylusPoint(endPoint.X, startPoint.Y);
            }
            else if (angle > SnapVerticalThreshold)
            {
                // Snap to vertical
                newEndPoint = new StylusPoint(startPoint.X, endPoint.Y);
            }
            else
            {
                // Snap to 45° diagonal
                double length = Math.Max(Math.Abs(dx), Math.Abs(dy));
                newEndPoint = new StylusPoint(
                    startPoint.X + (dx >= 0 ? length : -length),
                    startPoint.Y + (dy >= 0 ? length : -length));
            }
            
            // Replace stroke with a straight line
            var newPoints = new StylusPointCollection { startPoint, newEndPoint };
            stroke.StylusPoints = newPoints;
        }
        
        // Store metadata for the stroke (page number, timestamp, etc.)
        stroke.AddPropertyData(Guid.NewGuid(), ViewModel.CurrentPage);
        
        // Notify ViewModel that a stroke was added (marks pending changes + undo)
        ViewModel.NotifyStrokeAddedWithUndo(stroke);
        
        ViewModel.StatusMessage = $"Ink stroke added on page {ViewModel.CurrentPage}";
    }

    private void OnInkStrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
    {
        // Record the erased stroke for undo
        ViewModel.NotifyStrokeErasedWithUndo(e.Stroke);
        ViewModel.StatusMessage = "Erasing stroke...";
    }

    private void OnInkCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.IsPdfLoaded) return;

        // Handle Image tool - open file dialog to add image
        if (ViewModel.IsImageToolActive)
        {
            ViewModel.AddImage();
            RefreshImagesDisplay();
            return;
        }

        // Handle Text tool - add a movable text box at click location
        if (ViewModel.IsTextToolActive)
        {
            var clickPoint = e.GetPosition(PdfInkCanvas);
            ViewModel.AddTextBox(clickPoint.X, clickPoint.Y);
            RefreshTextBoxesDisplay();
            return;
        }

        // Handle non-ink tools (comment, shape)
        if (ViewModel.IsCommentToolActive || ViewModel.IsShapeToolActive)
        {
            _startPoint = e.GetPosition(PdfInkCanvas);
            _isDrawing = true;
            ViewModel.StartAnnotation(_startPoint);
        }
    }

    private void OnInkCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || !ViewModel.IsPdfLoaded) return;

        var currentPoint = e.GetPosition(PdfInkCanvas);
        ViewModel.UpdateAnnotation(currentPoint);
    }

    private void OnInkCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        var endPoint = e.GetPosition(PdfInkCanvas);
        ViewModel.FinishAnnotation(endPoint);
    }

    private void OnInkRenderRequested(Dictionary<int, byte[]> renderedPages)
    {
        // Render ink strokes for all pages that have them
        foreach (var pageStrokeEntry in ViewModel.GetType()
            .GetField("_pageStrokes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(ViewModel) as Dictionary<int, System.Windows.Ink.StrokeCollection> ?? new Dictionary<int, System.Windows.Ink.StrokeCollection>())
        {
            int pageNumber = pageStrokeEntry.Key;
            var strokes = pageStrokeEntry.Value;

            if (strokes.Count == 0) continue;

            // Get page dimensions
            double width = ViewModel.CanvasWidth;
            double height = ViewModel.CanvasHeight;

            // Create a visual to render the page with ink
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                // Draw white background
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Draw the PDF page image
                if (ViewModel.CurrentPageImage != null && pageNumber == ViewModel.CurrentPage)
                {
                    context.DrawImage(ViewModel.CurrentPageImage, new Rect(0, 0, width, height));
                }
                else
                {
                    // Load the page image for this specific page
                    var pageImage = RenderPageImage(pageNumber);
                    if (pageImage != null)
                    {
                        context.DrawImage(pageImage, new Rect(0, 0, width, height));
                    }
                }

                // Draw ink strokes
                foreach (var stroke in strokes)
                {
                    stroke.Draw(context, stroke.DrawingAttributes);
                }
            }

            // Render to bitmap
            var renderTarget = new RenderTargetBitmap(
                (int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);

            // Encode to PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            renderedPages[pageNumber] = stream.ToArray();
        }
    }

    private BitmapSource? RenderPageImage(int pageNumber)
    {
        try
        {
            // Access private field to get PDF bytes
            var pdfBytesField = ViewModel.GetType()
                .GetField("_pdfBytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pdfBytes = pdfBytesField?.GetValue(ViewModel) as byte[];

            if (pdfBytes == null) return null;

            var docLibField = ViewModel.GetType()
                .GetField("_docLib", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var docLib = docLibField?.GetValue(ViewModel) as Docnet.Core.IDocLib;

            if (docLib == null) return null;

            using var reader = docLib.GetDocReader(pdfBytes, new Docnet.Core.Models.PageDimensions(
                (int)ViewModel.CanvasWidth, (int)ViewModel.CanvasHeight));
            using var pageReader = reader.GetPageReader(pageNumber - 1);

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage();

            var bitmap = BitmapSource.Create(
                width, height, 96, 96,
                PixelFormats.Bgra32, null, rawBytes, width * 4);
            bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Image Handling

    private readonly Dictionary<string, Border> _imageElements = new();
    private string? _draggingImageId;
    private Point _dragStartPoint;
    private bool _isResizing;
    private string? _resizingImageId;

    private void RefreshImagesDisplay()
    {
        // Clear existing image elements from InkCanvas
        var elementsToRemove = PdfInkCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is string tag && tag.StartsWith("IMAGE:"))
            .ToList();
        
        foreach (var element in elementsToRemove)
        {
            PdfInkCanvas.Children.Remove(element);
        }
        _imageElements.Clear();

        // Add images for current page
        foreach (var imgAnnotation in ViewModel.CurrentPageImages)
        {
            AddImageToCanvas(imgAnnotation);
        }
    }

    private void AddImageToCanvas(PdfEditorViewModel.ImageAnnotation imgAnnotation)
    {
        if (imgAnnotation.Image == null) return;

        var image = new System.Windows.Controls.Image
        {
            Source = imgAnnotation.Image,
            Stretch = Stretch.Fill,
            Width = imgAnnotation.Width,
            Height = imgAnnotation.Height
        };

        // Create resize handle
        var resizeHandle = new Border
        {
            Width = 12,
            Height = 12,
            Background = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(2),
            Cursor = Cursors.SizeNWSE,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, -6, -6),
            Tag = $"RESIZE:{imgAnnotation.Id}"
        };

        resizeHandle.MouseLeftButtonDown += OnResizeHandleMouseDown;
        resizeHandle.MouseLeftButtonUp += OnResizeHandleMouseUp;
        resizeHandle.MouseMove += OnResizeHandleMouseMove;

        // Create delete button
        var deleteButton = new Button
        {
            Content = "✕",
            Width = 20,
            Height = 20,
            FontSize = 10,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -10, -10, 0),
            Background = new SolidColorBrush(Colors.Red),
            Foreground = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Tag = imgAnnotation.Id
        };
        deleteButton.Click += OnDeleteImageClick;

        // Create grid to hold image and controls
        var grid = new Grid();
        grid.Children.Add(image);
        grid.Children.Add(resizeHandle);
        grid.Children.Add(deleteButton);

        // Create container border (initially transparent)
        var container = new Border
        {
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Cursor = Cursors.SizeAll,
            Child = grid,
            Tag = $"IMAGE:{imgAnnotation.Id}",
            Focusable = true
        };

        container.MouseLeftButtonDown += OnImageMouseDown;
        container.MouseLeftButtonUp += OnImageMouseUp;
        container.MouseMove += OnImageMouseMove;
        
        // Show border when mouse enters or gets focus
        container.MouseEnter += (s, e) =>
        {
            container.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            container.BorderThickness = new Thickness(2);
        };
        
        // Hide border when mouse leaves and not focused
        container.MouseLeave += (s, e) =>
        {
            if (!container.IsKeyboardFocusWithin)
            {
                container.BorderBrush = Brushes.Transparent;
                container.BorderThickness = new Thickness(0);
            }
        };
        
        container.GotFocus += (s, e) =>
        {
            container.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            container.BorderThickness = new Thickness(2);
        };
        
        container.LostFocus += (s, e) =>
        {
            container.BorderBrush = Brushes.Transparent;
            container.BorderThickness = new Thickness(0);
        };

        // Position the image
        InkCanvas.SetLeft(container, imgAnnotation.X);
        InkCanvas.SetTop(container, imgAnnotation.Y);

        PdfInkCanvas.Children.Add(container);
        _imageElements[imgAnnotation.Id] = container;
    }

    private void OnDeleteImageClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string imageId)
        {
            ViewModel.DeleteImage(imageId);
            RefreshImagesDisplay();
        }
    }

    private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag && tag.StartsWith("IMAGE:"))
        {
            _draggingImageId = tag.Substring(6);
            _dragStartPoint = e.GetPosition(PdfInkCanvas);
            border.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnImageMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && _draggingImageId != null)
        {
            border.ReleaseMouseCapture();
            _draggingImageId = null;
            e.Handled = true;
        }
    }

    private void OnImageMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingImageId == null || e.LeftButton != MouseButtonState.Pressed) return;

        if (sender is Border border && _imageElements.TryGetValue(_draggingImageId, out var element))
        {
            var currentPoint = e.GetPosition(PdfInkCanvas);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            var newX = InkCanvas.GetLeft(element) + deltaX;
            var newY = InkCanvas.GetTop(element) + deltaY;

            // Get image dimensions from the annotation
            var imageAnnotation = ViewModel.CurrentPageImages.FirstOrDefault(i => i.Id == _draggingImageId);
            var elementWidth = imageAnnotation?.Width ?? 100;
            var elementHeight = imageAnnotation?.Height ?? 100;

            // Clamp to canvas bounds
            newX = Math.Max(0, Math.Min(newX, ViewModel.CanvasWidth - elementWidth));
            newY = Math.Max(0, Math.Min(newY, ViewModel.CanvasHeight - elementHeight));

            InkCanvas.SetLeft(element, newX);
            InkCanvas.SetTop(element, newY);

            ViewModel.UpdateImagePosition(_draggingImageId, newX, newY);

            _dragStartPoint = currentPoint;
            e.Handled = true;
        }
    }

    private void OnResizeHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border handle && handle.Tag is string tag && tag.StartsWith("RESIZE:"))
        {
            _resizingImageId = tag.Substring(7);
            _isResizing = true;
            _dragStartPoint = e.GetPosition(PdfInkCanvas);
            handle.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnResizeHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border handle && _isResizing)
        {
            handle.ReleaseMouseCapture();
            _isResizing = false;
            _resizingImageId = null;
            e.Handled = true;
        }
    }

    private void OnResizeHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isResizing || _resizingImageId == null || e.LeftButton != MouseButtonState.Pressed) return;

        if (_imageElements.TryGetValue(_resizingImageId, out var element) && element.Child is Grid grid)
        {
            var currentPoint = e.GetPosition(PdfInkCanvas);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            // Find the image inside the grid
            var image = grid.Children.OfType<System.Windows.Controls.Image>().FirstOrDefault();
            if (image != null)
            {
                var newWidth = Math.Max(50, image.Width + deltaX);
                var newHeight = Math.Max(50, image.Height + deltaY);

                image.Width = newWidth;
                image.Height = newHeight;

                ViewModel.UpdateImageSize(_resizingImageId, newWidth, newHeight);
            }

            _dragStartPoint = currentPoint;
            e.Handled = true;
        }
    }

    #endregion

    #region Text Box Handling

    private readonly Dictionary<string, Border> _textBoxElements = new();
    private string? _draggingTextBoxId;
    private bool _isResizingTextBox;
    private string? _resizingTextBoxId;

    private void RefreshTextBoxesDisplay()
    {
        // Clear existing text box elements from InkCanvas
        var elementsToRemove = PdfInkCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is string tag && tag.StartsWith("TEXTBOX:"))
            .ToList();
        
        foreach (var element in elementsToRemove)
        {
            PdfInkCanvas.Children.Remove(element);
        }
        _textBoxElements.Clear();

        // Add text boxes for current page
        foreach (var textBoxAnnotation in ViewModel.CurrentPageTextBoxes)
        {
            AddTextBoxToCanvas(textBoxAnnotation);
        }
    }

    private void AddTextBoxToCanvas(PdfEditorViewModel.TextBoxAnnotation textBoxAnnotation)
    {
        // Declare TextBox FIRST so it can be referenced in toolbar button handlers
        TextBox textBox = null!;
        TextBlock placeholderText = null!;
        Border toolbarBorder = null!;
        
        // Main container - Using Canvas for absolute positioning to prevent toolbar from affecting textbox position
        var mainContainer = new Canvas();

        // --- TextBox and Placeholder (at fixed position) ---
        // Calculate single-line height based on font size
        var singleLineHeight = Math.Ceiling(textBoxAnnotation.FontSize * 0.75);
        
        // TextBox for text editing with transparent background
        textBox = new TextBox
        {
            Text = textBoxAnnotation.Text,
            Width = Math.Max(150, textBoxAnnotation.Width),
            Height = string.IsNullOrEmpty(textBoxAnnotation.Text) ? singleLineHeight : Math.Max(singleLineHeight, textBoxAnnotation.Height),
            FontFamily = new FontFamily(textBoxAnnotation.FontFamily),
            FontSize = textBoxAnnotation.FontSize,
            Foreground = new SolidColorBrush(textBoxAnnotation.TextColor),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Padding = new Thickness(4, 2, 4, 2),
            Tag = textBoxAnnotation.Id,
            VerticalContentAlignment = VerticalAlignment.Top
        };

        // Placeholder text support
        placeholderText = new TextBlock
        {
            Text = "Start typing here...",
            FontSize = textBoxAnnotation.FontSize,
            FontFamily = new FontFamily(textBoxAnnotation.FontFamily),
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            IsHitTestVisible = false,
            Padding = new Thickness(4, 2, 4, 2),
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = string.IsNullOrEmpty(textBoxAnnotation.Text) ? Visibility.Visible : Visibility.Collapsed
        };

        // Update placeholder visibility on text change
        textBox.TextChanged += (s, e) =>
        {
            placeholderText.Visibility = string.IsNullOrEmpty(textBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            textBoxAnnotation.Text = textBox.Text;
            ViewModel.UpdateTextBoxContent(textBoxAnnotation.Id, textBox.Text);
        };

        // Click placeholder to focus TextBox
        placeholderText.MouseLeftButtonDown += (s, e) =>
        {
            textBox.Focus();
            e.Handled = true;
        };

        // --- Toolbar ---
        toolbarBorder = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(4),
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.3 },
            Padding = new Thickness(6, 4, 6, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Visible, // Show by default on drop
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            BorderThickness = new Thickness(1)
        };
        
        var toolbarStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };

        // Helper to create toolbar buttons
        Button CreateToolbarButton(object content, string tooltip)
        {
            return new Button
            {
                Content = content,
                Width = 30, Height = 30,
                Margin = new Thickness(2, 0, 2, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ToolTip = tooltip,
                Cursor = Cursors.Hand,
                Focusable = false,
                Style = null
            };
        }
        
        // 1. Color Picker Button - Vertical color circles like in image
        var colorPickerBtn = CreateToolbarButton(new Viewbox 
        { 
            Width = 20, 
            Height = 20,
            Child = new Canvas
            {
                Width = 24,
                Height = 24,
                Children =
                {
                    new Ellipse { Width = 8, Height = 8, Fill = Brushes.Red, Margin = new Thickness(8, 0, 0, 0) },
                    new Ellipse { Width = 8, Height = 8, Fill = Brushes.Green, Margin = new Thickness(8, 5, 0, 0) },
                    new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(textBoxAnnotation.TextColor), Margin = new Thickness(8, 10, 0, 0) },
                    new Ellipse { Width = 8, Height = 8, Fill = Brushes.Black, Margin = new Thickness(8, 15, 0, 0) }
                }
            }
        }, "Text Color");
        
        colorPickerBtn.Click += (s, e) => 
        {
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = colorPickerBtn,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                IsOpen = true,
                StaysOpen = false,
                AllowsTransparency = true
            };
            
            // Vertical stack of color circles like the image
            var colorStack = new StackPanel 
            { 
                Background = Brushes.White, 
                Margin = new Thickness(8)
            };
            
            var colors = new[] 
            { 
                Colors.Red, 
                Colors.Green, 
                (Color)ColorConverter.ConvertFromString("#0078D4"), // Blue from image
                Colors.Black 
            };
            
            foreach(var c in colors)
            {
                var colorEllipse = new Ellipse 
                { 
                    Width = 32, 
                    Height = 32, 
                    Fill = new SolidColorBrush(c), 
                    Stroke = c == textBoxAnnotation.TextColor ? new SolidColorBrush(Color.FromRgb(0, 120, 215)) : Brushes.Transparent,
                    StrokeThickness = 3,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 3, 0, 3) // Spacing between circles
                };
                
                colorEllipse.MouseLeftButtonDown += (sender, args) => 
                {
                    textBoxAnnotation.TextColor = c;
                    textBox.Foreground = new SolidColorBrush(c);
                    ViewModel.SaveCurrentPageTextBoxes();
                    popup.IsOpen = false;
                };
                
                colorStack.Children.Add(colorEllipse);
            }
            
            popup.Child = new Border 
            { 
                Child = colorStack, 
                BorderThickness = new Thickness(1), 
                BorderBrush = new SolidColorBrush(Color.FromRgb(200,200,200)), 
                Background = Brushes.White, 
                CornerRadius = new CornerRadius(4),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, Opacity = 0.3, ShadowDepth = 2 }
            };
        };

        // 2. Increase Font Size
        var incFontBtn = CreateToolbarButton(new TextBlock 
        { 
            Text = "A+", 
            FontSize = 13, 
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center, 
            HorizontalAlignment = HorizontalAlignment.Center 
        }, "Increase Font Size");
        incFontBtn.Click += (s, e) => 
        {
            textBoxAnnotation.FontSize = Math.Min(textBoxAnnotation.FontSize + 2, 72);
            textBox.FontSize = textBoxAnnotation.FontSize;
            placeholderText.FontSize = textBoxAnnotation.FontSize;
            ViewModel.SaveCurrentPageTextBoxes();
            e.Handled = true;
        };

        // 3. Decrease Font Size
        var decFontBtn = CreateToolbarButton(new TextBlock 
        { 
            Text = "A-", 
            FontSize = 13, 
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center, 
            HorizontalAlignment = HorizontalAlignment.Center 
        }, "Decrease Font Size");
        decFontBtn.Click += (s, e) => 
        {
            textBoxAnnotation.FontSize = Math.Max(textBoxAnnotation.FontSize - 2, 8);
            textBox.FontSize = textBoxAnnotation.FontSize;
            placeholderText.FontSize = textBoxAnnotation.FontSize;
            ViewModel.SaveCurrentPageTextBoxes();
            e.Handled = true;
        };

        // 4. Delete Button
        var trashPath = new System.Windows.Shapes.Path 
        { 
            Data = Geometry.Parse("M9,3V4H4V6H5V19A2,2 0 0,0 7,21H17A2,2 0 0,0 19,19V6H20V4H15V3H9M7,6H17V19H7V6M9,8V17H11V8H9M13,8V17H15V8H13Z"),
            Fill = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Width = 16, Height = 16,
            Stretch = Stretch.Uniform
        };
        var delBtn = CreateToolbarButton(trashPath, "Delete Text Box");
        delBtn.Click += (s, e) => 
        {
            ViewModel.DeleteTextBox(textBoxAnnotation.Id);
            RefreshTextBoxesDisplay();
        };

        toolbarStack.Children.Add(colorPickerBtn);
        var separator1 = new Rectangle { Width = 1, Height = 20, Fill = new SolidColorBrush(Color.FromRgb(220, 220, 220)), Margin = new Thickness(4, 0, 4, 0) };
        toolbarStack.Children.Add(separator1);
        toolbarStack.Children.Add(incFontBtn);
        toolbarStack.Children.Add(decFontBtn);
        var separator2 = new Rectangle { Width = 1, Height = 20, Fill = new SolidColorBrush(Color.FromRgb(220, 220, 220)), Margin = new Thickness(4, 0, 4, 0) };
        toolbarStack.Children.Add(separator2);
        toolbarStack.Children.Add(delBtn);
        toolbarBorder.Child = toolbarStack;

        // Position toolbar at top-left (0, -40) so it floats above without affecting textbox position
        Canvas.SetLeft(toolbarBorder, 0);
        Canvas.SetTop(toolbarBorder, -40);
        mainContainer.Children.Add(toolbarBorder);

        // --- Drag Handle (Left side of textbox) ---
        var dragHandle = new Border
        {
            Width = 20,
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)), // Blue
            CornerRadius = new CornerRadius(4, 0, 0, 4),
            Cursor = Cursors.SizeAll,
            Visibility = Visibility.Visible, // Show by default
            Tag = $"DRAG_TB:{textBoxAnnotation.Id}",
            Height = textBox.Height
        };
        var dotsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        for(int i=0; i<4; i++) 
        {
            dotsStack.Children.Add(new Ellipse { Width=4, Height=4, Fill=Brushes.White, Margin=new Thickness(0,2,0,2) });
        }
        dragHandle.Child = dotsStack;
        
        dragHandle.MouseLeftButtonDown += OnTextBoxDragHandleMouseDown;
        dragHandle.MouseLeftButtonUp += OnTextBoxDragHandleMouseUp;
        dragHandle.MouseMove += OnTextBoxDragHandleMouseMove;

        // Position drag handle at left edge
        Canvas.SetLeft(dragHandle, -20);
        Canvas.SetTop(dragHandle, 0);
        mainContainer.Children.Add(dragHandle);
        
        // Dashed Border using Rectangle
        var borderRect = new Rectangle
        {
            Width = textBox.Width,
            Height = textBox.Height,
            Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 }, // Dashed pattern
            RadiusX = 4, RadiusY = 4,
            IsHitTestVisible = false // Let clicks pass through to TextBox
        };

        // Position border at same location as textbox
        Canvas.SetLeft(borderRect, 0);
        Canvas.SetTop(borderRect, 0);
        mainContainer.Children.Add(borderRect);

        // Position TextBox at (0, 0) in canvas
        Canvas.SetLeft(textBox, 0);
        Canvas.SetTop(textBox, 0);
        mainContainer.Children.Add(textBox);

        // Position placeholder at same location
        Canvas.SetLeft(placeholderText, 0);
        Canvas.SetTop(placeholderText, 0);
        mainContainer.Children.Add(placeholderText);

        // Resize Handle (Right-bottom corner)
        var resizeHandle = new Ellipse
        {
            Width = 10, Height = 10,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            StrokeThickness = 2,
            Cursor = Cursors.SizeNWSE,
            Visibility = Visibility.Visible, // Show by default
            Tag = $"RESIZE_TB:{textBoxAnnotation.Id}"
        };
        
        resizeHandle.MouseLeftButtonDown += OnTextBoxResizeHandleMouseDown;
        resizeHandle.MouseLeftButtonUp += OnTextBoxResizeHandleMouseUp;
        resizeHandle.MouseMove += OnTextBoxResizeHandleMouseMove;

        // Position resize handle at bottom-right corner
        Canvas.SetLeft(resizeHandle, textBox.Width - 5);
        Canvas.SetTop(resizeHandle, textBox.Height - 5);
        mainContainer.Children.Add(resizeHandle);

        // Update border and resize handle when textbox size changes
        textBox.SizeChanged += (s, e) =>
        {
            borderRect.Width = textBox.Width;
            borderRect.Height = textBox.Height;
            dragHandle.Height = textBox.Height;
            Canvas.SetLeft(resizeHandle, textBox.Width - 5);
            Canvas.SetTop(resizeHandle, textBox.Height - 5);
        };

        // Focus Logic - only toggle visibility, don't affect layout
        textBox.GotFocus += (s, e) =>
        {
            toolbarBorder.Visibility = Visibility.Visible;
            dragHandle.Visibility = Visibility.Visible;
            resizeHandle.Visibility = Visibility.Visible;
            borderRect.Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Show border
        };
        
        textBox.LostFocus += (s, e) =>
        {
             toolbarBorder.Visibility = Visibility.Collapsed;
             dragHandle.Visibility = Visibility.Collapsed;
             resizeHandle.Visibility = Visibility.Collapsed;
             borderRect.Stroke = Brushes.Transparent; // Hide border
        };

        // Wrap in Border for _textBoxElements
        var containerBorder = new Border
        {
            Background = Brushes.Transparent,
            Child = mainContainer,
            Tag = $"TEXTBOX:{textBoxAnnotation.Id}"
        };

        InkCanvas.SetLeft(containerBorder, textBoxAnnotation.X);
        InkCanvas.SetTop(containerBorder, textBoxAnnotation.Y);

        PdfInkCanvas.Children.Add(containerBorder);
        _textBoxElements[textBoxAnnotation.Id] = containerBorder;
    }


    private void OnDeleteTextBoxClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string textBoxId)
        {
            ViewModel.DeleteTextBox(textBoxId);
            RefreshTextBoxesDisplay();
        }
    }

    private void OnTextBoxDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border handle && handle.Tag is string tag && tag.StartsWith("DRAG_TB:"))
        {
            _draggingTextBoxId = tag.Substring(8);
            _dragStartPoint = e.GetPosition(PdfInkCanvas);
            handle.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnTextBoxDragHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border handle && _draggingTextBoxId != null)
        {
            handle.ReleaseMouseCapture();
            _draggingTextBoxId = null;
            e.Handled = true;
        }
    }

    private void OnTextBoxDragHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingTextBoxId == null || e.LeftButton != MouseButtonState.Pressed) return;

        if (_textBoxElements.TryGetValue(_draggingTextBoxId, out var element))
        {
            var currentPoint = e.GetPosition(PdfInkCanvas);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            var newX = InkCanvas.GetLeft(element) + deltaX;
            var newY = InkCanvas.GetTop(element) + deltaY;

            // Get text box dimensions
            var textBoxAnnotation = ViewModel.CurrentPageTextBoxes.FirstOrDefault(t => t.Id == _draggingTextBoxId);
            var elementWidth = textBoxAnnotation?.Width ?? 200;
            var elementHeight = textBoxAnnotation?.Height ?? 50;

            // Clamp to canvas bounds
            newX = Math.Max(0, Math.Min(newX, ViewModel.CanvasWidth - elementWidth));
            newY = Math.Max(0, Math.Min(newY, ViewModel.CanvasHeight - elementHeight));

            InkCanvas.SetLeft(element, newX);
            InkCanvas.SetTop(element, newY);

            ViewModel.UpdateTextBoxPosition(_draggingTextBoxId, newX, newY);

            _dragStartPoint = currentPoint;
            e.Handled = true;
        }
    }

    private void OnTextBoxResizeHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement handle && handle.Tag is string tag && tag.StartsWith("RESIZE_TB:"))
        {
            _resizingTextBoxId = tag.Substring(10);
            _isResizingTextBox = true;
            _dragStartPoint = e.GetPosition(PdfInkCanvas);
            handle.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnTextBoxResizeHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement handle && _isResizingTextBox)
        {
            handle.ReleaseMouseCapture();
            _isResizingTextBox = false;
            _resizingTextBoxId = null;
            e.Handled = true;
        }
    }

    private void OnTextBoxResizeHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isResizingTextBox || _resizingTextBoxId == null || e.LeftButton != MouseButtonState.Pressed) return;

        if (_textBoxElements.TryGetValue(_resizingTextBoxId, out var element))
        {
            var currentPoint = e.GetPosition(PdfInkCanvas);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            // Find the TextBox inside the Canvas
            TextBox? textBox = null;
            if (element.Child is Canvas mainContainer)
            {
                textBox = mainContainer.Children.OfType<TextBox>().FirstOrDefault();
            }

            if (textBox != null)
            {
                var newWidth = Math.Max(MinTextBoxWidth, textBox.Width + deltaX);
                var newHeight = Math.Max(MinTextBoxHeight, textBox.Height + deltaY);

                textBox.Width = newWidth;
                textBox.Height = newHeight;

                ViewModel.UpdateTextBoxSize(_resizingTextBoxId, newWidth, newHeight);
            }

            _dragStartPoint = currentPoint;
            e.Handled = true;
        }
    }

    #endregion

    #region File Drop Handlers

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // Get the first PDF file
                var pdfFile = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                if (pdfFile != null && ViewModel.OpenPdfCommand.CanExecute(pdfFile))
                {
                    ViewModel.OpenPdfCommand.Execute(pdfFile);
                }
            }
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    #endregion
}
