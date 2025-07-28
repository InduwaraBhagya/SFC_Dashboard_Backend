using SFCDashboard.Models;

public interface IProjectsApiClient
{
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project> GetProjectByIdAsync(int id);
    Task<List<PlannedEventDto>> SearchPlannedEventsAsync(string searchTerm, int projectId);
    Task<bool> CreateProjectAsync(Project project);
    Task<bool> AssignPEToProjectAsync(int plannedEventId, int projectId);
    Task<bool> AssignMultiplePEsToProjectAsync(int projectId, List<int> plannedEventIds);
    Task<bool> RemovePEFromProjectAsync(int plannedEventId, int projectId);
    Task<bool> DeleteProjectAsync(int id);
}
