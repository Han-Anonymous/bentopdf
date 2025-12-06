using System.IO;
using System.Text.Json;

namespace PDFKawankasi.Services;

/// <summary>
/// Service for managing recently opened PDF documents
/// </summary>
public static class RecentDocumentsService
{
    private const int MaxRecentDocuments = 10;
    private static readonly string RecentFilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PDFKawankasi",
        "recent_documents.json"
    );

    /// <summary>
    /// Add a document to recent documents list
    /// </summary>
    public static void AddRecentDocument(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        var recentDocs = GetRecentDocuments();
        
        // Remove if already exists (to move to top)
        recentDocs.RemoveAll(d => d.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        
        // Add to beginning
        recentDocs.Insert(0, new RecentDocument
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            LastAccessed = DateTime.Now
        });
        
        // Keep only max items
        if (recentDocs.Count > MaxRecentDocuments)
            recentDocs = recentDocs.Take(MaxRecentDocuments).ToList();
        
        SaveRecentDocuments(recentDocs);
    }

    /// <summary>
    /// Get list of recent documents
    /// </summary>
    public static List<RecentDocument> GetRecentDocuments()
    {
        try
        {
            if (!File.Exists(RecentFilesPath))
                return new List<RecentDocument>();

            var json = File.ReadAllText(RecentFilesPath);
            var docs = JsonSerializer.Deserialize<List<RecentDocument>>(json) ?? new List<RecentDocument>();
            
            // Filter out files that no longer exist
            return docs.Where(d => File.Exists(d.FilePath)).ToList();
        }
        catch
        {
            return new List<RecentDocument>();
        }
    }

    /// <summary>
    /// Clear all recent documents
    /// </summary>
    public static void ClearRecentDocuments()
    {
        try
        {
            if (File.Exists(RecentFilesPath))
                File.Delete(RecentFilesPath);
        }
        catch
        {
            // Ignore errors
        }
    }

    private static void SaveRecentDocuments(List<RecentDocument> documents)
    {
        try
        {
            var directory = Path.GetDirectoryName(RecentFilesPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(documents, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(RecentFilesPath, json);
        }
        catch
        {
            // Ignore errors
        }
    }
}

/// <summary>
/// Model for a recent document
/// </summary>
public class RecentDocument
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
}
