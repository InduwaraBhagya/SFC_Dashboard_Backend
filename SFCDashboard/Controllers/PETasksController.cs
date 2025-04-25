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

        // POST: PETasks/RequestUrgent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestUrgent(int id)
        {
            var task = await _context.PETasks.FindAsync(id);
            if (task == null || task.TaskStatus != "INPROGRESS")
            {
                return NotFound();
            }

            // Mark the task as pending urgent confirmation
            task.TaskStatus = "PENDING_URGENT_CONFIRMATION";
            _context.Update(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task ID {taskId} marked as pending urgent confirmation", id);
            TempData["SuccessMessage"] = "Urgent request submitted for approval.";
            
            // Return to the referring page or planned event details
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            
            return RedirectToAction("Details", "PlannedEvents", new { id = task.PlannedEvent?.Id });
        }

        // GET: PETasks/UrgentRequestConfirmation/5
        public async Task<IActionResult> UrgentRequestConfirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == id && t.TaskStatus == "PENDING_URGENT_CONFIRMATION");

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: PETasks/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _context.PETasks
                .Where(t => t.TaskStatus == "PENDING_URGENT_CONFIRMATION")
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
            if (task == null || task.TaskStatus != "PENDING_URGENT_CONFIRMATION")
            {
                return NotFound();
            }

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    task.TaskStatus = "INPROGRESS";
                    task.IsUrgent = true;
                    task.Priority = (task.Priority ?? "") + " [URGENT: Opening Ceremony - Priority 1]";
                    break;

                case "CriticalCustomer":
                    task.TaskStatus = "INPROGRESS";
                    task.IsUrgent = true;
                    task.Priority = (task.Priority ?? "") + " [URGENT: Critical Customer - Priority 2]";
                    break;

                case "NetworkOutage":
                    task.TaskStatus = "INPROGRESS";
                    task.IsUrgent = true;
                    task.Priority = (task.Priority ?? "") + " [URGENT: Network Outage - Priority 0]";
                    break;

                case "Reject":
                    task.TaskStatus = "INPROGRESS";
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
    }
}