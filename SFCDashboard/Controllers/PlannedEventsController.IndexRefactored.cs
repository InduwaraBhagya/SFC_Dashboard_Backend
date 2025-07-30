using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    // Example refactored controller methods using API services
    public partial class PlannedEventsController
    {
        /// <summary>
        /// Refactored Index method using API services instead of direct database calls
        /// This demonstrates the new architecture pattern
        /// </summary>
        public async Task<IActionResult> IndexRefactored(string searchType, string? peNumber, string? customer,
            string? jobReference, string? soNumber, int? workgroupId, int pageIndex = 1)
        {
            try
            {
                // Get current user information using API service
                var currentUserId = await GetCurrentUserIdAsync();
                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Check for redirect conditions using API services
                var redirectResult = await RedirectBasedOnUserTypeRefactored(searchType, peNumber, customer, jobReference, soNumber, new List<int> { workgroupId ?? 0 }, pageIndex);
                if (redirectResult != null)
                {
                    return redirectResult;
                }

                // Get user workgroups using API service
                var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
                var hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);

                // Filter planned events using API service
                IEnumerable<PlannedEvent> plannedEvents;
                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        var selectedWorkgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (selectedWorkgroup != null)
                        {
                            plannedEvents = await _plannedEventsApi.GetPlannedEventsByWorkgroupAsync(
                                new List<string> { selectedWorkgroup.Name }, hasDrawFiberAccess);
                            ViewData["FilteredWorkgroup"] = selectedWorkgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }
                        else
                        {
                            plannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
                        }
                    }
                    else
                    {
                        plannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
                    }
                }
                else
                {
                    // Regular user: show records for their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        plannedEvents = await _plannedEventsApi.GetPlannedEventsByWorkgroupAsync(userWorkgroupNames, hasDrawFiberAccess);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                    else
                    {
                        plannedEvents = new List<PlannedEvent>();
                    }
                }

                // Calculate dashboard counts using API services
                ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountAsync(userWorkgroupNames, hasDrawFiberAccess);
                ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountAsync(userWorkgroupNames, hasDrawFiberAccess);
                ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolateCountAsync(userWorkgroupNames, hasDrawFiberAccess);
                ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountAsync(userWorkgroupNames, hasDrawFiberAccess);

                // Get user's primary workgroup for task queue
                var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
                ViewBag.UserWorkgroupId = userWorkgroupId;

                // Get next task using API client
                var nextTaskList = await _taskQueueApiClient.GetPrioritizedTasksAsync(
                    workgroupId: userWorkgroupId,
                    take: 1);
                ViewBag.NextTask = nextTaskList;
                ViewBag.HasNextTask = nextTaskList != null && nextTaskList.Any();

                // Get pending requests using API services
                var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();
                ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

                var pendingTaskRequests = await _peTasksApi.GetPendingUrgentTaskRequestsAsync();
                ViewData["PendingTaskRequests"] = pendingTaskRequests;

                // Get inbox issues using API service
                var inboxIssues = await _peIssuesApi.GetInboxIssuesAsync(currentUserId);

                // Calculate unread count
                var unreadCount = inboxIssues.Count(i => !i.IsRead);
                unreadCount += pendingUrgentRequests.Count();
                unreadCount += pendingTaskRequests.Count();

                ViewData["InboxIssues"] = inboxIssues;
                ViewData["TotalMessages"] = inboxIssues.Count();
                ViewData["UnreadMessages"] = unreadCount;

                // Process resolution details using API service
                if (inboxIssues.Any())
                {
                    var issueIds = inboxIssues
                        .Where(i => i.IsResolutionRequest)
                        .Select(i => i.OriginalIssueId ?? i.Id)
                        .ToList();

                    if (issueIds.Any())
                    {
                        var resolutionsByIssueId = await _peIssueResolutionsApi.GetResolutionsByIssueIdsAsync(issueIds);
                        ViewBag.ResolutionsByIssueId = resolutionsByIssueId;
                    }
                }

                ViewBag.InboxIssues = inboxIssues;

                // Set search parameters
                ViewData["SearchType"] = searchType ?? "peNumber";
                ViewData["PENumberFilter"] = peNumber;
                ViewData["CustomerFilter"] = customer;
                ViewData["JobReferenceFilter"] = jobReference;
                ViewData["SONumberFilter"] = soNumber;

                // Apply search filters if provided
                if (!string.IsNullOrEmpty(searchType) && (
                    !string.IsNullOrEmpty(peNumber) ||
                    !string.IsNullOrEmpty(customer) ||
                    !string.IsNullOrEmpty(jobReference) ||
                    !string.IsNullOrEmpty(soNumber)))
                {
                    string searchValue = searchType.ToLower() switch
                    {
                        "customer" => customer ?? "",
                        "jobreference" => jobReference ?? "",
                        "sonumber" => soNumber ?? "",
                        _ => peNumber ?? ""
                    };

                    // Use API service for search
                    int pageSize = 10;
                    var searchResult = await _plannedEventsApi.SearchPlannedEventsAsync(searchType, searchValue, string.Join(",", userWorkgroupNames), hasDrawFiberAccess, pageIndex, pageSize);
                    
                    // Get PE tasks for the paginated events using API service
                    var peNumbers = searchResult.Select(pe => pe.PeNumber).Where(pn => !string.IsNullOrEmpty(pn)).Cast<string>().ToList();
                    if (peNumbers.Any())
                    {
                        var allTasks = await _peTasksApi.GetPETasksByPENumbersAsync(peNumbers);
                        var peTasksByPeNumber = allTasks
                            .GroupBy(t => t.PENumber)
                            .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
                        ViewBag.PETasksByPeNumber = peTasksByPeNumber;
                    }
                    else
                    {
                        ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                    }

                    return View(searchResult);
                }
                else
                {
                    // Return empty list but still show dashboard data
                    ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                    return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, 10));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IndexRefactored method");
                return View("Error");
            }
        }

        /// <summary>
        /// Refactored redirect method using API services
        /// </summary>
        private async Task<IActionResult?> RedirectBasedOnUserTypeRefactored(string? searchType, string? peNumber, string? customer,
            string? jobReference, string? soNumber, List<int> workgroupIds, int pageIndex)
        {
            try
            {
                // Check for sales workgroup using API service
                if (await IsUserInSalesWorkgroup())
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

                // Check for multiple workgroups using API service
                if (await HasMultipleWorkgroups())
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

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RedirectBasedOnUserTypeRefactored");
                return null;
            }
        }

        /// <summary>
        /// Refactored reminders methods using API services
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRemindersRefactored(bool showAll = true)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                var reminders = await _peIssuesApi.GetRemindersAsync(userId, showAll);

                var result = reminders.Select(r => new
                {
                    r.Id,
                    r.PlannedEventId,
                    message = r.IssueText,
                    createdDate = r.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                    isRead = r.IsRead
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders");
                return BadRequest("Error retrieving reminders");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReminderCountRefactored()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                var count = await _peIssuesApi.GetReminderCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminder count");
                return Json(new { count = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRemindersAsReadRefactored()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                var success = await _peIssuesApi.MarkAllRemindersAsReadAsync(userId);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reminders as read");
                return Json(new { success = false });
            }
        }
    }
}
