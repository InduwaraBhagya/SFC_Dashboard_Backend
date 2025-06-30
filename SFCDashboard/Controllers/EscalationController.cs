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
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (currentUser == null || currentUser.UserRoleId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get user's role level (0 for normal users, higher for management)
            int userRoleLevel = currentUser.UserRole?.Level ?? 0;
            
            // Get escalations based on user role level using the service
            var escalations = await _escalationService.GetEscalationsByUserRoleAsync(userRoleLevel);
            
            // Apply filter
            if (filter == "unread")
            {
                escalations = escalations.Where(e => !e.IsRead).ToList();
            }
            
            // Project to anonymous objects with all required fields
            var result = escalations.Select(e => new {
                id = e.Id,
                taskId = e.TaskId,
                level = e.Level,
                createdAt = e.CreatedAt,
                isRead = e.IsRead,
                title = e.Title,
                message = e.Message
            }).ToList();
            
            return Json(new { count = result.Count, escalations = result });
        }        public async Task<IActionResult> Details(int id)
        {
            var escalation = await _context.Escalations
                .Include(e => e.PETask) // Fixed navigation property
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (escalation == null)
            {
                return NotFound();
            }
            
            // Mark as read when viewed
            if (!escalation.IsRead)
            {
                await _escalationService.MarkAsReadAsync(id);
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
              // Create the view model from the entity
            var viewModel = new EscalationViewModel
            {
                Id = escalation.Id,
                Title = escalation.Title,
                Message = escalation.Message,
                CreatedAt = escalation.CreatedAt,
                IsRead = escalation.IsRead,
                IsResolved = escalation.IsResolved,
                TaskId = escalation.TaskId,
                TaskName = escalation.PETask?.Task ?? "Unknown Task",  // Fixed navigation
                PENumber = escalation.PETask?.PENumber ?? "Unknown",   // Fixed navigation
                TaskStatus = escalation.PETask?.TaskStatus,            // Fixed navigation
                Level = escalation.Level ?? 0,
            };
            
            ViewData["PlannedEventId"] = plannedEventId;
            
            return View(viewModel);
        }
        
        [HttpPost]
        public async Task<IActionResult> Ignore(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest("Reason is required");
            }
            
            // Use the service to resolve the escalation
            await _escalationService.ResolveEscalationAsync(id);
            
            return RedirectToAction("Index", "Home");
        }
        
        // POST: Escalation/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { success = false });
            
            // Get current user with role information
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (currentUser == null)
                return Json(new { success = false });
            
            // Get user's role level
            int userRoleLevel = currentUser.UserRole?.Level ?? 0;
            
            // Get all unread escalations for this user's role level
            var escalations = await _escalationService.GetEscalationsByUserRoleAsync(userRoleLevel);
            var unreadEscalations = escalations.Where(e => !e.IsRead).ToList();
            
            // Mark them all as read
            foreach (var escalation in unreadEscalations)
            {
                await _escalationService.MarkAsReadAsync(escalation.Id);
            }
            
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
                
                // Get the current user with role information
                var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (currentUser == null)
                    return RedirectToAction("Index", "Home");
                
                // Get user's role level
                int userRoleLevel = currentUser.UserRole?.Level ?? 0;
                
                // Get escalations using the service
                var escalations = await _escalationService.GetEscalationsByUserRoleAsync(userRoleLevel);
                  // Map to view models
                var viewModels = escalations.Select(e => new EscalationViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Message = e.Message,
                    CreatedAt = e.CreatedAt,
                    IsRead = e.IsRead,
                    IsResolved = e.IsResolved,
                    TaskId = e.TaskId,
                    TaskName = e.PETask?.Task ?? "Unknown Task",
                    PENumber = e.PETask?.PENumber ?? "Unknown",
                    TaskStatus = e.PETask?.TaskStatus,
                    Level = e.Level ?? 0,
                    // Role-based display instead of recipient
                    RecipientRole = GetRoleNameByLevel(e.Level ?? 0)
                }).ToList();

                return View(viewModels);            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving escalations.";
                return RedirectToAction("Index", "Home");
            }
        }
        

        // Helper method to get role name by escalation level
        private string GetRoleNameByLevel(int level)
        {
            return level switch
            {
                1 => "Normal Users",
                2 => "Supervisors",
                3 => "Managers",
                _ => "All Users"
            };
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
                    _context.Update(resolution);                    // Mark issue as resolved
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
                        IsReminder = false,
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

                // Use the service to resolve the escalation
                await _escalationService.ResolveEscalationAsync(id);

                TempData["SuccessMessage"] = "Escalation has been resolved.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error resolving escalation: {ex.Message}";                return RedirectToAction("Details", new { id });
            }
        }
    }
}