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
        public async Task<IActionResult> RequestUrgent(int id)
        {
            var task = await _context.PETasks.FindAsync(id);
            if (task == null || task.TaskStatus?.ToUpper() == "COMPLETED" || task.IsUrgent)
            {
                return NotFound();
            }

            // Mark the task with UrgentRequested flag instead of changing status
            task.UrgentRequested = true;
            task.Priority = (task.Priority ?? "") + " [URGENT REQUEST PENDING]";
            
            // Keep the original task status
            string originalStatus = task.TaskStatus;
            
            _context.Update(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task ID {taskId} marked with urgent request flag, status remains {status}", id, originalStatus);
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
                .FirstOrDefaultAsync(t => t.Id == id && t.UrgentRequested && !t.IsUrgent);

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

            // Clear the urgent requested flag
            task.UrgentRequested = false;
            
            // Remove the pending marker from the priority
            task.Priority = task.Priority?.Replace("[URGENT REQUEST PENDING]", "").Trim();

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    task.IsUrgent = true;
                    task.Priority = (task.Priority ?? "") + " [URGENT: Opening Ceremony - Priority 1]";
                    break;

                case "CriticalCustomer":
                    task.IsUrgent = true;
                    task.Priority = (task.Priority ?? "") + " [URGENT: Critical Customer - Priority 2]";
                    break;

                case "NetworkOutage":
                    task.IsUrgent = true;
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
    }
}