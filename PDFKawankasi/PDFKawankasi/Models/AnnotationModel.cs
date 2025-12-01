using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFKawankasi.Models;

/// <summary>
/// Represents an annotation in the PDF Editor
/// </summary>
public partial class AnnotationModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public AnnotationType Type { get; set; }
    
    [ObservableProperty]
    private double _x;
    
    [ObservableProperty]
    private double _y;
    
    [ObservableProperty]
    private double _width;
    
    [ObservableProperty]
    private double _height;
    
    public Color Color { get; set; }
    public string? Text { get; set; }
    public List<Point>? Points { get; set; }
    public DateTime CreatedAt { get; set; }

    // Display properties
    public string TypeIcon { get; private set; } = "📝";
    public string TypeName { get; private set; } = "Annotation";
    public string Description { get; set; } = "";
    public string PageInfo => $"Page {PageNumber}";

    public void UpdateTypeInfo()
    {
        (TypeIcon, TypeName) = Type switch
        {
            AnnotationType.Highlight => ("🖌️", "Highlight"),
            AnnotationType.Drawing => ("✏️", "Drawing"),
            AnnotationType.Text => ("📝", "Text"),
            AnnotationType.Shape => ("🔷", "Shape"),
            AnnotationType.Comment => ("💬", "Comment"),
            AnnotationType.Redaction => ("█", "Redaction"),
            _ => ("📝", "Annotation")
        };

        if (string.IsNullOrEmpty(Description))
        {
            Description = Type switch
            {
                AnnotationType.Highlight => $"Highlighted area ({Width:F0}x{Height:F0})",
                AnnotationType.Drawing => $"Freehand drawing ({Points?.Count ?? 0} points)",
                AnnotationType.Text => Text ?? "Text annotation",
                AnnotationType.Shape => $"Shape ({Width:F0}x{Height:F0})",
                AnnotationType.Comment => Text ?? "Comment",
                AnnotationType.Redaction => $"Redacted area ({Width:F0}x{Height:F0})",
                _ => "Annotation"
            };
        }
    }
}

/// <summary>
/// Types of annotations
/// </summary>
public enum AnnotationType
{
    None,
    Highlight,
    Drawing,
    Text,
    Shape,
    Comment,
    Redaction
}
