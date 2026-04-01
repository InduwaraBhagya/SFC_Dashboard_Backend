namespace SFCDashboard.Api.Services
{
    public interface IFileHandlingService
    {
        Task<string?> SaveIssueAttachmentAsync(IFormFile file, int plannedEventId);
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]?> GetFileAsync(string filePath);
        string GetFileUrl(string relativePath);
        bool IsValidFileType(IFormFile file);
        bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760); // 10MB default
    }
}
