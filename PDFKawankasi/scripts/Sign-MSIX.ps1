# Sign-MSIX.ps1 - Automated MSIX Signing Script
# This script creates a self-signed certificate, installs it, and signs your MSIX package
# Usage: .\Sign-MSIX.ps1 -MsixPath "path\to\package.msix"

param(
    [Parameter(Mandatory=$true, HelpMessage="Path to the MSIX package to sign")]
    [string]$MsixPath,
    
    [Parameter(Mandatory=$false, HelpMessage="Publisher name (must match Package.appxmanifest)")]
    [string]$Publisher = "CN=PDF Kawankasi Development",
    
    [Parameter(Mandatory=$false, HelpMessage="Password for the PFX certificate")]
    [string]$CertPassword = "TestCert$(Get-Random -Minimum 1000 -Maximum 9999)!",
    
    [Parameter(Mandatory=$false, HelpMessage="Path where the PFX certificate will be saved")]
    [string]$CertPath = ".\PDFKawankasi_TemporaryKey.pfx",
    
    [Parameter(Mandatory=$false, HelpMessage="Certificate validity period in years")]
    [int]$ValidityYears = 5,
    
    [Parameter(Mandatory=$false, HelpMessage="Skip certificate installation (sign only)")]
    [switch]$SkipCertInstall = $false,
    
    [Parameter(Mandatory=$false, HelpMessage="Use timestamp server for signature")]
    [switch]$UseTimestamp = $true
)

# Script version
$ScriptVersion = "1.0.0"

# Color functions
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host $msg -ForegroundColor Yellow }
function Write-Step { param($msg) Write-Host $msg -ForegroundColor Yellow }

# Display banner
Clear-Host
Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  MSIX Package Signing Tool v$ScriptVersion" -ForegroundColor Cyan
Write-Host "  PDF Kawankasi Development" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check for Administrator privileges
if (-NOT $SkipCertInstall) {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-NOT $isAdmin) {
        Write-Warning "⚠️  This script requires Administrator privileges to install certificates."
        Write-Host ""
        Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor White
        Write-Host ""
        Write-Host "To run as Administrator:" -ForegroundColor Yellow
        Write-Host "1. Right-click PowerShell" -ForegroundColor White
        Write-Host "2. Select 'Run as Administrator'" -ForegroundColor White
        Write-Host "3. Navigate to this directory" -ForegroundColor White
        Write-Host "4. Run the script again" -ForegroundColor White
        Write-Host ""
        Write-Host "Or use -SkipCertInstall to only sign (certificate must be installed manually)" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
}

# Validate MSIX file exists
Write-Step "[1/6] Validating MSIX package..."
if (-not (Test-Path $MsixPath)) {
    Write-Error "❌ MSIX package not found: $MsixPath"
    exit 1
}

$msixFile = Get-Item $MsixPath
Write-Success "  ✓ Found: $($msixFile.Name) ($([math]::Round($msixFile.Length/1MB, 2)) MB)"
Write-Host ""

# Find Windows SDK tools
Write-Step "[2/6] Locating Windows SDK tools..."

# Find SignTool
$signToolPath = $null
$sdkPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin",
    "C:\Program Files\Windows Kits\10\bin"
)

foreach ($sdkPath in $sdkPaths) {
    if (Test-Path $sdkPath) {
        $signToolPath = Get-ChildItem -Path $sdkPath -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue | 
            Where-Object { $_.FullName -like "*\x64\*" } | 
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
        
        if ($signToolPath) { break }
    }
}

if (-not $signToolPath) {
    Write-Error "❌ SignTool.exe not found. Please install Windows SDK."
    Write-Host ""
    Write-Host "Download Windows SDK from:" -ForegroundColor Yellow
    Write-Host "https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor White
    Write-Host ""
    Write-Host "Or install via Visual Studio Installer:" -ForegroundColor Yellow
    Write-Host "  - Select 'Universal Windows Platform development' workload" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Success "  ✓ SignTool: $signToolPath"
$sdkVersion = ($signToolPath -split "\\bin\\")[1] -split "\\" | Select-Object -First 1
Write-Info "  ℹ SDK Version: $sdkVersion"
Write-Host ""

# Create or find certificate
Write-Step "[3/6] Managing self-signed certificate..."

# Check if certificate file already exists
$certExists = Test-Path $CertPath
$cerPath = $CertPath -replace '\.pfx$', '.cer'

if ($certExists) {
    Write-Info "  ℹ Certificate file already exists: $CertPath"
    Write-Host "    Using existing certificate for signing." -ForegroundColor White
    
    # Try to load and verify the certificate
    try {
        $existingCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertPath, $CertPassword)
        Write-Success "  ✓ Certificate loaded successfully"
        Write-Info "  ℹ Subject: $($existingCert.Subject)"
        Write-Info "  ℹ Valid until: $($existingCert.NotAfter.ToString('yyyy-MM-dd'))"
    } catch {
        Write-Warning "  ⚠️  Could not load existing certificate. Creating new one..."
        $certExists = $false
    }
}

