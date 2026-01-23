# Windows Startup & System Tray Integration

PDF Kawankasi now includes Windows startup and system tray integration for quick access and background operation.

## Features

### 🚀 Windows Startup
- **Auto-start with Windows**: Configure the app to launch automatically when Windows starts
- **Minimized startup**: When enabled, the app starts minimized to the system tray for fast background loading
- **Registry-based**: Uses Windows Registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run) for reliable startup

### 📍 System Tray Integration
- **System tray icon**: Always accessible from the Windows notification area
- **Quick access**: Double-click the tray icon to restore the window
- **Context menu**: Right-click for quick actions:
  - Open PDF Kawankasi
  - Toggle "Run at Windows Startup"
  - Exit application

### ⚡ Smart Minimize Behavior
- **Minimize to tray**: Clicking the minimize button hides the app to the system tray instead of the taskbar
- **Close to tray**: Closing the window minimizes to tray instead of exiting
- **Force exit**: Hold **Shift** while closing to force exit the application

## How to Use

### Enable Windows Startup
1. Right-click the system tray icon
2. Check "Run at Windows Startup"
3. The app will now start automatically with Windows in minimized mode

### Disable Windows Startup
1. Right-click the system tray icon
2. Uncheck "Run at Windows Startup"

### Restore Window from Tray
- **Method 1**: Double-click the system tray icon
- **Method 2**: Right-click the icon → "Open PDF Kawankasi"

### Force Exit Application
- **Method 1**: Right-click system tray icon → "Exit"
- **Method 2**: Hold **Shift** + click window close button (X)

## Command-Line Arguments

### `--minimized`
Starts the application minimized to the system tray
```cmd
PDFKawankasi.exe --minimized
```

This argument is automatically added by the startup manager when Windows startup is enabled.

## Technical Implementation

### Components

1. **StartupManager.cs** (`Services/StartupManager.cs`)
   - Manages Windows Registry entries for startup
   - Methods: `IsStartupEnabled()`, `SetStartupEnabled(bool)`, `ToggleStartup()`

2. **SystemTrayService.cs** (`Services/SystemTrayService.cs`)
   - Manages system tray icon and menu
   - Handles window show/hide operations
   - Shows balloon notifications

3. **App.xaml.cs** (Modified)
   - Detects `--minimized` argument
   - Initializes SystemTrayService
   - Manages application lifecycle

4. **MainWindow.xaml.cs** (Modified)
   - Implements minimize-to-tray behavior
   - Handles startup minimized state
   - Intercepts window closing to hide to tray

### NuGet Dependencies
- **H.NotifyIcon.Wpf** (v2.1.4): Modern WPF system tray icon implementation

## Benefits

### For Users
- **Faster access**: Pre-loaded app launches instantly when needed
- **Reduced clutter**: Minimizes to tray instead of taskbar
- **Convenience**: Always available without taking up taskbar space

### For Performance
- **Preloading**: App assemblies and resources are already in memory
- **Quick activation**: Opening PDFs is nearly instant
- **Low overhead**: Minimal resource usage when minimized

## Privacy & Security

- ✅ **No background tracking**: App runs silently without data collection
- ✅ **Local only**: All operations are performed locally on your device
- ✅ **User controlled**: Easy to disable startup or exit completely
- ✅ **Transparent**: Open-source implementation, no hidden behavior

## Troubleshooting

### App doesn't start with Windows
- Check Windows Task Manager → Startup tab
- Verify registry entry exists: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\PDFKawankasi`
- Try toggling the setting off and on again

### System tray icon doesn't appear
- Check Windows Settings → Personalization → Taskbar → System tray
- Ensure "PDF Kawankasi" is set to show icon
- Restart the application

### Can't force exit the application
- Use the system tray context menu → "Exit"
- Or use Task Manager to end the process

## Future Enhancements

Potential improvements for future versions:
- [ ] Settings UI for startup configuration
- [ ] Custom tray icon tooltips showing recent files
- [ ] Quick actions from tray menu (Open Recent, etc.)
- [ ] Notification settings customization
- [ ] Hot key for global PDF opening

---

**Note**: This feature requires Windows 10 version 1809 or later.
