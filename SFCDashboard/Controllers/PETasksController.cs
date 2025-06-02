using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    public class PETasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETasksController> _logger;

        public PETasksController(ApplicationDbContext context, ILogger<PETasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PETasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(m => m.Id == id);

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
            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null || task.TaskStatus?.ToUpper() != "ONGOING")
            {
                return NotFound();
            }

            // Mark this task as urgent immediately (no approval needed)
            task.IsUrgent = true;
            task.UrgentRequested = false;
            task.Priority = (task.Priority ?? "") + " [URGENT]";

            _context.Update(task);

            // Also update the parent PE status to urgent
            if (task.PlannedEvent != null)
            {
                task.PlannedEvent.PEStatus = "urgent";
                _context.Update(task.PlannedEvent);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task ID {taskId} marked as urgent directly", id);
            TempData["SuccessMessage"] = "Task marked as urgent. This PE will now appear in the urgent records list.";

            // Return to the details page
            return RedirectToAction("Details", "PlannedEvents", new { id = task.PlannedEvent?.Id });
        }



        // GET: PETasks/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _context.PETasks
                .Where(t => t.UrgentRequested && !t.IsUrgent)
                .Include(t => t.PlannedEvent)
                .OrderBy(t => t.PENumber)
                .ToListAsync();

            _logger.LogInformation("Retrieved {count} pending urgent task requests", pendingRequests.Count);
            return View(pendingRequests);
        }

        // POST: PETasks/ProcessUrgentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUrgentRequest(int id, string urgentReason)
        {
            // Fix: Load the PETask first, not the PlannedEvent
            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null || task.TaskStatus == "COMPLETED")
            {
                return NotFound();
            }

            bool markAsUrgent = false;
            string priorityMessage = "";
            int priorityLevel = 0; // Numeric priority level

            // Extract numeric priority for debugging and display
            switch (urgentReason)
            {
                case "OpeningCeremony":
                    markAsUrgent = true;
                    task.IsUrgent = true;
                    priorityMessage = "[URGENT: Opening Ceremony - Priority 1]";
                    priorityLevel = 1;
                    // Clear any previous priority and set the new one
                    task.Priority = priorityMessage;
                    break;

                case "CriticalCustomer":
                    markAsUrgent = true;
                    task.IsUrgent = true;
                    priorityMessage = "[URGENT: Critical Customer - Priority 2]";
                    priorityLevel = 2;
                    // Clear any previous priority and set the new one
                    task.Priority = priorityMessage;
                    break;

                case "NetworkOutage":
                    markAsUrgent = true;
                    task.IsUrgent = true;
                    priorityMessage = "[URGENT: Network Outage - Priority 0]";
                    priorityLevel = 0;
                    // Clear any previous priority and set the new one
                    task.Priority = priorityMessage;
                    break;

                case "Reject":
                    task.UrgentRequested = false;
                    task.Priority = "Urgent Request Rejected";
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid option selected.";
                    return RedirectToAction(nameof(UrgentRequestsList));
            }

            _context.Update(task);

            // Update the PlannedEvent if needed
            if (markAsUrgent && task.PlannedEvent != null)
            {
                task.PlannedEvent.PEStatus = "URGENT";
                task.PlannedEvent.Priority = priorityMessage;
                _context.Update(task.PlannedEvent);

                _logger.LogInformation("PE {id} marked as urgent with priority level {level}",
                    task.PlannedEvent.Id, priorityLevel);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {id} priority set to: '{priority}' with level: {level}",
                id, task.Priority, priorityLevel);

            TempData["SuccessMessage"] = markAsUrgent
                ? $"Task marked as urgent with priority {priorityLevel}."
                : "Urgent request processed.";

            return RedirectToAction("Details", "PlannedEvents", new { id = task.PlannedEvent?.Id });
        }

        // GET: PETasks/OLAViolationsList
        public async Task<IActionResult> OLAViolationsList()
        {
            var currentDate = DateTime.Today;

            // Find all tasks that:
            // 1. Are not completed (status is not "COMPLETED")
            // 2. Have a TaskCompleteDate in the past
            var violatingTasks = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .Where(t => t.TaskStatus != "COMPLETED" &&
                           t.TaskCompleteDate.Date < currentDate)
                .OrderBy(t => t.TaskCompleteDate)  // Show oldest violations first
                .ToListAsync();

            // Update PE status for OLA violations
            foreach (var task in violatingTasks)
            {
                if (task.PlannedEvent != null && task.PlannedEvent.PEStatus != "ola-violated")
                {
                    task.PlannedEvent.PEStatus = "ola-violated";
                    _context.Update(task.PlannedEvent);
                }
            }

            // Save changes if any
            if (violatingTasks.Any(t => t.PlannedEvent != null))
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Retrieved {count} tasks with OLA violations", violatingTasks.Count);
            return View(violatingTasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteViolatedTask(int id)
        {
            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Mark task as completed
            task.TaskStatus = "COMPLETED";
            task.ACtualTaskCompleteDate = DateTime.Now;

            // Check if there are other violated tasks for this PE
            var otherViolationsExist = await _context.PETasks
                .AnyAsync(t => t.PENumber == task.PENumber &&
                              t.Id != task.Id &&
                              t.TaskStatus != "COMPLETED" &&
                              t.TaskCompleteDate.Date < DateTime.Today);

            // If no other violations exist, update the PE status
            if (!otherViolationsExist && task.PlannedEvent != null && task.PlannedEvent.PEStatus == "ola-violated")
            {
                // Return to ongoing status (or whatever is appropriate)
                task.PlannedEvent.PEStatus = "ongoing";
                _context.Update(task.PlannedEvent);
            }

            _context.Update(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task marked as completed successfully.";

            return RedirectToAction(nameof(OLAViolationsList));
        }

        // Add this to your PETasksController
        [HttpGet]
        public async Task<IActionResult> GetUrgentRequestDetails(int id)
        {
            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == id);

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
                urgentRequestReason = ExtractUrgentRequestReason(task.Priority)
            };

            return Json(details);
        }

        private string ExtractUrgentRequestReason(string priority)
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
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return NotFound("Task not found");

                // Only allow for "Draw Fiber" tasks
                if (!string.Equals(task.Task?.Trim(), "Draw Fiber", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Estimated Time can only be set for 'Draw Fiber' tasks.");

                var status = task.TaskStatus?.ToUpper();
                if (status == "COMPLETED")
                    return BadRequest("Already completed task.");

                // FIXED CHECK: For OLA violated tasks, skip the status check entirely
                if (!task.IsOLAViolate && status != "ONGOING" && status != "WAITING")
                    return BadRequest("Estimated Time can only be set when TaskStatus is ONGOING or WAITING or task is OLA Violated.");

                if (estimatedTime.Date < DateTime.Today)
                    return BadRequest("Estimated Time cannot be in the past.");

                // Create history record
                var historyRecord = new TaskEstimationHistory
                {
                    TaskId = taskId,
                    EstimatedDate = estimatedTime,
                    CreatedAt = DateTime.UtcNow
                };

                // Add history record
                _context.TaskEstimationHistory.Add(historyRecord);

                // Update task's estimated time
                task.EstimatedTime = estimatedTime;
                task.TaskCompleteDate = estimatedTime;
                
                // If this is an OLA violated task and we're setting a future date, clear the violation flag
                if (task.IsOLAViolate && estimatedTime.Date >= DateTime.Today)
                {
                    task.IsOLAViolate = false;
                    _logger.LogInformation("Cleared OLA violation for task {id} after setting new estimated time", taskId);
                }

                // 2. Get all tasks for this PE, ordered by TaskSeq
                var allTasks = await _context.PETasks
                    .Where(t => t.PENumber == task.PENumber)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();

                // 3. Find index of the updated task
                int idx = allTasks.FindIndex(t => t.Id == taskId);

                // 4. Update only the create/target dates for subsequent tasks
                DateTime prevCompleteDate = estimatedTime;
                for (int i = idx + 1; i < allTasks.Count; i++)
                {
                    var currentTask = allTasks[i];

                    // Set created date as previous task's complete date
                    currentTask.TaskCreatedDate = prevCompleteDate;

                    // Parse OLA (assume it's in days, as int)
                    int olaDays = 0;
                    int.TryParse(currentTask.OLA, out olaDays);

                    // Set complete date as created date + OLA days
                    currentTask.TaskCompleteDate = currentTask.TaskCreatedDate.AddDays(olaDays);

                    prevCompleteDate = currentTask.TaskCompleteDate;
                }

                await _context.SaveChangesAsync();
                
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
                    _context.Update(pETask);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task updated successfully.";
                    // Redirect to the PETask details page
                    return RedirectToAction("Details", new { id = pETask.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PETasks.Any(e => e.Id == pETask.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            // If model state is invalid, stay on edit page
            return View(pETask);
        }
        [HttpGet]
        public IActionResult GetForPE(string peNumber)
        {
            var tasks = _context.PETasks
                .Where(t => t.PENumber == peNumber)
                .Select(t => new { id = t.Id, name = t.Task })
                .ToList();
            return Json(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> GetEstimationHistory(int id)
        {
            var history = await _context.TaskEstimationHistory
                .Where(h => h.TaskId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.EstimatedDate,
                    h.CreatedAt
                })
                .ToListAsync();

            return Json(history);
        }
    }
}