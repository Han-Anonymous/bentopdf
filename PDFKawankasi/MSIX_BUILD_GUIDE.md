# MSIX Package Build and Microsoft Store Submission Guide

This guide explains how to build MSIX packages for PDF Kawankasi and prepare them for Microsoft Store submission.

## Prerequisites

1. **Visual Studio 2022** with the following workloads:
   - .NET Desktop Development
   - Universal Windows Platform development
   - Windows Application Packaging Project

2. **Windows SDK** version 10.0.26100.0 or later

3. **Windows 10 version 1809 (build 17763)** or later for development

## Project Structure

The solution contains two packaging projects:

- **PDFKawankasi.csproj**: Main WPF application project
- **WapProjTemplate1.wapproj**: Official Windows Application Packaging Project for Microsoft Store (RECOMMENDED)
- **PDFKawankasi.Package.wapproj**: Alternative packaging project (legacy)

> **Note:** Use **WapProjTemplate1** for all Microsoft Store submissions. It is configured with the official Microsoft template and proper Store settings.

## Building MSIX Packages

### Option 1: Using Visual Studio (Recommended for Store Submission)

1. Open `PDFKawankasi.sln` in Visual Studio 2022
2. Select the **WapProjTemplate1** project as the startup project
3. Choose your target platform (x86, x64, or ARM64)
4. Select **Release** configuration
5. Right-click on **WapProjTemplate1** project
6. Select **Publish** > **Create App Packages**
7. Choose **Microsoft Store using an existing app name**
8. Sign in to Partner Center and select your app
9. Configure architectures (x86, x64, ARM64) and create the bundle

### Option 2: Using MSBuild Command Line

For **Microsoft Store bundle** (all architectures):
```powershell
# First build the main project for all platforms
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=x64
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=x86
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=ARM64

# Then create the Store bundle
msbuild WapProjTemplate1\WapProjTemplate1.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64" `
  /p:AppxPackageSigningEnabled=false
```

For **individual platform builds** (testing/sideload):
```powershell
# x64
msbuild WapProjTemplate1\WapProjTemplate1.wapproj /p:Configuration=Release /p:Platform=x64 /p:UapAppxPackageBuildMode=SideloadOnly

# x86
msbuild WapProjTemplate1\WapProjTemplate1.wapproj /p:Configuration=Release /p:Platform=x86 /p:UapAppxPackageBuildMode=SideloadOnly

# ARM64
msbuild WapProjTemplate1\WapProjTemplate1.wapproj /p:Configuration=Release /p:Platform=ARM64 /p:UapAppxPackageBuildMode=SideloadOnly
```

For **multi-platform bundle** (recommended for Store):
```powershell
msbuild PDFKawankasi\PDFKawankasi.Package\PDFKawankasi.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:UapAppxPackageBuildMode=StoreUpload /p:AppxBundle=Always /p:AppxBundlePlatforms="x86|x64|ARM64"
```

### Output Location

The MSIX packages will be generated in:
```
PDFKawankasi\PDFKawankasi.Package\AppPackages\PDFKawankasi.Package_{version}_Test\
```

For Store upload, you'll find:
- `PDFKawankasi.Package_{version}_x86_x64_ARM64.msixbundle` - The bundle package for Store submission
- Individual platform-specific MSIX files

## Preparing for Microsoft Store Submission

### Step 1: Configure App Identity

Before submitting to the Microsoft Store, you need to associate your app with the Store:

1. **Create a Microsoft Store app listing**:
   - Go to [Partner Center](https://partner.microsoft.com/dashboard)
   - Create a new app submission
   - Reserve your app name (e.g., "PDF Kawankasi")

2. **Associate your app with the Store**:
   - In Visual Studio, right-click on **PDFKawankasi.Package** project
   - Select **Publish** > **Associate App with the Store**
   - Sign in with your Microsoft Partner Center account
   - Select your app from the list
   - This will automatically update `Package.appxmanifest` with the correct:
     - Identity Name
     - Publisher (from your Partner Center certificate)
     - Publisher Display Name

### Step 2: Update Package.appxmanifest

If not using Visual Studio's association wizard, manually update these fields in `Package.appxmanifest`:

```xml
<Identity
  Name="YourPublisher.PDFKawankasi"
  Publisher="CN=YourPublisher, O=YourOrganization, L=City, S=State, C=Country"
  Version="1.0.0.0" />

<Properties>
  <DisplayName>PDF Kawankasi</DisplayName>
  <PublisherDisplayName>Your Publisher Name</PublisherDisplayName>
  <Logo>Assets\StoreLogo.png</Logo>
