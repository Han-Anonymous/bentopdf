# PDFKawankasi - AI Coding Agent Instructions

## Project Overview
PDFKawankasi is a **privacy-first, offline PDF toolkit** for Windows, packaged as an MSIX app for Microsoft Store distribution. Built with WPF (.NET 8) using MVVM architecture, it provides 50+ PDF manipulation tools with a tabbed PDF editor interface.

## Core Architecture

### Tech Stack
- **Framework**: WPF with .NET 8 (Windows 10.0.19041.0+)
- **UI**: MaterialDesignThemes for modern Material Design styling
- **MVVM**: CommunityToolkit.Mvvm for observable properties and commands
- **PDF Libraries**: 
  - `PdfSharpCore` (primary) - page manipulation, merging, splitting
  - `iText7` - advanced operations, image-to-PDF conversion
  - `Docnet.Core` - PDF rendering for thumbnails
- **SVG**: SharpVectors.Wpf for vector graphics
- **Packaging**: MSIX with multi-architecture support (x64, x86, ARM64)

### Key Architectural Patterns

#### 1. Dual-Mode Application Structure
The app has TWO operational modes accessed through a single tool grid:
- **Tool Mode** (default): 50+ PDF tools accessed via [MainViewModel](MainViewModel.cs) → each tool processes files with [PdfService](Services/PdfService.cs)
- **PDF Editor Mode**: Full-featured PDF editor with tabs, accessed via [PdfEditorViewModel](ViewModels/PdfEditorViewModel.cs)

**Critical**: When `SelectedTool.ToolType == ToolType.PdfEditor`, the app switches to tabbed editor mode in [MainWindow](Views/MainWindow.xaml.cs).

#### 2. Tool Type System
All 50+ tools are defined in [ToolsService](Services/ToolsService.cs) using the `ToolType` enum from [Models/PdfTool.cs](Models/PdfTool.cs). Tools are organized into categories: Popular, Edit & Annotate, Convert to PDF, Convert from PDF, Organize & Manage, Optimize & Repair, Secure PDF.

**Pattern**: Tools in "Popular" category are **references** to tools in other categories (mirrors BentoPDF web app design).

#### 3. Working Copy Pattern (Excel-style)
[PdfWorkingCopyService](Services/PdfWorkingCopyService.cs) creates temporary copies in `%TEMP%\PDFKawankasi\` for all edits. Changes accumulate in the working copy until explicit Save/Save As. This prevents data loss and supports undo workflows.

**Usage**: All `PdfEditorViewModel` operations use `CurrentFilePath` from working copy service, NOT original file paths.

#### 4. Single Instance Architecture
[App.xaml.cs](App.xaml.cs) enforces single instance using named mutex. When a second instance launches (e.g., double-clicking a PDF):
- Uses named pipe IPC (`PDFKawankasi_IPC_Pipe`) to send file path to first instance
- First instance opens PDF in new tab via `OpenPdfInNewTab()` in [MainWindow.xaml.cs](Views/MainWindow.xaml.cs)
- Second instance terminates

**Critical**: Named pipe listener runs on background thread; UI updates use `Dispatcher.Invoke()`.

#### 5. Library Alias Strategy
To avoid namespace conflicts between PdfSharpCore and iText7 (both have `PdfDocument` classes):
```csharp
using PdfSharpDocument = PdfSharpCore.Pdf.PdfDocument;
using PdfSharpReader = PdfSharpCore.Pdf.IO.PdfReader;
```
**Always use aliases** in [PdfService.cs](Services/PdfService.cs) to prevent ambiguity.

## Critical Development Workflows

### Building & Packaging
**DO NOT** use `dotnet publish` directly. The project uses MSIX packaging:
```powershell
# For local testing
msbuild PDFKawankasi.Package.wapproj /p:Configuration=Debug /p:Platform=x64

# For Store submission (creates .msixupload bundle)
msbuild PDFKawankasi.Package.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxBundle=Always `
  /p:AppxBundlePlatforms="x86|x64|ARM64"
```
Output: `PDFKawankasi.Package/AppPackages/`

See [PDFKawankasi.Package/README.md](PDFKawankasi.Package/README.md) for details.

### Running/Debugging
- **In Visual Studio**: Set `PDFKawankasi.Package` as startup project, select platform (x64 recommended), press F5
- **Command-line args**:
  - `--convert-logo`: SVG→PNG→ICO conversion for app icons
  - `--test-svg`: Opens SVG test window
  - `file.pdf`: Opens PDF in editor (file association)
- **Global shortcut**: `Ctrl+Shift+T` opens SVG test window from any window

### Testing File Association
After installing MSIX package:
1. Right-click any PDF → Open With → Choose Another App
2. Select PDFKawankasi → Set as default
3. Double-click PDF should open in new tab via single instance IPC

## Project-Specific Conventions

