using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;
using SFCDashboard.Controllers;


public class ProjectController : BaseController
{
    private readonly IProjectsApiClient _projectsApiClient;
    private readonly IPETasksApiClient _peTasksApiClient;

    public ProjectController(IProjectsApiClient projectsApiClient, IUsersApiClient usersApiClient, IPETasksApiClient peTasksApiClient) : base(usersApiClient)
    {
        _projectsApiClient = projectsApiClient;
        _peTasksApiClient = peTasksApiClient;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectsApiClient.GetAllProjectsAsync();

        // Get current user info
        var userName = User?.Identity?.Name;
        var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
            ? userName.Substring(0, 6)
            : string.Empty;

        SystemUser? currentUser = null;
        bool canManageProjects = false;
        if (!string.IsNullOrEmpty(serviceId))
        {
            var userId = await _usersApiClient.GetCurrentUserIdAsync(serviceId);
            if (userId > 0)
            {
                currentUser = await _usersApiClient.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (currentUser?.UserRole != null)
                {
                    canManageProjects = currentUser.UserRole.HasPermission("ManageProjects");
                }
            }
        }

        ViewBag.CanManageProjects = canManageProjects;
        ViewBag.CurrentUser = currentUser;

        return View(projects);
    }

    public async Task<IActionResult> Search(string searchTerm, int projectId)
    {
        var results = await _projectsApiClient.SearchPlannedEventsAsync(searchTerm, projectId);
        return Json(new { items = results });
    }

    [HttpPost]
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
        var project = await _projectsApiClient.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }


        // Prepare PE numbers for all assigned PEs, filtering out nulls
        var peNumbers = project.ProjectPEs
            .Select(pe => pe.PlannedEvent?.PeNumber)
            .Where(peNum => !string.IsNullOrEmpty(peNum))
            .Cast<string>()
            .ToList();
        var peTasksDict = await _peTasksApiClient.GetTasksByPeNumbersAsync(peNumbers);

        var peViewModels = new List<ProjectPEViewModel>();
        foreach (var pe in project.ProjectPEs)
        {
            var peNumber = pe.PlannedEvent?.PeNumber;
            if (string.IsNullOrEmpty(peNumber)) continue;
            var tasks = peTasksDict.TryGetValue(peNumber, out var tlist) && tlist != null ? tlist.ToList() : new List<PETask>();

            // Get current task (first not completed by TaskSeq)
            var currentTask = tasks
                .Where(t => !string.Equals(t.TaskStatus, "completed", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.TaskSeq)
                .FirstOrDefault();

            // Progress calculation
            decimal totalOLA = tasks.Sum(t => decimal.TryParse(t.OLA, out var ola) ? ola : 0);
            decimal completedOLA = tasks
                .Where(t => string.Equals(t.TaskStatus, "completed", StringComparison.OrdinalIgnoreCase))
                .Sum(t => decimal.TryParse(t.OLA, out var ola) ? ola : 0);
            var progressPercent = totalOLA > 0 ? Math.Round((completedOLA / totalOLA) * 100, 2) : 0;

            // Exceeded OLA
            var exceededOLA = tasks.Any(t =>
                string.Equals(t.TaskStatus, "completed", StringComparison.OrdinalIgnoreCase) &&
                t.ActualTaskCreatedDate.HasValue &&
                t.ACtualTaskCompleteDate.HasValue &&
                ((decimal)(t.ACtualTaskCompleteDate.Value - t.ActualTaskCreatedDate.Value).TotalDays) >
                (decimal.TryParse(t.OLA, out var ola) ? ola : 0)
            );

            var progressClass = exceededOLA ? "bg-danger" : progressPercent switch
            {
                100 => "bg-success",
                var p when p > 60 => "bg-info",
                var p when p > 30 => "bg-warning",
                _ => "bg-danger"
            };

            peViewModels.Add(new ProjectPEViewModel
            {
                Id = pe.Id,
                PlannedEventId = pe.PlannedEventId,
                PlannedEvent = pe.PlannedEvent!,
                CurrentTask = currentTask?.Task,
                ProgressPercent = progressPercent,
                ProgressClass = progressClass
            });
        }

        var vm = new ProjectDetailsViewModel
        {
            Id = project.Id,
            ProjectName = project.ProjectName,
            CreatedDate = project.CreatedDate,
            ProjectPEs = peViewModels
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