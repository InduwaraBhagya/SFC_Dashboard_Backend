namespace SFCDashboard.Api.Services
{
    public class FileHandlingService : IFileHandlingService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileHandlingService> _logger;
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif" };
        private const long DefaultMaxFileSize = 10485760; // 10MB

        public FileHandlingService(IWebHostEnvironment environment, ILogger<FileHandlingService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string?> SaveIssueAttachmentAsync(IFormFile file, int plannedEventId)
        {
            try
            {
                if (!IsValidFileType(file) || !IsValidFileSize(file))
                {
                    return null;
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "issues", plannedEventId.ToString());
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"/uploads/issues/{plannedEventId}/{uniqueFileName}";
                _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file attachment for PE {PlannedEventId}", plannedEventId);
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<byte[]?> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllBytesAsync(fullPath);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                return null;
            }
        }

        public string GetFileUrl(string relativePath)
        {
            return relativePath.StartsWith("/") ? relativePath : $"/{relativePath}";
        }

        public bool IsValidFileType(IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(IFormFile file, long maxSizeInBytes = DefaultMaxFileSize)
        {
            return file != null && file.Length > 0 && file.Length <= maxSizeInBytes;
        }
    }
}
