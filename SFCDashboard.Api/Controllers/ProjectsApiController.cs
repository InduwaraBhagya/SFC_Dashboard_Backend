using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.Services;

[ApiController]
[Route("api/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly IProjectsApiService _projectsService;
    
    public ProjectsApiController(IProjectsApiService projectsService)
    {
        _projectsService = projectsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Project>>> GetAllProjects()
    {
        var projects = await _projectsService.GetAllProjectsAsync();
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProjectById(int id)
    {
        var project = await _projectsService.GetProjectByIdAsync(id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<ProjectDetailsDto>> GetProjectDetails(int id)
    {
        var projectDetails = await _projectsService.GetProjectDetailsAsync(id);
        if (projectDetails == null) return NotFound();
        return Ok(projectDetails);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PlannedEventDto>>> SearchPlannedEvents([FromQuery] string searchTerm, [FromQuery] int projectId)
    {
        var results = await _projectsService.SearchPlannedEventsAsync(searchTerm, projectId);
        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProject([FromBody] Project project)
    {
        var success = await _projectsService.CreateProjectAsync(project);
        if (!success) return BadRequest("Failed to create project");
        return Ok();
    }

    [HttpPost("{projectId}/assign-pe")]
    public async Task<ActionResult> AssignPEToProject(int projectId, [FromBody] AssignPEModel model)
    {
        var success = await _projectsService.AssignPEToProjectAsync(model.PlannedEventId, projectId);
        if (!success) return BadRequest("Failed to assign PE to project");
        return Ok();
    }

    [HttpPost("{projectId}/assign-multiple-pes")]
    public async Task<ActionResult> AssignMultiplePEsToProject(int projectId, [FromBody] List<int> plannedEventIds)
    {
        var success = await _projectsService.AssignMultiplePEsToProjectAsync(projectId, plannedEventIds);
        if (!success) return BadRequest("Failed to assign PEs to project");
        return Ok();
    }

    [HttpPost("{projectId}/remove-pe")]
    public async Task<ActionResult> RemovePEFromProject(int projectId, [FromBody] AssignPEModel model)
    {
        var success = await _projectsService.RemovePEFromProjectAsync(model.PlannedEventId, projectId);
        if (!success) return NotFound("Mapping not found");
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProject(int id)
    {
        var success = await _projectsService.DeleteProjectAsync(id);
        if (!success) return NotFound("Project not found");
        return Ok();
    }

    public class AssignPEModel
    {
        public int PlannedEventId { get; set; }
    }
}