</Properties>
```

**Important**: The Publisher field must match the certificate from your Microsoft Partner Center account.

### Step 3: Verify Assets

Ensure all required Store assets are present in the `Assets` folder:

- ✅ **Square44x44Logo.png** (44x44) - App list icon
- ✅ **Square71x71Logo.png** (71x71) - Small tile
- ✅ **Square150x150Logo.png** (150x150) - Medium tile
- ✅ **Square310x310Logo.png** (310x310) - Large tile
- ✅ **Wide310x150Logo.png** (310x150) - Wide tile
- ✅ **StoreLogo.png** (50x50) - Store listing icon
- ✅ **SplashScreen.png** (620x300) - Splash screen

All assets are already in place in the `PDFKawankasi/Assets` folder.

### Step 4: Build for Store Submission

1. Set configuration to **Release**
2. Build the packaging project with **StoreUpload** mode:
   ```powershell
   msbuild PDFKawankasi\PDFKawankasi.Package\PDFKawankasi.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:UapAppxPackageBuildMode=StoreUpload /p:AppxBundle=Always /p:AppxBundlePlatforms="x86|x64|ARM64"
   ```

3. The output will include:
   - `.msixupload` or `.appxupload` file - This is what you upload to Partner Center
   - `.msixsym` files - Symbol files for crash reporting

### Step 5: Upload to Microsoft Store

1. Go to [Partner Center](https://partner.microsoft.com/dashboard)
2. Navigate to your app submission
3. In the **Packages** section, upload the `.msixupload` or `.appxupload` file
4. Complete the Store listing with:
   - App description
   - Screenshots
   - Privacy policy URL
   - Age ratings
   - Categories
5. Submit for certification

## Configuration Details

### App Capabilities

The app declares the following capabilities in `Package.appxmanifest`:

- **runFullTrust**: Required for WPF desktop apps
- **internetClient**: For any network operations (if needed)

### Platform Support

The package supports:
- **x86**: 32-bit Intel/AMD processors
- **x64**: 64-bit Intel/AMD processors
- **ARM64**: ARM-based devices (like Surface Pro X)

### Target Windows Versions

- **Minimum Version**: Windows 10, version 1809 (build 17763)
- **Maximum Tested Version**: Windows 11, version 22H2 (build 22621)

## File Type Associations

The app registers as a handler for PDF files:
- File extension: `.pdf`
- Content type: `application/pdf`

Users can set PDF Kawankasi as their default PDF viewer through Windows Settings.

## Testing the MSIX Package

### Local Installation Testing

1. Build the package in Debug or Release configuration
2. Right-click on **PDFKawankasi.Package** project
3. Select **Deploy**
4. The app will be installed on your local machine
5. Find it in the Start menu as "PDF Kawankasi"

### Sideloading for Testing

Before Store submission, you can test the package:

1. Build the package without Store association
2. Enable Developer Mode in Windows Settings
3. Install the certificate (if self-signed)
4. Double-click the `.msix` file to install
5. Or use PowerShell:
   ```powershell
   Add-AppxPackage -Path "path\to\PDFKawankasi.Package.msix"
   ```

## Troubleshooting

### Build Errors

**Error: "The project needs to be associated with the Store"**
- Solution: Either associate with Store or set `AppxPackageSigningEnabled` to `False` for local builds

**Error: "Windows SDK not found"**
- Solution: Install Windows SDK 10.0.22621.0 or later via Visual Studio Installer

**Error: "Platform target not available"**
- Solution: Ensure you've built the main PDFKawankasi project for the target platform first

### Deployment Issues

**Error: "App didn't start"**
- Check Event Viewer > Windows Logs > Application for errors
- Verify all dependencies are included in the package

**Error: "Certificate not trusted"**
- For sideloading, install the certificate first
- For Store apps, this won't be an issue

## CI/CD Integration

For automated builds in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Build MSIX Package
  run: |
    msbuild PDFKawankasi\PDFKawankasi.Package\PDFKawankasi.Package.wapproj `
      /p:Configuration=Release `
      /p:Platform=x64 `
      /p:UapAppxPackageBuildMode=StoreUpload `
      /p:AppxBundle=Always `
      /p:AppxBundlePlatforms="x86|x64|ARM64"
```

## Additional Resources

- [Microsoft Store Documentation](https://learn.microsoft.com/en-us/windows/apps/publish/)
- [MSIX Packaging Documentation](https://learn.microsoft.com/en-us/windows/msix/)
- [Windows Application Packaging Project](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)
- [App Certification Requirements](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)

## Support

For issues with the packaging process, please refer to:
- Project repository issues
- Microsoft Developer Forums
- Partner Center support (for Store-related issues)