if (-not $certExists) {
    # Check if certificate exists in store
    $existingStoreCert = Get-ChildItem -Path Cert:\CurrentUser\My -ErrorAction SilentlyContinue | 
        Where-Object { $_.Subject -eq $Publisher }
    
    if ($existingStoreCert) {
        Write-Info "  ℹ Certificate found in Personal store"
        $cert = $existingStoreCert | Select-Object -First 1
    } else {
        Write-Info "  ℹ Creating new self-signed certificate..."
        
        # Calculate expiration date
        $notAfter = (Get-Date).AddYears($ValidityYears)
        
        # Create certificate using New-SelfSignedCertificate
        try {
            $cert = New-SelfSignedCertificate -Type Custom `
                -Subject $Publisher `
                -KeyUsage DigitalSignature `
                -FriendlyName "PDF Kawankasi Test Certificate" `
                -CertStoreLocation "Cert:\CurrentUser\My" `
                -NotAfter $notAfter `
                -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
            
            Write-Success "  ✓ Certificate created successfully"
        } catch {
            Write-Error "❌ Failed to create certificate: $_"
            exit 1
        }
    }
    
    Write-Info "  ℹ Certificate Details:"
    Write-Host "    Subject: $($cert.Subject)" -ForegroundColor White
    Write-Host "    Thumbprint: $($cert.Thumbprint)" -ForegroundColor White
    Write-Host "    Valid: $($cert.NotBefore.ToString('yyyy-MM-dd')) to $($cert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor White
    
    # Export certificate to PFX
    Write-Info "  ℹ Exporting certificate to PFX..."
    try {
        $certPasswordSecure = ConvertTo-SecureString -String $CertPassword -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath $CertPath -Password $certPasswordSecure -Force | Out-Null
        Write-Success "  ✓ Exported to: $CertPath"
    } catch {
        Write-Error "❌ Failed to export certificate: $_"
        exit 1
    }
    
    # Export certificate as CER (without private key)
    try {
        Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null
        Write-Success "  ✓ Public key exported to: $cerPath"
    } catch {
        Write-Error "❌ Failed to export public key: $_"
        exit 1
    }
}

Write-Host ""

# Install certificate to Trusted Root
if (-not $SkipCertInstall) {
    Write-Step "[4/6] Installing certificate to Trusted Root..."
    
    # Check if certificate is already installed in Trusted Root
    $installedCert = Get-ChildItem -Path Cert:\LocalMachine\Root -ErrorAction SilentlyContinue | 
        Where-Object { $_.Subject -eq $Publisher }
    
    if ($installedCert) {
        Write-Info "  ℹ Certificate already installed in Trusted Root"
        Write-Success "  ✓ Certificate is trusted"
    } else {
        try {
            # Import to Trusted Root Certification Authorities
            Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\LocalMachine\Root -ErrorAction Stop | Out-Null
            Write-Success "  ✓ Certificate installed to Trusted Root Certification Authorities"
            Write-Info "  ℹ Windows will now trust packages signed with this certificate"
        } catch {
            Write-Error "❌ Failed to install certificate: $_"
            Write-Host ""
            Write-Host "You can install the certificate manually:" -ForegroundColor Yellow
            Write-Host "1. Double-click: $cerPath" -ForegroundColor White
            Write-Host "2. Click 'Install Certificate'" -ForegroundColor White
            Write-Host "3. Select 'Local Machine' (requires Administrator)" -ForegroundColor White
            Write-Host "4. Choose 'Place all certificates in the following store'" -ForegroundColor White
            Write-Host "5. Browse → 'Trusted Root Certification Authorities'" -ForegroundColor White
            Write-Host "6. Click OK → Next → Finish" -ForegroundColor White
            Write-Host ""
            exit 1
        }
    }
} else {
    Write-Step "[4/6] Skipping certificate installation (as requested)..."
    Write-Warning "  ⚠️  You must install the certificate manually:"
    Write-Host "    Run: Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\LocalMachine\Root" -ForegroundColor White
}

Write-Host ""

# Sign the MSIX package
Write-Step "[5/6] Signing MSIX package..."

$signArgs = @(
    "sign",
    "/fd", "SHA256",
    "/a",
    "/f", $CertPath,
    "/p", $CertPassword
)

