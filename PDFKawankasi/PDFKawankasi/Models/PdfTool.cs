namespace PDFKawankasi.Models;

/// <summary>
/// Represents a PDF tool in the application
/// </summary>
public class PdfTool
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public string? Subtitle { get; init; }
    public required string Category { get; init; }
    public required ToolType ToolType { get; init; }
}

/// <summary>
/// Categories of PDF tools
/// </summary>
public enum ToolCategory
{
    Popular,
    EditAnnotate,
    ConvertToPdf,
    ConvertFromPdf,
    OrganizeManage,
    OptimizeRepair,
    SecurePdf
}

/// <summary>
/// Types of PDF operations
/// </summary>
public enum ToolType
{
    Merge,
    Split,
    Compress,
    Edit,
    ImageToPdf,
    JpgToPdf,
    PngToPdf,
    PdfToJpg,
    PdfToPng,
    Rotate,
    ExtractPages,
    DeletePages,
    SignPdf,
    Crop,
    AddWatermark,
    AddPageNumbers,
    Encrypt,
    Decrypt,
    ViewMetadata,
    EditMetadata,
    Flatten,
    InvertColors,
    BackgroundColor,
    PdfToGreyscale,
    Ocr,
    AlternateMerge,
    Organize,
    AddBlankPage,
    ReversePages,
    NUp,
    Compare,
    PdfToZip,
    RemoveAnnotations,
    RemoveMetadata,
    ChangePermissions,
    Linearize,
    FixDimensions,
    PageDimensions,
    RemoveBlankPages,
    SplitInHalf,
    CombineSinglePage,
    Posterize,
    DuplicateOrganize,
    AddAttachments,
    ExtractAttachments,
    EditAttachments,
    SanitizePdf,
    RemoveRestrictions,
    EditBookmarks,
    TableOfContents,
    FormFiller,
    FormCreator,
    AddStamps,
    WebpToPdf,
    SvgToPdf,
    BmpToPdf,
    HeicToPdf,
    TiffToPdf,
    TextToPdf,
    JsonToPdf,
    PdfToWebp,
    PdfToBmp,
    PdfToTiff,
    PdfToJson,
    HeaderFooter,
    TextColor
}
