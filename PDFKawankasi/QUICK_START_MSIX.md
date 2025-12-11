# Quick Start: Building MSIX Package

This is a quick reference guide for building and testing the MSIX package for PDF Kawankasi.

## For Developers: Local Testing

### Using Visual Studio (Recommended)

1. Open `PDFKawankasi.sln` in Visual Studio 2022
2. Set **PDFKawankasi.Package** as the startup project (right-click → Set as Startup Project)
3. Select **Debug** configuration and **x64** platform
4. Press **F5** or click **Debug** → **Start Debugging**
5. The app will be deployed and launched automatically

### Quick Deploy Without Debugging

1. In Visual Studio, right-click **PDFKawankasi.Package** project
2. Select **Deploy**
3. Find the app in Start menu as "PDF Kawankasi"

## For Release Builds: Microsoft Store

### Step 1: Associate with Microsoft Store (First time only)

1. Right-click **PDFKawankasi.Package** project
2. Select **Publish** → **Associate App with the Store**
3. Sign in with Partner Center account
4. Select your app reservation
5. Click **Associate**

This updates `Package.appxmanifest` with the correct publisher identity.

### Step 2: Create Store Package

1. Right-click **PDFKawankasi.Package** project
2. Select **Publish** → **Create App Packages**
3. Select **Microsoft Store using a new app name** or **Microsoft Store under an existing app name**
4. Follow the wizard:
   - Select architectures: ✅ x86, ✅ x64, ✅ ARM64
   - ✅ Create app bundle: Always
   - Output location: default
5. Click **Create**

### Step 3: Upload to Store

1. Go to [Partner Center](https://partner.microsoft.com/dashboard)
2. Navigate to your app → Packages section
3. Upload the `.msixupload` file from:
   ```
   PDFKawankasi\PDFKawankasi.Package\AppPackages\PDFKawankasi.Package_{version}\
   ```
4. Complete Store listing and submit for certification

## Command Line Builds

### Prerequisites
```powershell
# Check MSBuild is available
msbuild -version
```

### Build for Local Testing (x64)
```powershell
cd PDFKawankasi
msbuild PDFKawankasi.sln /p:Configuration=Debug /p:Platform=x64
```

### Build Store Package
```powershell
cd PDFKawankasi
msbuild PDFKawankasi\PDFKawankasi.Package\PDFKawankasi.Package.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64"
```

## Testing Checklist

Before submitting to Store:

- [ ] App launches successfully
- [ ] PDF files can be opened via file association
- [ ] All features work as expected
- [ ] App icon appears correctly
- [ ] Start menu tile shows correctly
- [ ] Privacy policy URL is set in manifest
- [ ] No console windows or debug outputs
- [ ] App follows Windows design guidelines

## Troubleshooting

### "The project needs to be associated with the Store"
**Solution**: Associate with Store OR disable signing in `.wapproj`:
```xml
<AppxPackageSigningEnabled>False</AppxPackageSigningEnabled>
```

### "Cannot find Windows SDK"
**Solution**: Install via Visual Studio Installer:
- Workload: **Universal Windows Platform development**
- Individual component: **Windows 10 SDK (10.0.22621.0)**

### "Deployment failed"
**Solution**: 
1. Enable Developer Mode: Settings → Update & Security → For developers
2. Uninstall previous version if exists
3. Check Event Viewer for detailed errors

## CI/CD

GitHub Actions workflow is configured in `.github/workflows/build-msix.yml`

- **On push**: Builds individual platform packages
- **On release**: Creates Store upload bundle

Artifacts are available in the Actions tab for 30-90 days.

## More Information

For detailed documentation, see [MSIX_BUILD_GUIDE.md](MSIX_BUILD_GUIDE.md)

## Common Build Targets

| Configuration | Platform | Use Case |
|--------------|----------|----------|
| Debug | x64 | Local development and testing |
| Release | x64 | Production build for 64-bit systems |
| Release | x86 | Production build for 32-bit systems |
| Release | ARM64 | Production build for ARM devices |
| Release | Bundle | Microsoft Store submission (all platforms) |

## Output Locations

After build, packages are in:
```
PDFKawankasi\PDFKawankasi.Package\AppPackages\
├── PDFKawankasi.Package_{version}_Test\     (for sideloading)
│   ├── PDFKawankasi.Package_{version}_x64.msix
│   ├── PDFKawankasi.Package_{version}_x86.msix
│   └── PDFKawankasi.Package_{version}_ARM64.msix
└── PDFKawankasi.Package_{version}\          (for Store)
    ├── PDFKawankasi.Package_{version}_x86_x64_ARM64.msixbundle
    └── PDFKawankasi.Package_{version}_x86_x64_ARM64.msixupload
```
