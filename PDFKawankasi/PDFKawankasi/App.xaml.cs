using System.Windows;
using System.Windows.Input;

namespace PDFKawankasi;

/// <summary>
/// PDF Kawankasi - A powerful, privacy-first PDF toolkit for Windows
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Check for command-line arguments
        if (e.Args.Length > 0)
        {
            // Convert logo: --convert-logo
            if (e.Args[0] == "--convert-logo")
            {
                ConvertSvgToPng.ConvertLogo();
                CreateIcoFromPng.CreateIco();
                Shutdown();
                return;
            }
            
            // Test SVG: --test-svg
            if (e.Args[0] == "--test-svg")
            {
                var testWindow = new SvgTestWindow();
                testWindow.Show();
                return;
            }
        }
        
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
        
        // Add global keyboard shortcut: Ctrl+Shift+T to open SVG test window
        EventManager.RegisterClassHandler(typeof(Window),
            Keyboard.KeyDownEvent, new KeyEventHandler(OnGlobalKeyDown), true);
    }
    
    private void OnGlobalKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Shift+T = Open SVG Test Window
        if (e.Key == Key.T && 
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
            (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            var testWindow = new SvgTestWindow();
            testWindow.Show();
            e.Handled = true;
        }
    }
}
