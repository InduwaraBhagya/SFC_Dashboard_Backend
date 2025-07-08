using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;

namespace SFCDashboard.Controllers
{
    public partial class PlannedEventsController : Controller
    {
        private readonly IPlannedEventsApiService _plannedEventsApi;
        private readonly IUsersApiService _usersApi;
        private readonly IPETasksApiService _peTasksApi;
        private readonly IPEIssuesApiService _peIssuesApi;
        private readonly IWorkGroupsApiService _workGroupsApi;
        private readonly IAreaNetworkEngineersApiService _areaNetworkEngineersApi;
        private readonly IPETaskListsApiService _peTaskListsApi;
        private readonly IEscalationsApiService _escalationsApi;
        private readonly IPEIssueResolutionsApiService _peIssueResolutionsApi;
        private readonly ICustomerUserAssignmentsApiService _customerUserAssignmentsApi;
        private readonly ILogger<PlannedEventsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITaskQueueService _taskQueueService;

        public PlannedEventsController(
            IPlannedEventsApiService plannedEventsApi,
            IUsersApiService usersApi,
            IPETasksApiService peTasksApi,
            IPEIssuesApiService peIssuesApi,
            IWorkGroupsApiService workGroupsApi,
            IAreaNetworkEngineersApiService areaNetworkEngineersApi,
            IPETaskListsApiService peTaskListsApi,
            IEscalationsApiService escalationsApi,
            IPEIssueResolutionsApiService peIssueResolutionsApi,
            ICustomerUserAssignmentsApiService customerUserAssignmentsApi,
            ILogger<PlannedEventsController> logger,
            IWebHostEnvironment webHostEnvironment,
            ITaskQueueService taskQueueService)
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
            _taskQueueService = taskQueueService;
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

            // Check for sales workgroup first
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

            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

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
            ViewData["CanAcceptUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanAcceptUrgentRequests") == true;

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = string.IsNullOrEmpty(peNumber) ? peNumber : peNumber.Trim();
            customer = string.IsNullOrEmpty(customer) ? customer : customer.Trim();
            jobReference = string.IsNullOrEmpty(jobReference) ? jobReference : jobReference.Trim();
            soNumber = string.IsNullOrEmpty(soNumber) ? soNumber : soNumber.Trim();

            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Keep track of user's assigned workgroup(s) separately from the filter selection
            ViewData["UserAssignedWorkgroupIds"] = userWorkgroupIds;
            ViewData["UserAssignedWorkgroupNames"] = userWorkgroupNames;

            ViewData["CanViewAll"] = canViewAll;
            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsForUserAsync(userWorkgroupIds, canViewAll);

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

            // Calculate dashboard counts based on current workgroup context
            ViewData["UrgentCount"] = await GetUrgentCount(effectiveWorkgroupIds);
            ViewData["InProgressCount"] = await GetInProgressCount(effectiveWorkgroupIds);
            ViewData["OLAViolateCount"] = await GetOLAViolateCount(effectiveWorkgroupIds);
            ViewData["HoldCount"] = await GetHoldCount(effectiveWorkgroupIds);


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

            // Pre-load next task instead of loading it directly in the view
            var nextTaskList = await _taskQueueService.GetPrioritizedTasksAsync(
                workgroupId: userWorkgroupId,
                take: 1);

            ViewBag.NextTask = nextTaskList;
            ViewBag.HasNextTask = nextTaskList != null && nextTaskList.Any();

            // Pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest issues for the inbox
            var inboxIssues = await _peIssuesApi.GetInboxIssuesAsync(currentUserId);

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
                        issue.ResolutionDetails = resolution?.ResolutionDetails ?? string.Empty;
                        issue.ResolutionId = resolution?.Id;
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
                // Use the first effective workgroup name for search
                string? workgroupName = null;
                if (effectiveWorkgroupIds != null && effectiveWorkgroupIds.Any())
                {
                    var firstWorkgroupId = effectiveWorkgroupIds.First();
                    workgroupName = await _workGroupsApi.GetWorkGroupNameAsync(firstWorkgroupId);
                }

                var searchResults = await _plannedEventsApi.SearchPlannedEventsAsync(
                    searchType ?? "peNumber", searchString, 
                    workgroupName != null ? new List<string> { workgroupName } : new List<string>(), 
                    hasDrawFiberAccess, canViewAll);

