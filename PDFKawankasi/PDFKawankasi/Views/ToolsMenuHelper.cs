using PDFKawankasi.Models;
using PDFKawankasi.Services;

namespace PDFKawankasi.Views;

/// <summary>
/// Helper class to provide static tool references for XAML menu binding
/// </summary>
public static class ToolsMenuHelper
{
    private static readonly Lazy<List<PdfTool>> _allTools = new(() => ToolsService.GetAllTools());

    public static PdfTool MergePdf => GetTool(ToolType.Merge);
    public static PdfTool SplitPdf => GetTool(ToolType.Split);
    public static PdfTool CompressPdf => GetTool(ToolType.Compress);
    public static PdfTool ExtractPages => GetTool(ToolType.ExtractPages);
    public static PdfTool DeletePages => GetTool(ToolType.DeletePages);
    public static PdfTool RotatePdf => GetTool(ToolType.Rotate);
    public static PdfTool AddPageNumbers => GetTool(ToolType.AddPageNumbers);
    public static PdfTool AddWatermark => GetTool(ToolType.AddWatermark);
    public static PdfTool InvertColors => GetTool(ToolType.InvertColors);
    public static PdfTool BackgroundColor => GetTool(ToolType.BackgroundColor);
    public static PdfTool RemoveAnnotations => GetTool(ToolType.RemoveAnnotations);
    public static PdfTool ImageToPdf => GetTool(ToolType.ImageToPdf);
    public static PdfTool PdfToJpg => GetTool(ToolType.PdfToJpg);
    public static PdfTool PdfToGreyscale => GetTool(ToolType.PdfToGreyscale);
    public static PdfTool ViewMetadata => GetTool(ToolType.ViewMetadata);
    public static PdfTool EditMetadata => GetTool(ToolType.EditMetadata);

    private static PdfTool GetTool(ToolType type)
    {
        return _allTools.Value.FirstOrDefault(t => t.ToolType == type) 
            ?? new PdfTool 
            { 
                Id = type.ToString().ToLower(),
                Name = type.ToString(), 
                Icon = "📄",
                Category = "Other",
                ToolType = type 
            };
    }
}
