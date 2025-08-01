using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    // This partial class contains refactored helper methods using API services
    public partial class PlannedEventsController
    {
        // Refactored helper methods using API services
        private async Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (new List<int>(), new List<string>(), false);

            return await _usersApi.GetCurrentUserWorkGroupsAsync(serviceId);
        }

        private async Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (0, string.Empty);

            return await _usersApi.GetCurrentUserWorkGroupAsync(serviceId);
        }

        private async Task<bool> HasMultipleWorkgroups()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            return await _usersApi.HasMultipleWorkgroupsAsync(serviceId);
        }

        private async Task<bool> IsUserInSalesWorkgroup()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            return await _usersApi.IsUserInSalesWorkgroupAsync(serviceId);
        }

        private async Task<bool> HasDrawFiberAccessAsync(int userId)
        {
            return await _usersApi.HasDrawFiberAccessAsync(userId);
        }

        private async Task<List<string>> GetUserAssignedCustomersAsync()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            return await _usersApi.GetUserAssignedCustomersAsync(currentUserId);
        }

        private async Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroups()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (new List<string>(), false);

            return await _usersApi.GetUserSalesWorkgroupsAsync(serviceId);
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            return await _usersApi.GetCurrentUserIdAsync(serviceId);
        }

        private async Task<IActionResult?> RedirectBasedOnUserType(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, List<int> workgroupIds, int pageIndex)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            // Get current user's workgroup info first to check ViewAll permission
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Check for sales workgroup, but only redirect if user doesn't have ViewAll permission
            if (await IsUserInSalesWorkgroup() && !canViewAll)
            {
                return RedirectToAction(nameof(SalesView), new
                {
                    searchType,
                    peNumber,
                    customer,
                    jobReference,
                    soNumber,
                    pageIndex
                });
            }

            // If user does NOT have ViewAll and has multiple workgroups, redirect to MultiWorkgroupView
            if (!canViewAll && userWorkgroupIds.Count > 1)
            {
                return RedirectToAction(nameof(MultiWorkgroupView), new
                {
                    searchType,
                    peNumber,
                    customer,
                    jobReference,
                    soNumber,
                    workgroupIds,
                    pageIndex
                });
            }

            // No redirect needed - user should see the regular Index view
            return null;
        }

        private async Task<IQueryable<PlannedEvent>> ApplyCustomerFilteringAsync(IQueryable<PlannedEvent> query, List<string> salesWorkgroups, bool canViewAll)
        {
            // Get user's assigned customers
            var assignedCustomers = await GetUserAssignedCustomersAsync();

            if (!canViewAll)
            {
                // Apply workgroup filtering first with case-insensitive matching
                // Convert workgroups to lowercase for comparison
                var lowerSalesWorkgroups = salesWorkgroups.Select(wg => wg.ToLower()).ToList();

                var beforeFilterCount = query.Count();
                query = query.Where(p => lowerSalesWorkgroups.Any(wg =>
                    (p.TaskWg != null && (
                        p.TaskWg.ToLower() == wg ||
                        p.TaskWg.ToLower().Contains(wg)
                    )) ||
                    (p.SectionHandledBy != null && (
                        p.SectionHandledBy.ToLower() == wg ||
                        p.SectionHandledBy.ToLower().Contains(wg)
                    ))
                ));
                var afterWorkgroupFilterCount = query.Count();

                // If user has assigned customers, further filter by those customers
                if (assignedCustomers.Any())
                {
                    query = query.Where(p => p.Customer != null && assignedCustomers.Contains(p.Customer));
                    var afterCustomerFilterCount = query.Count();
                }
            }
            else
            {
                // For users with ViewAll permission, still apply customer filtering if they have assigned customers
                if (assignedCustomers.Any())
                {
                    query = query.Where(p => p.Customer != null && assignedCustomers.Contains(p.Customer));
                }
            }

            return query;
        }

        private async Task<bool> PlannedEventExists(int id)
        {
            return await _plannedEventsApi.PlannedEventExistsAsync(id);
        }

        private string? ExtractUrgentRequestReason(string? priority)
        {
            if (string.IsNullOrEmpty(priority))
                return null;

            if (priority.Contains("Opening Ceremony"))
                return "Opening Ceremony - Priority 1";

            if (priority.Contains("Critical Customer"))
                return "Critical Customer - Priority 2";

            return null;
        }

        // Helper to extract date from PE number
        private DateTime? GetDateFromPeNumber(string peNumber)
        {
            // Expects format: PEYYYYMMDDxxxx
            if (string.IsNullOrEmpty(peNumber) || peNumber.Length < 10)
                return null;
            try
            {
                var year = int.Parse(peNumber.Substring(2, 4));
                var month = int.Parse(peNumber.Substring(6, 2));
                var day = int.Parse(peNumber.Substring(8, 2));
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        // Call this after loading tasks for a PE (e.g., in Details or when recalculating tasks)
        private void SetTaskDatesFromPeNumber(string peNumber, List<PETask> tasks)
        {
            var peCreatedDate = GetDateFromPeNumber(peNumber) ?? DateTime.Today;
            DateTime currentCreatedDate = peCreatedDate;

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];

                // For "Draw Fiber", use EstimatedTime if set
                if (task.Task?.Trim().ToLower() == "draw fiber" && task.EstimatedTime.HasValue)
                {
                    task.TaskCreatedDate = currentCreatedDate;
                    task.TaskCompleteDate = task.EstimatedTime.Value;
                    currentCreatedDate = task.TaskCompleteDate;
                }
                else
                {
                    task.TaskCreatedDate = currentCreatedDate;
                    int olaDays = 0;
                    int.TryParse(task.OLA, out olaDays);
                    task.TaskCompleteDate = currentCreatedDate.AddDays(olaDays);
                    currentCreatedDate = task.TaskCompleteDate;
                }
            }
        }
    }
}
