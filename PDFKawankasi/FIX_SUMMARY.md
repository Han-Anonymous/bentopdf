# Fix Summary: System Tray & Windows Startup

## Issues Found

1. **System tray icon not appearing**: H.NotifyIcon.Wpf requires TaskbarIcon to be defined in XAML resources, not created programmatically
2. **App not starting on Windows startup**: Registry path was pointing to .dll instead of .exe for .NET 8 apps

## Fixes Applied

### 1. TaskbarIcon XAML Definition (App.xaml)

**Problem**: Creating TaskbarIcon in code-behind doesn't work reliably with H.NotifyIcon.Wpf 2.x

**Solution**: Defined TaskbarIcon as an application resource in App.xaml

```xml
<Application xmlns:tb="clr-namespace:H.NotifyIcon;assembly=H.NotifyIcon.Wpf">
    <Application.Resources>
        <tb:TaskbarIcon x:Key="TrayIcon"
                        IconSource="/Assets/app-icon.ico"
                        ToolTipText="PDF Kawankasi"
                        MenuActivation="RightClick"
                        TrayMouseDoubleClick="TrayIcon_TrayMouseDoubleClick" />
    </Application.Resources>
</Application>
```

### 2. SystemTrayService Rewrite

**Changes**:
- Retrieve TaskbarIcon from App resources instead of creating new instance
- Remove Dispose() of TaskbarIcon (managed by XAML)
- Context menu still created programmatically for dynamic startup checkbox

```csharp
_trayIcon = Application.Current.FindResource("TrayIcon") as TaskbarIcon;
```

### 3. Event Handler (App.xaml.cs)

Added event handler for tray icon double-click:

```csharp
private void TrayIcon_TrayMouseDoubleClick(object? sender, RoutedEventArgs e)
{
    TrayService?.ShowMainWindow();
}
```

### 4. Executable Path Fix (StartupManager.cs)

**Problem**: .NET 8 apps use `PDFKawankasi.dll` as the assembly location, but we need `PDFKawankasi.exe` for startup

**Solution**: Properly resolve the .exe path

```csharp
var exePath = Assembly.GetExecutingAssembly().Location;

if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
{
    var directory = Path.GetDirectoryName(exePath);
    var exeName = Path.GetFileNameWithoutExtension(exePath) + ".exe";
    exePath = Path.Combine(directory ?? "", exeName);
}

// Verify the .exe exists
if (!File.Exists(exePath))
{
    return false;
}
```

## Testing Instructions

### Run the Test Script

```powershell
cd c:\Users\Acer\source\repos\Han-Anonymous\bentopdf\PDFKawankasi
.\Test-StartupTray.ps1
```

### Manual Testing

1. **Build the app**:
   ```powershell
   dotnet build -c Release
   ```

2. **Run the app**:
   ```powershell
   .\PDFKawankasi\bin\Release\net8.0-windows10.0.19041.0\PDFKawankasi.exe
   ```

3. **Check system tray**:
   - Look in the Windows notification area (bottom-right)
   - You should see the PDF Kawankasi icon
   - Hover over it - tooltip should say "PDF Kawankasi"

4. **Test double-click**:
   - Minimize the app window
   - Double-click the tray icon
   - Window should restore

5. **Test right-click menu**:
   - Right-click the tray icon
   - Menu should show:
     - Open PDF Kawankasi
     - Run at Windows Startup (checkbox)
     - Exit

6. **Enable startup**:
   - Right-click tray icon → Check "Run at Windows Startup"
   - A notification balloon should appear
   - Verify registry entry:
     ```powershell
     Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "PDFKawankasi"
     ```

7. **Test minimized startup**:
   - Close the app (Shift + Close to force exit)
   - Run with --minimized:
     ```powershell
     .\PDFKawankasi\bin\Release\net8.0-windows10.0.19041.0\PDFKawankasi.exe --minimized
     ```
   - App should start hidden in tray
   - Notification balloon should appear
   - Double-click tray icon to show window

## Verification Checklist

- [ ] Build completes successfully
- [ ] App launches normally
- [ ] System tray icon appears
- [ ] Tray icon tooltip works
- [ ] Double-click tray icon restores window
- [ ] Right-click tray menu appears
- [ ] "Run at Windows Startup" toggle works
- [ ] Registry entry is created correctly
- [ ] App starts minimized with --minimized argument
- [ ] Notification balloons appear
- [ ] Minimize button hides to tray
- [ ] Close button hides to tray (Shift+Close exits)
- [ ] Exit from tray menu works

## Files Changed

1. **App.xaml** - Added TaskbarIcon resource definition
2. **App.xaml.cs** - Added TrayIcon_TrayMouseDoubleClick event handler
3. **Services/SystemTrayService.cs** - Rewritten to use XAML TaskbarIcon
4. **Services/StartupManager.cs** - Fixed executable path resolution

## Build Output

```
Build succeeded with 27 warning(s)
PDFKawankasi.exe location:
PDFKawankasi\bin\Release\net8.0-windows10.0.19041.0\PDFKawankasi.exe
```

## Next Steps

1. Run Test-StartupTray.ps1 to verify functionality
2. Test all checklist items above
3. Test actual Windows startup (log out and back in)
4. Verify the app behavior matches expectations

---

**Status**: ✅ **Fixed and Ready for Testing**
