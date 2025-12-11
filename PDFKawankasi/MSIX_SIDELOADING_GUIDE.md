# MSIX Sideloading Guide - Testing with Self-Signed Certificates

This guide explains how to sideload and test MSIX packages locally using self-signed certificates, resolving the certificate verification error `0x800B010A`.

## Table of Contents

1. [Understanding the Error](#understanding-the-error)
2. [Prerequisites](#prerequisites)
3. [Method 1: Using PowerShell Scripts (Recommended)](#method-1-using-powershell-scripts-recommended)
4. [Method 2: Manual Certificate Creation](#method-2-manual-certificate-creation)
5. [Signing Your MSIX Package](#signing-your-msix-package)
6. [Installing the MSIX Package](#installing-the-msix-package)
7. [Troubleshooting](#troubleshooting)
8. [Microsoft Learn Resources](#microsoft-learn-resources)

## Understanding the Error

**Error Code: 0x800B010A**
```
This app package's publisher certificate could not be verified. Contact your 
system administrator or the app developer to obtain a new app package with 
verified certificates. The root certificate and all immediate certificates of 
the signature in the app package must be verified.
```

**What This Means:**
- The MSIX package is signed with a certificate that Windows doesn't trust
- This is common for locally built packages or packages not from the Microsoft Store
- Solution: Create a self-signed certificate, sign your package, and install the certificate

**When You Need This Guide:**
- Testing locally built MSIX packages
- Distributing packages outside the Microsoft Store
- Development and QA testing
- Enterprise deployments without Store

## Prerequisites

### Required Software

1. **Windows SDK** (includes SignTool.exe and MakeCert.exe)
   - Install via Visual Studio 2022: Select "Universal Windows Platform development"
   - Or download: [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/)
   - Default location: `C:\Program Files (x86)\Windows Kits\10\bin\`

2. **PowerShell** (Administrator access required)
   - Built into Windows 10/11
   - Run as Administrator for certificate operations

3. **Windows 10/11**
   - Developer Mode enabled (recommended but not required for sideloading with certificate)
   - Settings → Update & Security → For developers → Developer mode

### Required Files

- Your built MSIX package (`.msix` or `.msixbundle` file)
- Package manifest information (Publisher name from `Package.appxmanifest`)

## Method 1: Using PowerShell Scripts (Recommended)

We provide automated PowerShell scripts to simplify the entire process.

### Step 1: Create Certificate and Sign Package

Create a file named `Sign-MSIX.ps1` with the following content, or use the provided script:

```powershell
# Sign-MSIX.ps1 - Automated MSIX Signing Script
# This script creates a self-signed certificate, installs it, and signs your MSIX package

param(
    [Parameter(Mandatory=$true)]
    [string]$MsixPath,
    
    [Parameter(Mandatory=$false)]
    [string]$Publisher = "CN=PDF Kawankasi Development",
    
    [Parameter(Mandatory=$false)]
    [string]$CertPassword = "YourSecurePassword123!",
    
    [Parameter(Mandatory=$false)]
    [string]$CertPath = ".\PDFKawankasi_TemporaryKey.pfx"
)

# ⚠️ SECURITY NOTE: In production, use a strong unique password and store it securely.
# The default password shown here is just an example. The actual script generates a random password.

# Requires Administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

Write-Host "MSIX Package Signing Tool" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Validate MSIX file exists
if (-not (Test-Path $MsixPath)) {
    Write-Error "MSIX package not found: $MsixPath"
    exit 1
}

Write-Host "[1/5] Locating Windows SDK tools..." -ForegroundColor Yellow

# Find SignTool
$signToolPath = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -like "*\x64\*" } | 
    Select-Object -First 1 -ExpandProperty FullName

if (-not $signToolPath) {
    Write-Error "SignTool.exe not found. Please install Windows SDK."
    Write-Host "Download from: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}

Write-Host "  Found SignTool: $signToolPath" -ForegroundColor Green

# Find MakeCert
$makeCertPath = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "makecert.exe" -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -like "*\x64\*" } | 
    Select-Object -First 1 -ExpandProperty FullName

if (-not $makeCertPath) {
    Write-Warning "MakeCert.exe not found. Will use New-SelfSignedCertificate instead."
    $makeCertPath = $null
}

# Find Pvk2Pfx
$pvk2PfxPath = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "pvk2pfx.exe" -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -like "*\x64\*" } | 
    Select-Object -First 1 -ExpandProperty FullName

Write-Host ""
Write-Host "[2/5] Creating self-signed certificate..." -ForegroundColor Yellow

# Check if certificate already exists
$existingCert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $Publisher }

if ($existingCert) {
    Write-Host "  Certificate already exists in Personal store." -ForegroundColor Green
    $cert = $existingCert
} else {
    # Create certificate using New-SelfSignedCertificate (built-in PowerShell)
    $cert = New-SelfSignedCertificate -Type Custom `
        -Subject $Publisher `
        -KeyUsage DigitalSignature `
        -FriendlyName "PDF Kawankasi Test Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    
    Write-Host "  Certificate created successfully." -ForegroundColor Green
}

Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Cyan

Write-Host ""
Write-Host "[3/5] Exporting certificate to PFX..." -ForegroundColor Yellow

# Export certificate to PFX with password
$certPasswordSecure = ConvertTo-SecureString -String $CertPassword -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $CertPath -Password $certPasswordSecure | Out-Null

Write-Host "  Exported to: $CertPath" -ForegroundColor Green

Write-Host ""
Write-Host "[4/5] Installing certificate to Trusted Root..." -ForegroundColor Yellow

# Export certificate as CER (without private key)
$cerPath = $CertPath -replace '.pfx', '.cer'
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

# Import to Trusted Root
Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null

Write-Host "  Certificate installed to Trusted Root Certification Authorities." -ForegroundColor Green

Write-Host ""
Write-Host "[5/5] Signing MSIX package..." -ForegroundColor Yellow

# Sign the MSIX package
$signResult = & $signToolPath sign /fd SHA256 /a /f $CertPath /p $CertPassword $MsixPath 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  Package signed successfully!" -ForegroundColor Green
} else {
    Write-Error "Failed to sign package. Error: $signResult"
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "SUCCESS! Your MSIX package is now signed." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Double-click the MSIX file to install: $MsixPath" -ForegroundColor White
Write-Host "2. Or use PowerShell: Add-AppxPackage -Path '$MsixPath'" -ForegroundColor White
Write-Host ""
Write-Host "Certificate Details:" -ForegroundColor Cyan
Write-Host "  - PFX File: $CertPath" -ForegroundColor White
Write-Host "  - Password: $CertPassword" -ForegroundColor White
Write-Host "  - Publisher: $Publisher" -ForegroundColor White
Write-Host ""
Write-Host "NOTE: Keep the PFX file if you need to sign updates." -ForegroundColor Yellow
```

### Step 2: Run the Script

```powershell
# Navigate to your MSIX package directory
cd C:\Path\To\Your\MSIX

# Run as Administrator
.\Sign-MSIX.ps1 -MsixPath ".\PDFKawankasi.Package_1.0.0.0_x64.msix"

# Or with custom publisher name (must match Package.appxmanifest)
.\Sign-MSIX.ps1 -MsixPath ".\PDFKawankasi.Package_1.0.0.0_x64.msix" -Publisher "CN=YourPublisher"
```

### Step 3: Install the Package

After successful signing:

```powershell
# Method 1: Double-click the MSIX file in Explorer

# Method 2: Use PowerShell
Add-AppxPackage -Path ".\PDFKawankasi.Package_1.0.0.0_x64.msix"

# Method 3: Use GUI (double-click opens App Installer)
```

## Method 2: Manual Certificate Creation

If you prefer manual control or need to understand each step:

### Step 1: Create Self-Signed Certificate

**Option A: Using PowerShell (Simplest)**

```powershell
# Run PowerShell as Administrator
New-SelfSignedCertificate -Type Custom `
    -Subject "CN=PDF Kawankasi Development" `
    -KeyUsage DigitalSignature `
    -FriendlyName "PDF Kawankasi Test Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
```

**Option B: Using MakeCert (Windows SDK)**

```powershell
# Find MakeCert.exe (adjust path to your SDK version)
$makecert = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makecert.exe"

# Create certificate
& $makecert -r -h 0 -n "CN=PDF Kawankasi Development" `
    -eku 1.3.6.1.5.5.7.3.3 `
    -pe -sv PDFKawankasi_Key.pvk PDFKawankasi_Cert.cer

# Convert to PFX
$pvk2pfx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\pvk2pfx.exe"
& $pvk2pfx -pvk PDFKawankasi_Key.pvk -spc PDFKawankasi_Cert.cer `
    -pfx PDFKawankasi_TemporaryKey.pfx -po "YourSecurePassword123!"
```

### Step 2: Export Certificate

If you used PowerShell `New-SelfSignedCertificate`:

```powershell
# Find your certificate
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | 
    Where-Object { $_.Subject -eq "CN=PDF Kawankasi Development" }

# Export as PFX (for signing)
$password = ConvertTo-SecureString -String "YourSecurePassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert `
    -FilePath ".\PDFKawankasi_TemporaryKey.pfx" `
    -Password $password

# Export as CER (for installation)
Export-Certificate -Cert $cert -FilePath ".\PDFKawankasi_TemporaryKey.cer"
```

### Step 3: Install Certificate to Trusted Root

**Critical Step:** Windows only trusts certificates from trusted authorities. You must install your self-signed certificate.

```powershell
# Run PowerShell as Administrator

# Import certificate to Trusted Root Certification Authorities
Import-Certificate -FilePath ".\PDFKawankasi_TemporaryKey.cer" `
    -CertStoreLocation Cert:\LocalMachine\Root

# Verify installation
Get-ChildItem -Path Cert:\LocalMachine\Root | 
    Where-Object { $_.Subject -eq "CN=PDF Kawankasi Development" }
```

**Alternative: Using GUI**

1. Double-click `PDFKawankasi_TemporaryKey.cer`
2. Click **Install Certificate**
3. Select **Local Machine** (requires Administrator)
4. Choose **Place all certificates in the following store**
5. Click **Browse** → Select **Trusted Root Certification Authorities**
6. Click **OK** → **Next** → **Finish**

## Signing Your MSIX Package

### Update Package Manifest

Before signing, ensure `Package.appxmanifest` Publisher matches your certificate:

```xml
<Identity
  Name="PDFKawankasi"
  Publisher="CN=PDF Kawankasi Development"
  Version="1.0.0.0" />
```

### Sign the Package

```powershell
# Find SignTool.exe
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

# Sign MSIX with PFX
& $signtool sign /fd SHA256 /a /f ".\PDFKawankasi_TemporaryKey.pfx" /p "YourSecurePassword123!" `
    ".\PDFKawankasi.Package_1.0.0.0_x64.msix"
```

**Verify Signature:**

```powershell
# Check signature
& $signtool verify /pa ".\PDFKawankasi.Package_1.0.0.0_x64.msix"
```

Expected output:
```
Successfully verified: .\PDFKawankasi.Package_1.0.0.0_x64.msix
```

## Installing the MSIX Package

### Method 1: App Installer (Double-Click)

1. Navigate to your signed MSIX file in File Explorer
2. Double-click the `.msix` file
3. App Installer will open
4. Click **Install**
5. Wait for installation to complete
6. Find app in Start Menu

### Method 2: PowerShell

```powershell
# Install package
Add-AppxPackage -Path ".\PDFKawankasi.Package_1.0.0.0_x64.msix"

# Verify installation
Get-AppxPackage -Name "*PDFKawankasi*"
```

### Method 3: Enable Developer Mode (Alternative)

If you don't want to install certificates:

1. Open **Settings** → **Update & Security** → **For developers**
2. Enable **Developer Mode**
3. Install unsigned packages (less secure, not recommended for production)

```powershell
Add-AppxPackage -Path ".\PDFKawankasi.Package_1.0.0.0_x64.msix" -AllowUnsigned
```

**Note:** Developer Mode allows unsigned packages but is less secure. Using signed packages with installed certificates is recommended.

## Troubleshooting

### Error: 0x800B010A (Original Error)

**Problem:** Certificate not trusted or not found.

**Solutions:**
1. ✅ Ensure certificate is installed in **Trusted Root Certification Authorities**
2. ✅ Verify Publisher in `Package.appxmanifest` matches certificate Subject
3. ✅ Re-sign the package after certificate installation
4. ✅ Restart your computer after certificate installation (sometimes required)

**Verify Certificate Installation:**
```powershell
Get-ChildItem -Path Cert:\LocalMachine\Root | 
    Where-Object { $_.Subject -like "*PDF Kawankasi*" }
```

### Error: 0x80073CF0

**Problem:** Package conflicts with existing installation.

**Solution:**
```powershell
# Uninstall existing version
Get-AppxPackage -Name "*PDFKawankasi*" | Remove-AppxPackage

# Then reinstall
Add-AppxPackage -Path ".\PDFKawankasi.Package_1.0.0.0_x64.msix"
```

### Error: 0x80070032

**Problem:** Another installation is in progress.

**Solution:**
1. Wait for other installations to complete
2. Restart your computer
3. Try again

### Error: "The publisher could not be verified"

**Problem:** Certificate chain not complete.

**Solutions:**
1. Ensure you installed to **Trusted Root** (not just Personal)
2. Use `-CertStoreLocation Cert:\LocalMachine\Root` when importing
3. Verify with:
```powershell
certmgr.msc
# Navigate to Trusted Root Certification Authorities → Certificates
# Look for your certificate
```

### Error: SignTool not found

**Problem:** Windows SDK not installed or not in PATH.

**Solution:**
```powershell
# Find SignTool
Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
    -Recurse -Filter "signtool.exe"

# Add to PATH or use full path
$env:PATH += ";C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"
```

### Certificate Creation Fails

**Problem:** Insufficient permissions.

**Solution:**
- Run PowerShell as **Administrator**
- Check if antivirus is blocking certificate creation
- Ensure you have permission to modify certificate stores

### Package Installation Fails After Signing

**Problem:** Signature timestamp expired or invalid.

**Solution:**
```powershell
# Re-sign with timestamp server
& $signtool sign /fd SHA256 /a /f ".\PDFKawankasi_TemporaryKey.pfx" /p "YourSecurePassword123!" `
    /tr http://timestamp.digicert.com /td SHA256 `
    ".\PDFKawankasi.Package_1.0.0.0_x64.msix"
```

## Building with Self-Signed Certificate in Visual Studio

### Configure Visual Studio to Use Your Certificate

1. Open `PDFKawankasi.sln` in Visual Studio 2022
2. Right-click **WapProjTemplate1** project
3. Select **Properties**
4. Go to **Signing** tab
5. Check **Sign the package**
6. Click **Select from File**
7. Browse to your PFX file (`PDFKawankasi_TemporaryKey.pfx`)
8. Enter password
9. Click **OK**

Now when you build, Visual Studio will automatically sign the package.

### Automated Build with Certificate

Update your build command:

```powershell
# Build and sign in one step
msbuild WapProjTemplate1\WapProjTemplate1.wapproj `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:AppxBundle=Never `
    /p:AppxPackageSigningEnabled=true `
    /p:PackageCertificateKeyFile="C:\Path\To\PDFKawankasi_TemporaryKey.pfx" `
    /p:PackageCertificatePassword="YourSecurePassword123!"
```

## Automating for CI/CD

If using GitHub Actions or other CI/CD:

```yaml
# Example GitHub Actions workflow
- name: Create and install certificate
  run: |
    $cert = New-SelfSignedCertificate -Type Custom `
        -Subject "CN=PDF Kawankasi Development" `
        -KeyUsage DigitalSignature `
        -CertStoreLocation "Cert:\CurrentUser\My"
    
    $password = ConvertTo-SecureString -String "${{ secrets.CERT_PASSWORD }}" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath cert.pfx -Password $password
    
    Import-Certificate -Cert $cert -CertStoreLocation Cert:\LocalMachine\Root

- name: Sign MSIX package
  run: |
    $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
    & $signtool sign /fd SHA256 /a /f cert.pfx /p "${{ secrets.CERT_PASSWORD }}" `
        "PDFKawankasi\WapProjTemplate1\AppPackages\**\*.msix"
```

## Security Best Practices

### For Development

- ✅ Use self-signed certificates only for testing
- ✅ Keep certificate password secure
- ✅ Don't commit PFX files to version control
- ✅ Add `*.pfx` to `.gitignore`
- ✅ Use different certificates for dev/prod

### For Production

- ✅ **Microsoft Store**: Use Store signing (automatic)
- ✅ **Enterprise**: Use organization's code signing certificate
- ✅ **Public Distribution**: Purchase code signing certificate from trusted CA:
  - DigiCert
  - Sectigo
  - GlobalSign
  - SSL.com
- ✅ Use timestamp servers to extend signature validity
- ✅ Store certificates in secure key vault (Azure Key Vault, etc.)

### Certificate Validity

Self-signed certificates are valid for:
- **Default**: 1 year
- **Can extend**: Up to 10 years

```powershell
# Create certificate valid for 5 years
New-SelfSignedCertificate -Type Custom `
    -Subject "CN=PDF Kawankasi Development" `
    -KeyUsage DigitalSignature `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation "Cert:\CurrentUser\My"
```

## Microsoft Learn Resources

### Essential Documentation

**MSIX Packaging and Signing:**
- [Sign an MSIX package with SignTool](https://learn.microsoft.com/windows/msix/package/signing-package-with-signtool)
- [Create a certificate for package signing](https://learn.microsoft.com/windows/msix/package/create-certificate-package-signing)
- [Sign an app package using SignTool](https://learn.microsoft.com/windows/uwp/packaging/sign-app-package-using-signtool)

**Certificate Management:**
- [How to: Create a self-signed certificate](https://learn.microsoft.com/dotnet/core/additional-tools/self-signed-certificates-guide)
- [Certificate stores](https://learn.microsoft.com/windows-hardware/drivers/install/certificate-stores)
- [Trusted Root Certification Authorities Certificate Store](https://learn.microsoft.com/windows-hardware/drivers/install/trusted-root-certification-authorities-certificate-store)

**Sideloading:**
- [Sideload LOB apps in Windows](https://learn.microsoft.com/windows/application-management/sideload-apps-in-windows-10)
- [Install Windows 10/11 apps with App Installer](https://learn.microsoft.com/windows/msix/app-installer/app-installer-root)
- [Add-AppxPackage](https://learn.microsoft.com/powershell/module/appx/add-appxpackage)

**Windows SDK:**
- [Windows SDK Download](https://developer.microsoft.com/windows/downloads/windows-sdk/)
- [SignTool.exe (Sign Tool)](https://learn.microsoft.com/windows/win32/seccrypto/signtool)
- [MakeCert](https://learn.microsoft.com/windows/win32/seccrypto/makecert)

**Troubleshooting:**
- [Troubleshoot packaging, deployment, and query of Windows apps](https://learn.microsoft.com/windows/win32/appxpkg/troubleshooting)
- [Common MSIX deployment errors](https://learn.microsoft.com/windows/msix/desktop/managing-your-msix-deployment-troubleshooting)
- [App package architectures](https://learn.microsoft.com/windows/msix/package/device-architecture)

### Video Tutorials

- [MSIX: A complete guide for developers](https://learn.microsoft.com/shows/one-dev-minute/what-is-msix)
- [Packaging and deploying Windows apps](https://learn.microsoft.com/shows/)

### Community Support

- [Microsoft Q&A - Windows MSIX](https://learn.microsoft.com/answers/topics/windows-msix.html)
- [MSIX Tech Community](https://techcommunity.microsoft.com/t5/msix/ct-p/MSIX)
- [Stack Overflow - MSIX](https://stackoverflow.com/questions/tagged/msix)

## Quick Reference Commands

### Create Certificate
```powershell
New-SelfSignedCertificate -Type Custom -Subject "CN=PDF Kawankasi Development" -KeyUsage DigitalSignature -CertStoreLocation "Cert:\CurrentUser\My"
```

### Export Certificate
```powershell
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=PDF Kawankasi Development" }
$password = ConvertTo-SecureString "YourSecurePassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "cert.pfx" -Password $password
```

### Install Certificate
```powershell
Import-Certificate -FilePath "cert.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

### Sign Package
```powershell
signtool sign /fd SHA256 /a /f "cert.pfx" /p "YourSecurePassword123!" "package.msix"
```

### Install Package
```powershell
Add-AppxPackage -Path "package.msix"
```

### Uninstall Package
```powershell
Get-AppxPackage -Name "*PDFKawankasi*" | Remove-AppxPackage
```

## Summary

This guide provides everything you need to:

1. ✅ **Resolve error 0x800B010A** by creating and installing self-signed certificates
2. ✅ **Sideload MSIX packages** for testing and distribution outside the Store
3. ✅ **Automate signing** in Visual Studio and CI/CD pipelines
4. ✅ **Troubleshoot common issues** with detailed solutions
5. ✅ **Follow best practices** for secure certificate management

For **production distribution**, consider:
- **Microsoft Store** (recommended): Automatic signing and trusted distribution
- **Enterprise deployment**: Use organization code signing certificate
- **Public distribution**: Purchase commercial code signing certificate

For **testing and development**, the self-signed certificate approach in this guide is perfect.

---

**Related Documentation:**
- [MICROSOFT_STORE_SUBMISSION.md](MICROSOFT_STORE_SUBMISSION.md) - For Store publishing
- [MSIX_BUILD_GUIDE.md](MSIX_BUILD_GUIDE.md) - For building MSIX packages
- [QUICK_START_MSIX.md](QUICK_START_MSIX.md) - Quick reference for MSIX builds

**Last Updated**: 2025-12-11
**Document Version**: 1.0
