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
                .Include(e => e.PETask)
                .Include(e => e.IgnoredBy)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (escalation == null)
            {
                return NotFound();
            }
            
            // Mark as read when viewed
            if (!escalation.IsRead)
            {
                escalation.IsRead = true;
                await _context.SaveChangesAsync();
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
            
            // Get recipient information if available
            string recipientName = "Unknown";
            string recipientRole = "Unknown";
            
            if (escalation.RecipientId.HasValue)
            {
                var recipient = await _context.Users
                    .Include(u => u.UserRole)
                    .FirstOrDefaultAsync(u => u.Id == escalation.RecipientId.Value);
                
                if (recipient != null)
                {
                    recipientName = recipient.Name;
                    recipientRole = recipient.UserRole?.Name ?? "Unknown Role";
                }
            }
                string resolvedByName = "Not resolved";
    if (escalation.IgnoredById.HasValue)
    {
        var resolver = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == escalation.IgnoredById.Value);
        resolvedByName = resolver?.Name ?? "Unknown User";
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
                RecipientId = escalation.RecipientId,
                TaskId = escalation.TaskId,
                TaskName = escalation.PETask?.Task ?? "Unknown Task",  // Make sure this is set
                PENumber = escalation.PETask?.PENumber ?? "Unknown",
                RecipientName = recipientName,
                RecipientRole = recipientRole,
                // Include any other task-related properties you want to display
                TaskStatus = escalation.PETask?.TaskStatus,
                ResolvedByName = resolvedByName,  // Add this property to show name instead of ID

            };
            
            ViewData["PlannedEventId"] = plannedEventId;
            
            return View(viewModel); // Pass the view model instead of the entity
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
                var userWorkgroups = currentUser.UserWorkGroups
                    .Select(uwg => uwg.WorkGroup.Name)
                    .ToList();
                    

                
                // Get escalations where this user is the recipient
                var escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Include(e => e.IgnoredBy)
                    .Where(e => e.RecipientId == userId)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
                
                // Manually retrieve recipient information
                var userIds = escalations.Where(e => e.RecipientId.HasValue).Select(e => e.RecipientId.Value).Distinct().ToList();
                
                // Fetch users data and create a lookup dictionary
                var users = await _context.Users
                    .Include(u => u.UserRole)
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u);
                
                // Map to view models
                var viewModels = escalations.Select(e => 
                {
                    // Get recipient information if available
                    string recipientName = "Unknown";
                    string recipientRole = "Unknown Role";
                    if (e.RecipientId.HasValue && users.TryGetValue(e.RecipientId.Value, out var recipient))
                    {
                        recipientName = recipient.Name;
                        recipientRole = recipient.UserRole?.Name ?? "Unknown Role";
                    }
                    
                    return new EscalationViewModel
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
                        RecipientId = e.RecipientId,
                        RecipientName = recipientName,
                        RecipientRole = recipientRole,
                        ResolvedByName = e.IgnoredBy?.Name ?? "Not Resolved"
                    };
                }).ToList();

                
                return View(viewModels);
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
                // var task = await _context.PETasks.FindAsync(escalation.TaskId);
                // if (task != null)
                // {
                //      Make sure the IsOLAViolate property exists using reflection
                //     var property = typeof(PETask).GetProperty("IsOLAViolate");
                //     if (property != null)
                //     {
                //         property.SetValue(task, false);
                //         _context.Update(task);
                //     }
                // }

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