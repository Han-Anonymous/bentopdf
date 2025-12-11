using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using System.Text;

namespace PDFKawankasi;

/// <summary>
/// PDF Kawankasi - A powerful, privacy-first PDF toolkit for Windows
/// </summary>
public partial class App : Application
{
    // File extension constant for PDF files
    private const string PdfExtension = ".pdf";
    
    // Single instance mechanism
    private const string MutexName = "PDFKawankasi_SingleInstance_Mutex";
    private const string PipeName = "PDFKawankasi_IPC_Pipe";
    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Try to create or open the mutex
        bool createdNew;
        _instanceMutex = new Mutex(true, MutexName, out createdNew);
        
        if (!createdNew)
        {
            // Another instance is already running
            // Send PDF files to the existing instance via named pipe
            var pdfFiles = e.Args.Where(arg => 
                !arg.StartsWith("--") && 
                System.IO.File.Exists(arg) && 
                arg.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (pdfFiles.Any())
            {
                SendFilesToExistingInstance(pdfFiles);
            }
            
            // Shutdown this instance
            Shutdown();
            return;
        }
        
        // This is the first instance, continue with normal startup
        
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
            
            // PDF file passed as command-line argument (file association)
            // Filter to only process PDF files
            var pdfFiles = e.Args.Where(arg => 
                !arg.StartsWith("--") && 
                System.IO.File.Exists(arg) && 
                arg.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (pdfFiles.Any())
            {
                // Store PDF files to open after MainWindow is initialized
                Current.Properties["PdfFilesToOpen"] = pdfFiles;
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

    private void SendFilesToExistingInstance(List<string> pdfFiles)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipeClient.Connect(5000); // 5 second timeout
            
            using var writer = new StreamWriter(pipeClient, Encoding.UTF8);
            foreach (var file in pdfFiles)
            {
                writer.WriteLine(file);
            }
            writer.Flush();
        }
        catch
        {
            // If we can't send to the existing instance, fail silently
            // The mutex holder might have crashed or be shutting down
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
