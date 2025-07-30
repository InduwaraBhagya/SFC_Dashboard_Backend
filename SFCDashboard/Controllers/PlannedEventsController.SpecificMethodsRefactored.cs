using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    // Example of how to refactor specific methods using API services
    public partial class PlannedEventsController
    {
        /// <summary>
        /// Refactored InProgressRecords method using API services
        /// This shows the complete pattern for refactoring complex methods
        /// </summary>
        public async Task<IActionResult> InProgressRecordsRefactored(int? workgroupId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var currentUser = await _usersApi.GetUserAsync(currentUserId);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                // Set permissions
                ViewData["SelectedWorkgroupId"] = workgroupId;

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    // Admin user: filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(
                                new List<string> { workgroup.Name }, hasDrawFiberAccess, canViewAll);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(
                                new List<string>(), hasDrawFiberAccess, canViewAll);
                        }
                    }
                    else
                    {
                        // Show all in-progress records
                        records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(
                            new List<string>(), hasDrawFiberAccess, canViewAll);
                    }
                }
                else
                {
                    // Regular user: show records for their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(
                            userWorkgroupNames, hasDrawFiberAccess, canViewAll);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                return View(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading in-progress records");
                return View(new List<PlannedEvent>());
            }
        }

        /// <summary>
        /// Refactored OLAViolateRecords method using API services
        /// </summary>
        public async Task<IActionResult> OLAViolateRecordsRefactored(int? workgroupId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var currentUser = await _usersApi.GetUserAsync(currentUserId);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                ViewData["SelectedWorkgroupId"] = workgroupId;

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(
                                new List<string> { workgroup.Name }, hasDrawFiberAccess);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(
                                new List<string>(), hasDrawFiberAccess);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(
                            new List<string>(), hasDrawFiberAccess);
                    }
                }
                else
                {
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(
                            userWorkgroupNames, hasDrawFiberAccess);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                return View(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading OLA violate records");
                return View(new List<PlannedEvent>());
            }
        }

        /// <summary>
        /// Refactored HoldRecords method using API services
        /// </summary>
        public async Task<IActionResult> HoldRecordsRefactored(int? workgroupId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var currentUser = await _usersApi.GetUserAsync(currentUserId);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                ViewData["SelectedWorkgroupId"] = workgroupId;

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetHoldPlannedEventsAsync(
                                new List<string> { workgroup.Name }, hasDrawFiberAccess);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetHoldPlannedEventsAsync(
                                new List<string>(), hasDrawFiberAccess);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetHoldPlannedEventsAsync(
                            new List<string>(), hasDrawFiberAccess);
                    }
                }
                else
                {
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetHoldPlannedEventsAsync(
                            userWorkgroupNames, hasDrawFiberAccess);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                return View(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading hold records");
                return View(new List<PlannedEvent>());
            }
        }

        /// <summary>
        /// Refactored UrgentRecords method using API services
        /// </summary>
        public async Task<IActionResult> UrgentRecordsRefactored(int? workgroupId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var currentUser = await _usersApi.GetUserAsync(currentUserId);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                ViewData["SelectedWorkgroupId"] = workgroupId;

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(
                                new List<string> { workgroup.Name }, hasDrawFiberAccess);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(
                                new List<string>(), hasDrawFiberAccess);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(
                            new List<string>(), hasDrawFiberAccess);
                    }
                }
                else
                {
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(
                            userWorkgroupNames, hasDrawFiberAccess);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                return View(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading urgent records");
                return View(new List<PlannedEvent>());
            }
        }

        /// <summary>
        /// Refactored Edit GET method using API services
        /// </summary>
        public async Task<IActionResult> EditRefactored(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plannedEvent = await _plannedEventsApi.GetPlannedEventAsync(id.Value);
            if (plannedEvent == null)
            {
                return NotFound();
            }
            return View(plannedEvent);
        }

        /// <summary>
        /// Refactored Create GET method using API services
        /// </summary>
        public IActionResult CreateRefactored()
        {
            return View();
        }
    }
}
