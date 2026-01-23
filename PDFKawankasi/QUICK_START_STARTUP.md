# Quick Start: Windows Startup & System Tray

## 🎯 What's New

PDF Kawankasi now supports:
- ✅ Run at Windows startup
- ✅ Minimize to system tray
- ✅ Quick access from tray icon
- ✅ Preloaded for instant PDF opening

## 🚀 Quick Setup

### Enable Startup with Windows

1. **Launch** PDF Kawankasi
2. **Find** the app icon in the system tray (notification area)
3. **Right-click** the tray icon
4. **Check** "Run at Windows Startup"

That's it! The app will now start with Windows, minimized to the tray.

## 📖 Usage

### Open the App
- **Double-click** the tray icon
- Or **right-click** → "Open PDF Kawankasi"

### Minimize to Tray
- Click the **minimize button** (—)
- Window hides to tray

### Close to Tray
- Click the **close button** (X)
- Window hides to tray instead of exiting
- **Force exit**: Hold **Shift** + click close button

### Exit Completely
- **Right-click** tray icon → "Exit"
- Or **Shift + Close** the window

## 🎨 Benefits

### Speed
- App opens **instantly** (already loaded)
- No cold start delay
- PDFs open immediately

### Convenience
- Always accessible from tray
- Doesn't clutter taskbar
- Starts automatically with Windows

### Privacy
- Runs completely offline
- No tracking or telemetry
- All data stays local

## ⚙️ Technical Details

### For Power Users

**Registry Location:**
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
Value: PDFKawankasi
Data: "C:\Path\To\PDFKawankasi.exe" --minimized
```

**Command-Line:**
```bash
# Start minimized
PDFKawankasi.exe --minimized

# Open specific PDF files
PDFKawankasi.exe document.pdf

# Both
PDFKawankasi.exe --minimized document.pdf
```

**Disable via Registry:**
```powershell
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "PDFKawankasi"
```

## 🔧 Troubleshooting

### App doesn't start with Windows
1. Check Windows Settings → Apps → Startup
2. Ensure "PDF Kawankasi" is enabled
3. Try disabling and re-enabling in the tray menu

### Tray icon missing
1. Check system tray overflow (hidden icons)
2. Windows Settings → Personalization → Taskbar
3. Enable "PDF Kawankasi" icon visibility

### Want to disable startup?
- Right-click tray icon
- Uncheck "Run at Windows Startup"

## 📚 More Information

See [WINDOWS_STARTUP_GUIDE.md](WINDOWS_STARTUP_GUIDE.md) for complete documentation.

---

**Requirements:** Windows 10 version 1809 or later
