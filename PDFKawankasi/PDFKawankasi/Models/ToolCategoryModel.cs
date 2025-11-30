namespace PDFKawankasi.Models;

/// <summary>
/// Represents a category of PDF tools
/// </summary>
public class ToolCategoryModel
{
    public required string Name { get; init; }
    public required List<PdfTool> Tools { get; init; }
}
