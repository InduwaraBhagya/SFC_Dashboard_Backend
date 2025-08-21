using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers.Api
{
    [Route("api/projects")]
    public class ProjectsApiController : ApiBaseController
    {
        private readonly IProjectsApiClient _projectsApi;
        private readonly ILogger<ProjectsApiController> _logger;

        public ProjectsApiController(
            IProjectsApiClient projectsApi,
            ILogger<ProjectsApiController> logger)
        {
            _projectsApi = projectsApi;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] bool includeInactive = false)
        {
            try
            {
                var projects = await _projectsApi.GetAllProjectsAsync();
                
                if (!includeInactive)
                {
                    // Filter out inactive projects if the Project model has an IsActive property
                    // For now, return all projects
                }

                return ApiResponse(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects");
                return ApiException(ex);
            }
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetProject(int projectId)
        {
            try
            {
                var project = await _projectsApi.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return ApiError("Project not found", 404);
                }

                return ApiResponse(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project {ProjectId}", projectId);
                return ApiException(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var project = new Project
                {
                    ProjectName = request.Name,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _projectsApi.CreateProjectAsync(project);
                if (result)
                {
                    return ApiResponse(project, "Project created successfully");
                }
                else
                {
                    return ApiError("Failed to create project", 500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return ApiException(ex);
            }
        }

        [HttpPut("{projectId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(int projectId, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var existingProject = await _projectsApi.GetProjectByIdAsync(projectId);
                if (existingProject == null)
                {
                    return ApiError("Project not found", 404);
                }

                existingProject.ProjectName = request.Name ?? existingProject.ProjectName;
                // Update other properties as needed

                // Note: No UpdateAsync method available in IProjectsApiClient
                return ApiError("Project update not implemented", 501);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", projectId);
                return ApiException(ex);
            }
        }

        [HttpDelete("{projectId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var result = await _projectsApi.DeleteProjectAsync(projectId);
                if (result)
                {
                    return ApiResponse(new { }, "Project deleted successfully");
                }
                else
                {
                    return ApiError("Failed to delete project", 500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
                return ApiException(ex);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProjects([FromQuery] string query, [FromQuery] int limit = 50)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return ApiError("Search query is required", 400);
                }

                var allProjects = await _projectsApi.GetAllProjectsAsync();
                var filteredProjects = allProjects
                    .Where(p => p.ProjectName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList();

                return ApiResponse(filteredProjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching projects with query {Query}", query);
                return ApiException(ex);
            }
        }

        [HttpGet("{projectId}/statistics")]
        public async Task<IActionResult> GetProjectStatistics(int projectId)
        {
            try
            {
                var project = await _projectsApi.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return ApiError("Project not found", 404);
                }

                // This would need to be implemented to get project-specific statistics
                var statistics = new
                {
                    projectId = projectId,
                    totalTasks = 0, // Would need to count related tasks
                    completedTasks = 0, // Would need to count completed tasks
                    activeUsers = 0, // Would need to count active users on project
                    lastActivity = DateTime.UtcNow // Would need to get actual last activity
                };

                return ApiResponse(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for project {ProjectId}", projectId);
                return ApiException(ex);
            }
        }
    }

    public class CreateProjectRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateProjectRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
