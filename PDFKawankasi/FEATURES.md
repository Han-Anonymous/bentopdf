# PDF Kawankasi Features Documentation

## Continuous Scrolling Feature

### Overview
Continuous scrolling allows users to seamlessly navigate through PDF pages by scrolling, similar to modern PDF readers like Adobe Acrobat and web browsers.

### How It Works

#### User Experience
1. Open a multi-page PDF document in the PDF Editor
2. Click the continuous scroll toggle button (📜 icon) in the toolbar
3. When continuous scrolling is enabled:
   - Scroll down at the bottom of a page → automatically moves to the next page and scrolls to the top
   - Scroll up at the top of a page → automatically moves to the previous page and scrolls to the bottom
   - Status message confirms: "Continuous scrolling enabled"

#### Technical Implementation
The continuous scrolling feature is implemented through the following components:

**1. UI Toggle Button** (`PdfEditorView.xaml` line 283-289)
```xaml
<ToggleButton Content="📜"
              Style="{StaticResource ToolToggleButtonStyle}"
              IsChecked="{Binding IsContinuousScrollMode}"
              ToolTip="Continuous scrolling mode"
              IsEnabled="{Binding IsPdfLoaded}"/>
```

**2. Mouse Wheel Handler** (`PdfEditorView.xaml.cs` line 162-234)
- Detects when user is scrolling at page boundaries
- Automatically navigates to next/previous page
- Preserves scroll position (scrolls to top when going forward, bottom when going backward)
- Saves and restores page state (ink strokes, images, text boxes)

**3. State Management** (`PdfEditorViewModel.cs`)
- `IsContinuousScrollMode` property tracks the enabled/disabled state
- Persists across page navigation
- Integrated with existing page navigation commands

### Benefits
- Natural reading experience for long documents
- No need to manually click next/previous buttons
- Smooth transition between pages
- Maintains annotation state during navigation

### Future Enhancements
Potential improvements for continuous scrolling:
- Virtual scrolling: Display multiple pages in a single scrollable view
- Smooth page transitions with animations
- Configurable scroll sensitivity
- Page pre-loading for faster navigation

---

## PDF File Association

### Overview
PDF Kawankasi can be set as the default application for opening PDF files in Windows. When configured, double-clicking any PDF file in File Explorer will automatically open it in PDF Kawankasi.

### How It Works

#### Setting as Default PDF Viewer
1. Launch PDF Kawankasi
2. Go to **File** menu → **Register as Default PDF Viewer**
3. Windows Settings will open to the Default Apps page
4. Find ".pdf" in the file type list
5. Select "PDF Kawankasi" as the default app
6. All PDF files will now open in PDF Kawankasi

#### Opening PDF Files
Once configured as the default viewer:
- **Double-click** any PDF file in File Explorer → Opens in new tab in PDF Kawankasi
- **Right-click** PDF file → "Open with" → PDF Kawankasi
- **Multiple files**: Select multiple PDFs and press Enter → Each opens in a separate tab

### Technical Implementation

**1. File Type Association Declaration** (`Package.appxmanifest` line 52-61)
```xml
<Extensions>
  <uap:Extension Category="windows.fileTypeAssociation">
    <uap:FileTypeAssociation Name="pdfkawankasi">
      <uap:DisplayName>PDF Kawankasi</uap:DisplayName>
      <uap:Logo>Assets\Square44x44Logo.png</uap:Logo>
      <uap:SupportedFileTypes>
        <uap:FileType ContentType="application/pdf">.pdf</uap:FileType>
      </uap:SupportedFileTypes>
    </uap:FileTypeAssociation>
  </uap:Extension>
</Extensions>
```

**2. Command-Line Argument Handling** (`App.xaml.cs` line 36-47)
- Filters command-line arguments for PDF files
- Validates file existence
- Stores file paths in `Application.Current.Properties["PdfFilesToOpen"]`
- Defers opening until MainWindow is fully initialized

**3. Tab-Based Opening** (`MainWindow.xaml.cs` line 37-50, 287-314)
- `MainWindow_Loaded` event retrieves stored PDF file paths
- `OpenPdfInNewTab` creates a new tab for each PDF file
- Each tab gets its own `PdfEditorView` instance
- PDF is loaded automatically when the view is initialized

