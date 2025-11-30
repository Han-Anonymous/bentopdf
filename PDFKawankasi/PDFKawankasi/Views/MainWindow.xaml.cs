using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

/// <summary>
/// Main window for PDF Kawankasi application
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        
        // Set up keyboard shortcuts
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+K to focus search
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        // Escape to go back
        else if (e.Key == Key.Escape)
        {
            if (ViewModel.IsToolViewVisible)
            {
                ViewModel.BackToGridCommand.Execute(null);
            }
            else
            {
                ViewModel.SearchQuery = string.Empty;
            }
            e.Handled = true;
        }
    }

    private void OnBrowseFilesClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = GetFileFilter()
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.AddFilesCommand.Execute(dialog.FileNames);
        }
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                ViewModel.AddFilesCommand.Execute(files);
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

    private string GetFileFilter()
    {
        if (ViewModel.SelectedTool == null)
            return "All Files (*.*)|*.*";

        return ViewModel.SelectedTool.ToolType switch
        {
            Models.ToolType.JpgToPdf => "JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            Models.ToolType.PngToPdf => "PNG Images (*.png)|*.png",
            Models.ToolType.ImageToPdf => "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff",
            Models.ToolType.TextToPdf => "Text Files (*.txt)|*.txt",
            _ => "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*"
        };
    }
}
