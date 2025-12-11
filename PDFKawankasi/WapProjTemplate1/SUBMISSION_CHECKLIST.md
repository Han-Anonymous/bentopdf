# Microsoft Store Submission Checklist

Use this checklist to ensure your WapProjTemplate1 package is ready for Microsoft Store submission.

## Before Building

- [ ] **Partner Center Account**: You have an active Microsoft Partner Center account
- [ ] **App Reservation**: You have reserved "PDF Kawankasi" in Partner Center
- [ ] **Store Association**: You have completed "Associate App with the Store" in Visual Studio
  - Right-click WapProjTemplate1 → Publish → Associate App with the Store
  - This updates Package.appxmanifest with your actual Publisher identity
- [ ] **Privacy Policy**: You have a published privacy policy URL
- [ ] **App Icon Assets**: All required icons are present in the Images/ folder
  - SplashScreen.scale-200.png
  - Square150x150Logo.scale-200.png
  - Square44x44Logo.scale-200.png
  - StoreLogo.png
  - Wide310x150Logo.scale-200.png

## Building the Package

- [ ] **Configuration**: Using Release configuration (not Debug)
- [ ] **Platforms**: Building for all architectures (x86, x64, ARM64)
- [ ] **Bundle Mode**: AppxBundle is set to "Always"
- [ ] **Build Mode**: UapAppxPackageBuildMode is "StoreUpload"
- [ ] **Build Success**: Package builds without errors
- [ ] **Output Location**: .msixupload file is generated in AppPackages/ folder

## Pre-Submission Validation

- [ ] **WACK Test**: Package passes Windows App Certification Kit (optional but recommended)
  - Open Windows Kits → Windows App Cert Kit
  - Select your installed app
  - Run full test suite
- [ ] **Local Testing**: App installs and runs correctly from the package
- [ ] **PDF Association**: PDF files open correctly with the app
- [ ] **Version Number**: Package.appxmanifest Version is correct (e.g., 1.0.0.0)

## Partner Center Submission

- [ ] **Create Submission**: New submission created in Partner Center
- [ ] **Upload Package**: .msixupload file uploaded successfully
- [ ] **Pricing**: Pricing tier selected (Free or Paid)
- [ ] **Markets**: Target markets/regions selected
- [ ] **Age Rating**: Age rating questionnaire completed
- [ ] **Privacy Policy**: Privacy policy URL provided
- [ ] **App Description**: Store listing description written (200+ characters)
- [ ] **Screenshots**: At least 1 screenshot uploaded (recommended: 3-5)
  - Minimum resolution: 1366x768
  - Show key features of PDF Kawankasi
- [ ] **Store Logos**: Logos configured (auto-populated from package)
- [ ] **Promotional Assets**: Optional promotional images added
- [ ] **Search Terms**: Relevant keywords added (max 7)
- [ ] **Contact Info**: Support email or website provided

## Required Store Policies

Verify your app complies with:
- [ ] **Content Policies**: No prohibited content
- [ ] **Security**: App is secure and doesn't contain malware
- [ ] **Privacy**: Handles user data appropriately
- [ ] **Performance**: App is stable and responsive
- [ ] **Functionality**: All advertised features work as described

## Certification Notes

- [ ] **Release Notes**: Submission notes provided for certification team
- [ ] **Test Instructions**: Any special testing instructions noted
- [ ] **Account Credentials**: Test account provided if needed

## After Submission

- [ ] **Submission Status**: Monitor submission status in Partner Center
- [ ] **Respond to Feedback**: Address any certification failures promptly
- [ ] **Update Tracking**: Keep version numbers consistent across updates
- [ ] **Release Notes**: Maintain changelog for users

## Quick Build Command (PowerShell)

```powershell
# Navigate to PDFKawankasi directory
cd PDFKawankasi

# Build all platforms
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=x64
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=x86
msbuild PDFKawankasi\PDFKawankasi.csproj /p:Configuration=Release /p:Platform=ARM64

# Create Store bundle
msbuild WapProjTemplate1\WapProjTemplate1.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64" `
  /p:AppxPackageSigningEnabled=false
```

## Resources

- [Microsoft Store Policies](https://learn.microsoft.com/windows/apps/publish/store-policies)
- [App Certification Requirements](https://learn.microsoft.com/windows/apps/publish/certification-requirements)
- [Partner Center Dashboard](https://partner.microsoft.com/dashboard)
- [Full Submission Guide](../MICROSOFT_STORE_SUBMISSION.md)
