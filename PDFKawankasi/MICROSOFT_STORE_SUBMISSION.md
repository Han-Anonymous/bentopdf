# Microsoft Store Submission Guide for PDF Kawankasi

This guide provides step-by-step instructions for submitting PDF Kawankasi to the Microsoft Store, leveraging Microsoft Learn documentation and best practices.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Partner Center Setup](#partner-center-setup)
3. [App Identity Configuration](#app-identity-configuration)
4. [Building the MSIX Package](#building-the-msix-package)
5. [Store Listing Preparation](#store-listing-preparation)
6. [Submission Process](#submission-process)
7. [Post-Submission](#post-submission)
8. [Microsoft Learn Resources](#microsoft-learn-resources)

## Prerequisites

### Developer Account
- **Microsoft Partner Center account** (required)
  - Individual account: $19 one-time registration fee
  - Company account: $99 one-time registration fee
  - Sign up at: https://partner.microsoft.com/dashboard/registration

### Development Environment
- ✅ Windows 10/11 with Developer Mode enabled
- ✅ Visual Studio 2022 with UWP development workload
- ✅ Windows SDK 10.0.22621.0 or later
- ✅ Valid code signing certificate (for Store, Microsoft provides automatic signing)

### App Requirements
- ✅ Follows Microsoft Store Policies: https://learn.microsoft.com/windows/apps/publish/store-policies
- ✅ Passes Windows App Certification Kit (WACK)
- ✅ Privacy policy URL (required for apps that access personal information)
- ✅ Age rating completed
- ✅ All required assets and metadata

## Partner Center Setup

### Step 1: Create App Reservation

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard)
2. Navigate to **Apps and games** → **Overview**
3. Click **+ New product** → **MSIX or PWA app**
4. Enter your app name: **PDF Kawankasi**
5. Check name availability
6. Reserve the name (valid for 3 months)

**Microsoft Learn Reference:**
- [Create an app by reserving a name](https://learn.microsoft.com/windows/apps/publish/create-app-submission)

### Step 2: Configure App Properties

1. In Partner Center, go to **Product management** → **Properties**
2. Set **Category**: Productivity
3. Set **Subcategory**: Office tools or Document management
4. Select **Privacy policy URL**: (provide your privacy policy URL)
5. Set **System requirements** (optional but recommended):
   - Minimum OS: Windows 10 version 1809 (17763)
   - Recommended OS: Windows 11
6. Save changes

**Microsoft Learn Reference:**
- [Enter app properties](https://learn.microsoft.com/windows/apps/publish/enter-app-properties)

## App Identity Configuration

### Get Publisher Information

After creating your app in Partner Center:

1. Go to **Product management** → **Product identity**
2. Note down:
   - **Package/Identity/Name**: (e.g., `12345PublisherName.PDFKawankasi`)
   - **Package/Identity/Publisher**: (e.g., `CN=12345678-ABCD-EFGH-IJKL-123456789012`)
   - **Package ID**: (for reference)

### Associate App in Visual Studio

1. Open `PDFKawankasi.sln` in Visual Studio 2022
2. Right-click **PDFKawankasi.Package** project
3. Select **Publish** → **Associate App with the Store**
4. Sign in with your Partner Center credentials
5. Select **PDF Kawankasi** from the list
6. Click **Associate**

This automatically updates `Package.appxmanifest` with:
- Correct Identity Name
- Publisher certificate DN
- Package family name

**Microsoft Learn Reference:**
- [Package a desktop or UWP app in Visual Studio](https://learn.microsoft.com/windows/msix/package/packaging-uwp-apps)

## Building the MSIX Package

### Pre-Build Validation

Run the Windows App Certification Kit (WACK) before creating Store package:

1. Build your app in Release mode
2. Deploy locally
3. Run WACK:
   ```
   Start → Windows Kits → Windows App Cert Kit
   ```
4. Select your installed app
5. Run tests (takes 10-15 minutes)
6. Review results and fix any issues

**Microsoft Learn Reference:**
- [Windows App Certification Kit](https://learn.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit)

### Create Store Package

#### Option A: Using Visual Studio (Recommended)

1. Open `PDFKawankasi.sln`
2. Right-click **PDFKawankasi.Package** project
3. Select **Publish** → **Create App Packages**
4. Select **Microsoft Store using an existing app name**
5. Sign in and select your app
6. Configure packages:
   - ✅ Generate app bundle: Always
   - ✅ Include public symbol files: Yes (for debugging crash reports)
   - Architecture selection:
     - ✅ x86 (32-bit Intel/AMD)
     - ✅ x64 (64-bit Intel/AMD)
     - ✅ ARM64 (ARM processors like Surface Pro X)
7. Click **Create**
8. Optionally run WACK tests
9. Note the output location

**Output:** `PDFKawankasi.Package_{version}_x86_x64_ARM64.msixupload`

#### Option B: Using MSBuild

```powershell
cd PDFKawankasi
msbuild PDFKawankasi\PDFKawankasi.Package\PDFKawankasi.Package.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64" `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile="PDFKawankasi.Package_TemporaryKey.pfx"
```

**Microsoft Learn Reference:**
- [Create an MSIX package from a desktop installer](https://learn.microsoft.com/windows/msix/packaging-tool/create-app-package)
- [Package a desktop app using MSBuild](https://learn.microsoft.com/windows/msix/desktop/source-code-overview)

## Store Listing Preparation

### Required Assets

Prepare the following assets for your Store listing:

#### App Screenshots (Required - at least 1)
- **Recommended size**: 1366 x 768 or larger
- **Aspect ratio**: 16:9 or 16:10
- **Format**: PNG or JPG
- **Minimum**: 1 screenshot
- **Maximum**: 10 screenshots per language
- Show key features of PDF Kawankasi:
  - Main interface with PDF open
  - Editing features (annotations, highlights)
  - Merge/split functionality
  - Settings panel

#### Store Logos (Required)
All are already created in `PDFKawankasi/Assets/`:
- ✅ `Square150x150Logo.png` (150x150)
- ✅ `Square44x44Logo.png` (44x44)
- ✅ `Square71x71Logo.png` (71x71)
- ✅ `Square310x310Logo.png` (310x310)
- ✅ `Wide310x150Logo.png` (310x150)
- ✅ `StoreLogo.png` (50x50)

#### Promotional Images (Optional but recommended)
- **Hero image**: 1920 x 1080 (for Store feature placement)
- **Promotional art**: 2400 x 1200 (for marketing)

### Description Content

#### App Title
**PDF Kawankasi** (max 256 characters)

#### Short Description (Recommended)
```
Privacy-first PDF toolkit for Windows. Edit, merge, split, and annotate PDFs locally on your device.
```
(Max 200 characters)

#### Full Description
```markdown
PDF Kawankasi is a powerful, privacy-first PDF toolkit designed exclusively for Windows. Process your PDF files with complete confidence knowing that everything happens locally on your device - no uploads, no servers, no compromises.

🔒 PRIVACY FIRST
• All processing happens on your device
• No internet connection required
• Your files never leave your computer
• No tracking or telemetry

✨ POWERFUL FEATURES
• Edit PDFs with Windows Ink support (pen and touch)
• Annotate and highlight with natural handwriting
• Merge multiple PDFs into one
• Split PDFs by page ranges
• Reorder and organize pages
• Add text and images
• Fill forms electronically

🎨 MODERN INTERFACE
• Native Windows 11 design
• Dark mode support
• Touch and pen optimized
• Multiple tabs for multitasking
• Drag-and-drop file handling

⚡ FAST & EFFICIENT
• Native .NET performance
• Handles large PDF files smoothly
• Low memory footprint
• Instant processing

📱 WINDOWS INTEGRATION
• Set as default PDF viewer
• Open PDFs from File Explorer
• Windows Ink and touch support
• Keyboard shortcuts for productivity

Perfect for students, professionals, and anyone who values their privacy while working with PDF documents.
```

#### Keywords (Maximum 7)
1. PDF editor
2. PDF viewer
3. Privacy
4. Offline
5. Annotation
6. Windows Ink
7. Document

**Microsoft Learn Reference:**
- [Create app Store listings](https://learn.microsoft.com/windows/apps/publish/create-app-store-listings)

### Age Ratings

Complete the age rating questionnaire:

1. In Partner Center, go to **Age ratings**
2. Answer questionnaire honestly
3. For PDF Kawankasi, likely ratings:
   - IARC: 3+ (Everyone)
   - ESRB: Everyone
   - PEGI: 3

**Microsoft Learn Reference:**
- [Age ratings](https://learn.microsoft.com/windows/apps/publish/age-ratings)

### Pricing and Availability

1. Navigate to **Pricing and availability**
2. Select markets (countries/regions) where app will be available
3. Set pricing:
   - **Free** (recommended for initial release)
   - Or set price tier
4. Set release date:
   - **As soon as possible after certification**
   - Or schedule specific date

**Microsoft Learn Reference:**
- [Set app pricing and availability](https://learn.microsoft.com/windows/apps/publish/set-app-pricing-and-availability)

## Submission Process

### Step 1: Upload Package

1. In Partner Center, navigate to your app submission
2. Go to **Packages** section
3. Drag and drop or browse for:
   - `PDFKawankasi.Package_{version}_x86_x64_ARM64.msixupload`
4. Wait for package validation (1-5 minutes)
5. Verify package details:
   - Version number
   - Supported architectures
   - Capabilities
   - File associations

### Step 2: Complete Store Listing

1. **Store listings** section
2. Add for each supported language (start with English (United States))
3. Fill in:
   - Description
   - Screenshots
   - Release notes
   - Keywords

### Step 3: Notes for Certification (Optional)

Provide any special instructions for Microsoft testers:

```
TESTING NOTES:

1. App is a desktop PDF editor and viewer
2. No account or sign-in required
3. To test core features:
   - Open any PDF file via File menu or drag-and-drop
   - Test annotation tools in the toolbar
   - Test file operations (merge, split) in File menu

4. Privacy note: All operations are local, no network calls except for optional update checks

5. Test credentials: N/A (no login required)

Known limitations:
- Some complex PDF forms may not render perfectly (inherent to PDF standard)
- Very large PDFs (>500 pages) may take a few seconds to load
```

### Step 4: Submit for Certification

1. Review all sections for completeness
2. Click **Review and submit**
3. Review summary page
4. Click **Submit to the Store**

**Certification Timeline:**
- Initial review: 1-3 business days (sometimes faster)
- If issues found: You'll receive feedback and can resubmit
- Once approved: Published within 24 hours (unless scheduled)

**Microsoft Learn Reference:**
- [The app certification process](https://learn.microsoft.com/windows/apps/publish/app-certification-process)

## Post-Submission

### Monitor Certification Status

1. Check Partner Center dashboard regularly
2. You'll receive email notifications at:
   - Certification started
   - Certification passed/failed
   - App published

### Handle Certification Failures

If certification fails:

1. Read the certification report carefully
2. Common issues:
   - App crashes on launch
   - Missing privacy policy
   - Incorrect capabilities declared
   - Age rating issues
   - Store policy violations
3. Fix issues in your code/manifest
4. Rebuild package
5. Resubmit (no additional fee)

**Microsoft Learn Reference:**
- [Resolve package errors](https://learn.microsoft.com/windows/apps/publish/resolve-submission-errors)

### Post-Publication Checklist

After app is live:

- [ ] Verify app appears in Store search
- [ ] Test installation from Store
- [ ] Verify app metadata displays correctly
- [ ] Test update mechanism
- [ ] Monitor crash reports in Partner Center
- [ ] Respond to user reviews
- [ ] Monitor download analytics

### Updates and Maintenance

To publish updates:

1. Update version number in:
   - `Package.appxmanifest`: Increment Version attribute
   - `PDFKawankasi.csproj`: Update Version property
2. Build new MSIX package
3. Create new submission in Partner Center
4. Upload new package
5. Update release notes
6. Submit for certification

**Version numbering best practices:**
- Major.Minor.Build.Revision (e.g., 1.0.0.0)
- Increment Build for bug fixes
- Increment Minor for new features
- Increment Major for major releases

**Microsoft Learn Reference:**
- [Package versioning](https://learn.microsoft.com/windows/apps/publish/package-version-numbering)

## Microsoft Learn Resources

### Essential Documentation

#### Getting Started
- [Publish Windows apps](https://learn.microsoft.com/windows/apps/publish/)
- [Overview of the submission process](https://learn.microsoft.com/windows/apps/publish/publish-your-app/overview)

#### MSIX Packaging
- [What is MSIX?](https://learn.microsoft.com/windows/msix/overview)
- [Package a desktop application using Visual Studio](https://learn.microsoft.com/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)
- [MSIX packaging fundamentals](https://learn.microsoft.com/windows/msix/package/packaging-basics)

#### Store Policies
- [Microsoft Store Policies](https://learn.microsoft.com/windows/apps/publish/store-policies)
- [Store app quality guidelines](https://learn.microsoft.com/windows/apps/publish/store-app-quality)

#### Partner Center
- [Partner Center documentation](https://learn.microsoft.com/partner-center/)
- [Analytics for Windows apps](https://learn.microsoft.com/windows/apps/publish/analytics)

#### Testing and Certification
- [Windows App Certification Kit](https://learn.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit)
- [Certification requirements](https://learn.microsoft.com/windows/apps/publish/app-certification-requirements)

#### Advanced Topics
- [Microsoft Store submission API](https://learn.microsoft.com/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services)
- [Flighting (staged rollout)](https://learn.microsoft.com/windows/apps/publish/package-flights)
- [A/B testing](https://learn.microsoft.com/windows/apps/publish/a-b-testing)

### Video Tutorials

- [Publishing to Microsoft Store](https://learn.microsoft.com/shows/)
- [MSIX packaging tutorial](https://learn.microsoft.com/shows/one-dev-minute/what-is-msix)

### Community Resources

- [Microsoft Q&A - Windows Apps](https://learn.microsoft.com/answers/topics/windows-apps.html)
- [Windows Dev Center](https://developer.microsoft.com/windows/)
- [MSIX Tech Community](https://techcommunity.microsoft.com/t5/msix/ct-p/MSIX)

## Support and Troubleshooting

### Partner Center Support

- **In-app help**: Click ? icon in Partner Center
- **Support ticket**: Partner Center → Support → New support request
- **Phone support**: Available for account and payment issues

### Common Issues and Solutions

#### Issue: "Package validation failed"
**Solution**: Check package using WACK before upload. Fix any reported issues.

#### Issue: "Name already in use"
**Solution**: Choose different name or wait for expired reservation to become available.

#### Issue: "Publisher not recognized"
**Solution**: Ensure app is properly associated with Store in Visual Studio.

#### Issue: "Age rating incomplete"
**Solution**: Complete age rating questionnaire in Partner Center before submission.

#### Issue: "Privacy policy required"
**Solution**: Add privacy policy URL in Properties section.

## Checklist Before Submission

Use this checklist to ensure everything is ready:

### Technical
- [ ] App builds without errors in Release mode
- [ ] WACK tests pass with no errors
- [ ] App launches successfully when deployed
- [ ] All features work as expected
- [ ] App doesn't crash on startup or during normal use
- [ ] Package manifest has correct identity information
- [ ] Version number is correct and incremented
- [ ] All supported platforms are included (x86, x64, ARM64)

### Assets
- [ ] All required logos present and correct size
- [ ] At least 1 high-quality screenshot
- [ ] Icons display correctly in Start menu
- [ ] Splash screen shows correctly

### Metadata
- [ ] App name reserved in Partner Center
- [ ] Description written and proofread
- [ ] Keywords selected (max 7)
- [ ] Age rating completed
- [ ] Privacy policy URL provided
- [ ] Category and subcategory selected
- [ ] Pricing and markets configured

### Compliance
- [ ] App follows Store Policies
- [ ] No prohibited content
- [ ] Proper content ratings applied
- [ ] Privacy policy matches app behavior
- [ ] All capabilities justified and necessary

### Post-Submission
- [ ] Monitor certification status
- [ ] Respond to certification feedback promptly
- [ ] Plan for responding to user reviews
- [ ] Analytics tracking configured

---

## Quick Links

- **Partner Center**: https://partner.microsoft.com/dashboard
- **Microsoft Learn**: https://learn.microsoft.com/windows/apps/
- **Store Policies**: https://learn.microsoft.com/windows/apps/publish/store-policies
- **Developer Forums**: https://learn.microsoft.com/answers/topics/windows-apps.html

## Next Steps

1. ✅ **Complete**: MSIX packaging infrastructure is ready
2. 🔄 **Next**: Create Partner Center account and reserve app name
3. 🔄 **Next**: Associate app with Store in Visual Studio
4. 🔄 **Next**: Build Store package and test with WACK
5. 🔄 **Next**: Prepare screenshots and marketing materials
6. 🔄 **Next**: Submit to Microsoft Store

---

**Last Updated**: 2025-12-10
**Document Version**: 1.0
