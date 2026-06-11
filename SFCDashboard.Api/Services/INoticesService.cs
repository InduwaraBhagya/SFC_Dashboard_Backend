using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface INoticesService
    {
        Task<IEnumerable<Notice>> GetActiveNoticesAsync();
        Task<Notice?> GetNoticeByIdAsync(int id);
        Task<Notice> CreateNoticeAsync(Notice notice);
        Task<Notice?> UpdateNoticeAsync(int id, string title, string description, int updatedBy, string updatedUserName, DateTime? startDate, DateTime? expireDate, bool isActive);
        Task<Notice?> TogglePinNoticeAsync(int id, bool isPinned, int updatedBy, string updatedUserName);
        Task<bool> DeleteNoticeAsync(int id, int updatedBy, string updatedUserName);
    }
}
