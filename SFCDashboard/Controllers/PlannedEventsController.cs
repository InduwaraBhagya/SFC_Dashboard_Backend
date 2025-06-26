using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;

namespace SFCDashboard.Controllers
{
    public class PlannedEventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlannedEventsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITaskQueueService _taskQueueService;

        public PlannedEventsController(ApplicationDbContext context, ILogger<PlannedEventsController> logger, IWebHostEnvironment webHostEnvironment, ITaskQueueService taskQueueService)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _taskQueueService = taskQueueService;
        }

        private async Task<int>
        GetCurrentUserIdAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            // Extract the substring before the query (standardize to 6 chars)
            var serviceIdShort = ExtractServiceId(serviceId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);
            return user?.Id ?? 0;
        }

        // GET: PlannedEvents/Index
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, List<int> workgroupIds, int pageIndex = 1)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

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

            // Check if user belongs to NET-PROJ-ACC-CABLE workgroup
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["HasDrawFiberAccess"] = hasDrawFiberAccess;
            ViewData["CanAcceptUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanAcceptUrgentRequests") == true;

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = peNumber?.Trim();
            customer = customer?.Trim();
            jobReference = jobReference?.Trim();
            soNumber = soNumber?.Trim();

            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();


            ViewData["CanViewAll"] = canViewAll;
            ViewData["UserWorkGroups"] = await _context.WorkGroups
                .Where(w => canViewAll || userWorkgroupIds.Contains(w.Id))
                .OrderBy(w => w.Name)
                .ToListAsync();

            // Keep track of selected workgroups
            ViewData["SelectedWorkgroupIds"] = workgroupIds ?? new List<int>();

            // Base query
            var query = _context.PlannedEvents.AsQueryable();

            // Workgroup filtering logic
            if (canViewAll)
            {
                // If user has ViewAll and selected workgroups, filter by those
                if (workgroupIds != null && workgroupIds.Count > 0)
                {
                    var selectedWorkgroupNames = await _context.WorkGroups
                        .Where(w => workgroupIds.Contains(w.Id))
                        .Select(w => w.Name)
                        .ToListAsync();

                    if (selectedWorkgroupNames.Any())
                    {
                        // If user has DrawFiberAccess, include Draw Fiber tasks from all workgroups
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p =>
                                p.TaskWg != null && (
                            selectedWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                                // NEW: Include events with any related PETask named "draw fiber"
                                || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                                )
                            );
                        }
                        else
                        {
                            // Normal workgroup filtering
                            query = query.Where(p => p.TaskWg != null &&
                                selectedWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }

                        ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames);
                        ViewData["SelectedWorkgroupIds"] = workgroupIds;
                        ViewData["SelectedWorkgroupNames"] = selectedWorkgroupNames;
                    }
                }
                // else: show all records (no filter)
            }
            else
            {
                // User does NOT have ViewAll: always restrict to their assigned workgroups
                var userWorkgroupNamesDb = await _context.WorkGroups
                    .Where(w => userWorkgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (workgroupIds != null && workgroupIds.Count > 0)
                {
                    // If user selected a subset, filter by those
                    var selectedWorkgroupNames = await _context.WorkGroups
                        .Where(w => workgroupIds.Contains(w.Id))
                        .Select(w => w.Name)
                        .ToListAsync();

                    if (selectedWorkgroupNames.Any())
                    {
                        // If user has DrawFiberAccess, include Draw Fiber tasks from all workgroups
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p =>
                                p.TaskWg != null && (
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                                // NEW: Include events with any related PETask named "draw fiber"
                                || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                                )
                            );
                        }
                        else
                        {
                            // Normal workgroup filtering
                            query = query.Where(p => p.TaskWg != null &&
                                selectedWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }

                        ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames);
                        ViewData["SelectedWorkgroupIds"] = workgroupIds;
                        ViewData["SelectedWorkgroupNames"] = selectedWorkgroupNames;
                    }
                }
                else
                {
                    // No filter selected: restrict to all user's assigned workgroups
                    if (userWorkgroupNames.Any())
                    {
                        // If user has DrawFiberAccess, include Draw Fiber tasks from all workgroups
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p =>
                                p.TaskWg != null && (
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                                // NEW: Include events with any related PETask named "draw fiber"
                                || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                                )
                            );
                        }
                        else
                        {
                            // Normal workgroup filtering
                            query = query.Where(p => p.TaskWg != null &&
                                userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }

                        ViewData["FilteredWorkgroups"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupIds"] = userWorkgroupIds;
                    }
                }
            }


            // Calculate dashboard counts based on current workgroup context
            var effectiveWorkgroupIds = canViewAll && workgroupIds != null && workgroupIds.Count > 0
                ? workgroupIds.ToList()
                : userWorkgroupIds;

            ViewData["UrgentCount"] = await GetUrgentCount(effectiveWorkgroupIds);
            ViewData["InProgressCount"] = await GetInProgressCount(effectiveWorkgroupIds);
            ViewData["OLAViolateCount"] = await GetOLAViolateCount(effectiveWorkgroupIds);
            ViewData["HoldCount"] = await GetHoldCount(effectiveWorkgroupIds);


            if (canViewAll)
            {
                var workgroups = await _context.WorkGroups
                    .OrderBy(w => w.Name)
                    .ToListAsync();
                ViewData["Workgroups"] = workgroups;
            }

            ViewData["CanViewAll"] = canViewAll;
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


            // Pending urgent requests (same as before)
            var pendingUrgentRequests = await _context.PlannedEvents
                .Where(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                .OrderByDescending(p => p.PECreatedDate)
                .Take(10)
                .ToListAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            var pendingTaskRequests = await _context.PETasks
                .Where(t => t.UrgentRequested && !t.IsUrgent)
                .Include(t => t.PlannedEvent)
                .OrderByDescending(t => t.TaskCreatedDate)
                .Take(5)
                .ToListAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest issues for the inbox
            // In your inbox action methods
            // In PlannedEventsController.cs, in the Index action
            var inboxIssues = await _context.PEIssues
                .Where(i => i.ReceiverId == currentUserId
                    && !i.IsHiddenFromInbox)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id,
                    SenderId = i.SenderId,
                    SenderName = _context.Users
                        .Where(u => u.Id == i.SenderId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Sender",
                    ReceiverId = i.ReceiverId,
                    ReceiverName = _context.Users
                        .Where(u => u.Id == i.ReceiverId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Receiver",
                    IssueText = i.IssueText,
                    AttachmentPath = i.AttachmentPath,
                    CreatedAt = i.CreatedAt,
                    PlannedEventId = i.PlannedEventId,
                    IsRead = i.IsRead,
                    IsReply = i.IsReply,
                    OriginalIssueId = i.OriginalIssueId,
                    IsResolved = i.IsResolved,
                    IsResolutionRequest = i.IsResolutionRequest,
                    PETaskId = i.PETaskId
                })
                .ToListAsync();

            // Add unread count
            var unreadCount = inboxIssues.Count(i => !i.IsRead);
            // Add pending urgent PE requests to unread count
            if (ViewData["PendingUrgentRequests"] != null)
            {
                unreadCount += ((IEnumerable<PlannedEvent>)ViewData["PendingUrgentRequests"])
                    .Count(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION");
            }

            // Add pending urgent task requests to unread count
            if (ViewData["PendingTaskRequests"] != null)
            {
                unreadCount += ((IEnumerable<PETask>)ViewData["PendingTaskRequests"])
                    .Count(t => t.UrgentRequested && !t.IsUrgent);
            }

            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues.Count;
            ViewData["UnreadMessages"] = unreadCount;

            if (inboxIssues != null)
            {
                foreach (var issue in inboxIssues)
                {
                    if (issue.IsResolutionRequest)
                    {
                        var resolution = await _context.PEIssueResolutions
                            .FirstOrDefaultAsync(r => r.IssueId == (issue.OriginalIssueId ?? issue.Id) && !r.IsConfirmed);

                        issue.ResolutionDetails = resolution?.ResolutionDetails;
                        issue.ResolutionId = resolution?.Id;
                    }
                }
            }

            ViewBag.InboxIssues = inboxIssues;
            ViewData["InboxIssues"] = inboxIssues;

            // Create a lookup dictionary for resolutions
            if (inboxIssues != null && inboxIssues.Any())
            {
                Dictionary<int, PEIssueResolution> resolutionsByIssueId = new Dictionary<int, PEIssueResolution>();

                // Get all issue IDs that need resolution details
                var issueIds = inboxIssues
                    .Where(i => i.IsResolutionRequest)
                    .Select(i => i.OriginalIssueId ?? i.Id)
                    .ToList();

                if (issueIds.Any())
                {
                    // Fetch all resolutions in one query
                    var resolutions = await _context.PEIssueResolutions
                        .Where(r => issueIds.Contains(r.IssueId) && !r.IsConfirmed)
                        .ToListAsync();

                    foreach (var resolution in resolutions)
                    {
                        resolutionsByIssueId[resolution.IssueId] = resolution;
                    }
                }

                // Add to ViewBag for use in the view
                ViewBag.ResolutionsByIssueId = resolutionsByIssueId;
            }

            if (workgroupIds != null && workgroupIds.Count > 0)
            {
                var selectedWorkgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (selectedWorkgroupNames.Any())
                {
                    query = query.Where(p => p.TaskWg != null &&
                        selectedWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    ViewData["FilteredWorkgroups"] = string.Join(", ", selectedWorkgroupNames);
                    ViewData["SelectedWorkgroupIds"] = workgroupIds;
                    ViewData["SelectedWorkgroupNames"] = selectedWorkgroupNames;
                    _logger.LogInformation("User filtering by: {WorkgroupNames}", string.Join(", ", selectedWorkgroupNames));
                }
            }


            // Apply search filters based on type
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchType)
                {
                    case "customer":
                        if (!string.IsNullOrEmpty(customer))
                            query = query.Where(p => p.Customer != null &&
                                EF.Functions.Like(p.Customer, $"%{customer}%"));
                        break;
                    case "jobReference":
                        if (!string.IsNullOrEmpty(jobReference))
                            query = query.Where(p => p.JobReference != null &&
                                EF.Functions.Like(p.JobReference, $"%{jobReference}%"));
                        break;
                    case "soNumber":
                        if (!string.IsNullOrEmpty(soNumber))
                            query = query.Where(p => p.SoNumber != null &&
                                EF.Functions.Like(p.SoNumber, $"%{soNumber}%"));
                        break;
                    default: // peNumber
                        if (!string.IsNullOrEmpty(peNumber))
                            query = query.Where(p => p.PeNumber != null &&
                                EF.Functions.Like(p.PeNumber, $"%{peNumber}%"));
                        break;
                }

                query = query.OrderByDescending(p => p.PECreatedDate)
                            .ThenBy(p => p.PeNumber);

                int pageSize = 10;
                var paginatedList = await PaginatedList<PlannedEvent>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

                // Update PECreatedDate for each PE based on PE number
                foreach (var pe in paginatedList)
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

                // --- PETasksByPeNumber population ---
                var peNumbers = paginatedList.Select(pe => pe.PeNumber).ToList();
                var allTasks = await _context.PETasks
                    .Where(t => peNumbers.Contains(t.PENumber))
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
                var peTasksByPeNumber = allTasks
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
                ViewBag.PETasksByPeNumber = peTasksByPeNumber;
                // --- END PETasksByPeNumber population ---

                return View(paginatedList);
            }
            else
            {
                // Return empty list but still show dashboard data
                ViewBag.PETasksByPeId = new Dictionary<int, IEnumerable<PETask>>();
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, 10));
            }
        }

        private async Task<bool> IsUserInSalesWorkgroup()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return false;

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return false;

            var user = await _context.Users
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(email));

            return user?.UserWorkGroups
                ?.Any(uwg => uwg.WorkGroup.Name.Contains("SALES", StringComparison.OrdinalIgnoreCase))
                ?? false;
        }

        public async Task<IActionResult> SalesView(string searchType, string peNumber, string customer,
        string jobReference, string soNumber, int? pageIndex = 1)
        {
            var currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _context.Users
            .Include(u => u.UserRole)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

            ViewData["CanAcceptUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanAcceptUrgentRequests") == true;

            // Get user's sales workgroup
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(Index));
            }


            // Get PE numbers with OLA violation
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            // Base query for records where user's workgroup matches either TaskWg or SectionHandledBy
            var baseQuery = _context.PlannedEvents.AsQueryable();

            if (!canViewAll)
            {
                baseQuery = baseQuery.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
            }

            // Calculate dashboard counts using the same logic as specific views
            ViewData["UrgentCount"] = await baseQuery
                .Where(p =>
                    p.PEStatus == "urgent" &&
                    !p.IsHold &&
                    !violatingPENumbers.Contains(p.PeNumber))
                .CountAsync();

            ViewData["InProgressCount"] = await baseQuery
                .Where(p =>
                    (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                    !p.IsHold &&
                    !violatingPENumbers.Contains(p.PeNumber))
                .CountAsync();

            ViewData["OLAViolateCount"] = await baseQuery
                .Where(p => violatingPENumbers.Contains(p.PeNumber))
                .CountAsync();

            ViewData["HoldCount"] = await baseQuery
                .Where(p => p.IsHold)
                .CountAsync();


            // Get pending urgent requests
            var pendingUrgentRequests = await _context.PlannedEvents
                .Where(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                .OrderByDescending(p => p.PECreatedDate)
                .Take(10)
                .ToListAsync();
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;

            // Get pending task urgent requests
            var pendingTaskRequests = await _context.PETasks
                .Where(t => t.UrgentRequested && !t.IsUrgent)
                .Include(t => t.PlannedEvent)
                .OrderByDescending(t => t.TaskCreatedDate)
                .Take(5)
                .ToListAsync();
            ViewData["PendingTaskRequests"] = pendingTaskRequests;

            // Get latest inbox issues
            var inboxIssues = await _context.PEIssues
                .Where(i => i.ReceiverId == currentUserId && !i.IsHiddenFromInbox)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id,
                    SenderId = i.SenderId,
                    SenderName = _context.Users
                        .Where(u => u.Id == i.SenderId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Sender",
                    ReceiverId = i.ReceiverId,
                    ReceiverName = _context.Users
                        .Where(u => u.Id == i.ReceiverId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Receiver",
                    IssueText = i.IssueText,
                    AttachmentPath = i.AttachmentPath,
                    CreatedAt = i.CreatedAt,
                    PlannedEventId = i.PlannedEventId,
                    IsRead = i.IsRead,
                    IsReply = i.IsReply,
                    OriginalIssueId = i.OriginalIssueId,
                    IsResolved = i.IsResolved,
                    IsResolutionRequest = i.IsResolutionRequest,
                    PETaskId = i.PETaskId
                })
                .ToListAsync();

            // Calculate unread count
            var unreadCount = inboxIssues.Count(i => !i.IsRead);
            unreadCount += pendingUrgentRequests.Count;
            unreadCount += pendingTaskRequests.Count;

            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues.Count;
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
                    var resolutions = await _context.PEIssueResolutions
                        .Where(r => issueIds.Contains(r.IssueId) && !r.IsConfirmed)
                        .ToListAsync();

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

            var query = baseQuery;

            if (isSearchPerformed)
            {
                switch (searchType.ToLower())
                {
                    case "customer" when !string.IsNullOrEmpty(customer):
                        query = query.Where(p => p.Customer.Contains(customer));
                        ViewData["CustomerFilter"] = customer;
                        break;
                    case "jobreference" when !string.IsNullOrEmpty(jobReference):
                        query = query.Where(p => p.JobReference.Contains(jobReference));
                        ViewData["JobReferenceFilter"] = jobReference;
                        break;
                    case "sonumber" when !string.IsNullOrEmpty(soNumber):
                        query = query.Where(p => p.SoNumber.Contains(soNumber));
                        ViewData["SONumberFilter"] = soNumber;
                        break;
                    default: // peNumber
                        if (!string.IsNullOrEmpty(peNumber))
                            query = query.Where(p => p.PeNumber.Contains(peNumber));
                        ViewData["PENumberFilter"] = peNumber;
                        break;
                }
            }
            else
            {
                // Return empty list if no search performed
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, 1, 10));
            }

            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["SalesWorkgroups"] = string.Join(", ", salesWorkgroups);
            ViewData["CanViewAll"] = canViewAll;

            // Get tasks for the paginated PEs
            int pageSize = 10;
            var paginatedList = await PaginatedList<PlannedEvent>.CreateAsync(
                query.OrderByDescending(p => p.ServiceRequiredDate),
                pageIndex ?? 1,
                pageSize);

            var peNumbers = paginatedList.Select(pe => pe.PeNumber).ToList();
            var allTasks = await _context.PETasks
                .Where(t => peNumbers.Contains(t.PENumber))
                .OrderBy(t => t.TaskSeq)
                .ToListAsync();

            var peTasksByPeNumber = allTasks
                .GroupBy(t => t.PENumber)
                .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
            ViewBag.PETasksByPeNumber = peTasksByPeNumber;

            // Get issues for the paginated PEs
            var peIds = paginatedList.Select(pe => pe.Id).ToList();
            var allIssues = await _context.PEIssues
                .Where(i => peIds.Contains(i.PlannedEventId) && i.IsReminder == false)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id,
                    SenderId = i.SenderId,
                    SenderName = _context.Users
                        .Where(u => u.Id == i.SenderId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Sender",
                    ReceiverId = i.ReceiverId,
                    ReceiverName = _context.Users
                        .Where(u => u.Id == i.ReceiverId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Receiver",
                    IssueText = i.IssueText,
                    AttachmentPath = i.AttachmentPath,
                    CreatedAt = i.CreatedAt,
                    PlannedEventId = i.PlannedEventId,
                    IsResolved = i.IsResolved,
                    IsHiddenFromInbox = i.IsHiddenFromInbox
                })
                .ToListAsync();

            var issuesByPlannedEventId = allIssues
                .GroupBy(i => i.PlannedEventId)
                .ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.IssuesByPlannedEventId = issuesByPlannedEventId;

            return View(paginatedList);
        }

        private string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            // Extract up to the first 6 characters of the email or service ID
            return email.Length > 6 ? email.Substring(0, 6) : email;
        }


        //here this part for handle reminder as notification
        [HttpGet]
        public async Task<IActionResult> GetReminders(bool showAll = true)
        {
            var userId = await GetCurrentUserIdAsync();
            var query = _context.PEIssues
                .Where(i => i.ReceiverId == userId && i.SenderId == 1);

            if (!showAll)
            {
                query = query.Where(i => !i.IsRead);
            }

            var reminders = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.PlannedEventId,
                    message = i.IssueText,
                    createdDate = i.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                    isRead = i.IsRead
                })
                .ToListAsync();

            return Json(reminders);
        }

        [HttpGet]
        public async Task<IActionResult> GetReminderCount()
        {
            var userId = await GetCurrentUserIdAsync();
            var count = await _context.PEIssues
                .CountAsync(i => i.ReceiverId == userId && i.SenderId == 1 && !i.IsRead);

            return Json(new { count });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRemindersAsRead()
        {
            var userId = await GetCurrentUserIdAsync();
            var unreadReminders = await _context.PEIssues
                .Where(i => i.ReceiverId == userId && i.SenderId == 1 && !i.IsRead)
                .ToListAsync();

            foreach (var reminder in unreadReminders)
            {
                reminder.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET: PlannedEvents/Details/5
        public async Task<IActionResult> Details(int? id, string returnUrl = null)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            ViewData["CanMakeTasksUrgent"] = currentUser?.UserRole?.HasPermission("CanMakeTasksUrgent") == true;
            ViewData["CanReportIssues"] = currentUser?.UserRole?.HasPermission("CanReportIssues") == true;

            // Get the planned event first
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Find the engineer by LEA code
            string engineerName = null;
            if (!string.IsNullOrEmpty(plannedEvent.Lea))
            {
                engineerName = await _context.AreaNetworkEngineers
                    .Where(e => e.Area == plannedEvent.Lea)
                    .Select(e => e.EngineerName)
                    .FirstOrDefaultAsync();
            }
            ViewBag.NetworkEngineer = engineerName ?? "Not Assigned";

            // Check if current task is "Draw Fiber"
            bool isCurrentTaskDrawFiber = plannedEvent.TaskName?.Trim().ToLower() == "draw fiber";
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // Only allow estimated time management if user has permission, is in the right workgroup,
            // AND the current task is "Draw Fiber"
            ViewData["CanManageEstimatedTime"] =
                currentUser?.UserRole?.HasPermission("CanManageEstimatedTime") == true &&
                hasDrawFiberAccess &&
                isCurrentTaskDrawFiber;


            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Get related PE tasks for this event
            var peTasks = await _context.PETasks
                .Where(t => t.PENumber == plannedEvent.PeNumber)
                .OrderBy(t => t.TaskSeq)
                .ToListAsync();
            // Set the correct dates for display
            SetTaskDatesFromPeNumber(plannedEvent.PeNumber, peTasks);

            ViewBag.PETasks = peTasks;

            // Find PETaskListId for the current task name
            var taskList = await _context.PETaskLists
                .FirstOrDefaultAsync(tl => tl.Name == plannedEvent.TaskName);
            ViewBag.CurrentTaskListId = taskList?.Id;

            // Load issues/subtasks for this PE (assuming you use PEIssue or Subtask table)
            var issues = await _context.PEIssues
    .Where(i => i.PlannedEventId == plannedEvent.Id && i.IsReminder == false)
    .OrderByDescending(i => i.CreatedAt)
    .Select(i => new PEIssueViewModel
    {
        Id = i.Id,
        SenderId = i.SenderId,
        SenderName = _context.Users.Where(u => u.Id == i.SenderId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Sender",
        ReceiverId = i.ReceiverId,
        ReceiverName = _context.Users.Where(u => u.Id == i.ReceiverId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Receiver",
        IssueText = i.IssueText,
        AttachmentPath = i.AttachmentPath,
        CreatedAt = i.CreatedAt,
        PlannedEventId = i.PlannedEventId,
        IsResolved = i.IsResolved,
        IsHiddenFromInbox = i.IsHiddenFromInbox  // Add this property
    })
    .ToListAsync();



            ViewBag.PEReportedIssues = issues;

            ViewBag.ReturnUrl = returnUrl;


            var peTaskIds = peTasks.Select(t => t.Id).ToList();

            var escalations = await _context.Escalations
                .Include(e => e.PETask)
                .Include(e => e.IgnoredBy)
                .Where(e => peTaskIds.Contains(e.TaskId) && e.IsResolved)
                .ToListAsync();

            plannedEvent.Escalations = escalations;
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
                _context.Add(plannedEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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

            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
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
                    _context.Update(plannedEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlannedEventExists(plannedEvent.Id))
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

        private bool PlannedEventExists(int id)
        {
            throw new NotImplementedException();
        }

        // GET: PlannedEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plannedEvent = await _context.PlannedEvents
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent != null)
            {
                _context.PlannedEvents.Remove(plannedEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> InProgressRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);
            try
            {
                // Get PE numbers with OLA violation
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);


                // Base query, EXCLUDING OLA Violate records
                var query = _context.PlannedEvents
                    .Where(p =>
                        (p.PEStatus == "ongoing" && p.IsHold == false || p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                        && !violatingPENumbers.Contains(p.PeNumber))
                    .AsNoTracking();

                if (canViewAll)
                {
                    // If admin, filter by selected workgroup if provided
                    if (workgroupId.HasValue)
                    {
                        var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                        if (workgroup != null)
                        {


                            query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(workgroup.Name));

                            ViewData["FilteredWorkgroup"] = workgroup.Name;
                            ViewData["SelectedWorkgroupId"] = workgroupId;
                        }

                    }
                }
                else
                {
                    // Regular user: show all records for ALL their workgroups
                    if (userWorkgroupNames.Any())
                    {
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                (userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                                || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"))
                            );
                        }
                        else
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;

                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                ViewData["CanSendUrgentRequests"] = currentUser?.UserRole?.HasPermission("CanSendPEUrgentRequests") == true;

                var records = await query.ToListAsync();
                return View(records);
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
            workgroupId = workgroupId ?? userWorkgroupIds.FirstOrDefault();

            var currentUser = await _context.Users
                   .Include(u => u.UserRole)
                   .ThenInclude(r => r.RolePermissions)
                   .ThenInclude(rp => rp.Permission)
                   .Include(u => u.UserWorkGroups)
                   .ThenInclude(uwg => uwg.WorkGroup)
                   .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            try
            {
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                // Get PE numbers with OLA violation
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                // Get the PE records with at least one OLA-violated task
                var query = _context.PlannedEvents
                    .Where(p => violatingPENumbers.Contains(p.PeNumber));

                // --- UPDATED WORKGROUP FILTERING ---
                if (canViewAll)
                {
                    // If workgroupId is provided, filter by that workgroup
                    // if (workgroupId.HasValue)
                    // {
                    //     var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    //     if (workgroup != null)
                    //     {
                    //         if (hasDrawFiberAccess)
                    //         {
                    //             query = query.Where(p => p.TaskWg != null && (
                    //                 p.TaskWg.Contains(workgroup.Name) ||
                    //                 (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                    //             ));
                    //         }
                    //         else
                    //         {
                    //             query = query.Where(p => p.TaskWg != null &&
                    //                 p.TaskWg.Contains(workgroup.Name));
                    //         }

                    //         ViewData["FilteredWorkgroup"] = workgroup.Name;
                    //         ViewData["SelectedWorkgroupId"] = workgroupId;
                    //     }
                    // }
                }
                else
                {
                    // Non-ViewAll users: Filter by their assigned workgroups
                    if (userWorkgroupNames.Any())
                    {
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                (userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                                || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"))
                            );
                        }
                        else
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                }
                var olaViolateRecords = await query.OrderBy(p => p.PeNumber).ToListAsync();

                // For details, get all violating tasks for these PEs
                var violatingTasks = await _context.PETasks
                    .Where(t => t.IsOLAViolate && violatingPENumbers.Contains(t.PENumber))
                    .ToListAsync();

                var currentDate = DateTime.Today;
                var violationDetails = violatingTasks
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

                _logger.LogInformation("Retrieved {count} OLA violated records", olaViolateRecords.Count);
                return View(olaViolateRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OLAViolateRecords");
                TempData["ErrorMessage"] = "An error occurred while loading records.";
                return View(new List<PlannedEvent>());
            }
        }
        public async Task<IActionResult> HoldRecords(string peNumber, string reference, string customer, int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            // Get current user's workgroup info
            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // Start with base query that explicitly filters for IsHold = true
            var query = _context.PlannedEvents.Where(p => p.IsHold == true);

            // Apply workgroup filtering
            if (canViewAll)
            {
                if (workgroupId.HasValue)
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                    ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    ViewData["SelectedWorkgroupId"] = workgroupId;
                }
            }
            else
            {
                if (userWorkgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p => p.TaskWg != null &&
                           (userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                           || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"))
                       );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                    ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                    ViewData["SelectedWorkgroupId"] = workgroupId;
                }
            }
            // Apply search filters
            if (!string.IsNullOrEmpty(peNumber))
            {
                query = query.Where(p => p.PeNumber.Contains(peNumber));
                ViewData["SearchPeNumber"] = peNumber;
            }

            if (!string.IsNullOrEmpty(reference))
            {
                query = query.Where(p => p.RequestReferenceNo != null &&
                                       p.RequestReferenceNo.Contains(reference));
                ViewData["SearchReference"] = reference;
            }

            if (!string.IsNullOrEmpty(customer))
            {
                query = query.Where(p => p.Customer != null &&
                                       p.Customer.Contains(customer));
                ViewData["SearchCustomer"] = customer;
            }

            // Add logging to diagnose issues
            var holdRecords = await query.ToListAsync();
            _logger.LogInformation($"Found {holdRecords.Count} hold records");

            return View(holdRecords);
        }


        public async Task<IActionResult> UrgentRecords(int? workgroupId)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            var (userWorkgroupIds, userWorkgroupNames, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            try
            {
                // Get PE numbers with OLA violation
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                // Load workgroups for the dropdown
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                var query = _context.PlannedEvents
                    .Where(p => p.PEStatus == "urgent" && p.IsHold == false && !violatingPENumbers.Contains(p.PeNumber));

                if (canViewAll)
                {
                    if (workgroupId.HasValue)
                    {
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p =>
                                p.TaskWg != null && p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"
                            );
                        }
                        else
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;

                    }
                }
                else
                {
                    if (userWorkgroupNames.Any())
                    {
                        if (hasDrawFiberAccess)
                        {
                            query = query.Where(p => p.TaskWg != null &&
                               (userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                               || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"))
                           );
                        }
                        else
                        {
                            query = query.Where(p => p.TaskWg != null &&
                                userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                        }
                        ViewData["FilteredWorkgroup"] = string.Join(", ", userWorkgroupNames);
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                    }
                }


                var urgentRecords = await query.ToListAsync();

                _logger.LogInformation("Total URGENT records found: {Count} (Workgroup filter: {workgroup})",
                    urgentRecords.Count, workgroupId.HasValue ? workgroupId.Value.ToString() : "None");

                return View(urgentRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading urgent records with workgroup filter {workgroupId}", workgroupId);
                TempData["ErrorMessage"] = "An error occurred while loading records.";
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

            var plannedEvent = await _context.PlannedEvents
                .FirstOrDefaultAsync(p => p.Id == id && p.PEStatus == "PENDING_URGENT_CONFIRMATION");

            if (plannedEvent == null)
            {
                return NotFound();
            }

            return View(plannedEvent);
        }
        // GET: PETasks/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _context.PlannedEvents
                .Where(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                .OrderBy(p => p.PeNumber)
                .ToListAsync();

            _logger.LogInformation("Retrieved {count} pending urgent PE requests", pendingRequests.Count);
            return View(pendingRequests);
        }

        // POST: PlannedEvents/ProcessUrgentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUrgentRequest(int id, string urgentReason)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
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

            // Update and save the PE first
            _context.Update(plannedEvent);
            await _context.SaveChangesAsync();

            // Log the PE update
            _logger.LogInformation("PE {id} priority set to: '{priority}' with level {level}",
                id, plannedEvent.Priority, priorityLevel);

            // If PE was marked as urgent, update all its tasks to be urgent as well
            if (markAsUrgent)
            {
                try
                {
                    // Get all tasks for this PE
                    var peNumber = plannedEvent.PeNumber;
                    _logger.LogInformation("Updating tasks for PE: {peNumber}", peNumber);

                    // Use a separate query with AsNoTracking to avoid tracking conflicts
                    var taskIds = await _context.PETasks
                        .AsNoTracking()
                        .Where(t => t.PENumber == peNumber)
                        .Select(t => t.Id)
                        .ToListAsync();

                    _logger.LogInformation("Found {count} tasks to update", taskIds.Count);

                    // Process each task individually to ensure proper updates
                    foreach (var taskId in taskIds)
                    {
                        // Get a fresh instance of the task
                        var task = await _context.PETasks.FindAsync(taskId);
                        if (task != null)
                        {
                            task.IsUrgent = true;
                            task.UrgentMarkedDate = DateTime.Now;
                            task.UrgentRequested = false;
                            task.Priority = priorityMessage + " (Inherited from PE)";

                            // Explicitly mark as modified and save immediately
                            _context.Entry(task).State = EntityState.Modified;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Updated task {id} with priority: {priority}",
                                task.Id, task.Priority);
                        }
                    }

                    // Verify the update by checking one task
                    var verifyTask = await _context.PETasks
                        .FirstOrDefaultAsync(t => t.PENumber == peNumber);

                    if (verifyTask != null)
                    {
                        _logger.LogInformation("Verification - Task {id} has priority: {priority}",
                            verifyTask.Id, verifyTask.Priority);
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

        [HttpGet]
        public async Task<IActionResult> RequestUrgent(int id)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
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
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent == null || plannedEvent.PEStatus?.ToUpper() == "COMPLETED" || plannedEvent.PEStatus?.ToUpper() == "URGENT")
            {
                return NotFound();
            }
            plannedEvent.PECreatedDate = DateTime.Now; // Set the current date/time

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.ServiceId == User.Identity.Name);
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

            _context.Update(plannedEvent);
            await _context.SaveChangesAsync();

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
                var plannedEvent = await _context.PlannedEvents.FindAsync(id);

                if (plannedEvent == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    return RedirectToAction(nameof(OLAViolateRecords));
                }

                var currentDate = DateTime.Today;

                // Find the violating tasks for this PE
                var violatingTasks = await _context.PETasks
                    .Where(t => t.PENumber == plannedEvent.PeNumber &&
                              t.TaskStatus != "COMPLETED" &&
                              t.TaskCompleteDate.Date < currentDate)
                    .OrderBy(t => t.TaskCompleteDate)  // Start with the most overdue
                    .ToListAsync();

                if (violatingTasks.Any())
                {
                    // Mark the first/most overdue violating task as urgent
                    var mostOverdueTask = violatingTasks.First();
                    mostOverdueTask.IsUrgent = true;
                    mostOverdueTask.Priority = (mostOverdueTask.Priority ?? "") + " [URGENT: OLA VIOLATED]";
                    _context.Update(mostOverdueTask);

                    // Update PE status to urgent
                    plannedEvent.PEStatus = "urgent";
                    _context.Update(plannedEvent);

                    await _context.SaveChangesAsync();

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
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
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
                urgentRequestReason = ExtractUrgentRequestReason(plannedEvent.Priority)
            };

            return Json(details);
        }

        private string ExtractUrgentRequestReason(string priority)
        {
            if (string.IsNullOrEmpty(priority))
                return null;

            if (priority.Contains("Opening Ceremony"))
                return "Opening Ceremony - Priority 1";

            if (priority.Contains("Critical Customer"))
                return "Critical Customer - Priority 2";

            return null;
        }
        private async Task<(List<int> workgroupIds, List<string> workgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync()
        {
            int currentUserId = await GetCurrentUserIdAsync();

            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
                return (new List<int>(), new List<string>(), false);

            var workgroupIds = currentUser.UserWorkGroups?
                .Select(uwg => uwg.WorkGroupId)
                .ToList() ?? new List<int>();

            var workgroupNames = currentUser.UserWorkGroups?
                .Select(uwg => uwg.WorkGroup.Name)
                .ToList() ?? new List<string>();

            // Use the "ViewAll" permission instead of workgroup name
            bool canViewAll = currentUser.UserRole?.HasPermission("ViewAll") == true;

            var availableWorkgroups = canViewAll
                ? await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync()
                : await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .OrderBy(w => w.Name)
                    .ToListAsync();

            ViewData["UserWorkGroups"] = availableWorkgroups;
            ViewData["CanViewAll"] = canViewAll;

            return (workgroupIds, workgroupNames, canViewAll);
        }

        // Helper method to get the user's primary workgroup ID
        private async Task<(int workgroupId, bool canViewAll)> GetCurrentUserWorkGroupAsync()
        {
            var (workgroupIds, _, canViewAll) = await GetCurrentUserWorkGroupsAsync();
            return (workgroupIds.FirstOrDefault(), canViewAll);
        }


        public async Task<IActionResult> GlobalSearch(string searchType, string peNumber, string customer, string jobReference, string soNumber, int pageIndex = 1)
        {

            // Trim all search parameters to remove leading/trailing spaces
            peNumber = peNumber?.Trim();
            customer = customer?.Trim();
            jobReference = jobReference?.Trim();
            soNumber = soNumber?.Trim();

            var query = _context.PlannedEvents.AsQueryable(); // No workgroup restriction

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
                                EF.Functions.Like(p.JobReference, $"%{jobReference}%"));
                        break;
                    case "soNumber":
                        if (!string.IsNullOrEmpty(soNumber))
                            query = query.Where(p => p.SoNumber != null &&
                                EF.Functions.Like(p.SoNumber, $"%{soNumber}%"));
                        break;
                    default: // peNumber
                        if (!string.IsNullOrEmpty(peNumber))
                            query = query.Where(p => p.PeNumber != null &&
                                EF.Functions.Like(p.PeNumber, $"%{peNumber}%"));
                        break;
                }
            }

            int pageSize = 20;
            var result = await PaginatedList<PlannedEvent>.CreateAsync(query.OrderByDescending(x => x.PeNumber), pageIndex, pageSize);

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
            var peNumbers = result.Select(pe => pe.PeNumber).ToList();
            var allTasks = await _context.PETasks
                .Where(t => peNumbers.Contains(t.PENumber))
                .OrderBy(t => t.TaskSeq)
                .ToListAsync();
            var peTasksByPeNumber = allTasks
                .GroupBy(t => t.PENumber)
                .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
            ViewBag.PETasksByPeNumber = peTasksByPeNumber;
            // --- End PETasksByPeNumber block ---


            // --- Provide PEReportedIssuesByPeId for the view ---
            var peIds = result.Select(pe => pe.Id).ToList();
            var allIssues = await _context.PEIssues
        .Where(i => peIds.Contains(i.PlannedEventId))
        .OrderByDescending(i => i.CreatedAt)
        .Select(i => new PEIssueViewModel
        {
            Id = i.Id, // Make sure to include this
            SenderId = i.SenderId,
            SenderName = _context.Users.Where(u => u.Id == i.SenderId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Sender",
            ReceiverId = i.ReceiverId,
            ReceiverName = _context.Users.Where(u => u.Id == i.ReceiverId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Receiver",
            IssueText = i.IssueText,
            AttachmentPath = i.AttachmentPath ?? string.Empty,
            CreatedAt = i.CreatedAt,
            PlannedEventId = i.PlannedEventId,
            IsResolved = i.IsResolved,  // Add this
            IsHiddenFromInbox = i.IsHiddenFromInbox  // Add this
        })
        .ToListAsync();

            var issuesByPlannedEventId = allIssues.GroupBy(i => i.PlannedEventId)
                .ToDictionary(g => g.Key, g => g.ToList());  // Make sure we're using ToList() here
            ViewBag.IssuesByPlannedEventId = issuesByPlannedEventId;

            ViewData["SearchType"] = searchType;
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            // Additional logic for customer search - to populate the table and summary counts
            if (searchType == "customer" && !string.IsNullOrEmpty(customer))
            {
                // Get all PEs for this customer
                var customerPEs = await _context.PlannedEvents
                    .Where(p => p.Customer != null && p.Customer.ToLower().Contains(customer.ToLower()))
                    .ToListAsync();

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
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            // Get user's sales workgroup
            var (salesWorkgroups, canViewAll) = await GetUserSalesWorkgroups();
            if (!salesWorkgroups.Any())
            {
                return RedirectToAction(nameof(InProgressRecords));
            }

            // Get PE numbers with OLA violation
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();


            var query = _context.PlannedEvents.AsQueryable();

            if (!canViewAll)
            {
                query = query.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
            }

            query = query.Where(p =>
                (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                !p.IsHold &&
                !violatingPENumbers.Contains(p.PeNumber));

            var records = await query
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToListAsync();

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

            var query = _context.PlannedEvents.Where(p => p.IsHold);

            // Apply workgroup filtering only if user doesn't have ViewAll permission
            if (!canViewAll)
            {
                query = query.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
            }

            var records = await query
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToListAsync();

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

            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            var query = _context.PlannedEvents
                .Where(p => p.PEStatus == "urgent" &&
                       !p.IsHold &&
                       !violatingPENumbers.Contains(p.PeNumber));

            // Apply workgroup filtering only if user doesn't have ViewAll permission
            if (!canViewAll)
            {
                query = query.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
            }

            var records = await query
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToListAsync();

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

            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            var query = _context.PlannedEvents
                .Where(p => violatingPENumbers.Contains(p.PeNumber));

            if (!canViewAll)
            {
                query = query.Where(p => salesWorkgroups.Any(wg =>
                    (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                    (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                ));
            }

            var records = await query
                .OrderByDescending(p => p.ServiceRequiredDate)
                .ToListAsync();

            // Get violation details
            var violatingTasks = await _context.PETasks
                .Where(t => t.IsOLAViolate &&
                       records.Select(p => p.PeNumber).Contains(t.PENumber))
                .ToListAsync();

            var currentDate = DateTime.Today;
            var violationDetails = violatingTasks
                .GroupBy(t => t.PENumber)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TasksCount = g.Count(),
                        MaxDaysOverdue = g.Max(t =>
                            t.EstimatedTime.HasValue
                                ? (currentDate - t.EstimatedTime.Value).Days
                                : (t.ActualTaskCreatedDate.HasValue && t.OLA != null &&
                                   int.TryParse(t.OLA, out var olaDays))
                                    ? (currentDate - t.ActualTaskCreatedDate.Value.AddDays(olaDays)).Days
                                    : 0
                        ),
                        OldestViolation = g.Min(t =>
                            t.EstimatedTime ??
                            (t.ActualTaskCreatedDate.HasValue && t.OLA != null &&
                             int.TryParse(t.OLA, out var olaDays)
                                ? t.ActualTaskCreatedDate.Value.AddDays(olaDays)
                                : (DateTime?)null))
                    }
                );
            ViewBag.ViolationDetails = violationDetails;

            ViewData["CanViewAll"] = canViewAll;
            return View(records);
        }


        private async Task<int> GetUrgentCount(List<int> workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            _logger.LogInformation("Getting urgent count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check permissions
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool canViewAll = currentUser?.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // Get PE numbers with OLA violation
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            // Base query for urgent records
            var query = _context.PlannedEvents
                .Where(p => p.PEStatus == "urgent" &&
                           p.IsHold == false &&
                           !violatingPENumbers.Contains(p.PeNumber));

            // If user has ViewAll, no workgroup filtering needed
            if (canViewAll)
            {
                var countAll = await query.CountAsync();
                _logger.LogInformation("Urgent count (ViewAll): {count}", countAll);
                return countAll;
            }

            // For non-ViewAll users, get their workgroups
            if (workgroupIds == null || !workgroupIds.Any())
            {
                workgroupIds = currentUser?.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            if (workgroupIds.Any())
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        // Include both workgroup matches and Draw Fiber tasks
                        query = query.Where(p =>
                            p.TaskWg != null && (
                                workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                                (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        // Only include workgroup matches
                        query = query.Where(p =>
                            p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                        );
                    }
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("Urgent count: {count} for workgroups: {workgroups}",
                count, string.Join(", ", workgroupIds));

            return count;
        }


        private async Task<int> GetOLAViolateCount(List<int> workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting OLA violate count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check ViewAll permission
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool canViewAll = currentUser?.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            // Get PE numbers with OLA violation
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            // Base query for OLA violating records
            var query = _context.PlannedEvents
                .Where(p => violatingPENumbers.Contains(p.PeNumber));

            // If user has ViewAll, show count of all records
            if (canViewAll)
            {
                var countAll = await query.CountAsync();
                _logger.LogInformation("OLA violate count (ViewAll): {count}", countAll);
                return countAll;
            }

            // For non-ViewAll users, get their workgroups
            if (workgroupIds == null || !workgroupIds.Any())
            {
                workgroupIds = currentUser?.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            if (workgroupIds.Any())
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        // Include both workgroup matches and Draw Fiber tasks
                        query = query.Where(p =>
                            p.TaskWg != null && (
                                workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                                (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        // Only include workgroup matches
                        query = query.Where(p =>
                            p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                        );
                    }
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("OLA violate count: {count} for workgroups: {workgroups}",
                count, string.Join(", ", workgroupIds));

            return count;
        }


        private async Task<int> GetHoldCount(List<int> workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();

            _logger.LogInformation("Getting hold count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check permissions
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool canViewAll = currentUser?.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            _logger.LogInformation("Getting hold count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Use IsHold flag 
            var query = _context.PlannedEvents.Where(p => p.IsHold == true);

            // If user has ViewAll, show count of all records
            if (canViewAll)
            {
                var countAll = await query.CountAsync();
                _logger.LogInformation("Hold count (ViewAll): {count}", countAll);
                return countAll;
            }

            // For non-ViewAll users, get their workgroups
            if (workgroupIds == null || !workgroupIds.Any())
            {
                workgroupIds = currentUser?.UserWorkGroups?
                    .Select(uwg => uwg.WorkGroupId)
                    .ToList() ?? new List<int>();
            }

            if (workgroupIds.Any())
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        // Include both workgroup matches and Draw Fiber tasks
                        query = query.Where(p =>
                            p.TaskWg != null && (
                                workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                                (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        // Only include workgroup matches
                        query = query.Where(p =>
                            p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                        );
                    }
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("Hold count: {count} for workgroups: {workgroups}",
                count, string.Join(", ", workgroupIds));

            return count;
        }




        private async Task<int> GetInProgressCount(List<int> workgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            _logger.LogInformation("Getting in-progress count for workgroup: {workgroupId}",
                workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            // Get current user and check ViewAll permission
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool canViewAll = currentUser?.UserRole?.HasPermission("ViewAll") == true;
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            // Get PE numbers with OLA violation
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            var query = _context.PlannedEvents
                .Where(p =>
                    ((p.PEStatus == "ongoing" && p.IsHold == false) || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                    !violatingPENumbers.Contains(p.PeNumber)
                );

            // If user has ViewAll, show count of all records (ignore workgroupIds)
            if (canViewAll)
            {
                var countAll = await query.CountAsync();
                _logger.LogInformation("In-progress count (ViewAll): {count}", countAll);
                return countAll;
            }

            // Otherwise, restrict by workgroupIds
            if (workgroupIds == null || !workgroupIds.Any())
            {
                // Restrict to user's assigned workgroups if not ViewAll
                workgroupIds = currentUser?.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>();
            }

            if (workgroupIds.Any())
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            (workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                             || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber"))
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("In-progress count: {count} for workgroup: {workgroupId}",
                count, workgroupIds != null && workgroupIds.Any() ? string.Join(", ", workgroupIds) : "ALL");

            return count;
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
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
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

        private async Task<(List<string> workgroups, bool canViewAll)> GetUserSalesWorkgroups()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return (new List<string>(), false);

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return (new List<string>(), false);

            var user = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(email));

            bool canViewAll = user?.UserRole?.HasPermission("ViewAll") == true;

            // Get all workgroups
            var workgroups = user?.UserWorkGroups
                ?.Select(uwg => uwg.WorkGroup.Name)
                .ToList() ?? new List<string>();

            return (workgroups, canViewAll);
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkgroups(string search, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.WorkGroups.AsQueryable();

            // Get current user's workgroup permissions
            var (userWorkgroupIds, _, canViewAll) = await GetCurrentUserWorkGroupsAsync();

            // Filter based on permissions
            if (!canViewAll)
            {
                query = query.Where(w => userWorkgroupIds.Contains(w.Id));
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(w => w.Name.ToLower().Contains(search));
            }

            // Get total count for pagination
            var total = await query.CountAsync();

            // Get paginated results
            var items = await query
                .OrderBy(w => w.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new { id = w.Id, name = w.Name })
                .ToListAsync();

            return Json(new
            {
                items = items,
                hasMore = (page * pageSize) < total
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSelectedWorkgroups([FromQuery] List<int> ids)
        {
            var workgroups = await _context.WorkGroups
                .Where(w => ids.Contains(w.Id))
                .Select(w => new { id = w.Id, name = w.Name })
                .ToListAsync();

            return Json(workgroups);
        }

        private async Task<bool> HasDrawFiberAccessAsync(int userId)
        {
            if (userId == 0) return false;

            var userWorkgroups = await _context.UserWorkGroups
                .Include(uwg => uwg.WorkGroup)
                .Where(uwg => uwg.SystemUserId == userId)
                .Select(uwg => uwg.WorkGroup.Name)
                .ToListAsync();

            return userWorkgroups.Any(wg => wg == "NET-PROJ-ACC-CABLE");
        }





        // Add this action method
        [HttpGet]
        public async Task<IActionResult> TaskQueue(int? workgroupId, int take = 20)
        {
            var (userWorkgroupId, canViewAll) = await GetCurrentUserWorkGroupAsync();
            var effectiveWorkgroupId = canViewAll ? workgroupId : userWorkgroupId;

            try
            {
                // Load workgroups for the dropdown
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = effectiveWorkgroupId;
                ViewData["CanSwitchWorkgroup"] = canViewAll;

                // Get prioritized tasks from the queue service
                var prioritizedTasks = await _taskQueueService.GetPrioritizedTasksAsync(effectiveWorkgroupId, take);

                // Get statistics for the summary boxes
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
                _logger.LogError(ex, "Error loading task queue with workgroup filter {workgroupId}", workgroupId);
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
                var plannedEvent = await _context.PlannedEvents.FindAsync(id);

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

                    _context.Update(plannedEvent);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("PE {peNumber} urgent status removed, returned to normal records",
                        plannedEvent.PeNumber);

                    // Update related tasks to remove urgent status
                    var peNumber = plannedEvent.PeNumber;
                    var relatedTasks = await _context.PETasks
                        .Where(t => t.PENumber == peNumber)
                        .ToListAsync();

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
                        }
                    }

                    if (relatedTasks.Any())
                    {
                        _context.UpdateRange(relatedTasks);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Removed urgent status from {count} tasks for PE {peNumber}",
                            relatedTasks.Count, peNumber);
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
                var hasNextTask = nextTask != null && nextTask.Any();

                if (!hasNextTask)
                {
                    return PartialView("_NextTaskEmpty");
                }

                return PartialView("_NextTaskItem", nextTask[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing next task");
                return Json(new { success = false, message = "Error refreshing task queue." });
            }
        }
    }
}