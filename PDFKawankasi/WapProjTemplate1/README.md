# WapProjTemplate1 - Microsoft Store Packaging Project

This is the official Windows Application Packaging Project (WAP) for creating Microsoft Store submissions for PDF Kawankasi.

## Overview

The WapProjTemplate1 project uses the Windows Desktop Bridge to package the PDF Kawankasi WPF application as an MSIX package suitable for distribution through the Microsoft Store.

## Why WapProjTemplate1?

- **Official Microsoft Method**: Uses the standard Windows Application Packaging Project template recommended by Microsoft
- **Store Compliance**: Configured with proper settings for Microsoft Store submission
- **Multi-Architecture Support**: Builds packages for x86, x64, and ARM64 platforms
- **Automatic Bundling**: Creates `.msixupload` bundles ready for Partner Center submission

## Project Structure

```
WapProjTemplate1/
├── Images/                      # App icons and logos for Store listing
│   ├── SplashScreen.scale-200.png
│   ├── Square150x150Logo.scale-200.png
│   ├── Square44x44Logo.scale-200.png
│   ├── StoreLogo.png
│   └── Wide310x150Logo.scale-200.png
├── Package.appxmanifest        # App manifest with identity, capabilities, and file associations
└── WapProjTemplate1.wapproj    # MSBuild project file
```

## Building for Microsoft Store

### Using Visual Studio (Recommended)

1. Open `PDFKawankasi.sln` in Visual Studio 2022
2. Right-click the **WapProjTemplate1** project
3. Select **Publish** → **Create App Packages**
4. Choose **Microsoft Store using an existing app name**
5. Sign in to your Partner Center account
6. Select your app reservation
7. Configure architectures (x86, x64, ARM64)
8. Click **Create**

The output will be in: `WapProjTemplate1/AppPackages/`

### Using MSBuild (CI/CD)

```powershell
# Build all architectures for Store submission
msbuild WapProjTemplate1\WapProjTemplate1.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64" `
  /p:AppxPackageSigningEnabled=false
```

### Using GitHub Actions

The project is configured to automatically build MSIX packages via GitHub Actions:
- **On push/PR**: Builds individual platform packages for testing
- **On release**: Creates Store bundle (`.msixupload`) for submission

See `.github/workflows/build-msix.yml` for details.

## Configuration

### Key Settings in WapProjTemplate1.wapproj

- **AppxBundle**: Always - Creates a bundle containing all architectures
- **AppxBundlePlatforms**: x86|x64|ARM64 - Target platforms
- **UapAppxPackageBuildMode**: StoreUpload - Generates `.msixupload` for Store
- **AppxPackageSigningEnabled**: false - Store handles signing
- **TargetPlatformVersion**: 10.0.26100.0 (Windows 11, version 24H2)
- **TargetPlatformMinVersion**: 10.0.17763.0 (Windows 10, version 1809)

### Package.appxmanifest

The manifest defines:
- **Identity**: Package name and publisher (set via Store association)
- **Capabilities**: runFullTrust, internetClient
- **File Associations**: .pdf files
- **Visual Elements**: App display name, description, logos

## Store Association

Before building for Store submission:

1. Right-click **WapProjTemplate1** in Visual Studio
2. Select **Publish** → **Associate App with the Store**
3. Sign in with your Partner Center account
4. Select **PDF Kawankasi** from your app reservations
5. Click **Associate**

This updates the manifest with your app's Identity and Publisher information from Partner Center.

## Submission Process

1. **Build**: Create the Store package using one of the methods above
2. **Locate**: Find the `.msixupload` file in `AppPackages/`
3. **Upload**: In Partner Center, create a new submission and upload the `.msixupload` file
4. **Configure**: Complete Store listing, age ratings, pricing
5. **Submit**: Submit for certification

See [MICROSOFT_STORE_SUBMISSION.md](../MICROSOFT_STORE_SUBMISSION.md) for detailed submission instructions.

## Related Projects

- **PDFKawankasi.csproj**: The main WPF application project
- **PDFKawankasi.Package**: Alternative packaging project (legacy, not used for Store)

## Resources

- [Package desktop applications (MSIX)](https://learn.microsoft.com/windows/msix/desktop/desktop-to-uwp-root)
- [Create an MSIX package from a desktop installer](https://learn.microsoft.com/windows/msix/packaging-tool/create-app-package)
- [Publish your app in the Microsoft Store](https://learn.microsoft.com/windows/apps/publish/)