### Benefits
- Seamless integration with Windows File Explorer
- Support for opening multiple PDFs simultaneously
- Each PDF opens in its own tab for easy switching
- Preserves existing tabs when opening new files
- Professional user experience matching native Windows apps

### User Settings Location
Windows stores default app associations at:
- **Windows 10/11**: Settings → Apps → Default apps → Choose default apps by file type

### Troubleshooting
If PDF files don't open in PDF Kawankasi:
1. Verify the app is installed as an MSIX package
2. Check that file association is correctly set in Windows Settings
3. Restart File Explorer (`taskkill /f /im explorer.exe` then `start explorer.exe`)
4. Reinstall the app if the association is broken

---

## Tab Management

### Features
- **New Tab**: Ctrl+T or click the "+" button
- **Close Tab**: Ctrl+W or click the "✕" button on tab
- **Switch Tabs**: Click on tab headers
- **Drag to Reorder**: Click and drag tabs to reorder them
- **Unsaved Changes Warning**: Prompted when closing tabs with unsaved changes

### Tab Behavior
- Minimum 1 tab (last tab cannot be closed)
- Each tab maintains independent state:
  - Loaded PDF document
  - Current page number
  - Zoom level
  - Annotation data (strokes, images, text boxes)
  - Undo/redo history
  - Tool selection

---

## Integration Points

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open PDF |
| Ctrl+S | Save PDF |
| Ctrl+Shift+S | Save As |
| Ctrl+T | New Tab |
| Ctrl+W | Close Tab |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl++ | Zoom In |
| Ctrl+- | Zoom Out |
| Ctrl+0 | Reset Zoom |
| F11 | Fullscreen |
| Esc | Exit Fullscreen |

### Menu Integration
- File menu includes "Register as Default PDF Viewer" option
- Recent documents list for quick access
- Tools menu for PDF manipulation operations

---

## Developer Notes

### Testing Continuous Scrolling
1. Open a multi-page PDF (at least 3-4 pages)
2. Enable continuous scrolling mode
3. Scroll down to bottom of page 1 → Should auto-navigate to page 2 at top
4. Scroll up to top of page 2 → Should auto-navigate to page 1 at bottom
5. Verify annotations are preserved during navigation
6. Test with different zoom levels
7. Test with dual-page mode (should work independently)

### Testing File Association
1. Build and install the app as MSIX package
2. Run "Register as Default PDF Viewer" from File menu
3. Set PDF Kawankasi as default in Windows Settings
4. Double-click a PDF file in File Explorer
5. Verify it opens in PDF Kawankasi in a new tab
6. Test opening multiple PDFs simultaneously
7. Verify each PDF opens in its own tab
8. Test that command-line flags (--convert-logo, --test-svg) still work

### Build Requirements
- Must be built as MSIX package for file association to work
- Package.appxmanifest must be properly signed
- App must be installed for current user or all users
- Windows 10 version 1809 (17763) or later

---

## Known Limitations

### Continuous Scrolling
- Only works in single-page view mode
- Requires PDF to be loaded
- Page changes use discrete navigation (not virtual scrolling)
- Performance may vary with very large PDFs (100+ pages)

### File Association
- Requires MSIX package installation (not available for non-packaged builds)
- User must manually set as default (cannot be forced programmatically)
- File association only works for .pdf extension
- Command-line arguments limited to file paths (no advanced parameters)

---

## Future Improvements

### Continuous Scrolling Enhancements
- [ ] Virtual scrolling with all pages in single view
- [ ] Smooth scroll animations between pages
- [ ] Page preloading for instant navigation
- [ ] Configurable scroll behavior settings
- [ ] Support for dual-page continuous scrolling

### File Association Enhancements
- [ ] Open PDF at specific page via command-line (e.g., `file.pdf#page=5`)
- [ ] Support for PDF bookmarks in navigation
- [ ] Remember last position in recently opened files
- [ ] Drag-and-drop multiple PDFs onto application window
- [ ] Context menu integration ("Open in PDF Kawankasi")

### General Enhancements
- [ ] Settings page for user preferences
- [ ] Customizable keyboard shortcuts
- [ ] Session restoration (reopen tabs from last session)
- [ ] Tab groups/organization
- [ ] Split view for comparing PDFs side-by-side

---

<p align="center">
  <strong>PDF Kawankasi</strong> - Privacy-First PDF Toolkit for Windows<br>
  Made with ❤️ for PDF lovers everywhere
</p>
