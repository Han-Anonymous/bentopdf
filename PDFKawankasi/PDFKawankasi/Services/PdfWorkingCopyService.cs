using System;
using System.IO;
using System.Threading.Tasks;

namespace PDFKawankasi.Services
{
    /// <summary>
    /// Manages a temporary working copy of the PDF file (like Excel's temporary file system).
    /// All edits are saved to the working copy until the user explicitly saves to the original file.
    /// </summary>
    public class PdfWorkingCopyService : IDisposable
    {
        private string? _originalFilePath;
        private string? _workingCopyPath;
        private bool _hasWorkingCopy;

        /// <summary>
        /// Gets the path to the working copy (temp file) if it exists, otherwise the original file path
        /// </summary>
        public string? CurrentFilePath => _hasWorkingCopy ? _workingCopyPath : _originalFilePath;

        /// <summary>
        /// Gets whether there is an active working copy
        /// </summary>
        public bool HasWorkingCopy => _hasWorkingCopy;

        /// <summary>
        /// Initialize working copy from an original PDF file
        /// </summary>
        public async Task<string> InitializeWorkingCopyAsync(string originalFilePath)
        {
            _originalFilePath = originalFilePath;

            // Create temp file path
            var tempFolder = Path.Combine(Path.GetTempPath(), "PDFKawankasi");
            Directory.CreateDirectory(tempFolder);

            var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var extension = Path.GetExtension(originalFilePath);
            _workingCopyPath = Path.Combine(tempFolder, $"{fileName}_working_{Guid.NewGuid()}{extension}");

            // Copy original to working copy
            await Task.Run(() => File.Copy(originalFilePath, _workingCopyPath, true));

            _hasWorkingCopy = true;
            return _workingCopyPath;
        }

        /// <summary>
        /// Initialize working copy from byte array (for new documents)
        /// </summary>
        public async Task<string> InitializeWorkingCopyAsync(byte[] pdfBytes, string fileName = "document.pdf")
        {
            // Create temp file path
            var tempFolder = Path.Combine(Path.GetTempPath(), "PDFKawankasi");
            Directory.CreateDirectory(tempFolder);

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            _workingCopyPath = Path.Combine(tempFolder, $"{fileNameWithoutExt}_working_{Guid.NewGuid()}{extension}");

            // Write bytes to working copy
            await File.WriteAllBytesAsync(_workingCopyPath, pdfBytes);

            _hasWorkingCopy = true;
            return _workingCopyPath;
        }

        /// <summary>
        /// Save working copy back to the original file
        /// </summary>
        public async Task SaveToOriginalAsync()
        {
            if (!_hasWorkingCopy || string.IsNullOrEmpty(_workingCopyPath) || string.IsNullOrEmpty(_originalFilePath))
            {
                throw new InvalidOperationException("No working copy to save");
            }

            // Copy working copy to original
            await Task.Run(() => File.Copy(_workingCopyPath, _originalFilePath, true));
        }

        /// <summary>
        /// Save working copy to a new file
        /// </summary>
        public async Task SaveAsAsync(string newFilePath)
        {
            if (!_hasWorkingCopy || string.IsNullOrEmpty(_workingCopyPath))
            {
                throw new InvalidOperationException("No working copy to save");
            }

            // Copy working copy to new file
            await Task.Run(() => File.Copy(_workingCopyPath, newFilePath, true));

            // Update original path to new file
            _originalFilePath = newFilePath;
        }

        /// <summary>
        /// Get the working copy as byte array
        /// </summary>
        public async Task<byte[]> GetWorkingCopyBytesAsync()
        {
            if (!_hasWorkingCopy || string.IsNullOrEmpty(_workingCopyPath))
            {
                throw new InvalidOperationException("No working copy available");
            }

            return await File.ReadAllBytesAsync(_workingCopyPath);
        }

        /// <summary>
        /// Replace working copy with new content
        /// </summary>
        public async Task UpdateWorkingCopyAsync(byte[] pdfBytes)
        {
            if (!_hasWorkingCopy || string.IsNullOrEmpty(_workingCopyPath))
            {
                throw new InvalidOperationException("No working copy available");
            }

            await File.WriteAllBytesAsync(_workingCopyPath, pdfBytes);
        }

        /// <summary>
        /// Discard working copy and clean up temp files
        /// </summary>
        public void DiscardWorkingCopy()
        {
            if (_hasWorkingCopy && !string.IsNullOrEmpty(_workingCopyPath) && File.Exists(_workingCopyPath))
            {
                try
                {
                    File.Delete(_workingCopyPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _workingCopyPath = null;
            _hasWorkingCopy = false;
        }

        public void Dispose()
        {
            DiscardWorkingCopy();
        }
    }
}
