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

    public MainWindow()
    {
        InitializeComponent();
        
        // Set up keyboard shortcuts
        KeyDown += MainWindow_KeyDown;
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
            Header = $"Document {_tabCounter}",
            Content = new PdfEditorView()
        };
        PdfTabControl.Items.Add(newTab);
        PdfTabControl.SelectedItem = newTab;
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
