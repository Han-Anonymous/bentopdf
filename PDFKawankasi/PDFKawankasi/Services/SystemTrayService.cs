using H.NotifyIcon;
using System;
using System.Windows;
using System.Windows.Controls;
using PDFKawankasi.Views;

namespace PDFKawankasi.Services;

/// <summary>
/// Service to manage the system tray icon and related functionality
/// </summary>
public class SystemTrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isDisposed;

    /// <summary>
    /// Gets whether the application should run minimized to tray on startup
    /// </summary>
    public bool StartMinimized { get; private set; }

    /// <summary>
    /// Initializes the system tray icon
    /// </summary>
    public void Initialize(MainWindow mainWindow, bool startMinimized = false)
    {
        _mainWindow = mainWindow;
        StartMinimized = startMinimized;

        // Get the TaskbarIcon from App resources
        _trayIcon = Application.Current.FindResource("TrayIcon") as TaskbarIcon;
        
        if (_trayIcon == null)
        {
            throw new InvalidOperationException("TaskbarIcon resource 'TrayIcon' not found in App.xaml");
        }

        // CRITICAL: ForceCreate() is required to make the tray icon visible
        // when defined in App.xaml resources
        _trayIcon.ForceCreate();

        // Create context menu for the tray icon
        var contextMenu = new ContextMenu();

        // Open menu item
        var openItem = new MenuItem { Header = "Open PDF Kawankasi" };
        openItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(openItem);

        // Separator
        contextMenu.Items.Add(new Separator());

        // Run at Startup checkbox
        var startupItem = new MenuItem 
        { 
            Header = "Run at Windows Startup",
            IsCheckable = true,
            IsChecked = StartupManager.IsStartupEnabled()
        };
        startupItem.Click += (s, e) =>
        {
            var enabled = StartupManager.ToggleStartup();
            startupItem.IsChecked = StartupManager.IsStartupEnabled();
            
            if (enabled)
            {
                ShowBalloonTip("Startup Enabled", 
                    "PDF Kawankasi will now start with Windows.");
            }
        };
        contextMenu.Items.Add(startupItem);

        // Separator
        contextMenu.Items.Add(new Separator());

        // Exit menu item
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
        
        // Show balloon tip on first startup if minimized
        if (startMinimized)
        {
            ShowBalloonTip("PDF Kawankasi is running", 
                "The application is running in the background. Double-click the tray icon to open.");
        }
    }

    /// <summary>
    /// Shows the main window and brings it to the foreground
    /// </summary>
    public void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        // Show and restore the window
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        // Bring to foreground
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    /// <summary>
    /// Hides the main window to the system tray
    /// </summary>
    public void HideToTray()
    {
        if (_mainWindow == null) return;
        _mainWindow.Hide();
    }

    /// <summary>
    /// Shows a balloon tip notification
    /// </summary>
    public void ShowBalloonTip(string title, string message)
    {
        if (_trayIcon == null) return;
        
        // Use ShowNotification instead of ShowBalloonTip for H.NotifyIcon 2.x
        _trayIcon.ShowNotification(title, message);
    }

    /// <summary>
    /// Exits the application completely
    /// </summary>
    private void ExitApplication()
    {
        // This will bypass minimize-to-tray behavior
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Disposes of the system tray icon
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        // Don't dispose the TaskbarIcon - it's managed by XAML resources
        _trayIcon = null;
        _isDisposed = true;

        GC.SuppressFinalize(this);
    }
}
