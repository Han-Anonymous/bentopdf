using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PDFKawankasi.Models;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

/// <summary>
/// Interaction logic for PdfEditorView.xaml
/// </summary>
public partial class PdfEditorView : UserControl
{
    private PdfEditorViewModel ViewModel => (PdfEditorViewModel)DataContext;
    private bool _isDrawing;
    private Point _startPoint;

    public PdfEditorView()
    {
        InitializeComponent();
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
            ViewModel.GoToPage(thumbnail.PageNumber);
        }
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.IsPdfLoaded) return;

        _startPoint = e.GetPosition(PdfCanvas);
        _isDrawing = true;

        ViewModel.StartAnnotation(_startPoint);
        PdfCanvas.CaptureMouse();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || !ViewModel.IsPdfLoaded) return;

        var currentPoint = e.GetPosition(PdfCanvas);
        ViewModel.UpdateAnnotation(currentPoint);
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        var endPoint = e.GetPosition(PdfCanvas);
        ViewModel.FinishAnnotation(endPoint);
        PdfCanvas.ReleaseMouseCapture();
    }

    private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDrawing)
        {
            _isDrawing = false;
            ViewModel.CancelAnnotation();
            PdfCanvas.ReleaseMouseCapture();
        }
    }
}
