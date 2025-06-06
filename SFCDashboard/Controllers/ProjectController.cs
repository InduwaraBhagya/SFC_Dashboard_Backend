using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

public class ProjectController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProjectController(ApplicationDbContext context)
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

    public async Task<IActionResult> Search(string searchTerm, int projectId) // Added projectId parameter
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Json(Array.Empty<object>());
        }

        searchTerm = searchTerm.Trim().ToLower();

        // Get currently assigned PE IDs for this project
        var currentProjectPEs = await _context.ProjectPEMappings
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.PlannedEventId)
            .ToListAsync();

        var results = await _context.PlannedEvents
            .Where(p => (p.PeNumber != null && p.PeNumber.ToLower().Contains(searchTerm)) ||
                       (p.Customer != null && p.Customer.ToLower().Contains(searchTerm)))
            .Select(p => new
            {
                id = p.Id,
                peNumber = p.PeNumber,
                customer = p.Customer ?? "No Customer",
                isAssigned = currentProjectPEs.Contains(p.Id)
            })
            .Take(10)
            .ToListAsync();

        return Json(results);
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