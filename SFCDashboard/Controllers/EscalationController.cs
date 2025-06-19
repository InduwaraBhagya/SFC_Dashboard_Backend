using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFCDashboard.Controllers
{
    [Authorize]
    public class EscalationController : Controller
    {
        private readonly EscalationService _escalationService;
        private readonly ApplicationDbContext _context;
        
        public EscalationController(EscalationService escalationService, ApplicationDbContext context)
        {
            _escalationService = escalationService;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUserEscalations(string filter = "unread")
        {
            // Get current user ID using the provided method
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get the current user with role information
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (currentUser == null || currentUser.UserRoleId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get escalations based on filter - COMPARING USER ROLE ID
            List<Escalation> escalations;
            if (filter == "all")
            {
                // Get all escalations for this user's role (read and unread, but not ignored)
                escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Where(e => e.RecipientId == currentUser.Id)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // Get only unread escalations for this user's role
                escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Where(e => e.RecipientId == currentUser.Id )
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            
            // Project to anonymous objects with all required fields
            var result = escalations.Select(e => new {
                id = e.Id,
                taskId = e.TaskId,
                taskWorkGroup = e.PETask?.TaskWorkGroup, // Include TaskWorkGroup from PETask
                level = e.Level,
                createdAt = e.CreatedAt,
                isRead = e.IsRead
            }).ToList();
            
            return Json(new { count = result.Count, escalations = result });
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var escalation = await _context.Escalations
                .Include(e => e.PETask) // Assuming you have navigation property
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (escalation == null)
            {
                return NotFound();
            }
            
            // Get the planned event ID
            int? plannedEventId = null;
            var task = await _context.PETasks.FirstOrDefaultAsync(t => t.Id == escalation.TaskId);
            if (task != null && !string.IsNullOrEmpty(task.PENumber))
            {
                var plannedEvent = await _context.PlannedEvents
                    .FirstOrDefaultAsync(pe => pe.PeNumber == task.PENumber);
                plannedEventId = plannedEvent?.Id;
            }
            
            // Pass the ID to the view
            ViewData["PlannedEventId"] = plannedEventId;
            
            return View(escalation);
        }
        
        [HttpPost]
        public async Task<IActionResult> Ignore(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest("Reason is required");
            }
            
            // Get current user ID (implement according to your auth system)
            int userId = int.Parse(User.FindFirst("UserId").Value);
            
            //await _escalationService.IgnoreEscalationAsync(id, reason, userId);
            
            return RedirectToAction("Index", "Home");
        }
        
        // POST: Escalation/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { success = false });
            
            // Get all unread escalations for this user
            var unreadEscalations = await _context.Escalations
                .Where(e => e.RecipientId == userId && e.IsRead != true)
                .ToListAsync();
            
            // Mark them all as read
            foreach (var escalation in unreadEscalations)
            {
                escalation.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        // The provided method for getting current user ID
        private async Task<int> GetCurrentUserIdAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            // Extract the substring before the query
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);
            return user?.Id ?? 0;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID
                int userId = await GetCurrentUserIdAsync();
                if (userId == 0)
                    return RedirectToAction("Index", "Home");
                
                // Get the current user with role and workgroup information
                var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (currentUser == null)
                    return RedirectToAction("Index", "Home");
                
                // Get user's role ID and workgroup names
                int userRoleId = currentUser.UserRoleId ?? 0;
                var userWorkgroups = currentUser.UserWorkGroups
                    .Select(uwg => uwg.WorkGroup.Name)
                    .ToList();
                    

                
                // Get escalations where:
                // 1. This user is the direct recipient (RecipientId = userId)
                // OR
                // 2. The user has the same role as the recipient AND is in the same workgroup as the escalated task
                var escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Where(e => e.RecipientId == userRoleId && 
                            userWorkgroups.Contains(e.PETask.TaskWorkGroup))
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
                
                
                return View(escalations);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving escalations.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmResolution(int resolutionId, bool isConfirmed)
        {
            try
            {
                var resolution = await _context.PEIssueResolutions.FindAsync(resolutionId);
                if (resolution == null)
                {
                    return Json(new { success = false, message = "Resolution request not found." });
                }

                var issue = await _context.PEIssues.FindAsync(resolution.IssueId);
                if (issue == null)
                {
                    return Json(new { success = false, message = "Original issue not found." });
                }

                var pe = await _context.PlannedEvents.FindAsync(resolution.PlannedEventId);

                if (isConfirmed)
                {
                    // Update resolution status
                    resolution.IsConfirmed = true;
                    resolution.ConfirmedDate = DateTime.Now;
                    _context.Update(resolution);

                    // Mark issue as resolved
                    if (issue != null)
                    {
                        issue.IsResolved = true;
                        _context.Update(issue);

                        // Find and mark the original issue as resolved if this is a reply
                        if (issue.OriginalIssueId.HasValue)
                        {
                            var originalIssue = await _context.PEIssues.FindAsync(issue.OriginalIssueId);
                            if (originalIssue != null && !originalIssue.IsResolved)
                            {
                                originalIssue.IsResolved = true;
                                _context.Update(originalIssue);
                            }
                        }
                    }

                    // Update planned event - ONLY if no other active issues remain
                    if (pe != null)
                    {
                        // Check if any unresolved root issues remain
                        var hasOtherActiveIssues = await _context.PEIssues
                            .AnyAsync(i => i.PlannedEventId == pe.Id &&
                                      !i.IsResolved &&
                                      i.OriginalIssueId == null);
                        
                        pe.IsHold = hasOtherActiveIssues; // Set IsHold to false if no active issues remain
                        _context.Update(pe);
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Resolution confirmed and issue marked as resolved." });
                }
                else
                {
                    // If rejected, delete the resolution request and create a notification
                    _context.Remove(resolution);

                    // Notify the user who attempted to fix the issue
                    var notification = new PEIssue
                    {
                        PlannedEventId = resolution.PlannedEventId,
                        PETaskId = issue.PETaskId,
                        SenderId = issue.SenderId, // Original reporter
                        ReceiverId = issue.ReceiverId, // User who tried to fix it
                        IssueText = $"RESOLUTION REJECTED: The fix was not accepted. Please try again.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        IsReply = true,
                        OriginalIssueId = issue.Id
                    };

                    _context.PEIssues.Add(notification);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = "Resolution rejected. The responder has been notified." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing resolution: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Resolve(int id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "A reason is required to resolve the escalation.";
                    return RedirectToAction("Details", new { id });
                }

                var escalation = await _context.Escalations.FindAsync(id);
                if (escalation == null)
                {
                    TempData["ErrorMessage"] = "Escalation not found.";
                    return RedirectToAction("Index");
                }

                // Get current user for tracking who resolved it
                int userId = await GetCurrentUserIdAsync();

                // Update the escalation
                escalation.IsResolved = true;
                escalation.IgnoreReason = reason; // Use the IgnoreReason field to store resolution reason
                escalation.IgnoredAt = DateTime.Now;
                escalation.IgnoredById = userId;

                // Update the task's OLA violation status if it exists
                var task = await _context.PETasks.FindAsync(escalation.TaskId);
                if (task != null)
                {
                    // Make sure the IsOLAViolate property exists using reflection
                    var property = typeof(PETask).GetProperty("IsOLAViolate");
                    if (property != null)
                    {
                        property.SetValue(task, false);
                        _context.Update(task);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Escalation has been resolved.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error resolving escalation: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}