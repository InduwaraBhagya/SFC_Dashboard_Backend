using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ProjectsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Project>>> GetAllProjects()
    {
        var projects = await _context.Projects
            .Include(p => p.ProjectPEs)
            .ThenInclude(pe => pe.PlannedEvent)
            .ToListAsync();
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProjectById(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectPEs)
            .ThenInclude(pe => pe.PlannedEvent)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PlannedEventDto>>> SearchPlannedEvents([FromQuery] string searchTerm, [FromQuery] int projectId)
    {
        var currentProjectPEs = await _context.ProjectPEMappings
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.PlannedEventId)
            .ToListAsync();

        var results = await _context.PlannedEvents
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        (p.PeNumber != null && p.PeNumber.ToLower().Contains(searchTerm.ToLower())) ||
                        (p.Customer != null && p.Customer.ToLower().Contains(searchTerm.ToLower())))
            .Select(p => new PlannedEventDto
            {
                Id = p.Id,
                PeNumber = p.PeNumber,
                Customer = p.Customer ?? "No Customer",
                IsAssigned = currentProjectPEs.Contains(p.Id)
            })
            .ToListAsync();
        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProject([FromBody] Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{projectId}/assign-pe")]
    public async Task<ActionResult> AssignPEToProject(int projectId, [FromBody] AssignPEModel model)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return NotFound("Project not found");
        var plannedEvent = await _context.PlannedEvents.FirstOrDefaultAsync(pe => pe.Id == model.PlannedEventId);
        if (plannedEvent == null) return NotFound("PE not found");
        var projectPE = new ProjectPEMapping { ProjectId = projectId, PlannedEventId = model.PlannedEventId };
        _context.ProjectPEMappings.Add(projectPE);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{projectId}/assign-multiple-pes")]
    public async Task<ActionResult> AssignMultiplePEsToProject(int projectId, [FromBody] List<int> plannedEventIds)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return NotFound("Project not found");
        var existingMappings = await _context.ProjectPEMappings
            .Where(p => p.ProjectId == projectId && plannedEventIds.Contains(p.PlannedEventId))
            .Select(p => p.PlannedEventId)
            .ToListAsync();
        var newMappings = plannedEventIds
            .Except(existingMappings)
            .Select(peId => new ProjectPEMapping { ProjectId = projectId, PlannedEventId = peId });
        await _context.ProjectPEMappings.AddRangeAsync(newMappings);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{projectId}/remove-pe")]
    public async Task<ActionResult> RemovePEFromProject(int projectId, [FromBody] AssignPEModel model)
    {
        var mapping = await _context.ProjectPEMappings
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.PlannedEventId == model.PlannedEventId);
        if (mapping == null) return NotFound("Mapping not found");
        _context.ProjectPEMappings.Remove(mapping);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectPEs)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound("Project not found");
        _context.ProjectPEMappings.RemoveRange(project.ProjectPEs);
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return Ok();
    }

    public class AssignPEModel
    {
        public int PlannedEventId { get; set; }
    }

    public class PlannedEventDto
    {
        public int Id { get; set; }
        public string PeNumber { get; set; }
        public string Customer { get; set; }
        public bool IsAssigned { get; set; }
    }
}
