using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public partial class PlannedEventsController : BaseController
    {
        private readonly IPlannedEventsApiClient _plannedEventsApi;
        private readonly IUsersApiClient _usersApi;
        private readonly IPETasksApiClient _peTasksApi;
        private readonly IPEIssuesApiClient _peIssuesApi;
        private readonly IWorkGroupsApiClient _workGroupsApi;
        private readonly IAreaNetworkEngineersApiClient _areaNetworkEngineersApi;
        private readonly IPETaskListsApiClient _peTaskListsApi;
        private readonly IEscalationsApiClient _escalationsApi;
        private readonly IPEIssueResolutionsApiClient _peIssueResolutionsApi;
        private readonly ICustomerUserAssignmentsApiClient _customerUserAssignmentsApi;
        private readonly ILogger<PlannedEventsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITaskQueueApiClient _taskQueueApiClient;

        public PlannedEventsController(
            IPlannedEventsApiClient plannedEventsApi,
            IUsersApiClient usersApi,
            IPETasksApiClient peTasksApi,
            IPEIssuesApiClient peIssuesApi,
            IWorkGroupsApiClient workGroupsApi,
            IAreaNetworkEngineersApiClient areaNetworkEngineersApi,
            IPETaskListsApiClient peTaskListsApi,
            IEscalationsApiClient escalationsApi,
            IPEIssueResolutionsApiClient peIssueResolutionsApi,
            ICustomerUserAssignmentsApiClient customerUserAssignmentsApi,
            ILogger<PlannedEventsController> logger,
            IWebHostEnvironment webHostEnvironment,
            ITaskQueueApiClient taskQueueApiClient) : base(usersApi)
        {
            _plannedEventsApi = plannedEventsApi;
            _usersApi = usersApi;
            _peTasksApi = peTasksApi;
            _peIssuesApi = peIssuesApi;
            _workGroupsApi = workGroupsApi;
            _areaNetworkEngineersApi = areaNetworkEngineersApi;
            _peTaskListsApi = peTaskListsApi;
            _escalationsApi = escalationsApi;
            _peIssueResolutionsApi = peIssueResolutionsApi;
            _customerUserAssignmentsApi = customerUserAssignmentsApi;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _taskQueueApiClient = taskQueueApiClient;
        }

        // GET: PlannedEvents/Index
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, int? workgroupId, int pageIndex = 1)
        {
            // Redirect based on user type and workgroup access
            var redirectResult = await RedirectBasedOnUserType(searchType, peNumber, customer,
                jobReference, soNumber, workgroupId.HasValue ? new List<int> { workgroupId.Value } : new List<int>(), pageIndex);
            if (redirectResult != null)
            {
                return redirectResult;
            }

            int currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            // Check if user belongs to NET-PROJ-ACC-CABLE workgroup
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["HasDrawFiberAccess"] = hasDrawFiberAccess;

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = string.IsNullOrEmpty(peNumber) ? peNumber : peNumber.Trim();
            customer = string.IsNullOrEmpty(customer) ? customer : customer.Trim();
            jobReference = string.IsNullOrEmpty(jobReference) ? jobReference : jobReference.Trim();
            soNumber = string.IsNullOrEmpty(soNumber) ? soNumber : soNumber.Trim();


            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            
            // Pass ViewAll permission to the view
            ViewData["CanViewAll"] = canViewAll;
            

            // Keep track of user's assigned workgroup(s) separately from the filter selection
            ViewData["UserAssignedWorkgroupIds"] = userWorkgroupIds;
            ViewData["UserAssignedWorkgroupNames"] = userWorkgroupNames;

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsForUserAsync(userWorkgroupIds, canViewAll);

            // Provide all workgroups for ViewAll users
            if (canViewAll)
            {
                var allGroups = await _workGroupsApi.GetWorkGroupsAsync();
                ViewBag.AllWorkGroups = allGroups.ToList();
            }

            // Keep track of selected filter workgroup (separate from user's assigned workgroup)
            ViewData["SelectedWorkgroupId"] = workgroupId;

            // Determine effective workgroup and setup query
            string? selectedWorkgroupName = null;
            List<int>? effectiveWorkgroupIds = null;

            if (canViewAll)
            {
                // If user has ViewAll and selected a workgroup filter, filter by it
                if (workgroupId.HasValue)
                {
                    selectedWorkgroupName = await _workGroupsApi.GetWorkGroupNameAsync(workgroupId.Value);
                    effectiveWorkgroupIds = new List<int> { workgroupId.Value };

                    if (!string.IsNullOrEmpty(selectedWorkgroupName))
                    {
                        ViewData["FilteredWorkgroup"] = selectedWorkgroupName;
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                        ViewData["SelectedWorkgroupName"] = selectedWorkgroupName;
                    }
                }
                // else: show all records (no filter)
            }
            else
            {
                // User does NOT have ViewAll: always restrict to their assigned workgroup (should only be one)
                if (userWorkgroupNames.Any())
                {
                    selectedWorkgroupName = userWorkgroupNames.First();
                    effectiveWorkgroupIds = userWorkgroupIds;
                    ViewData["FilteredWorkgroup"] = selectedWorkgroupName;
                    ViewData["SelectedWorkgroupId"] = userWorkgroupIds.FirstOrDefault();
                    ViewData["SelectedWorkgroupName"] = selectedWorkgroupName;
                }
            }

            // Calculate dashboard counts based on user permissions and workgroup selection
            if (canViewAll)
            {
                if (workgroupId.HasValue)
                {
                    // ViewAll user with specific workgroup selected - use workgroup-specific counts
                    var selectedWorkgroupIds = new List<int> { workgroupId.Value };
                    ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolateCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                }
                else
                {
                    // ViewAll user with no workgroup selected - use user-based endpoints for consistent logic
                    ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountByUserIdAsync(currentUserId);
                    ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountByUserIdAsync(currentUserId);
                    ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolatingCountByUserIdAsync(currentUserId);
                    ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountByUserIdAsync(currentUserId);
                }
            }
            else
            {
                // Regular user - use user-based endpoints (existing behavior)
                ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountByUserIdAsync(currentUserId);
                ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountByUserIdAsync(currentUserId);
                ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolatingCountByUserIdAsync(currentUserId);
                ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountByUserIdAsync(currentUserId);
            }


            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            string searchString = searchType switch
            {
                "customer" => customer,
                "jobReference" => jobReference,
                "soNumber" => soNumber,
                _ => peNumber
            };

            // Get user's primary workgroup ID for the task queue
            var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
            ViewBag.UserWorkgroupId = userWorkgroupId;

            // Automatically load task queue for user's workgroup
            try
            {
                var taskQueueItems = await _taskQueueApiClient.GetPrioritizedTasksAsync(
                    workgroupId: userWorkgroupId, 
                    year: DateTime.Now.Year, // Add current year parameter
                    take: 5); // Load top 5 tasks initially
                
                ViewBag.NextTask = taskQueueItems;
                ViewBag.HasNextTask = taskQueueItems.Any();
                
                // Set task queue statistics
                ViewBag.TaskQueueUrgentCount = taskQueueItems.Count(t => t.Task.IsUrgent);
                ViewBag.TaskQueueOLACount = taskQueueItems.Count(t => t.Task.IsOLAViolate && !t.Task.IsUrgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading task queue for user workgroup {userWorkgroupId}", userWorkgroupId);
                ViewBag.NextTask = new List<TaskQueueItem>();
                ViewBag.HasNextTask = false;
            }

            // Pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest issues for the inbox
            var inboxIssues = await _peIssuesApi.GetInboxViewModelsAsync(currentUserId);

            // Add unread count
            var unreadCount = inboxIssues?.Count(i => !i.IsRead) ?? 0;
            if (ViewData["PendingUrgentRequests"] != null)
            {
                var pendingUrgent = ViewData["PendingUrgentRequests"] as IEnumerable<PlannedEvent>;
                unreadCount += pendingUrgent?.Count(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION") ?? 0;
            }
            if (ViewData["PendingTaskRequests"] != null)
            {
                var pendingTasks = ViewData["PendingTaskRequests"] as IEnumerable<PETask>;
                unreadCount += pendingTasks?.Count(t => t.UrgentRequested && !t.IsUrgent) ?? 0;
            }

            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues?.Count() ?? 0;
            ViewData["UnreadMessages"] = unreadCount;

            if (inboxIssues != null)
            {
                foreach (var issue in inboxIssues)
                {
                    if (issue.IsResolutionRequest)
                    {
                        var resolution = await _peIssueResolutionsApi.GetPendingResolutionAsync(issue.OriginalIssueId ?? issue.Id);
                        // Note: Resolution details are handled via ViewBag.ResolutionsByIssueId
                        // The issue model doesn't contain ResolutionDetails/ResolutionId properties
                    }
                }
            }

            ViewBag.InboxIssues = inboxIssues;
            ViewData["InboxIssues"] = inboxIssues;

            // Create a lookup dictionary for resolutions
            if (inboxIssues != null && inboxIssues.Any())
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

            // Apply search filters and get results using API service
            if (!string.IsNullOrEmpty(searchString))
            {
                // Use the new user-specific search method
                var searchResults = await _plannedEventsApi.SearchPlannedEventsForUserAsync(
                    searchType ?? "peNumber", searchString, currentUserId, pageIndex, 10);

                // Get PE tasks for the filtered results
                if (searchResults.Any())
                {
                    var peNumbers = searchResults.Select(pe => pe.PeNumber).Where(pn => pn != null).Cast<string>().ToList();
                    var peTasksByPeNumber = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);
                    ViewBag.PETasksByPeNumber = peTasksByPeNumber ?? new Dictionary<string, IEnumerable<PETask>>();
                }
                else
                {
                    ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                }

                return View(searchResults);
            }
            else
            {
                // Return empty list but still show dashboard data
                ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, 10));
            }
        }
        // New action for users with multiple workgroups
        public async Task<IActionResult> MultiWorkgroupView(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, List<int> workgroupIds, int pageIndex = 1)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            // Get current user's workgroup info first
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Check Draw Fiber access based on selected workgroups (prioritizing filter selection)
            var selectedWorkgroupIdsForAccess = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
            var workgroupNamesForAccess = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIdsForAccess);
            bool hasDrawFiberAccess = workgroupNamesForAccess.Any(wg =>
                wg.Name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

            ViewData["HasDrawFiberAccess"] = hasDrawFiberAccess;

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = string.IsNullOrEmpty(peNumber) ? peNumber : peNumber.Trim();
            customer = string.IsNullOrEmpty(customer) ? customer : customer.Trim();
            jobReference = string.IsNullOrEmpty(jobReference) ? jobReference : jobReference.Trim();
            soNumber = string.IsNullOrEmpty(soNumber) ? soNumber : soNumber.Trim();

            // Security check - if user only has one workgroup, redirect back to Index
            // Note: Users with ViewAll permission should be able to access multi-workgroup views
            if (!canViewAll && userWorkgroupIds.Count <= 1)
            {
                return RedirectToAction(nameof(Index), new
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

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);

            // Keep track of selected workgroups
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            // Get planned events using API service based on selected workgroups
            var selectedWorkgroupIds = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
            var selectedWorkgroupNames = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIds);

            var workgroupNamesList = selectedWorkgroupNames.Select(wg => wg.Name).ToList();

            if (workgroupNamesList.Any())
            {
                ViewData["FilteredWorkgroups"] = string.Join(", ", workgroupNamesList);
                ViewData["SelectedWorkgroupIds"] = selectedWorkgroupIds;
                ViewData["SelectedWorkgroupNames"] = workgroupNamesList;
            }

            // Calculate dashboard counts using new multi-workgroup count methods
            ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
            ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
            ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolateCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
            ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);

            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            string searchString = searchType switch
            {
                "customer" => customer,
                "jobReference" => jobReference,
                "soNumber" => soNumber,
                _ => peNumber
            };

            // Get user's primary workgroup ID for the task queue
            var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
            ViewBag.UserWorkgroupId = userWorkgroupId;

            // Don't pre-load next task - let user manually load it
            ViewBag.NextTask = new List<TaskQueueItem>();
            ViewBag.HasNextTask = false;

            // Pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get inbox issues
            var inboxIssues = await _peIssuesApi.GetInboxViewModelsAsync(currentUserId);

            // Process inbox data
            var unreadCount = inboxIssues?.Count(i => !i.IsRead) ?? 0;
            if (ViewData["PendingUrgentRequests"] != null)
            {
                var pendingUrgent = ViewData["PendingUrgentRequests"] as IEnumerable<PlannedEvent>;
                unreadCount += pendingUrgent?.Count(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION") ?? 0;
            }
            if (ViewData["PendingTaskRequests"] != null)
            {
                var pendingTasks = ViewData["PendingTaskRequests"] as IEnumerable<PETask>;
                unreadCount += pendingTasks?.Count(t => t.UrgentRequested && !t.IsUrgent) ?? 0;
            }
            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues?.Count() ?? 0;
            ViewData["UnreadMessages"] = unreadCount;

            // Process resolution details
            if (inboxIssues != null)
            {
                foreach (var issue in inboxIssues)
                {
                    if (issue.IsResolutionRequest)
                    {
                        var resolution = await _peIssueResolutionsApi.GetPendingResolutionAsync(issue.OriginalIssueId ?? issue.Id);
                        // Note: Resolution details are handled via ViewBag.ResolutionsByIssueId
                        // The issue model doesn't contain ResolutionDetails/ResolutionId properties
                    }
                }
            }
            ViewBag.InboxIssues = inboxIssues;

            // Create resolution lookup
            if (inboxIssues != null && inboxIssues.Any())
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

            // Apply search filters using API service
            if (!string.IsNullOrEmpty(searchString))
            {
                // Use the new user-specific search method
                var searchResults = await _plannedEventsApi.SearchPlannedEventsForUserAsync(
                    searchType ?? "peNumber", searchString, currentUserId, pageIndex, 10);

                // Get PE tasks for the filtered results
                if (searchResults.Any())
                {
                    var peNumbers = searchResults.Select(pe => pe.PeNumber).Where(pn => pn != null).Cast<string>().ToList();
                    var peTasksByPeNumber = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);
                    ViewBag.PETasksByPeNumber = peTasksByPeNumber ?? new Dictionary<string, IEnumerable<PETask>>();
                }
                else
                {
                    ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                }

                return View(searchResults);
            }
            else
            {
                // Return empty list but still show dashboard data
                ViewBag.PETasksByPeNumber = new Dictionary<string, IEnumerable<PETask>>();
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, 10));
            }
        }

        public async Task<IActionResult> SalesView(string searchType, string peNumber, string customer,
        string jobReference, string soNumber, int? pageIndex = 1)
        {
            var currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            // Get user's sales workgroup
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            
            // Check if user is actually in a sales workgroup and doesn't have ViewAll permission
            // This should match the logic in RedirectBasedOnUserType to prevent redirect loops
            bool isInSalesWorkgroup = await IsUserInSalesWorkgroup();
            if (!isInSalesWorkgroup || canViewAll)
            {
                return RedirectToAction(nameof(Index), new { searchType, peNumber, customer, jobReference, soNumber, pageIndex });
            }

            // Get user's ALL workgroups to include in filtering
            var (userWorkgroupIds, userWorkgroupNames, _) = await GetCurrentUserWorkGroupsAsync();

            // Debug logging
            _logger.LogInformation("SalesView - User Sales Workgroups: {workgroups}", string.Join(", ", salesWorkgroups));
            _logger.LogInformation("SalesView - User All Workgroups: {workgroups}", string.Join(", ", userWorkgroupNames));
            _logger.LogInformation("SalesView - CanViewAll: {canViewAll}", canViewAll);

            // Get draw fiber access for the current user
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // Get all planned events for search functionality
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();

            // Set dashboard counts using sales-specific endpoints
            ViewData["UrgentCount"] = await _plannedEventsApi.GetSalesUrgentCountAsync();
            ViewData["InProgressCount"] = await _plannedEventsApi.GetSalesInProgressCountAsync();
            ViewData["OLAViolateCount"] = await _plannedEventsApi.GetSalesOLAViolateCountAsync();
            ViewData["HoldCount"] = await _plannedEventsApi.GetSalesHoldCountAsync();


            // Get pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync(10);
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            // Get pending task urgent requests
            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync(5);
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest inbox issues
            var inboxIssues = await _peIssuesApi.GetInboxViewModelsAsync(currentUserId, 10);

            // Calculate unread count
            var unreadCount = inboxIssues.Count(i => !i.IsRead);
            unreadCount += pendingUrgentRequests.Count();
            unreadCount += pendingTaskRequests.Count();

            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues.Count();
            ViewData["UnreadMessages"] = unreadCount;

            // Add resolutions lookup for resolution requests
            if (inboxIssues.Any())
            {
                Dictionary<int, PEIssueResolution> resolutionsByIssueId = new Dictionary<int, PEIssueResolution>();
                var issueIds = inboxIssues
                    .Where(i => i.IsResolutionRequest)
                    .Select(i => i.OriginalIssueId ?? i.Id)
                    .ToList();

                if (issueIds.Any())
                {
                    var allResolutions = await _peIssueResolutionsApi.GetResolutionsByIssueIdsAsync(issueIds);
                    var resolutions = allResolutions.Values.Where(r => !r.IsConfirmed);

                    foreach (var resolution in resolutions)
                    {
                        resolutionsByIssueId[resolution.IssueId] = resolution;
                    }
                }
                ViewBag.ResolutionsByIssueId = resolutionsByIssueId;
            }

            bool isSearchPerformed = !string.IsNullOrEmpty(searchType) && (
            !string.IsNullOrEmpty(peNumber) ||
            !string.IsNullOrEmpty(customer) ||
            !string.IsNullOrEmpty(jobReference) ||
            !string.IsNullOrEmpty(soNumber));

            PaginatedList<PlannedEvent> paginatedList;

            if (isSearchPerformed)
            {
                string searchValue = "";
                switch (searchType.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer):
                        searchValue = customer;
                        ViewData["CustomerFilter"] = customer;
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference):
                        searchValue = jobReference;
                        ViewData["JobReferenceFilter"] = jobReference;
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber):
                        searchValue = soNumber;
                        ViewData["SONumberFilter"] = soNumber;
                        break;
                    default: // peNumber
                        searchValue = peNumber ?? "";
                        ViewData["PENumberFilter"] = peNumber;
                        break;
                }

                // Apply customer filtering with workgroup checks and ViewAll permission
                var filteredSearchEvents = await ApplyCustomerFilteringAsync(allPlannedEvents.AsQueryable(), salesWorkgroups, canViewAll);
                var materializedEvents = filteredSearchEvents.ToList();

                // Apply search filter
                var searchResults = materializedEvents.Where(p =>
                {
                    return searchType.ToLower() switch
                    {
                        "customer" => p.Customer?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                        "jobreference" => p.JobReference?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                        "sonumber" => p.SoNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                        _ => p.PeNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true
                    };
                }).OrderByDescending(p => p.PECreatedDate).ThenBy(p => p.PeNumber);

                // Apply pagination
                int pageSize = 10;
                var totalCount = searchResults.Count();
                var items = searchResults.Skip(((pageIndex ?? 1) - 1) * pageSize).Take(pageSize).ToList();
                paginatedList = new PaginatedList<PlannedEvent>(items, totalCount, pageIndex ?? 1, pageSize);
            }
            else
            {
                // Return empty list if no search performed
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, 1, 10));
            }

            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["SalesWorkgroups"] = string.Join(", ", salesWorkgroups);

            var peNumbers = paginatedList.Select(pe => pe.PeNumber).Where(pn => pn != null).Cast<string>().ToList();
            var tasksByPeNumber = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);

            ViewBag.PETasksByPeNumber = tasksByPeNumber;

            // Get issues for the paginated PEs
            var peIds = paginatedList.Select(pe => pe.Id).ToList();
            var issuesByPlannedEventId = await _peIssuesApi.GetIssuesByPlannedEventIdsAsync(peIds);

            ViewBag.IssuesByPlannedEventId = issuesByPlannedEventId;

            return View(paginatedList);
        }

        //here this part for handle reminder as notification
        [HttpGet]
        public async Task<IActionResult> GetReminders(bool showAll = true)
        {
            var userId = await GetCurrentUserIdAsync();
            var reminders = await _peIssuesApi.GetRemindersAsync(userId, showAll);

            var reminderData = reminders.Select(i => new
            {
                id = i.Id,
                plannedEventId = i.PlannedEventId,
                message = i.IssueText,
                createdDate = i.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                isRead = i.IsRead
            });

            return Json(reminderData);
        }

        [HttpGet]
        public async Task<IActionResult> GetReminderCount()
        {
            var userId = await GetCurrentUserIdAsync();
            var count = await _peIssuesApi.GetReminderCountAsync(userId);

            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRemindersAsRead()
        {
            var userId = await GetCurrentUserIdAsync();
            var success = await _peIssuesApi.MarkAllRemindersAsReadAsync(userId);
            return Json(new { success });
        }

        // GET: PlannedEvents/Details/5
        public async Task<IActionResult> Details(int? id, string? returnUrl = null)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            // Get the planned event first
            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Find the engineer by LEA code
            string engineerName = "";
            if (!string.IsNullOrEmpty(plannedEvent.Lea))
            {
                engineerName = await _areaNetworkEngineersApi.GetEngineerNameByAreaAsync(plannedEvent.Lea) ?? "Not Assigned";
            }
            ViewBag.NetworkEngineer = engineerName;

            // Check if current task is "Draw Fiber"
            bool isCurrentTaskDrawFiber = plannedEvent.TaskName?.Trim().ToLower() == "draw fiber";
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            bool hasCanManageEstimatedTime = currentUser?.UserRole?.HasPermission("CanManageEstimatedTime") == true;

            // DEBUG: Log the individual conditions
            _logger.LogInformation("EstimatedTime Debug - User {userId}: CanManageEstimatedTime={canManage}, DrawFiberAccess={drawAccess}, IsCurrentTaskDrawFiber={isDrawFiber}, TaskName='{taskName}'",
                currentUserId, hasCanManageEstimatedTime, hasDrawFiberAccess, isCurrentTaskDrawFiber, plannedEvent.TaskName);

            // Only allow estimated time management if user has permission, is in the right workgroup,
            // AND the current task is "Draw Fiber"
            ViewData["CanManageEstimatedTime"] =
                hasCanManageEstimatedTime &&
                hasDrawFiberAccess &&
                isCurrentTaskDrawFiber;

            // DEBUG: Set individual values for debugging in view
            ViewData["Debug_HasCanManageEstimatedTime"] = hasCanManageEstimatedTime;
            ViewData["Debug_HasDrawFiberAccess"] = hasDrawFiberAccess;
            ViewData["Debug_IsCurrentTaskDrawFiber"] = isCurrentTaskDrawFiber;


            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Get related PE tasks for this event
            var peTasks = await _peTasksApi.GetPETasksByPENumberAsync(plannedEvent.PeNumber ?? "");
            var peTasksList = peTasks.OrderBy(t => t.TaskSeq).ToList();
            // Set the correct dates for display
            SetTaskDatesFromPeNumber(plannedEvent.PeNumber ?? "", peTasksList);

            ViewBag.PETasks = peTasksList;

            // Find PETaskListId for the current task name
            var taskList = await _peTaskListsApi.GetPETaskListByNameAsync(plannedEvent.TaskName ?? "");
            ViewBag.CurrentTaskListId = taskList?.Id;

            // Load issues/subtasks for this PE (assuming you use PEIssue or Subtask table)
            var issues = await _peIssuesApi.GetPEIssueViewModelsByPlannedEventAsync(plannedEvent.Id);

            ViewBag.PEReportedIssues = issues;

            ViewBag.ReturnUrl = returnUrl;

            var peTaskIds = peTasksList.Select(t => t.Id).ToList();

            var escalations = await _escalationsApi.GetEscalationsByTaskIdsAsync(peTaskIds);

            plannedEvent.Escalations = escalations.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View(plannedEvent);
        }

        // GET: PlannedEvents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PlannedEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Province,Region,Rtom,RtomDescription,JobReference,ContractorName,PeNumber,PeActivity,PeNature,PeTitle,PeObjective,PeArea,SoNumber,TaskSeq,TaskName,TaskWg,WoActualStartDate,RequestReferenceNo,SoId,Region1,Province1,Rtom1,Lea,CctId,ServiceCategory,ServiceType,SoCreateDate,OrderType,CrmOrder,WoId,PendingTaskName,PendingWg,PEStatus,StartDate,ServiceSpeed,ServiceRequiredDate,FiberPeNo,FiberSoId,ProductSoId,FiberPeTaskName,FiberPeTaskWg,WoComments,Customer,CusType,AccountManager,SectionHandledBy,LocationAAddress,LocationBAddress,NtuType,AccessMedium,AccessMediumAEnd,AccessMediumBEnd")] PlannedEvent plannedEvent)
        {
            if (ModelState.IsValid)
            {
                var createdEvent = await _plannedEventsApi.CreatePlannedEventAsync(plannedEvent);
                if (createdEvent != null)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create planned event.");
            }
            return View(plannedEvent);
        }

        // GET: PlannedEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }
            return View(plannedEvent);
        }

        // POST: PlannedEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Province,Region,Rtom,RtomDescription,JobReference,ContractorName,PeNumber,PeActivity,PeNature,PeTitle,PeObjective,PeArea,SoNumber,TaskSeq,TaskName,TaskWg,WoActualStartDate,RequestReferenceNo,SoId,Region1,Province1,Rtom1,Lea,CctId,ServiceCategory,ServiceType,SoCreateDate,OrderType,CrmOrder,WoId,PendingTaskName,PendingWg,PEStatus,StartDate,ServiceSpeed,ServiceRequiredDate,FiberPeNo,FiberSoId,ProductSoId,FiberPeTaskName,FiberPeTaskWg,WoComments,Customer,CusType,AccountManager,SectionHandledBy,LocationAAddress,LocationBAddress,NtuType,AccessMedium,AccessMediumAEnd,AccessMediumBEnd")] PlannedEvent plannedEvent)
        {
            if (id != plannedEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updatedEvent = await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);
                    if (updatedEvent == null)
                    {
                        ModelState.AddModelError("", "Failed to update planned event.");
                        return View(plannedEvent);
                    }
                }
                catch (Exception)
                {
                    if (!await PlannedEventExists(plannedEvent.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(plannedEvent);
        }

        // GET: PlannedEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            return View(plannedEvent);
        }

        // POST: PlannedEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _plannedEventsApi.DeletePlannedEventAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> InProgressRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            
            // Get current user's workgroup info and ViewAll permission
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            
            try
            {
                IEnumerable<PlannedEvent> records;
                
                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        // ViewAll user with specific workgroup selected - use workgroup-specific records
                        var selectedWorkgroupIds = new List<int> { workgroupId.Value };
                        records = await _plannedEventsApi.GetInProgressPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    }
                    else
                    {
                        // ViewAll user with no workgroup selected - use user-based endpoints for consistent logic
                        records = await _plannedEventsApi.GetInProgressPlannedEventsByUserIdAsync(currentUserId);
                    }
                }
                else
                {
                    // Regular user - use user-based endpoints (existing behavior)
                    records = await _plannedEventsApi.GetInProgressPlannedEventsByUserIdAsync(currentUserId);
                }

                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading in-progress records");
                return View(new List<PlannedEvent>());
            }
        }


        // GET: PlannedEvents/OLAViolateRecords
        public async Task<IActionResult> OLAViolateRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            
            // Get current user's workgroup info and ViewAll permission
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            
            try
            {
                IEnumerable<PlannedEvent> records;
                
                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        // ViewAll user with specific workgroup selected - use workgroup-specific records
                        var selectedWorkgroupIds = new List<int> { workgroupId.Value };
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    }
                    else
                    {
                        // ViewAll user with no workgroup selected - use user-based endpoints for consistent logic
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsByUserIdAsync(currentUserId);
                    }
                }
                else
                {
                    // Regular user - use user-based endpoints (existing behavior)
                    records = await _plannedEventsApi.GetOLAViolatingPlannedEventsByUserIdAsync(currentUserId);
                }

                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                var recordsList = records.OrderBy(p => p.PeNumber).ToList();

                // Get violation details using the new API endpoint
                var peNumbers = recordsList.Select(r => r.PeNumber).Where(p => !string.IsNullOrEmpty(p)).Cast<string>().ToList();
                var violationDetails = await _peTasksApi.GetOLAViolationDetailsAsync(peNumbers);

                ViewBag.ViolationDetails = violationDetails;

                _logger.LogInformation("Retrieved {count} OLA violated records", recordsList.Count);
                return View(recordsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading OLA violate records");
                return View(new List<PlannedEvent>());
            }
        }

        public async Task<IActionResult> HoldRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            
            // Get current user's workgroup info and ViewAll permission
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            
            try
            {
                IEnumerable<PlannedEvent> records;
                
                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        // ViewAll user with specific workgroup selected - use workgroup-specific records
                        var selectedWorkgroupIds = new List<int> { workgroupId.Value };
                        records = await _plannedEventsApi.GetHoldPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    }
                    else
                    {
                        // ViewAll user with no workgroup selected - use user-based endpoints for consistent logic
                        records = await _plannedEventsApi.GetHoldPlannedEventsByUserIdAsync(currentUserId);
                    }
                }
                else
                {
                    // Regular user - use user-based endpoints (existing behavior)
                    records = await _plannedEventsApi.GetHoldPlannedEventsByUserIdAsync(currentUserId);
                }

                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading hold records");
                return View(new List<PlannedEvent>());
            }
        }


        public async Task<IActionResult> UrgentRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            
            // Get current user's workgroup info and ViewAll permission
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            
            try
            {
                IEnumerable<PlannedEvent> records;
                
                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        // ViewAll user with specific workgroup selected - use workgroup-specific records
                        var selectedWorkgroupIds = new List<int> { workgroupId.Value };
                        records = await _plannedEventsApi.GetUrgentPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                    }
                    else
                    {
                        // ViewAll user with no workgroup selected - use user-based endpoints for consistent logic
                        records = await _plannedEventsApi.GetUrgentPlannedEventsByUserIdAsync(currentUserId);
                    }
                }
                else
                {
                    // Regular user - use user-based endpoints (existing behavior)
                    records = await _plannedEventsApi.GetUrgentPlannedEventsByUserIdAsync(currentUserId);
                }

                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                // Only display urgent PE records, not urgent tasks
                // Remove urgent tasks from the view to comply with business requirements
                ViewData["UrgentTasks"] = new List<PETask>();

                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading urgent records");
                // Only display urgent PE records, not urgent tasks
                ViewData["UrgentTasks"] = new List<PETask>();
                return View(new List<PlannedEvent>());
            }
        }

        // GET: PlannedEvents/UrgentRequestConfirmation/5
        public async Task<IActionResult> UrgentRequestConfirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);

            if (plannedEvent == null || plannedEvent.PEStatus != "PENDING_URGENT_CONFIRMATION")
            {
                return NotFound();
            }

            return View(plannedEvent);
        }
        // GET: PETasks/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();

            _logger.LogInformation("Retrieved {count} pending urgent PE requests", pendingRequests.Count());
            return View(pendingRequests.ToList());
        }

        // GET: PlannedEvents/MultiWorkgroupInProgressView
        public async Task<IActionResult> MultiWorkgroupInProgressView(List<int> workgroupIds, string searchType,
            string peNumber, string customer, string jobReference, string soNumber)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Security check - if user only has one workgroup, redirect back to regular view
            // Note: Users with ViewAll permission should be able to access multi-workgroup views
            if (!canViewAll && userWorkgroupIds.Count <= 1)
            {
                return RedirectToAction(nameof(InProgressRecords), new { workgroupId = workgroupIds?.FirstOrDefault() });
            }

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            try
            {
                // Get PE numbers with OLA violation to exclude
                var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

                // Determine search criteria
                string searchValue = "";
                string searchTypeValue = searchType ?? "peNumber";

                switch (searchTypeValue.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer?.Trim()):
                        searchValue = customer.Trim();
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference?.Trim()):
                        searchValue = jobReference.Trim();
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber?.Trim()):
                        searchValue = soNumber.Trim();
                        break;
                    default:
                        searchValue = peNumber?.Trim() ?? "";
                        break;
                }

                // Filter by selected workgroups or all user's workgroups if none selected
                var selectedWorkgroupIds = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
                var selectedWorkgroupNames = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIds);

                ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames.Select(w => w.Name));

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get in-progress records using new multi-workgroup API method
                var allRecords = await _plannedEventsApi.GetInProgressPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchValue))
                {
                    allRecords = allRecords.Where(p =>
                    {
                        return searchTypeValue.ToLower() switch
                        {
                            "customer" => p.Customer?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "jobreference" => p.JobReference?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "sonumber" => p.SoNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            _ => p.PeNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true
                        };
                    }).ToList();
                }

                // Exclude OLA violating records
                allRecords = allRecords.Where(p => p.PeNumber != null && !violatingPENumbers.Contains(p.PeNumber)).ToList();

                // Order results
                allRecords = allRecords.OrderByDescending(p => p.PECreatedDate).ThenBy(p => p.PeNumber).ToList();

                return View(allRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading multi-workgroup in-progress records");
                return View(new List<PlannedEvent>());
            }
        }

        // GET: PlannedEvents/MultiWorkgroupHoldView
        public async Task<IActionResult> MultiWorkgroupHoldView(List<int> workgroupIds, string searchType,
            string peNumber, string customer, string jobReference, string soNumber)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Security check - if user only has one workgroup, redirect back to regular view
            // Note: Users with ViewAll permission should be able to access multi-workgroup views
            if (!canViewAll && userWorkgroupIds.Count <= 1)
            {
                return RedirectToAction(nameof(HoldRecords), new { workgroupId = workgroupIds?.FirstOrDefault() });
            }

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            try
            {
                // Determine search criteria
                string searchValue = "";
                string searchTypeValue = searchType ?? "peNumber";

                switch (searchTypeValue.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer?.Trim()):
                        searchValue = customer.Trim();
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference?.Trim()):
                        searchValue = jobReference.Trim();
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber?.Trim()):
                        searchValue = soNumber.Trim();
                        break;
                    default:
                        searchValue = peNumber?.Trim() ?? "";
                        break;
                }

                // Filter by selected workgroups or all user's workgroups if none selected
                var selectedWorkgroupIds = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
                var selectedWorkgroupNames = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIds);

                ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames.Select(w => w.Name));

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get hold records using new multi-workgroup API method
                var allRecords = await _plannedEventsApi.GetHoldPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchValue))
                {
                    allRecords = allRecords.Where(p =>
                    {
                        return searchTypeValue.ToLower() switch
                        {
                            "customer" => p.Customer?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "jobreference" => p.JobReference?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "sonumber" => p.SoNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            _ => p.PeNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true
                        };
                    }).ToList();
                }

                // Order results
                allRecords = allRecords.OrderByDescending(p => p.PECreatedDate).ThenBy(p => p.PeNumber).ToList();

                return View(allRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading multi-workgroup hold records");
                return View(new List<PlannedEvent>());
            }
        }

        // GET: PlannedEvents/MultiWorkgroupUrgentView
        public async Task<IActionResult> MultiWorkgroupUrgentView(List<int> workgroupIds, string searchType,
            string peNumber, string customer, string jobReference, string soNumber)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Security check - if user only has one workgroup, redirect back to regular view
            // Note: Users with ViewAll permission should be able to access multi-workgroup views
            if (!canViewAll && userWorkgroupIds.Count <= 1)
            {
                return RedirectToAction(nameof(UrgentRecords), new { workgroupId = workgroupIds?.FirstOrDefault() });
            }

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            try
            {
                // Determine search criteria
                string searchValue = "";
                string searchTypeValue = searchType ?? "peNumber";

                switch (searchTypeValue.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer?.Trim()):
                        searchValue = customer.Trim();
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference?.Trim()):
                        searchValue = jobReference.Trim();
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber?.Trim()):
                        searchValue = soNumber.Trim();
                        break;
                    default:
                        searchValue = peNumber?.Trim() ?? "";
                        break;
                }

                // Get PE numbers with OLA violation to exclude
                var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

                // Filter by selected workgroups or all user's workgroups if none selected
                var selectedWorkgroupIds = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
                var selectedWorkgroupNames = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIds);

                ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames.Select(w => w.Name));

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get urgent records using new multi-workgroup API method
                var allRecords = await _plannedEventsApi.GetUrgentPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchValue))
                {
                    allRecords = allRecords.Where(p =>
                    {
                        return searchTypeValue.ToLower() switch
                        {
                            "customer" => p.Customer?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "jobreference" => p.JobReference?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "sonumber" => p.SoNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            _ => p.PeNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true
                        };
                    }).ToList();
                }

                // Exclude OLA violating records
                allRecords = allRecords.Where(p => p.PeNumber != null && !violatingPENumbers.Contains(p.PeNumber)).ToList();

                // Order results
                allRecords = allRecords.OrderByDescending(p => p.PECreatedDate).ThenBy(p => p.PeNumber).ToList();

                return View(allRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading multi-workgroup urgent records");
                return View(new List<PlannedEvent>());
            }
        }

        // GET: PlannedEvents/MultiWorkgroupOLAViolateView
        public async Task<IActionResult> MultiWorkgroupOLAViolateView(List<int> workgroupIds, string searchType,
            string peNumber, string customer, string jobReference, string soNumber)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Security check - if user only has one workgroup, redirect back to regular view
            // Note: Users with ViewAll permission should be able to access multi-workgroup views
            if (!canViewAll && userWorkgroupIds.Count <= 1)
            {
                return RedirectToAction(nameof(OLAViolateRecords), new { workgroupId = workgroupIds?.FirstOrDefault() });
            }

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            try
            {
                // Determine search criteria
                string searchValue = "";
                string searchTypeValue = searchType ?? "peNumber";

                switch (searchTypeValue.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer?.Trim()):
                        searchValue = customer.Trim();
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference?.Trim()):
                        searchValue = jobReference.Trim();
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber?.Trim()):
                        searchValue = soNumber.Trim();
                        break;
                    default:
                        searchValue = peNumber?.Trim() ?? "";
                        break;
                }

                // Filter by selected workgroups or all user's workgroups if none selected
                var selectedWorkgroupIds = (workgroupIds != null && workgroupIds.Count > 0) ? workgroupIds : userWorkgroupIds;
                var selectedWorkgroupNames = await _workGroupsApi.GetWorkGroupsByIdsAsync(selectedWorkgroupIds);

                ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames.Select(w => w.Name));

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get OLA violating records using new multi-workgroup API method
                var allRecords = await _plannedEventsApi.GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchValue))
                {
                    allRecords = allRecords.Where(p =>
                    {
                        return searchTypeValue.ToLower() switch
                        {
                            "customer" => p.Customer?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "jobreference" => p.JobReference?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            "sonumber" => p.SoNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true,
                            _ => p.PeNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true
                        };
                    }).ToList();
                }

                // Order results
                allRecords = allRecords.OrderByDescending(p => p.PECreatedDate).ThenBy(p => p.PeNumber).ToList();

                // Calculate violation details for display using API service
                var peNumbers = allRecords.Select(pe => pe.PeNumber).Where(pn => pn != null).Cast<string>().ToList();
                var allTasksByPe = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);

                // Filter to only OLA violating tasks
                var violatingTasks = allTasksByPe.SelectMany(kvp => kvp.Value)
                    .Where(t => t.IsOLAViolate)
                    .ToList();

                var currentDate = DateTime.Now.Date;
                var violationDetails = violatingTasks
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(
                        g => g.Key,
                        g => (object)new Dictionary<string, object>
                        {
                            ["TasksCount"] = g.Count(),
                            ["MaxDaysOverdue"] = g.Max(t =>
                                t.EstimatedTime.HasValue
                                    ? (currentDate - t.EstimatedTime.Value).Days
                                    : (t.ActualTaskCreatedDate.HasValue && t.OLA != null && int.TryParse(t.OLA, out var olaDays))
                                        ? (currentDate - t.ActualTaskCreatedDate.Value.AddDays(olaDays)).Days
                                        : 0
                            ),
                            ["OldestViolation"] = DateTime.MinValue  // Simplified for now
                        }
                    );

                ViewBag.ViolationDetails = violationDetails;

                return View(allRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading multi-workgroup OLA violate records");
                return View(new List<PlannedEvent>());
            }
        }

        // POST: PlannedEvents/ProcessUrgentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUrgentRequest(int id, string urgentReason)
        {
            var plannedEvent = await _plannedEventsApi.GetPlannedEventAsync(id);
            if (plannedEvent == null || plannedEvent.PEStatus == "COMPLETED")
            {
                return NotFound();
            }

            bool markAsUrgent = false;
            string priorityMessage = "";
            int priorityLevel = 0;

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    markAsUrgent = true;
                    plannedEvent.PEStatus = "URGENT";
                    priorityMessage = "[URGENT: Opening Ceremony - Priority 1]";
                    priorityLevel = 1;
                    // Replace entire priority string
                    plannedEvent.Priority = priorityMessage;
                    break;

                case "CriticalCustomer":
                    markAsUrgent = true;
                    plannedEvent.PEStatus = "URGENT";
                    priorityMessage = "[URGENT: Critical Customer - Priority 2]";
                    priorityLevel = 2;
                    // Replace entire priority string
                    plannedEvent.Priority = priorityMessage;
                    break;

                case "Reject":
                    plannedEvent.PEStatus = "ongoing";
                    plannedEvent.Priority = "Urgent Request Rejected";
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid option selected.";
                    return RedirectToAction(nameof(UrgentRequestsList));
            }

            // Update and save the PE first using API service
            var updatedEvent = await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);
            if (updatedEvent == null)
            {
                TempData["ErrorMessage"] = "Failed to update planned event.";
                return RedirectToAction(nameof(UrgentRequestsList));
            }

            // Log the PE update
            _logger.LogInformation("PE {id} priority set to: '{priority}' with level {level}",
                id, plannedEvent.Priority, priorityLevel);

            // If PE was marked as urgent, update all its tasks to be urgent as well
            if (markAsUrgent)
            {
                try
                {
                    // Get all tasks for this PE using API service
                    var peNumber = plannedEvent.PeNumber;
                    if (string.IsNullOrEmpty(peNumber))
                    {
                        _logger.LogWarning("PE {id} has null or empty PeNumber, skipping task updates", id);
                        TempData["SuccessMessage"] = "Planned Event updated successfully (no tasks to update).";
                        return RedirectToAction(nameof(UrgentRequestsList));
                    }

                    _logger.LogInformation("Updating tasks for PE: {peNumber}", peNumber);

                    // Get all tasks for this PE using API service
                    var tasks = await _peTasksApi.GetPETasksByPENumberAsync(peNumber);
                    // Filter out completed tasks - only mark non-completed tasks as urgent
                    var tasksList = tasks.Where(t => t.TaskStatus?.ToUpper() != "COMPLETED").ToList();

                    _logger.LogInformation("Found {totalCount} total tasks, {activeCount} non-completed tasks to update",
                        tasks.Count(), tasksList.Count);

                    // Process each task individually to ensure proper updates
                    foreach (var task in tasksList)
                    {
                        _logger.LogInformation("Processing task {id} (Status: {status}) for PE {peNumber}",
                            task.Id, task.TaskStatus, task.PENumber);

                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        task.UrgentRequested = false;
                        task.Priority = priorityMessage + " (Inherited from PE)";

                        // Update task using API service
                        try
                        {
                            var updatedTask = await _peTasksApi.UpdatePETaskAsync(task);
                            if (updatedTask != null)
                            {
                                _logger.LogInformation("Successfully updated task {id} with urgent status",
                                    task.Id);
                            }
                            else
                            {
                                _logger.LogError("Failed to update task {id} - UpdatePETaskAsync returned null", task.Id);
                            }
                        }
                        catch (Exception taskUpdateEx)
                        {
                            _logger.LogError(taskUpdateEx, "Exception occurred while updating task {id}", task.Id);
                        }
                    }

                    // Verify the update by checking all tasks using API service
                    if (tasksList.Any())
                    {
                        var verifyTasks = await _peTasksApi.GetPETasksByPENumberAsync(peNumber);
                        var verifyTasksList = verifyTasks.Where(t => t.TaskStatus?.ToUpper() != "COMPLETED").ToList();

                        var urgentTasksCount = verifyTasksList.Count(t => t.IsUrgent);
                        var totalActiveTasksCount = verifyTasksList.Count;

                        _logger.LogInformation("Verification: {urgentCount} out of {totalCount} non-completed tasks are marked as urgent",
                            urgentTasksCount, totalActiveTasksCount);

                        if (urgentTasksCount != totalActiveTasksCount)
                        {
                            _logger.LogWarning("Not all tasks were marked as urgent. Expected: {expected}, Actual: {actual}",
                                totalActiveTasksCount, urgentTasksCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating tasks for PE {id}", id);
                    TempData["ErrorMessage"] = "There was a problem updating the tasks. Please check the details.";
                }
            }

            TempData["SuccessMessage"] = markAsUrgent
                ? $"Planned Event marked as urgent with priority {priorityLevel}. All related tasks have also been marked as urgent."
                : "Urgent request processed.";

            return RedirectToAction("Details", new { id = plannedEvent.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReminderAsRead([FromBody] MarkReminderRequest request)
        {
            try
            {
                var success = await _peIssuesApi.MarkReminderAsReadAsync(request.Id);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reminder {ReminderId} as read", request.Id);
                return Json(new { success = false });
            }
        }

        public class MarkReminderRequest
        {
            public int Id { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> RequestUrgent(int id)
        {
            var plannedEvent = await _plannedEventsApi.GetPlannedEventAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Redirect to the details page which has the urgent request form
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestUrgentWithReason(int id, string urgentReason)
        {
            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null || plannedEvent.PEStatus?.ToUpper() == "COMPLETED" || plannedEvent.PEStatus?.ToUpper() == "URGENT")
            {
                return NotFound();
            }

            // Get current user information - use the service ID correctly
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
            {
                _logger.LogError("No service ID found for current user when requesting urgent status for PE {id}", id);
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction("InProgressRecords");
            }

            // Extract service ID properly (first 6 characters)
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
            _logger.LogInformation("Requesting urgent status for PE {id} by user with service ID: {serviceId}", id, serviceIdShort);

            var currentUser = await _usersApi.GetByServiceIdAsync(serviceIdShort);
            if (currentUser == null)
            {
                _logger.LogError("User not found with service ID {serviceId} when requesting urgent status for PE {id}", serviceIdShort, id);
                TempData["ErrorMessage"] = "User information not found.";
                return RedirectToAction("InProgressRecords");
            }

            _logger.LogInformation("Found user: {userName} (ID: {userId}) requesting urgent status for PE {id}",
                currentUser.Name, currentUser.Id, id);

            // Set urgent request information
            plannedEvent.UrgentRequestedById = currentUser.Id;
            plannedEvent.UrgentRequestedByName = currentUser.Name;

            // Update PE status and priority based on the selected reason
            switch (urgentReason)
            {
                case "OpeningCeremony":
                    plannedEvent.PEStatus = "PENDING_URGENT_CONFIRMATION";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + " [URGENT REQUEST PENDING: Opening Ceremony]";
                    break;

                case "CriticalCustomer":
                    plannedEvent.PEStatus = "PENDING_URGENT_CONFIRMATION";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + " [URGENT REQUEST PENDING: Critical Customer]";
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid urgency reason selected.";
                    return RedirectToAction("InProgressRecords");
            }

            var updatedEvent = await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);
            if (updatedEvent == null)
            {
                _logger.LogError("Failed to update PE {id} with urgent request by user {userId}", id, currentUser.Id);
                TempData["ErrorMessage"] = "Failed to submit urgent request.";
                return RedirectToAction("InProgressRecords");
            }

            _logger.LogInformation("PE ID {id} marked with urgent request flag with reason: {reason} by user {userName} (ID: {userId})",
                id, urgentReason, currentUser.Name, currentUser.Id);
            TempData["SuccessMessage"] = "Urgent request submitted for approval.";

            return RedirectToAction("InProgressRecords");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOLARecordUrgent(int id)
        {
            try
            {
                var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);

                if (plannedEvent == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    return RedirectToAction(nameof(OLAViolateRecords));
                }

                var currentDate = DateTime.Today;

                // Find the violating tasks for this PE
                var allTasks = await _peTasksApi.GetPETasksByPENumberAsync(plannedEvent.PeNumber ?? "");
                var violatingTasks = allTasks
                    .Where(t => t.TaskStatus != "COMPLETED" &&
                              t.TaskCompleteDate.Date < currentDate)
                    .OrderBy(t => t.TaskCompleteDate)  // Start with the most overdue
                    .ToList();

                if (violatingTasks.Any())
                {
                    // Mark the first/most overdue violating task as urgent
                    var mostOverdueTask = violatingTasks.First();
                    mostOverdueTask.IsUrgent = true;
                    mostOverdueTask.Priority = (mostOverdueTask.Priority ?? "") + " [URGENT: OLA VIOLATED]";
                    await _peTasksApi.UpdatePETaskAsync(mostOverdueTask);

                    // Update PE status to urgent
                    plannedEvent.PEStatus = "urgent";
                    await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);

                    _logger.LogInformation("PE {peNumber} with OLA violation marked as urgent", plannedEvent.PeNumber);
                    TempData["SuccessMessage"] = $"PE {plannedEvent.PeNumber} marked as urgent due to OLA violation.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No violating tasks found for this PE.";
                }

                return RedirectToAction(nameof(OLAViolateRecords));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking OLA record as urgent");
                TempData["ErrorMessage"] = "An error occurred while marking the record as urgent.";
                return RedirectToAction(nameof(OLAViolateRecords));
            }
        }

        // Add this to your PlannedEventsController
        [HttpGet]
        public async Task<IActionResult> GetUrgentRequestDetails(int id)
        {
            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            var details = new
            {
                id = plannedEvent.Id,
                peNumber = plannedEvent.PeNumber,
                customer = plannedEvent.Customer,
                priority = plannedEvent.Priority,
                urgentRequestedByName = plannedEvent.UrgentRequestedByName,
                urgentRequestReason = ExtractUrgentRequestReason(plannedEvent.Priority)

            };

            return Json(details);
        }
        public async Task<IActionResult> GlobalSearch(string? searchType, string? peNumber, string? customer, string? jobReference, string? soNumber, int pageIndex = 1)
        {

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = peNumber?.Trim() ?? "";
            customer = customer?.Trim() ?? "";
            jobReference = jobReference?.Trim() ?? "";
            soNumber = soNumber?.Trim() ?? "";

            // Get all planned events and apply filtering based on search criteria
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
            var query = allPlannedEvents.AsQueryable();

            if (!string.IsNullOrEmpty(searchType))
            {
                switch (searchType)
                {
                    case "customer":
                        if (!string.IsNullOrEmpty(customer))
                            query = query.Where(p => p.Customer != null &&
                                p.Customer.ToLower().Contains(customer.ToLower()));
                        break;
                    case "jobReference":
                        if (!string.IsNullOrEmpty(jobReference))
                            query = query.Where(p => p.JobReference != null &&
                                p.JobReference.Contains(jobReference, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "soNumber":
                        if (!string.IsNullOrEmpty(soNumber))
                            query = query.Where(p => p.SoNumber != null &&
                                p.SoNumber.Contains(soNumber, StringComparison.OrdinalIgnoreCase));
                        break;
                    default: // peNumber
                        if (!string.IsNullOrEmpty(peNumber))
                            query = query.Where(p => p.PeNumber != null &&
                                p.PeNumber.Contains(peNumber, StringComparison.OrdinalIgnoreCase));
                        break;
                }
            }

            int pageSize = 20;
            var orderedQuery = query.OrderByDescending(x => x.PeNumber);
            var totalCount = orderedQuery.Count();
            var items = orderedQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            var result = new PaginatedList<PlannedEvent>(items, totalCount, pageIndex, pageSize);

            // Update PECreatedDate for each PE based on PE number
            foreach (var pe in result)
            {
                if (pe.PeNumber != null)
                {
                    var dateFromPeNumber = GetDateFromPeNumber(pe.PeNumber);
                    if (dateFromPeNumber.HasValue)
                    {
                        pe.PECreatedDate = dateFromPeNumber.Value;
                    }
                }
            }

            // --- Provide PETasksByPeNumber for the view ---
            var peNumbers = result.Select(pe => pe.PeNumber).Where(pn => !string.IsNullOrEmpty(pn)).Cast<string>().ToList();
            var allTasks = await _peTasksApi.GetPETasksByPENumbersAsync(peNumbers);
            var peTasksByPeNumber = allTasks
                .GroupBy(t => t.PENumber)
                .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
            ViewBag.PETasksByPeNumber = peTasksByPeNumber ?? new Dictionary<string, IEnumerable<PETask>>();
            // --- End PETasksByPeNumber block ---


            // --- Provide PEReportedIssuesByPeId for the view ---
            var peIds = result.Select(pe => pe.Id).ToList();
            var allIssues = await _peIssuesApi.GetPEIssueViewModelsByPlannedEventIdsAsync(peIds);

            var issuesByPlannedEventId = allIssues
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            ViewBag.IssuesByPlannedEventId = issuesByPlannedEventId;

            ViewData["SearchType"] = searchType;
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            // Additional logic for customer search - to populate the table and summary counts
            if (searchType == "customer" && !string.IsNullOrEmpty(customer))
            {
                // Get all PEs for this customer using API service
                var allCustomerPEs = await _plannedEventsApi.GetPlannedEventsAsync();
                var customerPEs = allCustomerPEs
                    .Where(p => p.Customer != null && p.Customer.ToLower().Contains(customer.ToLower()))
                    .ToList();

                // Only ongoing PEs
                var ongoingPEs = customerPEs.Where(p => p.PEStatus != null && p.PEStatus.ToLower() == "ongoing").ToList();

                int totalPEs = customerPEs.Count;
                int ongoingCount = ongoingPEs.Count;
                int urgent1Count = customerPEs.Count(p => p.Priority != null && p.Priority.Contains("Opening Ceremony"));
                int urgent2Count = customerPEs.Count(p => p.Priority != null && p.Priority.Contains("Critical Customer"));
                int regularCount = customerPEs.Count(p =>
                    (p.Priority == null || (!p.Priority.Contains("Opening Ceremony") && !p.Priority.Contains("Critical Customer"))));

                // Set ViewData for summary and table
                ViewData["TotalPEs"] = totalPEs;
                ViewData["OngoingCount"] = ongoingCount;
                ViewData["Priority1Count"] = urgent1Count;
                ViewData["Priority2Count"] = urgent2Count;
                ViewData["RegularCount"] = regularCount;

                // Table: Only ongoing PEs, show PE Number, Status, Work Group, and View Detail
                ViewData["CustomerOngoingTable"] = ongoingPEs
                    .OrderBy(p => p.ServiceRequiredDate)
                    .Select((p, idx) => new
                    {
                        Serial = idx + 1,
                        p.PeNumber,
                        ServiceRequiredDate = p.ServiceRequiredDate?.ToString("yyyy-MM-dd") ?? "",
                        p.TaskWg,
                        p.Id
                    }).ToList();
            }

            return View(result);
        }


        public async Task<IActionResult> SalesInProgressRecords(int? pageIndex = 1)
        {
            // Check if user is actually in a sales workgroup and doesn't have ViewAll permission
            bool isInSalesWorkgroup = await IsUserInSalesWorkgroup();
            var (_, canViewAll) = await GetUserSalesWorkgroups();
            
            if (!isInSalesWorkgroup || canViewAll)
            {
                return RedirectToAction(nameof(InProgressRecords));
            }

            try
            {
                // Use the new API endpoint that handles all filtering on the backend
                var records = await _plannedEventsApi.GetSalesInProgressRecordsAsync();
                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales in-progress records");
                TempData["ErrorMessage"] = "An error occurred while retrieving sales in-progress records.";
                return View(new List<PlannedEvent>());
            }
        }


        public async Task<IActionResult> SalesHoldRecords(int? pageIndex = 1)
        {
            // Check if user is actually in a sales workgroup and doesn't have ViewAll permission
            bool isInSalesWorkgroup = await IsUserInSalesWorkgroup();
            var (_, canViewAll) = await GetUserSalesWorkgroups();
            
            if (!isInSalesWorkgroup || canViewAll)
            {
                return RedirectToAction(nameof(HoldRecords));
            }

            try
            {
                // Use the new API endpoint that handles all filtering on the backend
                var records = await _plannedEventsApi.GetSalesHoldRecordsAsync();
                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales hold records");
                TempData["ErrorMessage"] = "An error occurred while retrieving sales hold records.";
                return View(new List<PlannedEvent>());
            }
        }

        public async Task<IActionResult> SalesUrgentRecords(int? pageIndex = 1)
        {
            // Check if user is actually in a sales workgroup and doesn't have ViewAll permission
            bool isInSalesWorkgroup = await IsUserInSalesWorkgroup();
            var (_, canViewAll) = await GetUserSalesWorkgroups();
            
            if (!isInSalesWorkgroup || canViewAll)
            {
                return RedirectToAction(nameof(UrgentRecords));
            }

            try
            {
                // Use the new API endpoint that handles all filtering on the backend
                var records = await _plannedEventsApi.GetSalesUrgentRecordsAsync();
                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales urgent records");
                TempData["ErrorMessage"] = "An error occurred while retrieving sales urgent records.";
                return View(new List<PlannedEvent>());
            }
        }

        public async Task<IActionResult> SalesOLAViolateRecords(int? pageIndex = 1)
        {
            // Check if user is actually in a sales workgroup and doesn't have ViewAll permission
            bool isInSalesWorkgroup = await IsUserInSalesWorkgroup();
            var (_, canViewAll) = await GetUserSalesWorkgroups();
            
            if (!isInSalesWorkgroup || canViewAll)
            {
                return RedirectToAction(nameof(OLAViolateRecords));
            }

            try
            {
                // Use the new API endpoint that handles all filtering on the backend
                var records = await _plannedEventsApi.GetSalesOLAViolateRecordsAsync();

                // Get PE tasks for the filtered results (for violation details)
                var peNumbers = records.Select(pe => pe.PeNumber).Where(pn => pn != null).Cast<string>().ToList();
                var violatingTasksByPeNumber = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);

                var currentDate = DateTime.Today;
                var violationDetails = new Dictionary<string, object>();

                foreach (var pe in records)
                {
                    if (pe.PeNumber != null && violatingTasksByPeNumber.ContainsKey(pe.PeNumber))
                    {
                        var violatingTasks = violatingTasksByPeNumber[pe.PeNumber].Where(t => t.IsOLAViolate);
                        foreach (var task in violatingTasks)
                        {
                            var daysOverdue = task.ActualTaskCreatedDate.HasValue 
                                ? (currentDate - task.ActualTaskCreatedDate.Value).Days 
                                : 0;

                            violationDetails[pe.PeNumber] = new
                            {
                                TaskName = task.Task,
                                StartDate = task.ActualTaskCreatedDate,
                                DaysOverdue = daysOverdue,
                                Status = pe.PEStatus
                            };
                        }
                    }
                }

                ViewBag.ViolationDetails = violationDetails;
                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales OLA violate records");
                TempData["ErrorMessage"] = "An error occurred while retrieving sales OLA violate records.";
                return View(new List<PlannedEvent>());
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetBasicDetails(int id)
        {
            var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Return basic details as JSON
            var details = new
            {
                peNumber = plannedEvent.PeNumber,
                customer = plannedEvent.Customer,
                peStatus = plannedEvent.PEStatus,
                serviceType = plannedEvent.ServiceType,
                taskName = plannedEvent.TaskName,
                taskWg = plannedEvent.TaskWg
            };

            return Json(details);
        }


        [HttpGet]
        public async Task<IActionResult> GetWorkgroups(string search, int page = 1)
        {
            const int pageSize = 10;

            // Get current user's workgroup permissions
            var (userWorkgroupIds, _, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Get workgroups based on permissions and search
            var allWorkgroups = await _workGroupsApi.GetWorkGroupsAsync();

            // Filter based on permissions
            if (!canViewAll)
            {
                allWorkgroups = allWorkgroups.Where(w => userWorkgroupIds.Contains(w.Id)).ToList();
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                allWorkgroups = allWorkgroups.Where(w => w.Name.ToLower().Contains(search)).ToList();
            }

            // Get total count for pagination
            var total = allWorkgroups.Count();

            // Get paginated results
            var items = allWorkgroups
                .OrderBy(w => w.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new { id = w.Id, name = w.Name })
                .ToList();

            return Json(new
            {
                items = items,
                hasMore = (page * pageSize) < total
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSelectedWorkgroups([FromQuery] List<int> ids)
        {
            var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(ids);
            var result = workgroups.Select(w => new { id = w.Id, name = w.Name }).ToList();
            return Json(result);
        }

        // GET: PlannedEvents/TaskQueue
        [HttpGet]
        public async Task<IActionResult> TaskQueue(int? workgroupId, int? year = null, int take = 20)
        {
            var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
            var (_, _, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var effectiveWorkgroupId = canViewAll ? workgroupId : userWorkgroupId;

            try
            {
                // Load workgroups for the dropdown
                var workgroups = await _workGroupsApi.GetWorkGroupsAsync();
                ViewData["Workgroups"] = workgroups.OrderBy(w => w.Name).ToList();
                ViewData["SelectedWorkgroupId"] = effectiveWorkgroupId;
                ViewData["SelectedYear"] = year;
                ViewData["CanSwitchWorkgroup"] = canViewAll; // Add this for the view logic

                // Get years for the dropdown from actual PE numbers
                var years = await _taskQueueApiClient.GetAvailableYearsAsync();
                ViewData["AvailableYears"] = years;

                // Get prioritized tasks from the queue service
                var prioritizedTasks = await _taskQueueApiClient.GetPrioritizedTasksAsync(
                    workgroupId: effectiveWorkgroupId,
                    year: year ?? DateTime.Now.Year, // Always provide a year - current year if not specified
                    take: take);

                // Get statistics for the summary boxes
                ViewData["UrgentCount"] = prioritizedTasks.Count(t => t.Task.IsUrgent);
                ViewData["OLAViolateCount"] = prioritizedTasks.Count(t => t.Task.IsOLAViolate && !t.Task.IsUrgent);
                ViewData["ApproachingDeadlineCount"] = prioritizedTasks.Count(t =>
                    !t.Task.IsUrgent &&
                    !t.Task.IsOLAViolate &&
                    t.DaysUntilDue >= 0 &&
                    t.DaysUntilDue <= Math.Min(2, Math.Ceiling(t.OLAInDays * 0.3)));

                // Fix the regular tasks count calculation:
                ViewData["RegularTaskCount"] = prioritizedTasks.Count(t =>
                    !t.Task.IsUrgent &&
                    !t.Task.IsOLAViolate &&
                    (t.DaysUntilDue < 0 || t.DaysUntilDue > Math.Min(2, Math.Ceiling(t.OLAInDays * 0.3))));

                return View(prioritizedTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading task queue with workgroupId: {workgroupId}, year: {year}, take: {take}",
                    workgroupId, year, take);
                TempData["ErrorMessage"] = "An error occurred while loading the task queue.";
                return View(new List<TaskQueueItem>());
            }
        }

        // POST: PlannedEvents/RemoveUrgentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUrgentStatus(int id)
        {
            try
            {
                var plannedEvent = await _plannedEventsApi.GetPlannedEventByIdAsync(id);

                if (plannedEvent == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    return RedirectToAction(nameof(UrgentRecords));
                }

                // Change status from urgent back to ongoing
                if (plannedEvent.PEStatus?.ToLower() == "urgent")
                {
                    plannedEvent.PEStatus = "ongoing";

                    // Reset priority by removing urgent-related text
                    if (!string.IsNullOrEmpty(plannedEvent.Priority))
                    {
                        plannedEvent.Priority = plannedEvent.Priority
                            .Replace("[URGENT: Opening Ceremony - Priority 1]", "")
                            .Replace("[URGENT: Critical Customer - Priority 2]", "")
                            .Trim();
                    }

                    await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);

                    _logger.LogInformation("PE {peNumber} urgent status removed, returned to normal records",
                        plannedEvent.PeNumber);

                    // Update related tasks to remove urgent status
                    var peNumber = plannedEvent.PeNumber;
                    if (!string.IsNullOrEmpty(peNumber))
                    {
                        var relatedTasks = await _peTasksApi.GetPETasksByPENumberAsync(peNumber);

                        var tasksToUpdate = new List<PETask>();
                        foreach (var task in relatedTasks)
                        {
                            if (task.IsUrgent)
                            {
                                task.IsUrgent = false;

                                // Remove urgent-related text from task priority
                                if (!string.IsNullOrEmpty(task.Priority))
                                {
                                    task.Priority = task.Priority
                                        .Replace(" (Inherited from PE)", "")
                                        .Replace("[URGENT: Opening Ceremony - Priority 1]", "")
                                        .Replace("[URGENT: Critical Customer - Priority 2]", "")
                                        .Trim();
                                }

                                tasksToUpdate.Add(task);
                            }
                        }

                        if (tasksToUpdate.Any())
                        {
                            foreach (var taskToUpdate in tasksToUpdate)
                            {
                                await _peTasksApi.UpdatePETaskAsync(taskToUpdate);
                            }
                            _logger.LogInformation("Removed urgent status from {count} tasks for PE {peNumber}",
                                tasksToUpdate.Count, peNumber);
                        }
                    }

                    TempData["SuccessMessage"] = $"PE {plannedEvent.PeNumber} has been moved back to regular records.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"PE {plannedEvent.PeNumber} is not currently marked as urgent.";
                }

                return RedirectToAction(nameof(UrgentRecords));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing urgent status from PE {id}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the record status.";
                return RedirectToAction(nameof(UrgentRecords));
            }
        }

        // Add this method to your PlannedEventsController
        [HttpGet]
        public async Task<IActionResult> RefreshNextTask()
        {
            try
            {
                // Get user's primary workgroup ID for the task queue
                var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
                var nextTask = await _taskQueueApiClient.GetPrioritizedTasksAsync(workgroupId: userWorkgroupId, take: 1);
                var hasNextTask = nextTask?.Any() == true;

                if (!hasNextTask)
                {
                    return PartialView("_NextTaskEmpty");
                }

                return PartialView("_NextTaskItem", nextTask!.First());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing next task");
                return Json(new { success = false, message = "Error refreshing task queue." });
            }
        }

        // GET: PlannedEvents/LoadNextTask - Manual task loading with caching
        [HttpGet]
        public async Task<IActionResult> LoadNextTask()
        {
            try
            {
                // Get user's primary workgroup ID for the task queue
                var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
                var nextTaskList = await _taskQueueApiClient.GetPrioritizedTasksAsync(workgroupId: userWorkgroupId, take: 1);
                var hasNextTask = nextTaskList?.Any() == true;

                return Json(new { 
                    success = true, 
                    hasTask = hasNextTask,
                    taskData = hasNextTask ? nextTaskList!.First() : null,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading next task");
                return Json(new { success = false, message = "Error loading task queue." });
            }
        }

        // GET: PlannedEvents/RefreshTaskQueue - Refresh cached task queue
        [HttpGet]
        public async Task<IActionResult> RefreshTaskQueue()
        {
            try
            {
                // Get user's primary workgroup ID for the task queue
                var (userWorkgroupId, _) = await GetCurrentUserWorkGroupAsync();
                
                // Get fresh data from the background service snapshots
                var nextTaskList = await _taskQueueApiClient.GetPrioritizedTasksAsync(workgroupId: userWorkgroupId, take: 1);
                var hasNextTask = nextTaskList?.Any() == true;

                return Json(new { 
                    success = true, 
                    hasTask = hasNextTask,
                    taskData = hasNextTask ? nextTaskList!.First() : null,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    refreshed = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing task queue");
                return Json(new { success = false, message = "Error refreshing task queue." });
            }
        }

        // GET: PlannedEvents/GetPETaskListsForPE
        // Note: peId parameter is kept for API compatibility but not currently used
        // The method returns all available task lists regardless of PE ID
        [HttpGet]
        public async Task<IActionResult> GetPETaskListsForPE(int peId)
        {
            try
            {
                // Get all available task lists using API service
                // TODO: In the future, this could be filtered by peId if PE-specific task lists are needed
                var taskLists = await _peTaskListsApi.GetPETaskListsAsync();
                var result = taskLists
                    .OrderBy(tl => tl.TaskSeq)
                    .Select(tl => new { id = tl.Id, name = tl.Name })
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE task lists for PE {PeId}", peId);
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Diagnostic endpoint to check users without proper workgroup assignments
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DiagnoseWorkgroupAssignments()
        {
            try
            {
                var allUsers = await _usersApi.GetAllAsync();
                var allWorkgroups = await _workGroupsApi.GetAllAsync();

                var usersWithoutWorkgroups = allUsers
                    .Where(u => u.UserWorkGroups == null || !u.UserWorkGroups.Any())
                    .Select(u => new
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        ServiceId = u.ServiceId,
                        Role = u.UserRole?.Name,
                        HasViewAll = u.UserRole?.HasPermission("ViewAll") == true
                    })
                    .ToList();

                var usersWithInvalidWorkgroups = allUsers
                    .Where(u => u.UserWorkGroups != null && u.UserWorkGroups.Any())
                    .Where(u =>
                    {
                        var userWorkgroupIds = u.UserWorkGroups.Select(uwg => uwg.WorkGroupId).ToList();
                        var validWorkgroupIds = allWorkgroups.Select(wg => wg.Id).ToList();
                        return !userWorkgroupIds.All(id => validWorkgroupIds.Contains(id));
                    })
                    .Select(u => new
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        ServiceId = u.ServiceId,
                        WorkgroupIds = u.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList(),
                        Role = u.UserRole?.Name,
                        HasViewAll = u.UserRole?.HasPermission("ViewAll") == true
                    })
                    .ToList();

                var diagnosticsResult = new
                {
                    Summary = new
                    {
                        TotalUsers = allUsers.Count(),
                        TotalWorkgroups = allWorkgroups.Count(),
                        UsersWithoutWorkgroups = usersWithoutWorkgroups.Count,
                        UsersWithInvalidWorkgroups = usersWithInvalidWorkgroups.Count
                    },
                    UsersWithoutWorkgroups = usersWithoutWorkgroups,
                    UsersWithInvalidWorkgroups = usersWithInvalidWorkgroups,
                    AllWorkgroups = allWorkgroups.Select(wg => new { wg.Id, wg.Name }).ToList()
                };

                _logger.LogInformation("Workgroup diagnostics: {usersWithoutWorkgroups} users without workgroups, {usersWithInvalidWorkgroups} with invalid workgroups",
                    usersWithoutWorkgroups.Count, usersWithInvalidWorkgroups.Count);

                return Json(diagnosticsResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running workgroup diagnostics");
                return Json(new { error = "Failed to run diagnostics" });
            }
        }
    }
}
