using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    [Authorize]
    public class EscalationController : BaseController
    {
        private readonly IEscalationsApiClient _escalationsApi;
        private readonly IPETasksApiClient _peTasksApiClient;
        private readonly IPlannedEventsApiClient _plannedEventsApiClient;
        private readonly IPEIssuesApiClient _peIssuesApiClient;
        private readonly IPEIssueResolutionsApiClient _peIssueResolutionsApiClient;
        // Removed obsolete constructor
        public EscalationController(
            IEscalationsApiClient escalationsApi,
            IPETasksApiClient peTasksApiClient,
            IPlannedEventsApiClient plannedEventsApiClient,
            IPEIssuesApiClient peIssuesApiClient,
            IPEIssueResolutionsApiClient peIssueResolutionsApiClient,
            IUsersApiClient usersApiClient
        ) : base(usersApiClient)
        {
            _escalationsApi = escalationsApi;
            _peTasksApiClient = peTasksApiClient;
            _plannedEventsApiClient = plannedEventsApiClient;
            _peIssuesApiClient = peIssuesApiClient;
            _peIssueResolutionsApiClient = peIssueResolutionsApiClient;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUserEscalations(string filter = "unread")
        {
            // Get current user ID using the provided method
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get current user with role information via API client
            var currentUser = await _usersApiClient.GetByIdAsync(userId);
            if (currentUser == null || currentUser.UserRole == null)
                return Json(new { count = 0, escalations = new List<object>() });

            int userRoleLevel = currentUser.UserRole.Level;
            var (_, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var escalations = await _escalationsApi.GetEscalationsByUserRoleAsync(
                userRoleLevel,
                canViewAll ? null : userWorkgroupNames);
            if (filter == "unread")
            {
                escalations = escalations.Where(e => !e.IsRead).ToList();
            }
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
            var escalation = await _escalationsApi.GetByIdAsync(id);
            if (escalation == null)
            {
                return NotFound();
            }

            // Mark as read when viewed
            if (!escalation.IsRead)
            {
                await _escalationsApi.MarkAsReadAsync(id);
            }

            // Get the PETask and PlannedEvent using API clients
            var task = escalation.PETask;
            if (task == null && escalation.TaskId > 0)
            {
                task = await _peTasksApiClient.GetByIdAsync(escalation.TaskId);
            }
            int? plannedEventId = null;
            if (task != null && !string.IsNullOrEmpty(task.PENumber))
            {
                var plannedEvent = await _plannedEventsApiClient.GetPlannedEventByPENumberAsync(task.PENumber);
                plannedEventId = plannedEvent?.Id;
            }

            var viewModel = new EscalationViewModel
            {
                Id = escalation.Id,
                Title = escalation.Title,
                Message = escalation.Message,
                CreatedAt = escalation.CreatedAt,
                IsRead = escalation.IsRead,
                TaskId = escalation.TaskId,
                TaskName = task?.Task ?? "Unknown Task",
                PENumber = task?.PENumber ?? "Unknown",
                TaskStatus = task?.TaskStatus ?? "Unknown",
                Level = escalation.Level ?? 0,
            };

            ViewData["PlannedEventId"] = plannedEventId;
            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ignore(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest("Reason is required");
            }

            // Simply mark the escalation as read since we removed resolve functionality
            await _escalationsApi.MarkAsReadAsync(id);
            
            return RedirectToAction("Index", "Home");
        }
        
        // POST: Escalation/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { success = false });
            
            // Get current user with role information via API client
            var currentUser = await _usersApiClient.GetByIdAsync(userId);
            if (currentUser == null || currentUser.UserRole == null)
                return Json(new { success = false });

            int userRoleLevel = currentUser.UserRole.Level;
            var (_, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var escalations = await _escalationsApi.GetEscalationsByUserRoleAsync(
                userRoleLevel,
                canViewAll ? null : userWorkgroupNames);
            var unreadEscalations = escalations.Where(e => !e.IsRead).ToList();
            foreach (var escalation in unreadEscalations)
            {
                await _escalationsApi.MarkAsReadAsync(escalation.Id);
            }
            return Json(new { success = true });
        }

        // The provided method for getting current user ID
        private async Task<int> GetCurrentUserIdAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
            var user = await _usersApiClient.GetByServiceIdAsync(serviceIdShort);
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
                
                // Get current user with role information via API client
                var currentUser = await _usersApiClient.GetByIdAsync(userId);
                if (currentUser == null || currentUser.UserRole == null)
                    return RedirectToAction("Index", "Home");

                int userRoleLevel = currentUser.UserRole.Level;
                var (_, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var escalations = await _escalationsApi.GetEscalationsByUserRoleAsync(
                    userRoleLevel,
                    canViewAll ? null : userWorkgroupNames);
                var viewModels = escalations.Select(e => new EscalationViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Message = e.Message,
                    CreatedAt = e.CreatedAt,
                    IsRead = e.IsRead,
                    TaskId = e.TaskId,
                    TaskName = e.PETask?.Task ?? "Unknown Task",
                    PENumber = e.PETask?.PENumber ?? "Unknown",
                    TaskStatus = e.PETask?.TaskStatus ?? "Unknown",
                    Level = e.Level ?? 0,
                    RecipientRole = GetRoleNameByLevel(e.Level ?? 0)
                }).ToList();
                return View(viewModels);
            }
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
                // Get resolution, issue, and planned event via API clients
                var resolution = await _peIssueResolutionsApiClient.GetByIdAsync(resolutionId);
                if (resolution == null)
                {
                    return Json(new { success = false, message = "Resolution request not found." });
                }

                var issue = await _peIssuesApiClient.GetByIdAsync(resolution.IssueId);
                if (issue == null)
                {
                    return Json(new { success = false, message = "Original issue not found." });
                }

                var pe = await _plannedEventsApiClient.GetByIdAsync(resolution.PlannedEventId);

                if (isConfirmed)
                {
                    // Use ConfirmResolutionAsync to trigger synchronization logic
                    // This method automatically handles setting IsResolved = true for the issue
                    await _peIssueResolutionsApiClient.ConfirmResolutionAsync(resolutionId, true);

                    // Note: Removed redundant issue updates as ConfirmResolutionAsync handles synchronization
                    // The API method automatically sets issue.IsResolved = true when resolution is confirmed
                    
                    // Handle original issue if this is a reply (but let ConfirmResolutionAsync handle the main issue)
                    if (issue.OriginalIssueId.HasValue)
                    {
                        var originalIssue = await _peIssuesApiClient.GetByIdAsync(issue.OriginalIssueId.Value);
                        if (originalIssue != null && !originalIssue.IsResolved)
                        {
                            // For original issues related to replies, we may need manual update
                            // But first check if ConfirmResolutionAsync already handled it
                            var refreshedOriginalIssue = await _peIssuesApiClient.GetByIdAsync(issue.OriginalIssueId.Value);
                            if (refreshedOriginalIssue != null && !refreshedOriginalIssue.IsResolved)
                            {
                                refreshedOriginalIssue.IsResolved = true;
                                await _peIssuesApiClient.UpdateAsync(refreshedOriginalIssue);
                            }
                        }
                    }

                    // Update planned event - ONLY if no other active issues remain
                    if (pe != null)
                    {
                        // Check if any unresolved root issues remain
                        var unresolvedIssues = await _peIssuesApiClient.GetByPlannedEventIdAsync(pe.Id);
                        var hasOtherActiveIssues = unresolvedIssues.Any(i => !i.IsResolved && i.OriginalIssueId == null);
                        pe.IsHold = hasOtherActiveIssues;
                        await _plannedEventsApiClient.UpdateAsync(pe);
                    }

                    return Json(new { success = true, message = "Resolution confirmed and issue marked as resolved." });
                }
                else
                {
                    // If rejected, delete the resolution request and create a notification via API client
                    await _peIssueResolutionsApiClient.DeleteAsync(resolutionId);

                    // Notify the user who attempted to fix the issue
                    var notification = new PEIssue
                    {
                        PlannedEventId = resolution.PlannedEventId,
                        PETaskId = issue.PETaskId,
                        SenderId = issue.SenderId,
                        ReceiverId = issue.ReceiverId,
                        IssueText = $"RESOLUTION REJECTED: The fix was not accepted. Please try again.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        IsReply = true,
                        IsReminder = false,
                        OriginalIssueId = issue.OriginalIssueId ?? issue.Id
                    };
                    await _peIssuesApiClient.CreateAsync(notification);
                    return Json(new { success = true, message = "Resolution rejected. The responder has been notified." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing resolution: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "A reason is required to resolve the escalation.";
                    return RedirectToAction("Details", new { id });
                }

                // Simply mark the escalation as read since we removed resolve functionality
                await _escalationsApi.MarkAsReadAsync(id);

                TempData["SuccessMessage"] = "Escalation has been marked as read.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating escalation: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetEscalationServiceStatus()
        {
            try
            {
                bool isEnabled = await _escalationsApi.IsEscalationEnabledAsync();
                return Json(new { enabled = isEnabled });
            }
            catch (Exception ex)
            {
                return Json(new { enabled = true, error = ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleEscalationService()
        {
            try
            {
                // Get current user ID for authorization check (optional)
                int userId = await GetCurrentUserIdAsync();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }
                
                // Get current status and toggle it
                bool currentStatus = await _escalationsApi.IsEscalationEnabledAsync();
                bool newStatus = !currentStatus;
                
                await _escalationsApi.SetEscalationEnabledAsync(newStatus);
                
                string statusMessage = newStatus ? "enabled" : "disabled";
                return Json(new { 
                    success = true, 
                    enabled = newStatus, 
                    message = $"Escalation service has been {statusMessage}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error toggling escalation service: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ManualEscalationCheck()
        {
            try
            {
                var result = await _escalationsApi.ManualEscalationCheckAsync();
                return Json(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error running manual escalation check: {ex.Message}" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOLAViolatedTasksDebugInfo()
        {
            try
            {
                var debugInfo = await _escalationsApi.GetOLAViolatedTasksDebugInfoAsync();
                return Json(new { success = true, data = debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting debug info: {ex.Message}" });
            }
        }

        // Helper method to get current user's workgroups
        private async Task<(List<int> workgroupIds, List<string> workgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync()
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApiClient.GetByIdAsync(currentUserId);
            if (currentUser == null)
                return (new List<int>(), new List<string>(), false);

            var workgroupIds = currentUser.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>();
            var workgroupNames = currentUser.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Name).ToList() ?? new List<string>();
            bool canViewAll = currentUser.UserRole?.HasPermission("ViewAll") == true;
            return (workgroupIds, workgroupNames, canViewAll);
        }
    }
}
