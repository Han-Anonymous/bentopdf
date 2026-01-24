# Test Script for Windows Startup & System Tray
# Run this script to test the implementation

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "PDF Kawankasi - Startup & Tray Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check if executable exists
$exePath = "PDFKawankasi\bin\Release\net8.0-windows10.0.19041.0\PDFKawankasi.exe"
Write-Host "1. Checking executable..." -ForegroundColor Yellow
if (Test-Path $exePath) {
    Write-Host "   ✓ Executable found: $exePath" -ForegroundColor Green
} else {
    Write-Host "   ✗ Executable NOT found!" -ForegroundColor Red
    Write-Host "   Please build the project first: dotnet build -c Release" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 2. Check current startup status
Write-Host "2. Checking Windows startup registry..." -ForegroundColor Yellow
$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$regValue = Get-ItemProperty -Path $regPath -Name "PDFKawankasi" -ErrorAction SilentlyContinue

if ($regValue) {
    Write-Host "   ✓ Startup entry exists:" -ForegroundColor Green
    Write-Host "     $($regValue.PDFKawankasi)" -ForegroundColor Gray
} else {
    Write-Host "   ○ No startup entry (will be created when you enable it)" -ForegroundColor Yellow
}
Write-Host ""

# 3. Launch the app
Write-Host "3. Launching PDF Kawankasi..." -ForegroundColor Yellow
$fullPath = Resolve-Path $exePath
Write-Host "   Starting: $fullPath" -ForegroundColor Gray

try {
    Start-Process -FilePath $fullPath
    Write-Host "   ✓ Application started!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "Now test the following:" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "SYSTEM TRAY ICON:" -ForegroundColor Yellow
    Write-Host "  1. Check the system tray (notification area)" -ForegroundColor White
    Write-Host "  2. You should see the PDF Kawankasi icon" -ForegroundColor White
    Write-Host "  3. Hover over it - tooltip should say 'PDF Kawankasi'" -ForegroundColor White
    Write-Host ""
    Write-Host "DOUBLE-CLICK TEST:" -ForegroundColor Yellow
    Write-Host "  1. Minimize the app window (click minimize button)" -ForegroundColor White
    Write-Host "  2. Window should hide to tray" -ForegroundColor White
    Write-Host "  3. Double-click the tray icon" -ForegroundColor White
    Write-Host "  4. Window should restore" -ForegroundColor White
    Write-Host ""
    Write-Host "RIGHT-CLICK MENU:" -ForegroundColor Yellow
    Write-Host "  1. Right-click the tray icon" -ForegroundColor White
    Write-Host "  2. You should see:" -ForegroundColor White
    Write-Host "     - Open PDF Kawankasi" -ForegroundColor Gray
    Write-Host "     - Run at Windows Startup (checkbox)" -ForegroundColor Gray
    Write-Host "     - Exit" -ForegroundColor Gray
    Write-Host ""
    Write-Host "ENABLE STARTUP:" -ForegroundColor Yellow
    Write-Host "  1. Right-click tray icon → Check 'Run at Windows Startup'" -ForegroundColor White
    Write-Host "  2. A notification should appear" -ForegroundColor White
    Write-Host "  3. Run this command to verify:" -ForegroundColor White
    Write-Host "     Get-ItemProperty -Path '$regPath' -Name 'PDFKawankasi'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "TEST STARTUP:" -ForegroundColor Yellow
    Write-Host "  1. Close the app (Shift + Close button to exit)" -ForegroundColor White
    Write-Host "  2. Run: Start-Process `"$fullPath`" -ArgumentList '--minimized'" -ForegroundColor White
    Write-Host "  3. App should start minimized to tray" -ForegroundColor White
    Write-Host "  4. Notification should appear" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host "   ✗ Failed to start application: $_" -ForegroundColor Red
}

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Press any key to check startup status..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "Current startup configuration:" -ForegroundColor Yellow
$regValue2 = Get-ItemProperty -Path $regPath -Name "PDFKawankasi" -ErrorAction SilentlyContinue
if ($regValue2) {
    Write-Host "  ✓ ENABLED" -ForegroundColor Green
    Write-Host "  Command: $($regValue2.PDFKawankasi)" -ForegroundColor Gray
} else {
    Write-Host "  ○ DISABLED" -ForegroundColor Yellow
}
