using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PDFKawankasi.Models;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

/// <summary>
/// Interaction logic for PdfEditorView.xaml with Windows Inking API integration
/// </summary>
public partial class PdfEditorView : UserControl
{
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
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfEditorViewModel.CurrentPageStrokes) && PdfInkCanvas != null)
        {
            // Sync InkCanvas strokes with ViewModel
            PdfInkCanvas.Strokes = ViewModel.CurrentPageStrokes;
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
            // Save current page strokes before switching
            if (PdfInkCanvas != null)
            {
                ViewModel.SaveCurrentPageStrokes(PdfInkCanvas.Strokes);
            }

            ViewModel.GoToPage(thumbnail.PageNumber);

            // Load new page strokes
            if (PdfInkCanvas != null)
            {
                PdfInkCanvas.Strokes = ViewModel.CurrentPageStrokes;
            }
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
        
        // Store metadata for the stroke (page number, timestamp, etc.)
        stroke.AddPropertyData(Guid.NewGuid(), ViewModel.CurrentPage);
        
        ViewModel.StatusMessage = $"Ink stroke added on page {ViewModel.CurrentPage}";
    }

    private void OnInkStrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
    {
        ViewModel.StatusMessage = "Erasing stroke...";
    }

    private void OnInkCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.IsPdfLoaded) return;

        // Handle non-ink tools (text, comment, shape)
        if (ViewModel.IsTextToolActive || ViewModel.IsCommentToolActive || ViewModel.IsShapeToolActive)
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
}