                // Get PE tasks for the filtered results
                if (searchResults.Any())
                {
                    var peNumbers = searchResults.Select(pe => pe.PeNumber).ToList();
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
            ViewData["CanAcceptUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanAcceptUrgentRequests") == true;

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

            ViewData["CanViewAll"] = canViewAll;
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

            // Calculate dashboard counts using multi-workgroup specific methods
            ViewData["UrgentCount"] = await GetUrgentCountForMultiWorkgroup(workgroupIds ?? new List<int>(), userWorkgroupIds);
            ViewData["InProgressCount"] = await GetInProgressCountForMultiWorkgroup(workgroupIds ?? new List<int>(), userWorkgroupIds);
            ViewData["OLAViolateCount"] = await GetOLAViolateCountForMultiWorkgroup(workgroupIds ?? new List<int>(), userWorkgroupIds);
            ViewData["HoldCount"] = await GetHoldCountForMultiWorkgroup(workgroupIds ?? new List<int>(), userWorkgroupIds);

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

            // Pre-load next task instead of loading it directly in the view
            var nextTaskList = await _taskQueueService.GetPrioritizedTasksAsync(
                workgroupId: userWorkgroupId,
                take: 1);

            ViewBag.NextTask = nextTaskList;
            ViewBag.HasNextTask = nextTaskList != null && nextTaskList.Any();

            // Pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get inbox issues
            var inboxIssues = await _peIssuesApi.GetInboxIssuesAsync(currentUserId);

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
                        issue.ResolutionDetails = resolution?.ResolutionDetails ?? string.Empty;
                        issue.ResolutionId = resolution?.Id;
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
                // Use the first workgroup name for search
                string? workgroupName = workgroupNamesList.FirstOrDefault();

                var searchResults = await _plannedEventsApi.SearchPlannedEventsAsync(
                    searchType ?? "peNumber", searchString, workgroupName, hasDrawFiberAccess, pageIndex, 10);

                // Get PE tasks for the filtered results
                if (searchResults.Any())
                {
                    var peNumbers = searchResults.Select(pe => pe.PeNumber).ToList();
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

        private async Task<IQueryable<PlannedEvent>> ApplyCustomerFilteringAsync(IQueryable<PlannedEvent> query, List<string> salesWorkgroups, bool canViewAll)
        {
            // Get user's assigned customers
            var assignedCustomers = await GetUserAssignedCustomersAsync();
            
            if (!canViewAll)
            {
                // Apply workgroup filtering first
                query = query.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
                
                // If user has assigned customers, further filter by those customers
                if (assignedCustomers.Any())
                {
                    query = query.Where(p => p.Customer != null && assignedCustomers.Contains(p.Customer));
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

        public async Task<IActionResult> SalesView(string searchType, string peNumber, string customer,
        string jobReference, string soNumber, int? pageIndex = 1)
        {
            var currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            ViewData["CanAcceptUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanAcceptUrgentRequests") == true;

            // Get user's sales workgroup
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            // Get PE numbers with OLA violation
            var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

            // Calculate dashboard counts using the appropriate API service methods
            ViewData["UrgentCount"] = await _plannedEventsApi.GetUrgentCountAsync(salesWorkgroups);
            ViewData["InProgressCount"] = await _plannedEventsApi.GetInProgressCountAsync(salesWorkgroups);
            ViewData["OLAViolateCount"] = await _plannedEventsApi.GetOLAViolateCountAsync(salesWorkgroups);
            ViewData["HoldCount"] = await _plannedEventsApi.GetHoldCountAsync(salesWorkgroups);


            // Get pending urgent requests
            var pendingUrgentRequests = await _plannedEventsApi.GetPendingUrgentRequestsAsync(10);
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            // Get pending task urgent requests
            var pendingTaskRequests = await _peTasksApi.GetPendingTaskRequestsAsync(5);
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest inbox issues
            var inboxIssues = await _peIssuesApi.GetInboxIssuesAsync(currentUserId, 10);

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

                // Use API service for search with pagination
                int pageSize = 10;
                paginatedList = await _plannedEventsApi.SearchPlannedEventsAsync(
                    searchType, searchValue, salesWorkgroups, false, pageIndex ?? 1, pageSize);
            }
            else
            {
                // Return empty list if no search performed
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, 1, 10));
            }

            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["SalesWorkgroups"] = string.Join(", ", salesWorkgroups);
            ViewData["CanViewAll"] = canViewAll;

            var peNumbers = paginatedList.Select(pe => pe.PeNumber).ToList();
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
                i.Id,
                i.PlannedEventId,
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

            ViewData["CanMakeTasksUrgent"] = currentUser?.UserRole?.HasPermission("CanMakeTasksUrgent") == true;
            ViewData["CanReportIssues"] = currentUser?.UserRole?.HasPermission("CanReportIssues") == true;

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
            var issues = await _peIssuesApi.GetPEIssuesByPlannedEventAsync(plannedEvent.Id);

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

        private async Task<bool> PlannedEventExists(int id)
        {
            return await _plannedEventsApi.PlannedEventExistsAsync(id);
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
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            try
            {
                // Get PE numbers with OLA violation
                var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

                bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    // If admin, filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(new List<string> { workgroup.Name }, hasDrawFiberAccess, canViewAll);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                    }
                }
                else
                {
                    // Regular user: show all records for ALL their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetInProgressPlannedEventsAsync(userWorkgroupNames, hasDrawFiberAccess, canViewAll);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                ViewData["CanSendUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanSendPEUrgentRequests") == true;

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
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            try
            {
                // Get PE numbers with OLA violation
                var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

                bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    // If admin, filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(new List<string> { workgroup.Name }, hasDrawFiberAccess, canViewAll);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                    }
                }
                else
                {
                    // Regular user: show all records for ALL their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(userWorkgroupNames, hasDrawFiberAccess, canViewAll);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                var recordsList = records.OrderBy(p => p.PeNumber).ToList();

                // For details, get all violating tasks for these PEs
                var peNumbers = recordsList.Select(r => r.PeNumber).Where(p => !string.IsNullOrEmpty(p)).Cast<string>().ToList();
                var violatingTasks = await _peTasksApi.GetPETasksByPENumbersAsync(peNumbers);

                var currentDate = DateTime.Today;
                var violationDetails = violatingTasks
                    .Where(t => t.IsOLAViolate)
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            TasksCount = g.Count(),
                            MaxDaysOverdue = g.Max(t =>
                                t.EstimatedTime.HasValue
                                    ? (currentDate - t.EstimatedTime.Value).Days
                                    : (t.ActualTaskCreatedDate.HasValue && t.OLA != null && int.TryParse(t.OLA, out var olaDays2))
                                        ? (currentDate - t.ActualTaskCreatedDate.Value.AddDays(olaDays2)).Days
                                        : 0
                            ),
                            OldestViolation = g.Min(t =>
                                t.EstimatedTime ?? (t.ActualTaskCreatedDate.HasValue && t.OLA != null && int.TryParse(t.OLA, out var olaDays3)
                                    ? t.ActualTaskCreatedDate.Value.AddDays(olaDays3)
                                    : (DateTime?)null))
                        }
                    );

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
            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            try
            {
                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    // If admin, filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetHoldPlannedEventsAsync(new List<string> { workgroup.Name }, hasDrawFiberAccess, canViewAll);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetHoldPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetHoldPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                    }
                }
                else
                {
                    // Regular user: show all records for ALL their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetHoldPlannedEventsAsync(userWorkgroupNames, hasDrawFiberAccess, canViewAll);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = workgroupId;

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
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            try
            {
                IEnumerable<PlannedEvent> records;

                if (canViewAll)
                {
                    // If admin, filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _workGroupsApi.GetWorkGroupAsync(workgroupId.Value);
                        if (workgroup != null)
                        {
                            records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(new List<string> { workgroup.Name }, hasDrawFiberAccess, canViewAll);
                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }
                        else
                        {
                            records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                        }
                    }
                    else
                    {
                        records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(new List<string>(), hasDrawFiberAccess, canViewAll);
                    }
                }
                else
                {
                    // Regular user: show all records for ALL their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        records = await _plannedEventsApi.GetUrgentPlannedEventsAsync(userWorkgroupNames, hasDrawFiberAccess, canViewAll);
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                    else
                    {
                        records = new List<PlannedEvent>();
                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                return View(records.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading urgent records");
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

            ViewData["CanViewAll"] = canViewAll;
            ViewData["UserWorkGroups"] = await _workGroupsApi.GetWorkGroupsByIdsAsync(userWorkgroupIds);
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();
            ViewData["CanSendUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanSendPEUrgentRequests") == true;

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

                var workgroupNamesList = selectedWorkgroupNames.Select(w => w.Name).ToList();

                // Check if filter contains NET-PROJ-ACC-CABLE workgroup for Draw Fiber access
                bool filterHasDrawFiberAccess = workgroupNamesList.Any(name =>
                    name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                ViewData["FilteredWorkgroups"] = string.Join(", ", workgroupNamesList);

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get in-progress records using API service (MultiWorkgroup views should not use ViewAll permission)
                var allRecords = await _plannedEventsApi.GetInProgressPlannedEventsAsync(workgroupNamesList, filterHasDrawFiberAccess, canViewAll: false);

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
                allRecords = allRecords.Where(p => !violatingPENumbers.Contains(p.PeNumber)).ToList();

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

            ViewData["CanViewAll"] = canViewAll;
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

                var workgroupNamesList = selectedWorkgroupNames.Select(w => w.Name).ToList();

                // Check if filter contains NET-PROJ-ACC-CABLE workgroup for Draw Fiber access
                bool filterHasDrawFiberAccess = workgroupNamesList.Any(name =>
                    name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                ViewData["FilteredWorkgroups"] = string.Join(", ", workgroupNamesList);

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get hold records using API service (MultiWorkgroup views should not use ViewAll permission)
                var allRecords = await _plannedEventsApi.GetHoldPlannedEventsAsync(workgroupNamesList, filterHasDrawFiberAccess, canViewAll: false);

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

            ViewData["CanViewAll"] = canViewAll;
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

                var workgroupNamesList = selectedWorkgroupNames.Select(w => w.Name).ToList();

                // Check if filter contains NET-PROJ-ACC-CABLE workgroup for Draw Fiber access
                bool filterHasDrawFiberAccess = workgroupNamesList.Any(name =>
                    string.Equals(name, "NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                ViewData["FilteredWorkgroups"] = string.Join(", ", workgroupNamesList);

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get urgent records using API service (MultiWorkgroup views should not use ViewAll permission)
                var allRecords = await _plannedEventsApi.GetUrgentPlannedEventsAsync(workgroupNamesList, filterHasDrawFiberAccess, canViewAll: false);

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
                allRecords = allRecords.Where(p => !violatingPENumbers.Contains(p.PeNumber)).ToList();

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

            ViewData["CanViewAll"] = canViewAll;
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

                var workgroupNamesList = selectedWorkgroupNames.Select(w => w.Name).ToList();

                // Check if filter contains NET-PROJ-ACC-CABLE workgroup for Draw Fiber access
                bool filterHasDrawFiberAccess = workgroupNamesList.Any(name =>
                    string.Equals(name, "NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                ViewData["FilteredWorkgroups"] = string.Join(", ", workgroupNamesList);

                // Apply search filters using API service
                ViewData["SearchType"] = searchTypeValue;
                ViewData["PENumberFilter"] = peNumber?.Trim();
                ViewData["CustomerFilter"] = customer?.Trim();
                ViewData["JobReferenceFilter"] = jobReference?.Trim();
                ViewData["SONumberFilter"] = soNumber?.Trim();

                // Get OLA violating records using API service (MultiWorkgroup views should not use ViewAll permission)
                var allRecords = await _plannedEventsApi.GetOLAViolatingPlannedEventsAsync(workgroupNamesList, filterHasDrawFiberAccess, canViewAll: false);

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
                var peNumbers = allRecords.Select(pe => pe.PeNumber).ToList();
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
                    var tasksList = tasks.ToList();

                    _logger.LogInformation("Found {count} tasks to update", tasksList.Count);

                    // Process each task individually to ensure proper updates
                    foreach (var task in tasksList)
                    {
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        task.UrgentRequested = false;
                        task.Priority = priorityMessage + " (Inherited from PE)";

                        // Update task using API service
                        var updatedTask = await _peTasksApi.UpdatePETaskAsync(task);
                        if (updatedTask != null)
                        {
                            _logger.LogInformation("Updated task {id} with priority: {priority}",
                                task.Id, task.Priority);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to update task {id}", task.Id);
                        }
                    }

                    // Verify the update by checking one task using API service
                    if (tasksList.Any())
                    {
                        var verifyTasks = await _peTasksApi.GetPETasksByPENumberAsync(peNumber);
                        var verifyTask = verifyTasks.FirstOrDefault();

                        if (verifyTask != null)
                        {
                            _logger.LogInformation("Verification - Task {id} has priority: {priority}",
                                verifyTask.Id, verifyTask.Priority);
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
            var reminder = await _peIssuesApi.GetPEIssueAsync(request.Id);
            if (reminder != null && reminder.IsReminder == true) // Ensure it's actually a reminder
            {
                reminder.IsRead = true;
                var updatedReminder = await _peIssuesApi.UpdatePEIssueAsync(reminder);
                if (updatedReminder != null)
                {
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
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
            plannedEvent.PECreatedDate = DateTime.Now; // Set the current date/time

            var currentUser = await _usersApi.GetUserByServiceIdAsync(User.Identity?.Name ?? "");
            plannedEvent.UrgentRequestedById = currentUser?.Id; // Add this property to your model/table if not present
            plannedEvent.UrgentRequestedByName = currentUser?.Name; // Or just store the name if you prefer

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

            await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);

            _logger.LogInformation("PE ID {id} marked with urgent request flag with reason: {reason}", id, urgentReason);
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
            ViewBag.PETasksByPeNumber = peTasksByPeNumber;
            // --- End PETasksByPeNumber block ---


            // --- Provide PEReportedIssuesByPeId for the view ---
            var peIds = result.Select(pe => pe.Id).ToList();
            var allIssues = await _peIssuesApi.GetIssuesByPlannedEventIdsAsync(peIds);

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
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            // Get user's sales workgroup
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(InProgressRecords));
            }

            // Get PE numbers with OLA violation
            var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

            // Get all planned events and apply filtering
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
            
            // Apply customer filtering with workgroup checks
            var filteredEvents = await ApplyCustomerFilteringAsync(allPlannedEvents.AsQueryable(), salesWorkgroups, canViewAll);

            var records = filteredEvents
                .Where(p =>
                    (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                    !p.IsHold &&
                    !violatingPENumbers.Contains(p.PeNumber))
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToList();

            ViewData["CanViewAll"] = canViewAll;
            ViewData["CanSendUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanSendPEUrgentRequests") == true;
            return View(records);

        }


        public async Task<IActionResult> SalesHoldRecords(int? pageIndex = 1)
        {
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(HoldRecords));
            }

            // Get all planned events and apply filtering
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
            var holdEvents = allPlannedEvents.Where(p => p.IsHold);

            // Apply customer filtering with workgroup checks
            var filteredEvents = await ApplyCustomerFilteringAsync(holdEvents.AsQueryable(), salesWorkgroups, canViewAll);

            var records = filteredEvents
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToList();

            ViewData["CanViewAll"] = canViewAll;
            return View(records);
        }

        public async Task<IActionResult> SalesUrgentRecords(int? pageIndex = 1)
        {
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(UrgentRecords));
            }

            var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

            // Get all planned events and apply filtering
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
            var urgentEvents = allPlannedEvents
                .Where(p => p.PEStatus == "urgent" &&
                       !p.IsHold &&
                       !violatingPENumbers.Contains(p.PeNumber));

            // Apply customer filtering with workgroup checks
            var filteredEvents = await ApplyCustomerFilteringAsync(urgentEvents.AsQueryable(), salesWorkgroups, canViewAll);

            var records = filteredEvents
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToList();

            ViewData["CanViewAll"] = canViewAll;
            return View(records);
        }

        public async Task<IActionResult> SalesOLAViolateRecords(int? pageIndex = 1)
        {
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(OLAViolateRecords));
            }

            var violatingPENumbers = await _peTasksApi.GetOLAViolatingPENumbersAsync();

            // Get all planned events and apply filtering
            var allPlannedEvents = await _plannedEventsApi.GetPlannedEventsAsync();
            var violatingEvents = allPlannedEvents
                .Where(p => violatingPENumbers.Contains(p.PeNumber) && !p.IsHold);

            // Apply customer filtering with workgroup checks
            var filteredEvents = await ApplyCustomerFilteringAsync(violatingEvents.AsQueryable(), salesWorkgroups, canViewAll);

            var records = filteredEvents
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToList();

            // Get violation details for view
            var peNumbers = records.Select(p => p.PeNumber).Where(pn => !string.IsNullOrEmpty(pn)).ToList();
            var violatingTasksByPeNumber = await _peTasksApi.GetTasksByPeNumbersAsync(peNumbers);
            
            var currentDate = DateTime.Today;
            var violationDetails = new Dictionary<string, object>();
            
            foreach (var kvp in violatingTasksByPeNumber)
            {
                var violatingTasks = kvp.Value.Where(t => t.IsOLAViolate).ToList();
                if (violatingTasks.Any())
                {
                    violationDetails[kvp.Key] = new
                    {
                        TasksCount = violatingTasks.Count(),
                        MaxDaysOverdue = violatingTasks.Max(t =>
                            t.EstimatedTime.HasValue
                                ? (currentDate - t.EstimatedTime.Value).Days
                                : (t.ActualTaskCreatedDate.HasValue && t.OLA != null &&
                                   int.TryParse(t.OLA, out var olaDays))
                                    ? (currentDate - t.ActualTaskCreatedDate.Value.AddDays(olaDays)).Days
                                    : 0
                        ),
                        OldestViolation = violatingTasks.Min(t =>
                            t.EstimatedTime ??
                            (t.ActualTaskCreatedDate.HasValue && t.OLA != null &&
                             int.TryParse(t.OLA, out var olaDays)
                                ? t.ActualTaskCreatedDate.Value.AddDays(olaDays)
                                : (DateTime?)null))
                    };
                }
            }
            
            ViewBag.ViolationDetails = violationDetails;

            ViewData["CanViewAll"] = canViewAll;
            return View(records);
        }


        private async Task<int> GetUrgentCount(List<int>? workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting urgent count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check permissions
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);

            bool canViewAll = currentUser?.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // If user has ViewAll and no specific workgroup selected, show ALL records
            if (canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                var countAll = await _plannedEventsApi.GetUrgentCountAsync(new List<string>(), hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("Urgent count (ViewAll): {count}", countAll);
                return countAll;
            }

            // If user has ViewAll and selected specific workgroup(s), filter by those workgroup(s)
            if (canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(w => w.Name).ToList();

                var countFiltered = await _plannedEventsApi.GetUrgentCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("Urgent count (ViewAll + filter): {count} for workgroups: {workgroups}",
                    countFiltered, string.Join(", ", workgroupIds));
                return countFiltered;
            }

            // For regular users (non-ViewAll), get their workgroups if not provided
            if (!canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                workgroupIds = currentUser?.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            // For regular users, filter by their workgroups
            if (!canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(w => w.Name).ToList();

                var count = await _plannedEventsApi.GetUrgentCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
                _logger.LogInformation("Urgent count: {count} for workgroups: {workgroups}",
                    count, workgroupIds != null ? string.Join(", ", workgroupIds) : "NULL");
                return count;
            }

            // Fallback - no workgroups found
            _logger.LogWarning("No workgroups found for urgent count calculation");
            return 0;
        }



        private async Task<int> GetOLAViolateCount(List<int>? workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting OLA violate count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check ViewAll permission
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found: {userId}", currentUserId);
                return 0;
            }

            bool canViewAll = currentUser.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // If user has ViewAll and no specific workgroup selected, show ALL records
            if (canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                var countAll = await _plannedEventsApi.GetOLAViolateCountAsync(new List<string>(), hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("OLA violate count (ViewAll): {count}", countAll);
                return countAll;
            }

            // If user has ViewAll and selected specific workgroup(s), filter by those workgroup(s)
            if (canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(w => w.Name).ToList();

                var countFiltered = await _plannedEventsApi.GetOLAViolateCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("OLA violate count (ViewAll + filter): {count} for workgroups: {workgroups}",
                    countFiltered, string.Join(", ", workgroupIds));
                return countFiltered;
            }

            // For regular users (non-ViewAll), get their workgroups if not provided
            if (!canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                workgroupIds = currentUser.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            // For regular users, filter by their workgroups
            if (!canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(wg => wg.Name).ToList();

                var count = await _plannedEventsApi.GetOLAViolateCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
                _logger.LogInformation("OLA violate count: {count} for workgroups: {workgroups}",
                    count, string.Join(", ", workgroupIds ?? new List<int>()));
                return count;
            }

            // Default return 0 if no matching events
            return 0;
        }


        private async Task<int> GetHoldCount(List<int>? workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting hold count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check permissions
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found: {userId}", currentUserId);
                return 0;
            }

            bool canViewAll = currentUser.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // If user has ViewAll and no specific workgroup selected, show ALL records
            if (canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                var countAll = await _plannedEventsApi.GetHoldCountAsync(new List<string>(), hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("Hold count (ViewAll): {count}", countAll);
                return countAll;
            }

            // If user has ViewAll and selected specific workgroup(s), filter by those workgroup(s)
            if (canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(wg => wg.Name).ToList();

                var countFiltered = await _plannedEventsApi.GetHoldCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("Hold count (ViewAll + filter): {count} for workgroups: {workgroups}",
                    countFiltered, string.Join(", ", workgroupIds));
                return countFiltered;
            }

            // For regular users (non-ViewAll), get their workgroups if not provided
            if (!canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                workgroupIds = currentUser.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            // For regular users, filter by their workgroups
            if (!canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(wg => wg.Name).ToList();

                var count = await _plannedEventsApi.GetHoldCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
                _logger.LogInformation("Hold count: {count} for workgroups: {workgroups}",
                    count, string.Join(", ", workgroupIds ?? new List<int>()));
                return count;
            }

            // Default return 0 if no matching events
            return 0;
        }


        private async Task<int> GetInProgressCount(List<int>? workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting in-progress count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check ViewAll permission
            var currentUser = await _usersApi.GetUserWithRoleAndWorkGroupsAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found: {userId}", currentUserId);
                return 0;
            }

            bool canViewAll = currentUser.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // If user has ViewAll and no specific workgroup selected, show ALL records
            if (canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                var countAll = await _plannedEventsApi.GetInProgressCountAsync(new List<string>(), hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("In-progress count (ViewAll): {count}", countAll);
                return countAll;
            }

            // If user has ViewAll and selected specific workgroup(s), filter by those workgroup(s)
            if (canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(wg => wg.Name).ToList();

                var countFiltered = await _plannedEventsApi.GetInProgressCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: true);
                _logger.LogInformation("In-progress count (ViewAll + filter): {count} for workgroups: {workgroups}",
                    countFiltered, string.Join(", ", workgroupIds));
                return countFiltered;
            }

            // For regular users (non-ViewAll), get their workgroups if not provided
            if (!canViewAll && (workgroupIds == null || !workgroupIds.Any()))
            {
                workgroupIds = currentUser.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            // For regular users, filter by their workgroups
            if (!canViewAll && workgroupIds != null && workgroupIds.Any())
            {
                var workgroups = await _workGroupsApi.GetWorkGroupsByIdsAsync(workgroupIds);
                var workgroupNames = workgroups.Select(wg => wg.Name).ToList();

                var count = await _plannedEventsApi.GetInProgressCountAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
                _logger.LogInformation("In-progress count: {count} for workgroups: {workgroups}",
                    count, string.Join(", ", workgroupIds ?? new List<int>()));
                return count;
            }

            // Default return 0 if no matching events
            return 0;
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
                ViewData["CanSwitchWorkgroup"] = canViewAll;

                // Get years for the dropdown from actual PE numbers
                var years = await _taskQueueService.GetAvailableYearsAsync();
                ViewData["AvailableYears"] = years;

                // Get prioritized tasks from the queue service
                var prioritizedTasks = await _taskQueueService.GetPrioritizedTasksAsync(
                    workgroupId: effectiveWorkgroupId,
                    year: year,
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
                var workgroupId = ViewBag.UserWorkgroupId;
                var nextTask = await _taskQueueService.GetPrioritizedTasksAsync(workgroupId: workgroupId, take: 1);
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
    }
}