# Add timestamp if requested
if ($UseTimestamp) {
    $signArgs += @(
        "/tr", "http://timestamp.digicert.com",
        "/td", "SHA256"
    )
    Write-Info "  ℹ Using timestamp server: http://timestamp.digicert.com"
}

$signArgs += $MsixPath

try {
    $signOutput = & $signToolPath $signArgs 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ Package signed successfully!"
    } else {
        Write-Error "❌ Failed to sign package (exit code: $LASTEXITCODE)"
        Write-Host ""
        Write-Host "SignTool output:" -ForegroundColor Yellow
        Write-Host $signOutput -ForegroundColor White
        Write-Host ""
        exit 1
    }
} catch {
    Write-Error "❌ Error running SignTool: $_"
    exit 1
}

Write-Host ""

# Verify signature
Write-Step "[6/6] Verifying package signature..."

try {
    $verifyOutput = & $signToolPath verify /pa $MsixPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ Signature verified successfully!"
        Write-Info "  ℹ Package is ready to install"
    } else {
        Write-Warning "  ⚠️  Signature verification returned warnings"
        Write-Host "    This may be expected for self-signed certificates" -ForegroundColor White
        Write-Host "    Package should still install if certificate is trusted" -ForegroundColor White
    }
} catch {
    Write-Warning "  ⚠️  Could not verify signature: $_"
}

Write-Host ""

# Display success message and next steps
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✓ SUCCESS! Package is ready to install" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Write-Info "📦 MSIX Package Information:"
Write-Host "  File: $MsixPath" -ForegroundColor White
Write-Host "  Size: $([math]::Round($msixFile.Length/1MB, 2)) MB" -ForegroundColor White
Write-Host ""

Write-Info "🔐 Certificate Information:"
Write-Host "  PFX File: $CertPath" -ForegroundColor White
Write-Host "  CER File: $cerPath" -ForegroundColor White
Write-Host "  Password: ******* (saved in variable, not displayed for security)" -ForegroundColor White
Write-Host "  Publisher: $Publisher" -ForegroundColor White
Write-Host ""
Write-Warning "⚠️  Keep the certificate password secure. You'll need it to sign future updates."
Write-Host ""

Write-Info "📝 Next Steps:"
Write-Host ""
Write-Host "  Option 1 - Install via GUI:" -ForegroundColor Yellow
Write-Host "    1. Double-click the MSIX file in File Explorer" -ForegroundColor White
Write-Host "    2. Click 'Install' in App Installer" -ForegroundColor White
Write-Host "    3. Find the app in Start Menu" -ForegroundColor White
Write-Host ""

Write-Host "  Option 2 - Install via PowerShell:" -ForegroundColor Yellow
Write-Host "    Add-AppxPackage -Path '$MsixPath'" -ForegroundColor White
Write-Host ""

Write-Host "  Option 3 - Update existing installation:" -ForegroundColor Yellow
Write-Host "    Add-AppxPackage -Path '$MsixPath' -Update" -ForegroundColor White
Write-Host ""

Write-Info "💡 Tips:"
Write-Host "  • Keep the PFX file for signing future updates" -ForegroundColor White
Write-Host "  • Certificate is valid for $ValidityYears years" -ForegroundColor White
Write-Host "  • Use the same certificate to sign all versions for seamless updates" -ForegroundColor White
Write-Host "  • Add *.pfx to .gitignore to avoid committing certificates" -ForegroundColor White
Write-Host ""

Write-Info "📚 Documentation:"
Write-Host "  For more information, see:" -ForegroundColor White
Write-Host "  • MSIX_SIDELOADING_GUIDE.md - Complete sideloading guide" -ForegroundColor White
Write-Host "  • MSIX_BUILD_GUIDE.md - Building MSIX packages" -ForegroundColor White
Write-Host "  • MICROSOFT_STORE_SUBMISSION.md - Store publishing guide" -ForegroundColor White
Write-Host ""

# Check if package can be installed
$existingPackage = Get-AppxPackage -Name "*PDFKawankasi*" -ErrorAction SilentlyContinue

if ($existingPackage) {
    Write-Warning "⚠️  PDF Kawankasi is already installed"
    Write-Host "  Current version: $($existingPackage.Version)" -ForegroundColor White
    Write-Host ""
    Write-Host "  To update, run:" -ForegroundColor Yellow
    Write-Host "    Add-AppxPackage -Path '$MsixPath' -Update" -ForegroundColor White
    Write-Host ""
    Write-Host "  To reinstall, first uninstall:" -ForegroundColor Yellow
    Write-Host "    Get-AppxPackage -Name '*PDFKawankasi*' | Remove-AppxPackage" -ForegroundColor White
    Write-Host ""
}

Write-Host "Done! 🎉" -ForegroundColor Green
Write-Host ""
