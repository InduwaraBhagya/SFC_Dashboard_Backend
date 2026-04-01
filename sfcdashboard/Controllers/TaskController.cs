using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class TaskController : Controller
    {
        private readonly IPlannedEventsApiClient _plannedEventsApiClient;

        public TaskController(IPlannedEventsApiClient plannedEventsApiClient)
        {
            _plannedEventsApiClient = plannedEventsApiClient;
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the PE Task using API client
            var peTask = await _plannedEventsApiClient.GetByIdAsync(id.Value);

            if (peTask == null)
            {
                return NotFound();
            }

            // Redirect to the PlannedEvents Details action with the task ID
            return RedirectToAction("Details", "PlannedEvents", new { id = peTask.Id });
        }
    }
}