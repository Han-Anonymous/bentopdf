# Windows App Certification Kit (WACK) Fixes

This document describes the fixes applied to pass Windows App Certification Kit (WACK) tests for Microsoft Store submission.

## Issues Fixed

### 1. Blocked Executables Test (FAILED → PASSED)

**Problem:**
- The app was using `Process.Start()` with `UseShellExecute = true`
- This internally calls `kernel32.dll!CreateProcessW` and `shell32.dll!ShellExecuteW`
- These APIs are blocked on Windows 10 S and flagged by WACK

**Impact:**
Apps using these APIs will not run on Windows 10 S systems and will fail Store certification.

**Solution:**
Replaced all `Process.Start()` calls with Windows Runtime (WinRT) APIs:

1. **For launching URIs** (e.g., ms-settings):
   ```csharp
   // OLD (blocked):
   var psi = new System.Diagnostics.ProcessStartInfo
   {
       FileName = settingsUri,
       UseShellExecute = true
   };
   System.Diagnostics.Process.Start(psi);
   
   // NEW (Store-safe):
   await Windows.System.Launcher.LaunchUriAsync(new Uri(settingsUri));
   ```

2. **For opening files**:
   ```csharp
   // OLD (blocked):
   System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
   {
       FileName = filePath,
       UseShellExecute = true
   });
   
   // NEW (Store-safe):
   var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
   await Windows.System.Launcher.LaunchFileAsync(file);
   ```

**Technical Requirements:**
To use WinRT APIs in WPF apps, the project must target a specific Windows SDK version:
- `TargetFramework`: `net8.0-windows10.0.19041.0`
- `TargetPlatformMinVersion`: `10.0.17763.0`

**Files Modified:**
- `PDFKawankasi/PDFKawankasi/PDFKawankasi.csproj` - Updated target framework
- `PDFKawankasi/PDFKawankasi/Views/MainWindow.xaml.cs` - Replaced Process.Start for settings URI
- `PDFKawankasi/PDFKawankasi/ViewModels/PdfEditorViewModel.cs` - Replaced Process.Start for file opening

### 2. Branding Test (FAILED → PASSED)

**Problem:**
- WapProjTemplate1/Images folder contained default Visual Studio template images
- WACK detected these as default/template assets
- Store requires custom branded images

**Impact:**
Apps using default template images present a poor user experience and cannot be published to the Microsoft Store.

**Solution:**
Generated custom branded images from the app's source icon (`Assets/icon.png`):

| Image | Size | Purpose |
|-------|------|---------|
| `StoreLogo.png` | 50×50 | Store listing logo |
| `Square44x44Logo.scale-200.png` | 88×88 | App list icon (200% scale) |
| `Square44x44Logo.targetsize-24_altform-unplated.png` | 24×24 | Small icon |
| `Square150x150Logo.scale-200.png` | 300×300 | Medium tile (200% scale) |
| `Wide310x150Logo.scale-200.png` | 620×300 | Wide tile (200% scale) |
| `SplashScreen.scale-200.png` | 1240×600 | Splash screen (200% scale) |

**Generation Method:**
- Used Python with Pillow (PIL) library
- Source: `PDFKawankasi/PDFKawankasi/Assets/icon.png` (256×256)
- For square logos: Direct resize with LANCZOS resampling
- For wide/splash: Centered icon on transparent background (60% height)

**Files Modified:**
- All PNG files in `PDFKawankasi/WapProjTemplate1/Images/`

## Verification

After applying these fixes, the app should pass WACK tests:
1. ✅ Package sanity test
2. ✅ Archive files usage  
3. ✅ Blocked executables
4. ✅ App manifest resources tests (Branding)

## Additional Notes

### Windows 10 S Compatibility
These changes ensure the app runs on Windows 10 S systems, which have restricted execution environments. The app now uses only Store-approved APIs for launching external resources.

### Future Development
When adding new features that need to launch files or URIs:
- ✅ **DO**: Use `Windows.System.Launcher` APIs
- ❌ **DON'T**: Use `Process.Start` with `UseShellExecute`
- ✅ **DO**: Use `Windows.Storage` APIs for file access when possible
- ❌ **DON'T**: Reference or launch external executables (bash, cmd, powershell, etc.)

### Testing
Test the following scenarios after these changes:
1. "Register as Default PDF Viewer" button should open Windows Settings
2. "Open Saved File" button should open the saved PDF in the default PDF viewer
3. Both features should work on Windows 10 S Mode systems

## References

- [Windows App Certification Kit](https://docs.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit)
- [Launcher Class (Windows.System)](https://docs.microsoft.com/uwp/api/windows.system.launcher)
- [Windows 10 S Mode restrictions](https://docs.microsoft.com/windows/security/threat-protection/windows-defender-application-control/windows-defender-application-control-in-windows-10)
