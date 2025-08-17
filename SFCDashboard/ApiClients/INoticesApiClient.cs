using SFCDB.Models;

namespace SFCDashboard.ApiClients
{
    public interface INoticesApiClient
    {
        Task<ApiResponse<List<Notice>>> GetNoticesAsync();
        Task<ApiResponse<Notice>> GetNoticeAsync(int id);
        Task<ApiResponse<Notice>> CreateNoticeAsync(string description, int createdBy, string createdUserName, bool isPinned = false);
        Task<ApiResponse<Notice>> UpdateNoticeAsync(int id, string description, int updatedBy, string updatedUserName);
        Task<ApiResponse<Notice>> TogglePinNoticeAsync(int id, bool isPinned, int updatedBy, string updatedUserName);
        Task<ApiResponse> DeleteNoticeAsync(int id, int updatedBy, string updatedUserName);
    }

    public class NoticeCreateRequest
    {
        public string Description { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatedUserName { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false;
    }

    public class NoticeUpdateRequest
    {
        public string Description { get; set; } = string.Empty;
        public int UpdatedBy { get; set; }
        public string UpdatedUserName { get; set; } = string.Empty;
    }

    public class NoticePinRequest
    {
        public bool IsPinned { get; set; }
        public int UpdatedBy { get; set; }
        public string UpdatedUserName { get; set; } = string.Empty;
    }

    public class NoticeDeleteRequest
    {
        public int UpdatedBy { get; set; }
        public string UpdatedUserName { get; set; } = string.Empty;
    }
}
