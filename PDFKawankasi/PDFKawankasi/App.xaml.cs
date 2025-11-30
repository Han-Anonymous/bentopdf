using System.Windows;

namespace PDFKawankasi;

/// <summary>
/// PDF Kawankasi - A powerful, privacy-first PDF toolkit for Windows
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set up global exception handling
        DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred: {args.Exception.Message}",
                "PDF Kawankasi - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
