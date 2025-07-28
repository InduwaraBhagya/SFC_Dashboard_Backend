using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class PETasksController : BaseController
    {
        private readonly IPETasksApiClient _peTasksApiClient;
        private readonly IPlannedEventsApiClient _plannedEventsApiClient;
        private readonly ILogger<PETasksController> _logger;

        public PETasksController(
            IPETasksApiClient peTasksApiClient, 
            IPlannedEventsApiClient plannedEventsApiClient,
            IUsersApiClient usersApiClient,
            ILogger<PETasksController> logger)
            : base(usersApiClient)
        {
            _peTasksApiClient = peTasksApiClient;
            _plannedEventsApiClient = plannedEventsApiClient;
            _logger = logger;
        }

        // GET: PETasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _peTasksApiClient.GetByIdAsync(id.Value);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUrgent(int id)
        {
            try
            {
                await _peTasksApiClient.MarkAsUrgentAsync(id);
                
                _logger.LogInformation("Task ID {taskId} marked as urgent directly", id);
                TempData["SuccessMessage"] = "Task marked as urgent. This PE will now appear in the urgent records list.";

                // Get the task to find the PlannedEvent ID for redirect
                var task = await _peTasksApiClient.GetByIdAsync(id);
                return RedirectToAction("Details", "PlannedEvents", new { id = task?.PlannedEvent?.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking task {taskId} as urgent", id);
                TempData["ErrorMessage"] = "Error marking task as urgent.";
                return RedirectToAction("Details", "PlannedEvents");
            }
        }



        // GET: PETasks/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _peTasksApiClient.GetUrgentRequestsAsync();

            _logger.LogInformation("Retrieved {count} pending urgent task requests", pendingRequests.Count());
            return View(pendingRequests);
        }

        // POST: PETasks/ProcessUrgentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUrgentRequest(int id, string urgentReason)
        {
            try
            {
                await _peTasksApiClient.ProcessUrgentRequestAsync(id, urgentReason);

                _logger.LogInformation("Task {id} urgent request processed with reason: {reason}", id, urgentReason);

                bool markAsUrgent = urgentReason != "Reject";
                TempData["SuccessMessage"] = markAsUrgent
                    ? "Task marked as urgent."
                    : "Urgent request processed.";

                // Get the task to find the PlannedEvent ID for redirect
                var task = await _peTasksApiClient.GetByIdAsync(id);
                return RedirectToAction("Details", "PlannedEvents", new { id = task?.PlannedEvent?.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing urgent request for task {id}", id);
                TempData["ErrorMessage"] = "Error processing urgent request.";
                return RedirectToAction(nameof(UrgentRequestsList));
            }
        }

        // GET: PETasks/OLAViolationsList
        public async Task<IActionResult> OLAViolationsList()
        {
            var violatingTasks = await _peTasksApiClient.GetOLAViolationsAsync();

            _logger.LogInformation("Retrieved {count} tasks with OLA violations", violatingTasks.Count());
            return View(violatingTasks);
        }

        public DateTime GetEffectiveOLADeadline(DateTime taskStart, int olaDays, List<(DateTime HoldStart, DateTime HoldEnd)> holds)
        {
            // Sum all hold durations
            TimeSpan totalHold = TimeSpan.Zero;
            foreach (var hold in holds)
            {
                // If HoldEnd is not set (still on hold), use DateTime.Now
                var end = hold.HoldEnd == DateTime.MinValue ? DateTime.Now : hold.HoldEnd;
                totalHold += (end - hold.HoldStart);
            }
            // Effective deadline = start + OLA + total hold
            return taskStart.AddDays(olaDays).Add(totalHold);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteViolatedTask(int id)
        {
            try
            {
                await _peTasksApiClient.CompleteViolatedTaskAsync(id);
                
                TempData["SuccessMessage"] = "Task marked as completed successfully.";
                return RedirectToAction(nameof(OLAViolationsList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing violated task {id}", id);
                TempData["ErrorMessage"] = "Error completing task.";
                return RedirectToAction(nameof(OLAViolationsList));
            }
        }

        // Add this to your PETasksController
        [HttpGet]
        public async Task<IActionResult> GetUrgentRequestDetails(int id)
        {
            var task = await _peTasksApiClient.GetByIdAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            var details = new
            {
                id = task.Id,
                peNumber = task.PENumber,
                customer = task.PlannedEvent?.Customer,
                taskName = task.Task,
                priority = task.Priority,
                urgentRequestReason = ExtractUrgentRequestReason(task.Priority ?? "")
            };

            return Json(details);
        }

        private string? ExtractUrgentRequestReason(string priority)
        {
            if (string.IsNullOrEmpty(priority))
                return null;

            if (priority.Contains("Opening Ceremony"))
                return "Opening Ceremony - Priority 1";

            if (priority.Contains("Critical Customer"))
                return "Critical Customer - Priority 2";

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEstimatedTime(int taskId, DateTime estimatedTime)
        {
            try
            {
                await _peTasksApiClient.UpdateEstimatedTimeAsync(taskId, estimatedTime);
                return Ok(new { success = true, message = "Estimated time updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimated time for task {taskId}", taskId);
                return StatusCode(500, "Error updating estimated time: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PENumber,TaskSeq,Task,TaskWorkGroup,OLA,TaskStatus,TaskCreatedDate,TaskCompleteDate,ActualTaskCreatedDate,ACtualTaskCompleteDate,IsUrgent,UrgentRequested,Priority,EstimatedTime")] PETask pETask)
        {
            if (id != pETask.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _peTasksApiClient.UpdateAsync(pETask);
                    TempData["SuccessMessage"] = "Task updated successfully.";
                    return RedirectToAction("Details", new { id = pETask.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating task {Id}", id);
                    TempData["ErrorMessage"] = "Error updating task.";
                }
            }
            return View(pETask);
        }
        [HttpGet]
        public async Task<IActionResult> GetForPE(string peNumber)
        {
            var tasks = await _peTasksApiClient.GetByPENumberAsync(peNumber);
            var taskList = tasks.Select(t => new { id = t.Id, name = t.Task }).ToList();
            return Json(taskList);
        }

        [HttpGet]
        public Task<IActionResult> GetEstimationHistory(int id)
        {
            // TODO: Implement API endpoint for estimation history
            // For now, return empty list
            var history = new List<object>();
            return Task.FromResult<IActionResult>(Json(history));
        }

        // GET: PETasks/UrgentTasks
        public async Task<IActionResult> UrgentTasks()
        {
            var urgentTasks = await _peTasksApiClient.GetUrgentTasksAsync();

            _logger.LogInformation("Retrieved {count} urgent tasks", urgentTasks.Count());
            return View(urgentTasks);
        }

        // POST: PETasks/RemoveUrgentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUrgentStatus(int id)
        {
            try
            {
                await _peTasksApiClient.RemoveUrgentStatusAsync(id);
                
                _logger.LogInformation("Task {id} urgent status removed", id);
                TempData["SuccessMessage"] = "Urgent status removed from task successfully.";
                return RedirectToAction(nameof(UrgentTasks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing urgent status from task {id}", id);
                TempData["ErrorMessage"] = "Error removing urgent status.";
                return RedirectToAction(nameof(UrgentTasks));
            }
        }

    }
}