using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;
using SFCDashboard.Controllers;

public class ProjectController : BaseController
{
    private readonly ApplicationDbContext _context;

    public ProjectController(ApplicationDbContext context, IUsersApiClient usersApiClient) : base(usersApiClient)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _context.Projects
            .Include(p => p.ProjectPEs)
            .ThenInclude(pe => pe.PlannedEvent)
            .ToListAsync();
        return View(projects);
    }

    public async Task<IActionResult> Search(string searchTerm, int projectId)
    {
        // Get currently assigned PE IDs for this project
        var currentProjectPEs = await _context.ProjectPEMappings
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.PlannedEventId)
            .ToListAsync();

        var results = await _context.PlannedEvents
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        (p.PeNumber != null && p.PeNumber.ToLower().Contains(searchTerm.ToLower())) ||
                        (p.Customer != null && p.Customer.ToLower().Contains(searchTerm.ToLower())))
            .Select(p => new
            {
                id = p.Id,
                peNumber = p.PeNumber,
                customer = p.Customer ?? "No Customer",
                isAssigned = currentProjectPEs.Contains(p.Id)
            })
            .ToListAsync();

        return Json(new { items = results });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Project project)  // Changed from CreateProject to Create
    {
        if (ModelState.IsValid)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> AssignPEToProject(int plannedEventId, int projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            return NotFound("Project not found");
        }

        var plannedEvent = await _context.PlannedEvents
            .FirstOrDefaultAsync(pe => pe.Id == plannedEventId);
        if (plannedEvent == null)
        {
            return NotFound("PE not found");
        }

        var projectPE = new ProjectPEMapping
        {
            ProjectId = projectId,
            PlannedEventId = plannedEventId
        };

        _context.ProjectPEMappings.Add(projectPE);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AssignMultiplePEsToProject([FromBody] MultipleAssignmentModel model)
    {
        if (model.PlannedEventIds == null || !model.PlannedEventIds.Any())
        {
            return BadRequest("No PEs selected");
        }

        var project = await _context.Projects.FindAsync(model.ProjectId);
        if (project == null)
        {
            return NotFound("Project not found");
        }

        // Get existing mappings
        var existingMappings = await _context.ProjectPEMappings
            .Where(p => p.ProjectId == model.ProjectId && model.PlannedEventIds.Contains(p.PlannedEventId))
            .Select(p => p.PlannedEventId)
            .ToListAsync();

        // Only add new mappings
        var newMappings = model.PlannedEventIds
            .Except(existingMappings)
            .Select(peId => new ProjectPEMapping
            {
                ProjectId = model.ProjectId,
                PlannedEventId = peId
            });

        await _context.ProjectPEMappings.AddRangeAsync(newMappings);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    public class MultipleAssignmentModel
    {
        public int ProjectId { get; set; }
        public List<int> PlannedEventIds { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectPEs)
                .ThenInclude(pe => pe.PlannedEvent)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> RemovePEFromProject(int plannedEventId, int projectId)
    {
        var mapping = await _context.ProjectPEMappings
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.PlannedEventId == plannedEventId);

        if (mapping == null)
        {
            return NotFound("Mapping not found");
        }

        _context.ProjectPEMappings.Remove(mapping);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectPEs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound("Project not found");
        }

        // Remove associated PEs first
        _context.ProjectPEMappings.RemoveRange(project.ProjectPEs);

        // Remove the project
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}