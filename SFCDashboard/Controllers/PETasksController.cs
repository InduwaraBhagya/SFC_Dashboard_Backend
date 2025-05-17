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
            var task = await _context.PETasks.FindAsync(id);
            if (task == null || !task.UrgentRequested)
            {
                return NotFound();
            }

            // Extract the originally requested reason from Priority field if it exists
            string originalReason = "";
            if (task.Priority?.Contains("URGENT REQUEST PENDING: ") == true)
            {
                var startIndex = task.Priority.IndexOf("URGENT REQUEST PENDING: ") + "URGENT REQUEST PENDING: ".Length;
                var endIndex = task.Priority.IndexOf("]", startIndex);
                if (endIndex > startIndex)
                {
                    originalReason = task.Priority.Substring(startIndex, endIndex - startIndex);
                }
            }

            // Clear the urgent requested flag
            task.UrgentRequested = false;

            // Remove the pending marker from the priority
            task.Priority = task.Priority?.Replace("[URGENT REQUEST PENDING]", "").Trim();

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    task.IsUrgent = true;  // Only mark THIS task as urgent
                    task.Priority = (task.Priority ?? "") + " [URGENT: Opening Ceremony - Priority 1]";
                    break;

                case "CriticalCustomer":
                    task.IsUrgent = true;  // Only mark THIS task as urgent
                    task.Priority = (task.Priority ?? "") + " [URGENT: Critical Customer - Priority 2]";
                    break;

                case "NetworkOutage":
                    task.IsUrgent = true;  // Only mark THIS task as urgent
                    task.Priority = (task.Priority ?? "") + " [URGENT: Network Outage - Priority 0]";
                    break;

                case "Reject":
                    // Just clearing the UrgentRequested flag and adding rejection note
                    task.Priority = (task.Priority ?? "") + " [Urgent Request Rejected]";
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid option selected.";
                    return RedirectToAction(nameof(UrgentRequestsList));
            }

            _context.Update(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task ID {taskId} urgent request processed with reason: {reason}", id, urgentReason);
            TempData["SuccessMessage"] = "Task urgent request processed.";
            return RedirectToAction(nameof(UrgentRequestsList));
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
            var task = await _context.PETasks.FindAsync(taskId);
            if (task == null)
                return NotFound();

            task.EstimatedTime = estimatedTime;
            await _context.SaveChangesAsync();

            // For AJAX: return 200 OK with no content
            return Ok();
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
    }
}