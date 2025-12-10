using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32;
using PDFKawankasi.Models;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

/// <summary>
/// Main window for PDF Kawankasi application
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private int _tabCounter = 1;
    private TabItem? _draggedTab;
    private Point _dragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
        
        // Set up keyboard shortcuts
        KeyDown += MainWindow_KeyDown;
        
        // Update maximize/restore button on state changed
        StateChanged += MainWindow_StateChanged;
        UpdateMaximizeRestoreButton();
        
        // Handle PDF files passed via file association
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Check if PDF files were passed via command-line (file association)
        if (Application.Current.Properties["PdfFilesToOpen"] is List<string> pdfFiles && pdfFiles.Any())
        {
            // Open each PDF file in a new tab
            foreach (var pdfFile in pdfFiles)
            {
                OpenPdfInNewTab(pdfFile);
            }
            
            // Clear the property so files aren't opened again
            Application.Current.Properties.Remove("PdfFilesToOpen");
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (WindowState == WindowState.Maximized)
        {
            MaximizeRestoreButton.Content = "\uE923"; // Restore icon
        }
        else
        {
            MaximizeRestoreButton.Content = "\uE922"; // Maximize icon
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void RegisterAsDefaultPdfViewer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the app's Application User Model ID from Package.appxmanifest
            // For packaged apps, use registeredAUMID parameter
            string appAumid = "PDFKawankasi_8wekyb3d8bbwe!App"; // Format: PackageFamilyName!AppId
            
            // Launch Windows Settings directly to the default apps page for this app
            string settingsUri = $"ms-settings:defaultapps?registeredAUMID={Uri.EscapeDataString(appAumid)}";
            
            // Use Process.Start for WPF apps (Windows.System.Launcher is for UWP)
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = settingsUri,
                UseShellExecute = true
            };
            
            System.Diagnostics.Process.Start(psi);
            
            MessageBox.Show(
                "Windows Settings will open where you can set PDF Kawankasi as the default PDF viewer.\n\n" +
                "Look for '.pdf' in the file type list and select PDF Kawankasi.",
                "Register as Default PDF Viewer",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open Settings: {ex.Message}\n\n" +
                "You can manually set PDF Kawankasi as default by going to:\n" +
                "Settings > Apps > Default apps > PDF Kawankasi",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Escape to go back
        if (e.Key == Key.Escape)
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
        
        // Ctrl+T to open new tab
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (ViewModel.SelectedTool?.ToolType == ToolType.PdfEditor)
            {
                AddNewPdfTab();
                e.Handled = true;
            }
        }
        
        // Ctrl+W to close current tab
        if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (ViewModel.SelectedTool?.ToolType == ToolType.PdfEditor && PdfTabControl.Items.Count > 1)
            {
                var currentTab = PdfTabControl.SelectedItem as TabItem;
                if (currentTab != null)
                {
                    CloseTab(currentTab);
                }
                e.Handled = true;
            }
        }
    }

    private void OnBrowseFilesClick(object sender, RoutedEventArgs e)
    {
        // For Split PDF, only allow single file selection
        var isSplitTool = ViewModel.SelectedTool?.ToolType == ToolType.Split;
        
        var dialog = new OpenFileDialog
        {
            Multiselect = !isSplitTool,
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

    private void OnPagePreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PagePreviewModel pagePreview)
        {
            ViewModel.TogglePageSelectionCommand.Execute(pagePreview);
        }
    }

    private void OnTabCloseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TabItem tabItem)
        {
            CloseTab(tabItem);
        }
    }

    private void OnNewTabClick(object sender, RoutedEventArgs e)
    {
        AddNewPdfTab();
    }

    private void CloseTab(TabItem tabItem)
    {
        // Don't close if it's the last tab
        if (PdfTabControl.Items.Count <= 1)
            return;

        // Check for unsaved changes
        if (tabItem.Content is PdfEditorView editorView && 
            editorView.DataContext is PdfEditorViewModel viewModel &&
            viewModel.HasPendingChanges)
        {
            var result = MessageBox.Show(
                "This document has unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                // Try to save
                if (viewModel.SavePdfCommand.CanExecute(null))
                {
                    viewModel.SavePdfCommand.Execute(null);
                }
            }
        }

        int index = PdfTabControl.Items.IndexOf(tabItem);
        PdfTabControl.Items.Remove(tabItem);

        // Select adjacent tab
        if (PdfTabControl.Items.Count > 0)
        {
            PdfTabControl.SelectedIndex = Math.Min(index, PdfTabControl.Items.Count - 1);
        }
    }

    private void AddNewPdfTab()
    {
        _tabCounter++;
        var newTab = new TabItem
        {
            // Header binding is handled by template binding to Content.DataContext.DocumentTitle
            Content = new PdfEditorView()
        };
        PdfTabControl.Items.Add(newTab);
        PdfTabControl.SelectedItem = newTab;
    }

    private void OpenPdfInNewTab(string pdfFilePath)
    {
        // Create a new tab with PdfEditorView
        _tabCounter++;
        var pdfEditorView = new PdfEditorView();
        var newTab = new TabItem
        {
            Content = pdfEditorView
        };
        
        PdfTabControl.Items.Add(newTab);
        PdfTabControl.SelectedItem = newTab;
        
        // Open the PDF file in the new tab after it's loaded
        pdfEditorView.Loaded += OnPdfEditorViewLoadedForFile;
        
        // Store the file path for the Loaded event handler
        pdfEditorView.Tag = pdfFilePath;
    }

    private void OnPdfEditorViewLoadedForFile(object sender, RoutedEventArgs e)
    {
        if (sender is not PdfEditorView editorView) return;
        
        // Remove the event handler to prevent multiple invocations
        editorView.Loaded -= OnPdfEditorViewLoadedForFile;
        
        // Get the file path from Tag
        if (editorView.Tag is not string pdfFilePath) return;
        
        // Get the ViewModel and open the file
        if (editorView.DataContext is PDFKawankasi.ViewModels.PdfEditorViewModel viewModel)
        {
            if (viewModel.OpenPdfCommand.CanExecute(pdfFilePath))
            {
                viewModel.OpenPdfCommand.Execute(pdfFilePath);
            }
        }
        
        // Clear the Tag
        editorView.Tag = null;
    }

    private void OnTabMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border)
        {
            _dragStartPoint = e.GetPosition(border);
        }
    }

    private void OnTabMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is Border border)
        {
            var tabItem = FindParent<TabItem>(border);
            if (tabItem != null && _draggedTab == null)
            {
                var currentPosition = e.GetPosition(border);
                if (Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _draggedTab = tabItem;
                    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
                    _draggedTab = null;
                }
            }
        }
    }

    private void OnTabDrop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            var targetTab = FindParent<TabItem>(border);
            if (targetTab != null && e.Data.GetData(typeof(TabItem)) is TabItem sourceTab && sourceTab != targetTab)
            {
                int sourceIndex = PdfTabControl.Items.IndexOf(sourceTab);
                int targetIndex = PdfTabControl.Items.IndexOf(targetTab);

                if (sourceIndex != -1 && targetIndex != -1)
                {
                    PdfTabControl.Items.RemoveAt(sourceIndex);
                    PdfTabControl.Items.Insert(targetIndex, sourceTab);
                    PdfTabControl.SelectedItem = sourceTab;
                }
            }
            border.Background = System.Windows.Media.Brushes.Transparent;
        }
    }

    private void OnTabDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border && e.Data.GetDataPresent(typeof(TabItem)))
        {
            border.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(30, 100, 149, 237));
        }
    }

    private void OnTabDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = System.Windows.Media.Brushes.Transparent;
        }
    }

    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parent == null) return null;
        if (parent is T typedParent) return typedParent;
        return FindParent<T>(parent);
    }

    private string GetFileFilter()
    {
        if (ViewModel.SelectedTool == null)
            return "All Files (*.*)|*.*";

        return ViewModel.SelectedTool.ToolType switch
        {
            ToolType.JpgToPdf => "JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            ToolType.PngToPdf => "PNG Images (*.png)|*.png",
            ToolType.ImageToPdf => "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff",
            ToolType.TextToPdf => "Text Files (*.txt)|*.txt",
            _ => "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*"
        };
    }
}
