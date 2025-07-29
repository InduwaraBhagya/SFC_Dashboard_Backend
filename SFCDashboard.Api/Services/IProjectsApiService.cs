using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IProjectsApiService
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
        Task<ProjectDetailsDto?> GetProjectDetailsAsync(int id);
        Task<List<PlannedEventDto>> SearchPlannedEventsAsync(string searchTerm, int projectId);
        Task<bool> CreateProjectAsync(Project project);
        Task<bool> AssignPEToProjectAsync(int plannedEventId, int projectId);
        Task<bool> AssignMultiplePEsToProjectAsync(int projectId, List<int> plannedEventIds);
        Task<bool> RemovePEFromProjectAsync(int plannedEventId, int projectId);
        Task<bool> DeleteProjectAsync(int id);
    }
}