### MVVM Binding Pattern
Use `CommunityToolkit.Mvvm` source generators:
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _myProperty; // Generates public MyProperty
    
    [RelayCommand]
    private void DoSomething() { } // Generates DoSomethingCommand
}
```

### PDF Operation Return Types
- **Transformation operations** (merge, split, rotate): Return `Task<byte[]>` 
- **Query operations** (metadata, page count): Return synchronously
- **Multi-file splits**: Return `Task<List<byte[]>>` for separate files
- **Progress reporting**: Use `IProgress<int>` parameter (0-100)

**Pattern**: All async operations wrap synchronous PdfSharp/iText code in `Task.Run()` to keep UI responsive.

### Thumbnail Rendering
[PdfService.GetPagePreviews()](Services/PdfService.cs) uses Docnet for rendering. Returns `List<PagePreviewModel>` with `BitmapSource` thumbnails. Must call `bitmap.Freeze()` for cross-thread safety.

### Placeholder Implementations
Several [PdfService](Services/PdfService.cs) methods are **placeholders** (documented in XML comments):
- `PdfToGreyscaleAsync`: Needs iText content stream manipulation
- `InvertColorsAsync`: Requires low-level PDF operator parsing
- `RemoveAnnotationsAsync`, `RemoveBlankPagesAsync`: Need page dictionary access
- `PdfToJpgAsync`, `PdfToPngAsync`: Need rendering library (SkiaSharp/PDFium suggested)

**When extending**: Check XML docs for implementation guidance. Use iText7 for advanced operations.

### MaterialDesign Integration
UI uses [MaterialDesignThemes](https://materialdesigninxaml.net/):
- Dark theme via `materialDesign:ThemeAssist.Theme="Dark"`
- Icons: `materialDesign:PackIcon Kind="IconName"`
- Inputs: `Style="{StaticResource MaterialDesignOutlinedTextBox}"`

Example: [PrintWindow.xaml](Views/PrintWindow.xaml)

## Integration Points

### Windows APIs
- **File Dialogs**: Use `Microsoft.WindowsAPICodePack-Shell` (`CommonOpenFileDialog`) for folder picker (Store-compatible)
- **Settings**: Launch via `ms-settings:defaultapps` URI with `Windows.System.Launcher.LaunchUriAsync()` (see [MainWindow.xaml.cs](Views/MainWindow.xaml.cs))
- **IPC**: Named pipes (`System.IO.Pipes`) for single instance communication
- **Window Management**: P/Invoke `user32.dll` (`SetForegroundWindow`, `ShowWindow`) for focus/restore

### Asset Management
All assets in [Assets/](Assets/) are embedded as `Resource` in [PDFKawankasi.csproj](PDFKawankasi.csproj):
```xml
<Resource Include="Assets\**\*.svg" />
<Resource Include="Assets\**\*.png" />
```
Reference in XAML: `pack://application:,,,/Assets/folder/file.svg`

### Package Manifest
[Package.appxmanifest](Package.appxmanifest) defines file associations, capabilities, and visual assets. **Must update `Identity` and `Publisher` after Store association** in Visual Studio (Project → Publish → Associate App with Store).

## Common Pitfalls

1. **ImageSharp Version Lock**: MUST use `SixLabors.ImageSharp 1.0.4` (not 2.x/3.x) for PdfSharpCore compatibility. Newer versions break.

2. **Async/Await in MVVM Commands**: Always use `RelayCommand` (sync) or async method directly. DO NOT use `async void`:
   ```csharp
   [RelayCommand]
   private async Task ProcessFilesAsync() { ... }
   ```

3. **Tab Closing Safety**: [MainWindow](Views/MainWindow.xaml.cs) `CloseTab()` checks `HasPendingChanges` before closing. Never remove this check.

4. **Working Copy Cleanup**: Always dispose `PdfWorkingCopyService` or call `DiscardWorkingCopy()` when closing documents to prevent temp file buildup.

5. **Thread Safety for Thumbnails**: Docnet rendering happens on background thread. Always `Freeze()` BitmapSource before adding to ObservableCollection.

6. **Split Tool Single File Mode**: When `ToolType == ToolType.Split`, ONLY allow single file selection and show page previews. See [MainViewModel.AddFiles](ViewModels/MainViewModel.cs).

## File Organization

- **ViewModels/**: MVVM view models (MainViewModel, PdfEditorViewModel, etc.)
- **Views/**: XAML views and code-behind (MainWindow, PdfEditorView, etc.)
- **Services/**: Business logic (PdfService, PdfWorkingCopyService, ToolsService, RecentDocumentsService)
- **Models/**: Data models (PdfTool, AnnotationModel, PagePreviewModel, etc.)
- **Converters/**: XAML value converters
- **Assets/**: Images, icons, SVG files (embedded as resources)
- **PDFKawankasi.Package/**: MSIX packaging project

## Privacy-First Principle

**ALL PDF processing happens locally**. Never add cloud features, telemetry, or external API calls without explicit user consent. This is the app's core value proposition.
