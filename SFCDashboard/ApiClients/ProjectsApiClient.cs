using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SFCDashboard.Models;

public class ProjectsApiClient : IProjectsApiClient
{
    private readonly HttpClient _httpClient;
    public ProjectsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Project>> GetAllProjectsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Project>>("/api/projects");
    }

    public async Task<Project> GetProjectByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Project>($"/api/projects/{id}");
    }

    public async Task<List<PlannedEventDto>> SearchPlannedEventsAsync(string searchTerm, int projectId)
    {
        var response = await _httpClient.GetAsync($"/api/projects/search?searchTerm={searchTerm}&projectId={projectId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PlannedEventDto>>();
    }

    public async Task<bool> CreateProjectAsync(Project project)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/projects", project);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignPEToProjectAsync(int plannedEventId, int projectId)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/projects/{projectId}/assign-pe", new { plannedEventId });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignMultiplePEsToProjectAsync(int projectId, List<int> plannedEventIds)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/projects/{projectId}/assign-multiple-pes", plannedEventIds);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemovePEFromProjectAsync(int plannedEventId, int projectId)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/projects/{projectId}/remove-pe", new { plannedEventId });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/projects/{id}");
        return response.IsSuccessStatusCode;
    }
}
