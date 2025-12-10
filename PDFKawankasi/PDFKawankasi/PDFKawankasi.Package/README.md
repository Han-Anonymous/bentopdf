# PDFKawankasi.Package - MSIX Packaging Project

This is the Windows Application Packaging Project for creating MSIX packages of PDF Kawankasi for distribution through the Microsoft Store and sideloading.

## Purpose

This project packages the main PDFKawankasi WPF application into an MSIX package that can be:
- Submitted to the Microsoft Store
- Sideloaded for testing and enterprise deployment
- Distributed as a bundled package supporting multiple architectures

## Project Structure

- **PDFKawankasi.Package.wapproj**: The packaging project file
- **Package.appxmanifest**: App manifest (located in parent directory `../Package.appxmanifest`)
- **Assets**: App icons and visual assets (located in parent directory `../Assets/`)

## Supported Platforms

- **x86**: 32-bit Intel/AMD processors
- **x64**: 64-bit Intel/AMD processors
- **ARM64**: ARM-based devices (e.g., Surface Pro X)

## Configuration

### Output Type
- **MSIX**: Modern Windows app package format

### Build Modes
- **Debug**: For local testing and development
- **Release**: For production and Store submission

### Bundle Configuration
- **AppxBundle**: Always (creates multi-architecture bundles)
- **AppxBundlePlatforms**: x86|x64|ARM64

### Store Upload Mode
- **UapAppxPackageBuildMode**: StoreUpload (for Store submissions)
- **AppxPackageSigningEnabled**: False (Store handles signing)

## Building

### In Visual Studio
1. Set this project as startup project
2. Select platform (x86, x64, or ARM64)
3. Build or deploy

### Via Command Line
```powershell
msbuild PDFKawankasi.Package.wapproj /p:Configuration=Release /p:Platform=x64
```

For Store submission:
```powershell
msbuild PDFKawankasi.Package.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64"
```

## Output Location

Built packages are placed in:
```
PDFKawankasi.Package/AppPackages/
```

## Dependencies

This project depends on:
- **PDFKawankasi.csproj**: The main WPF application
- **Package.appxmanifest**: App manifest with identity and capabilities
- **Assets**: Visual assets for tiles, icons, and splash screen

## Documentation

For detailed build and submission instructions, see:
- `../../QUICK_START_MSIX.md` - Quick reference guide
- `../../MSIX_BUILD_GUIDE.md` - Comprehensive build guide
- `../../MICROSOFT_STORE_SUBMISSION.md` - Store submission guide

## Microsoft Store

Before submitting to the Store:
1. Create app listing in Partner Center
2. Associate this project with your Store app
3. Build with StoreUpload mode
4. Upload the generated .msixupload file

## Notes

- The manifest references assets from `../Assets/` directory
- Publisher identity must be updated after Store association
- All packaging happens locally; no external dependencies
- Target Windows version: 10.0.17763.0 (minimum) to 10.0.22621.0 (tested)
