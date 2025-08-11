using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;
using SFCDashboard.Controllers;


public class ProjectController : BaseController
{
    private readonly IProjectsApiClient _projectsApiClient;

    public ProjectController(IProjectsApiClient projectsApiClient, IUsersApiClient usersApiClient) : base(usersApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectsApiClient.GetAllProjectsAsync();

        // Get current user permissions from backend
        var userName = User?.Identity?.Name;
        var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
            ? userName.Substring(0, 6)
            : string.Empty;

        var permissions = await _usersApiClient.GetProjectUserPermissionsAsync(serviceId);

        ViewBag.CanManageProjects = permissions?.CanManageProjects ?? false;
        ViewBag.CurrentUser = permissions?.CurrentUser;

        return View(projects);
    }

    public async Task<IActionResult> Search(string searchTerm, int projectId)
    {
        var results = await _projectsApiClient.SearchPlannedEventsAsync(searchTerm, projectId);
        return Json(new { items = results });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (ModelState.IsValid)
        {
            var success = await _projectsApiClient.CreateProjectAsync(project);
            if (success)
                return RedirectToAction(nameof(Index));
        }
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> AssignPEToProject(int plannedEventId, int projectId)
    {
        var success = await _projectsApiClient.AssignPEToProjectAsync(plannedEventId, projectId);
        if (!success)
            return BadRequest("Assignment failed");
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AssignMultiplePEsToProject([FromBody] MultipleAssignmentModel model)
    {
        if (model.PlannedEventIds == null || !model.PlannedEventIds.Any())
        {
            return BadRequest("No PEs selected");
        }
        var success = await _projectsApiClient.AssignMultiplePEsToProjectAsync(model.ProjectId, model.PlannedEventIds);
        if (!success)
            return BadRequest("Assignment failed");
        return Json(new { success = true });
    }

    public class MultipleAssignmentModel
    {
        public int ProjectId { get; set; }
        public List<int>? PlannedEventIds { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var projectDetails = await _projectsApiClient.GetProjectDetailsAsync(id);
        if (projectDetails == null)
        {
            return NotFound();
        }

        // Get current user permissions from backend
        var userName = User?.Identity?.Name;
        var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
            ? userName.Substring(0, 6)
            : string.Empty;

        var permissions = await _usersApiClient.GetProjectUserPermissionsAsync(serviceId);

        ViewBag.CanManageProjects = permissions?.CanManageProjects ?? false;
        ViewBag.CurrentUser = permissions?.CurrentUser;

        var vm = new ProjectDetailsViewModel
        {
            Id = projectDetails.Id,
            ProjectName = projectDetails.ProjectName,
            CreatedDate = projectDetails.CreatedDate,
            ProjectPEs = projectDetails.ProjectPEs.Select(pe => new ProjectPEViewModel
            {
                Id = pe.Id,
                PlannedEventId = pe.PlannedEventId,
                PlannedEvent = pe.PlannedEvent,
                CurrentTask = pe.CurrentTask,
                ProgressPercent = pe.ProgressPercent,
                ProgressClass = pe.ProgressClass
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> RemovePEFromProject(int plannedEventId, int projectId)
    {
        var success = await _projectsApiClient.RemovePEFromProjectAsync(plannedEventId, projectId);
        if (!success)
            return NotFound("Mapping not found");
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _projectsApiClient.DeleteProjectAsync(id);
        if (!success)
            return NotFound("Project not found");
        return Json(new { success = true });
    }
}