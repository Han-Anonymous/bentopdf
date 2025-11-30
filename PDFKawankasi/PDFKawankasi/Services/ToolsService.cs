using PDFKawankasi.Models;

namespace PDFKawankasi.Services;

/// <summary>
/// Service that provides the list of available PDF tools.
/// Note: The Popular Tools category contains references to tools that also appear 
/// in their respective functional categories. This mirrors the BentoPDF web app design.
/// </summary>
public static class ToolsService
{
    /// <summary>
    /// Get all tool categories with their tools
    /// </summary>
    public static List<ToolCategoryModel> GetCategories()
    {
        return new List<ToolCategoryModel>
        {
            new ToolCategoryModel
            {
                Name = "Popular Tools",
                Tools = new List<PdfTool>
                {
                    // Popular tools are quick-access shortcuts to commonly used tools
                    new() { Id = "popular-merge", Name = "Merge PDF", Icon = "📄+", Subtitle = "Combine multiple PDFs into one file.", Category = "Popular", ToolType = ToolType.Merge },
                    new() { Id = "popular-pdf-editor", Name = "PDF Editor", Icon = "✏️", Subtitle = "Annotate, draw, highlight, redact, add comments and shapes, take screenshots, and view PDFs.", Category = "Popular", ToolType = ToolType.PdfEditor },
                    new() { Id = "popular-split", Name = "Split PDF", Icon = "✂️", Subtitle = "Extract a range of pages into a new PDF.", Category = "Popular", ToolType = ToolType.Split },
                    new() { Id = "popular-compress", Name = "Compress PDF", Icon = "⚡", Subtitle = "Reduce the file size of your PDF.", Category = "Popular", ToolType = ToolType.Compress },
                    new() { Id = "popular-jpg-to-pdf", Name = "JPG to PDF", Icon = "🖼️", Subtitle = "Create a PDF from one or more JPG images.", Category = "Popular", ToolType = ToolType.JpgToPdf },
                    new() { Id = "popular-extract", Name = "Extract Pages", Icon = "📑", Subtitle = "Save a selection of pages as new files.", Category = "Popular", ToolType = ToolType.ExtractPages },
                    new() { Id = "popular-delete", Name = "Delete Pages", Icon = "🗑️", Subtitle = "Remove specific pages from your document.", Category = "Popular", ToolType = ToolType.DeletePages },
                    new() { Id = "popular-rotate", Name = "Rotate PDF", Icon = "🔄", Subtitle = "Turn pages in 90-degree increments.", Category = "Popular", ToolType = ToolType.Rotate },
                }
            },
            new ToolCategoryModel
            {
                Name = "Edit & Annotate",
                Tools = new List<PdfTool>
                {
                    new() { Id = "pdf-editor", Name = "PDF Editor", Icon = "✏️", Subtitle = "All-in-one PDF workspace: annotate, draw, highlight, redact, add comments/shapes, take screenshots, and view PDFs.", Category = "Edit", ToolType = ToolType.PdfEditor },
                    new() { Id = "add-page-numbers", Name = "Page Numbers", Icon = "🔢", Subtitle = "Insert page numbers into your document.", Category = "Edit", ToolType = ToolType.AddPageNumbers },
                    new() { Id = "add-watermark", Name = "Add Watermark", Icon = "💧", Subtitle = "Stamp text or an image over your PDF pages.", Category = "Edit", ToolType = ToolType.AddWatermark },
                    new() { Id = "invert-colors", Name = "Invert Colors", Icon = "🌓", Subtitle = "Create a 'dark mode' version of your PDF.", Category = "Edit", ToolType = ToolType.InvertColors },
                    new() { Id = "change-background-color", Name = "Background Color", Icon = "🎨", Subtitle = "Change the background color of your PDF.", Category = "Edit", ToolType = ToolType.BackgroundColor },
                    new() { Id = "remove-annotations", Name = "Remove Annotations", Icon = "🧹", Subtitle = "Strip comments, highlights, and links.", Category = "Edit", ToolType = ToolType.RemoveAnnotations },
                    new() { Id = "remove-blank-pages", Name = "Remove Blank Pages", Icon = "📄", Subtitle = "Automatically detect and delete blank pages.", Category = "Edit", ToolType = ToolType.RemoveBlankPages },
                }
            },
            new ToolCategoryModel
            {
                Name = "Convert to PDF",
                Tools = new List<PdfTool>
                {
                    new() { Id = "image-to-pdf", Name = "Image to PDF", Icon = "🖼️", Subtitle = "Convert JPG, PNG, WebP, BMP, TIFF to PDF.", Category = "Convert", ToolType = ToolType.ImageToPdf },
                    new() { Id = "jpg-to-pdf", Name = "JPG to PDF", Icon = "📷", Subtitle = "Create a PDF from one or more JPG images.", Category = "Convert", ToolType = ToolType.JpgToPdf },
                    new() { Id = "png-to-pdf", Name = "PNG to PDF", Icon = "🖼️", Subtitle = "Create a PDF from one or more PNG images.", Category = "Convert", ToolType = ToolType.PngToPdf },
                    new() { Id = "txt-to-pdf", Name = "Text to PDF", Icon = "📝", Subtitle = "Convert a plain text file into a PDF.", Category = "Convert", ToolType = ToolType.TextToPdf },
                }
            },
            new ToolCategoryModel
            {
                Name = "Convert from PDF",
                Tools = new List<PdfTool>
                {
                    new() { Id = "pdf-to-jpg", Name = "PDF to JPG", Icon = "📷", Subtitle = "Convert each PDF page into a JPG image.", Category = "ConvertFrom", ToolType = ToolType.PdfToJpg },
                    new() { Id = "pdf-to-png", Name = "PDF to PNG", Icon = "🖼️", Subtitle = "Convert each PDF page into a PNG image.", Category = "ConvertFrom", ToolType = ToolType.PdfToPng },
                    new() { Id = "pdf-to-greyscale", Name = "PDF to Greyscale", Icon = "⬛", Subtitle = "Convert all colors to black and white.", Category = "ConvertFrom", ToolType = ToolType.PdfToGreyscale },
                }
            },
            new ToolCategoryModel
            {
                Name = "Organize & Manage",
                Tools = new List<PdfTool>
                {
                    new() { Id = "merge", Name = "Merge PDF", Icon = "📄+", Subtitle = "Combine multiple PDFs into one file.", Category = "Organize", ToolType = ToolType.Merge },
                    new() { Id = "split", Name = "Split PDF", Icon = "✂️", Subtitle = "Extract a range of pages into a new PDF.", Category = "Organize", ToolType = ToolType.Split },
                    new() { Id = "extract-pages", Name = "Extract Pages", Icon = "📑", Subtitle = "Save a selection of pages as new files.", Category = "Organize", ToolType = ToolType.ExtractPages },
                    new() { Id = "delete-pages", Name = "Delete Pages", Icon = "🗑️", Subtitle = "Remove specific pages from your document.", Category = "Organize", ToolType = ToolType.DeletePages },
                    new() { Id = "add-blank-page", Name = "Add Blank Page", Icon = "➕", Subtitle = "Insert an empty page anywhere in your PDF.", Category = "Organize", ToolType = ToolType.AddBlankPage },
                    new() { Id = "reverse-pages", Name = "Reverse Pages", Icon = "🔃", Subtitle = "Flip the order of all pages in your document.", Category = "Organize", ToolType = ToolType.ReversePages },
                    new() { Id = "rotate", Name = "Rotate PDF", Icon = "🔄", Subtitle = "Turn pages in 90-degree increments.", Category = "Organize", ToolType = ToolType.Rotate },
                    new() { Id = "view-metadata", Name = "View Metadata", Icon = "ℹ️", Subtitle = "Inspect the hidden properties of your PDF.", Category = "Organize", ToolType = ToolType.ViewMetadata },
                    new() { Id = "edit-metadata", Name = "Edit Metadata", Icon = "✏️", Subtitle = "Change the author, title, and other properties.", Category = "Organize", ToolType = ToolType.EditMetadata },
                    new() { Id = "compare-pdfs", Name = "Compare PDFs", Icon = "⚖️", Subtitle = "Compare two PDFs side by side.", Category = "Organize", ToolType = ToolType.Compare },
                }
            },
            new ToolCategoryModel
            {
                Name = "Optimize & Repair",
                Tools = new List<PdfTool>
                {
                    new() { Id = "compress", Name = "Compress PDF", Icon = "⚡", Subtitle = "Reduce the file size of your PDF.", Category = "Optimize", ToolType = ToolType.Compress },
                    new() { Id = "linearize", Name = "Linearize PDF", Icon = "📊", Subtitle = "Optimize PDF for fast web viewing.", Category = "Optimize", ToolType = ToolType.Linearize },
                    new() { Id = "page-dimensions", Name = "Page Dimensions", Icon = "📐", Subtitle = "Analyze page size, orientation, and units.", Category = "Optimize", ToolType = ToolType.PageDimensions },
                }
            },
            new ToolCategoryModel
            {
                Name = "Secure PDF",
                Tools = new List<PdfTool>
                {
                    new() { Id = "encrypt", Name = "Encrypt PDF", Icon = "🔒", Subtitle = "Lock your PDF by adding a password.", Category = "Secure", ToolType = ToolType.Encrypt },
                    new() { Id = "decrypt", Name = "Decrypt PDF", Icon = "🔓", Subtitle = "Unlock PDF by removing password protection.", Category = "Secure", ToolType = ToolType.Decrypt },
                    new() { Id = "flatten", Name = "Flatten PDF", Icon = "📋", Subtitle = "Make form fields and annotations non-editable.", Category = "Secure", ToolType = ToolType.Flatten },
                    new() { Id = "remove-metadata", Name = "Remove Metadata", Icon = "🗑️", Subtitle = "Strip hidden data from your PDF.", Category = "Secure", ToolType = ToolType.RemoveMetadata },
                }
            }
        };
    }

    /// <summary>
    /// Get all tools as a flat list
    /// </summary>
    public static List<PdfTool> GetAllTools()
    {
        return GetCategories().SelectMany(c => c.Tools).ToList();
    }

    /// <summary>
    /// Search tools by name or subtitle
    /// </summary>
    public static List<PdfTool> SearchTools(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllTools();

        var lowerQuery = query.ToLowerInvariant();
        return GetAllTools()
            .Where(t => t.Name.ToLowerInvariant().Contains(lowerQuery) ||
                       (t.Subtitle?.ToLowerInvariant().Contains(lowerQuery) ?? false))
            .ToList();
    }
}
