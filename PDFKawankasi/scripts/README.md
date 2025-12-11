# PDF Kawankasi Scripts

This directory contains helper scripts for building, signing, and deploying PDF Kawankasi MSIX packages.

## Available Scripts

### Sign-MSIX.ps1

**Purpose:** Automated MSIX package signing with self-signed certificates

**Description:** This PowerShell script automates the entire process of creating a self-signed certificate, installing it to the trusted root store, and signing your MSIX package. Perfect for local testing and sideloading scenarios.

**Usage:**

```powershell
# Basic usage (requires Administrator privileges)
.\Sign-MSIX.ps1 -MsixPath "C:\Path\To\Package.msix"

# With custom publisher name (must match Package.appxmanifest)
.\Sign-MSIX.ps1 -MsixPath ".\Package.msix" -Publisher "CN=YourCompany"

# With custom certificate password
.\Sign-MSIX.ps1 -MsixPath ".\Package.msix" -CertPassword "MySecurePassword123"

# Sign only, without installing certificate
.\Sign-MSIX.ps1 -MsixPath ".\Package.msix" -SkipCertInstall

# Custom certificate path and validity
.\Sign-MSIX.ps1 -MsixPath ".\Package.msix" -CertPath ".\MyCustomCert.pfx" -ValidityYears 10
```

**Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `MsixPath` | string | Yes | - | Path to the MSIX package to sign |
| `Publisher` | string | No | "CN=PDF Kawankasi Development" | Publisher name (must match manifest) |
| `CertPassword` | string | No | (auto-generated) | Password for PFX certificate (random by default) |
| `CertPath` | string | No | ".\PDFKawankasi_TemporaryKey.pfx" | Where to save the certificate |
| `ValidityYears` | int | No | 5 | Certificate validity period in years |
| `SkipCertInstall` | switch | No | false | Skip certificate installation (sign only) |
| `UseTimestamp` | switch | No | true | Use timestamp server for signature |

**What it does:**

1. ✅ Validates the MSIX package exists
2. ✅ Locates Windows SDK tools (SignTool, etc.)
3. ✅ Creates or reuses a self-signed certificate
4. ✅ Exports certificate to PFX and CER files
5. ✅ Installs certificate to Trusted Root (requires Admin)
6. ✅ Signs the MSIX package with the certificate
7. ✅ Verifies the signature
8. ✅ Provides next steps for installation

**Requirements:**

- Windows 10/11
- PowerShell 5.1 or later
- Windows SDK (for SignTool.exe)
- Administrator privileges (for certificate installation)

**Example Output:**

```
═══════════════════════════════════════════
  MSIX Package Signing Tool v1.0.0
  PDF Kawankasi Development
═══════════════════════════════════════════

[1/6] Validating MSIX package...
  ✓ Found: PDFKawankasi_1.0.0.0_x64.msix (15.2 MB)

[2/6] Locating Windows SDK tools...
  ✓ SignTool: C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe
  ℹ SDK Version: 10.0.22621.0

[3/6] Managing self-signed certificate...
  ℹ Creating new self-signed certificate...
  ✓ Certificate created successfully
  ℹ Certificate Details:
    Subject: CN=PDF Kawankasi Development
    Thumbprint: 1234567890ABCDEF...
    Valid: 2025-12-11 to 2030-12-11

[4/6] Installing certificate to Trusted Root...
  ✓ Certificate installed to Trusted Root Certification Authorities

[5/6] Signing MSIX package...
  ℹ Using timestamp server: http://timestamp.digicert.com
  ✓ Package signed successfully!

[6/6] Verifying package signature...
  ✓ Signature verified successfully!
  ℹ Package is ready to install

═══════════════════════════════════════════
  ✓ SUCCESS! Package is ready to install
═══════════════════════════════════════════

Next Steps:
1. Double-click the MSIX file to install
2. Or use PowerShell: Add-AppxPackage -Path 'Package.msix'
```

**Troubleshooting:**

See [MSIX_SIDELOADING_GUIDE.md](../MSIX_SIDELOADING_GUIDE.md) for detailed troubleshooting information.

## Common Workflows

### Testing Locally Built MSIX

```powershell
# 1. Build the MSIX package
cd PDFKawankasi
msbuild WapProjTemplate1\WapProjTemplate1.wapproj /p:Configuration=Release /p:Platform=x64

# 2. Sign the package
cd scripts
.\Sign-MSIX.ps1 -MsixPath "..\WapProjTemplate1\AppPackages\**\*.msix"

# 3. Install the package
Add-AppxPackage -Path "..\WapProjTemplate1\AppPackages\**\*.msix"
```

### Updating an Existing Installation

```powershell
# Sign the new version
.\Sign-MSIX.ps1 -MsixPath ".\NewVersion.msix"

# Update (preserves user data)
Add-AppxPackage -Path ".\NewVersion.msix" -Update
```

### Sharing with Team Members

```powershell
# 1. Sign the package once
.\Sign-MSIX.ps1 -MsixPath ".\Package.msix"

# 2. Share both files:
#    - Package.msix (signed MSIX)
#    - PDFKawankasi_TemporaryKey.cer (public certificate)

# 3. Team members install the certificate:
Import-Certificate -FilePath ".\PDFKawankasi_TemporaryKey.cer" `
    -CertStoreLocation Cert:\LocalMachine\Root

# 4. Then install the package:
Add-AppxPackage -Path ".\Package.msix"
```

## Security Notes

⚠️ **Important Security Information:**

- **DO NOT** commit `.pfx` files to version control (already in .gitignore)
- **DO NOT** share PFX files publicly
- **DO** keep certificate passwords secure
- **DO** use different certificates for development and production
- Self-signed certificates are for **testing only**
- For production/Store distribution, use proper code signing certificates

## Related Documentation

- [MSIX_SIDELOADING_GUIDE.md](../MSIX_SIDELOADING_GUIDE.md) - Complete sideloading guide with manual steps
- [MSIX_BUILD_GUIDE.md](../MSIX_BUILD_GUIDE.md) - Building MSIX packages
- [MICROSOFT_STORE_SUBMISSION.md](../MICROSOFT_STORE_SUBMISSION.md) - Publishing to Microsoft Store
- [QUICK_START_MSIX.md](../QUICK_START_MSIX.md) - Quick reference for MSIX builds

## Contributing

To add new scripts:

1. Create the script with clear parameter documentation
2. Test thoroughly on clean Windows installation
3. Update this README with usage examples
4. Add error handling and user-friendly output

## Support

For issues with these scripts:
- Check the [Troubleshooting](#troubleshooting) section
- Review [MSIX_SIDELOADING_GUIDE.md](../MSIX_SIDELOADING_GUIDE.md)
- Search existing GitHub issues
- Open a new issue with detailed error information

---

**Last Updated**: 2025-12-11
**Scripts Version**: 1.0
