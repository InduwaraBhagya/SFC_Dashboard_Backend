using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SFCDashboard.Controllers
{
    public class PlannedEventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlannedEventsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PlannedEventsController(ApplicationDbContext context, ILogger<PlannedEventsController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

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

        // GET: PlannedEvents/Index
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, int? workgroupId, int pageIndex = 1)
        {
            // Get current user's workgroup info
            var (userWorkgroupId, canViewAll) = await GetCurrentUserWorkGroupAsync();
            
            // Base query
            var query = _context.PlannedEvents.AsQueryable();

            // Apply workgroup filtering
            if (canViewAll)
            {
                // If user has ALL-WORKGROUPS access and a specific workgroup is selected
                if (workgroupId.HasValue)
                {
                    var selectedWorkgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    if (selectedWorkgroup != null)
                    {
                        query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(selectedWorkgroup.Name));
                        ViewData["FilteredWorkgroup"] = selectedWorkgroup.Name;
                        ViewData["SelectedWorkgroupId"] = workgroupId;
                        _logger.LogInformation("ALL-WORKGROUPS user filtering by: {WorkgroupName}", selectedWorkgroup.Name);
                    }
                }
            }
            else
            {
                // Regular users can only see their own workgroup's records
                var workgroup = await _context.WorkGroups.FindAsync(userWorkgroupId);
                if (workgroup != null)
                {
                    query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(workgroup.Name));
                    ViewData["FilteredWorkgroup"] = workgroup.Name;
                    ViewData["SelectedWorkgroupId"] = userWorkgroupId;
                    _logger.LogInformation("Regular user viewing workgroup: {WorkgroupName}", workgroup.Name);
                }
            }

            // Calculate dashboard counts based on current workgroup context
            ViewData["UrgentCount"] = await GetUrgentCount(canViewAll ? workgroupId : userWorkgroupId);
            ViewData["InProgressCount"] = await GetInProgressCount(canViewAll ? workgroupId : userWorkgroupId);
            ViewData["OLAViolateCount"] = await GetOLAViolateCount(canViewAll ? workgroupId : userWorkgroupId);
            ViewData["HoldCount"] = await GetHoldCount(canViewAll ? workgroupId : userWorkgroupId);

            // Load workgroups for dropdown if user has ALL-WORKGROUPS access
            if (canViewAll)
            {
                var workgroups = await _context.WorkGroups
                    .Where(w => w.Name != "ALL-WORKGROUPS")
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

            

    

            var currentDate = DateTime.Today;
            var olaViolationQuery = _context.PETasks
                .Where(t => t.TaskStatus != "COMPLETED" && t.TaskCompleteDate.Date < currentDate);

            if (workgroupId.HasValue)
            {
                olaViolationQuery = olaViolationQuery.Where(t => t.TaskWorkGroup != null &&
                    _context.WorkGroups.Any(w => w.Id == workgroupId && t.TaskWorkGroup.Contains(w.Name)));
            }

            // Count unique PE Numbers (i.e., unique PEs with at least one violating task)
            ViewData["OLAViolateCount"] = await olaViolationQuery
                .Select(t => t.PENumber)
                .Distinct()
                .CountAsync();


            // Get top OLA violations with workgroup filter
            IOrderedQueryable<PETask> violationsQuery;

            if (workgroupId.HasValue)
            {
                // Apply both filter and ordering in one step
                violationsQuery = _context.PETasks
                    .Where(t => t.TaskStatus != "COMPLETED" && t.TaskCompleteDate.Date < currentDate)
                    .Where(t => t.TaskWorkGroup != null &&
                        _context.WorkGroups.Any(w => w.Id == workgroupId && t.TaskWorkGroup.Contains(w.Name)))
                    .OrderBy(t => t.TaskCompleteDate);
            }
            else
            {
                // No workgroup filter, just apply the basic filter and ordering
                violationsQuery = _context.PETasks
                    .Where(t => t.TaskStatus != "COMPLETED" && t.TaskCompleteDate.Date < currentDate)
                    .OrderBy(t => t.TaskCompleteDate);
            }

            var violationsData = await violationsQuery
                .Take(5)
                .Include(t => t.PlannedEvent)
                .ToListAsync();

            // Then transform it in memory
            var topViolations = violationsData
                .Select(t => new
                {
                    PENumber = t.PENumber,
                    TaskName = t.Task,
                    DueDate = t.TaskCompleteDate,
                    DaysOverdue = (currentDate - t.TaskCompleteDate.Date).Days,
                    PlannedEventId = t.PlannedEvent?.Id
                })
                .ToList();

            ViewData["TopOLAViolations"] = topViolations;

            //int currentUserId = int.Parse(User.FindFirst("UserId").Value); // Adjust as needed

            //var inboxIssues = _context.PEIssues
            //    .Where(i => i.ReceiverId == currentUserId)
            //    .OrderByDescending(i => i.CreatedAt)
            //    .ToList();

            //ViewData["InboxIssues"] = inboxIssues;



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

            // Get current user ID
            var currentUserId = await GetCurrentUserIdAsync();

            // Get latest issues for the inbox
            var inboxIssues = await _context.PEIssues
                .Where(i => i.ReceiverId == currentUserId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id, // Make sure to include this
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

            ViewData["InboxIssues"] = inboxIssues;
            ViewData["TotalMessages"] = inboxIssues.Count;
            ViewData["UnreadMessages"] = unreadCount;


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
        

                if (workgroupId.HasValue)
                {
                    query = query.Where(p => p.TaskWg != null &&
                        _context.WorkGroups.Any(w => w.Id == workgroupId && p.TaskWg.Contains(w.Name)));
                }

                query = query.OrderByDescending(p => p.PECreatedDate)
                            .ThenBy(p => p.PeNumber);

                int pageSize = 10;
                var paginatedList = await PaginatedList<PlannedEvent>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

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


        // GET: PlannedEvents/Details/5
        public async Task<IActionResult> Details(int id, string returnUrl = null)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Get related PE tasks for this event
            var peTasks = await _context.PETasks
                .Where(t => t.PENumber == plannedEvent.PeNumber)
                .OrderBy(t => t.TaskSeq)
                .ToListAsync();
            ViewBag.PETasks = peTasks;

            // Find PETaskListId for the current task name
            var taskList = await _context.PETaskLists
                .FirstOrDefaultAsync(tl => tl.Name == plannedEvent.TaskName);
            ViewBag.CurrentTaskListId = taskList?.Id;

            // Load issues/subtasks for this PE (assuming you use PEIssue or Subtask table)
            var issues = await _context.PEIssues
    .Where(i => i.PlannedEventId == plannedEvent.Id)
    .OrderByDescending(i => i.CreatedAt)
    .Select(i => new PEIssueViewModel
    {
        Id = i.Id, // Make sure to include this
        SenderId = i.SenderId,
        SenderName = _context.Users.Where(u => u.Id == i.SenderId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Sender",
        ReceiverId = i.ReceiverId,
        ReceiverName = _context.Users.Where(u => u.Id == i.ReceiverId).Select(u => u.Name).FirstOrDefault() ?? "Unknown Receiver",
        IssueText = i.IssueText,
        AttachmentPath = i.AttachmentPath,
        CreatedAt = i.CreatedAt,
        PlannedEventId = i.PlannedEventId
    })
    .ToListAsync();

            ViewBag.PEReportedIssues = issues;

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
            var (userWorkgroupId, canViewAll) = await GetCurrentUserWorkGroupAsync();
            var effectiveWorkgroupId = canViewAll ? workgroupId : userWorkgroupId;

            try
            {
                // Base query
                var query = _context.PlannedEvents
                    .Where(p => p.PEStatus == "ongoing"&& p.IsHold == false||p.PEStatus=="PENDING_URGENT_CONFIRMATION")
                    .AsNoTracking();

                // Apply workgroup filter
                if (effectiveWorkgroupId.HasValue)
                {
                    var workgroup = await _context.WorkGroups.FindAsync(effectiveWorkgroupId);
                    if (workgroup != null)
                    {
                        query = query.Where(p => p.TaskWg != null && 
                            EF.Functions.Like(p.TaskWg, $"%{workgroup.Name}%"));
                        ViewData["FilteredWorkgroup"] = workgroup.Name;
                    }
                }

                // Set ViewData
                ViewData["CanViewAll"] = canViewAll;
                ViewData["SelectedWorkgroupId"] = effectiveWorkgroupId;

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
            var (userWorkgroupId, canViewAll) = await GetCurrentUserWorkGroupAsync();
            workgroupId = workgroupId ?? userWorkgroupId;

            try
            {
                // Load workgroups for the dropdown
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                var currentDate = DateTime.Today;

                // Get all PE records with OLA violations (tasks past their due date)
                var olaViolatingTasks = await _context.PETasks
                    .Where(t => t.TaskStatus != "COMPLETED" &&
                               t.TaskCompleteDate.Date < currentDate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                // Get the actual PE records
                var olaViolateRecords = await _context.PlannedEvents
                    .Where(p => olaViolatingTasks.Contains(p.PeNumber))
                    .OrderBy(p => p.PeNumber)
                    .ToListAsync();

                // Create a dictionary to store violation details - do this calculation in memory
                var violatingTasksList = await _context.PETasks
                    .Where(t => t.TaskStatus != "COMPLETED" &&
                               t.TaskCompleteDate.Date < currentDate)
                    .ToListAsync();

                // Group and calculate in memory instead of in the query
                var violationDetails = violatingTasksList
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            TasksCount = g.Count(),
                            MaxDaysOverdue = g.Max(t => (currentDate - t.TaskCompleteDate.Date).Days),
                            OldestViolation = g.OrderBy(t => t.TaskCompleteDate).FirstOrDefault()?.TaskCompleteDate
                        }
                    );

                ViewBag.ViolationDetails = violationDetails;

                // When getting violatingTasksList, filter by workgroup:
                if (workgroupId.HasValue)
                {
                    var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    if (workgroup != null)
                    {
                        violatingTasksList = violatingTasksList
                            .Where(t => t.TaskWorkGroup != null && t.TaskWorkGroup.Contains(workgroup.Name))
                            .ToList();
                    }
                }

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

        public async Task<IActionResult> HoldRecords(int? workgroupId)
        {
            var (userWorkgroupId, canViewAll) = await GetCurrentUserWorkGroupAsync();
            workgroupId = workgroupId ?? userWorkgroupId;

            try
            {
                // Load workgroups for the dropdown
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = workgroupId;
                ViewData["CanViewAll"] = canViewAll;

                // Use IsHold instead of checking PEStatus
                var query = _context.PlannedEvents.Where(p => p.IsHold == true);

                // Apply workgroup filter if selected
                if (workgroupId.HasValue)
                {
                    var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    if (workgroup != null)
                    {
                        ViewData["FilteredWorkgroup"] = workgroup.Name;
                        query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(workgroup.Name));
                    }
                }

                var holdRecords = await query.ToListAsync();
                ViewData["HoldCount"] = holdRecords.Count;

                return View(holdRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading hold records with workgroup filter {workgroupId}", workgroupId);
                TempData["ErrorMessage"] = "An error occurred while loading records.";
                return View(new List<PlannedEvent>());
            }
        }

        public async Task<IActionResult> UrgentRecords(int? workgroupId)
        {
            try
            {
                // Load workgroups for the dropdown
                var workgroups = await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                ViewData["Workgroups"] = workgroups;
                ViewData["SelectedWorkgroupId"] = workgroupId;

                var query = _context.PlannedEvents.Where(p => p.PEStatus == "urgent"&&p.IsHold == false);

                // Apply workgroup filter if selected
                if (workgroupId.HasValue)
                {
                    var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    if (workgroup != null)
                    {
                        ViewData["FilteredWorkgroup"] = workgroup.Name;
                        query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(workgroup.Name));
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

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    markAsUrgent = true;
                    plannedEvent.PEStatus = "URGENT";
                    priorityMessage = " [URGENT: Opening Ceremony - Priority 1]";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + priorityMessage;
                    break;

                case "CriticalCustomer":
                    markAsUrgent = true;
                    plannedEvent.PEStatus = "URGENT";
                    priorityMessage = " [URGENT: Critical Customer - Priority 2]";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + priorityMessage;
                    break;

                case "Reject":
                    plannedEvent.PEStatus = "ongoing";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + " [Urgent Request Rejected]";
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid option selected.";
                    return RedirectToAction(nameof(UrgentRequestsList));
            }

            _context.Update(plannedEvent);
            await _context.SaveChangesAsync();

            // If PE was marked as urgent, update all its tasks to be urgent as well
            if (markAsUrgent)
            {
                var relatedTasks = await _context.PETasks
                    .Where(t => t.PENumber == plannedEvent.PeNumber)
                    .ToListAsync();

                foreach (var task in relatedTasks)
                {
                    task.IsUrgent = true;
                    task.UrgentRequested = false; // Clear any pending urgent requests
                    task.Priority = (task.Priority ?? "") + priorityMessage + " (Inherited from PE)";
                }

                if (relatedTasks.Any())
                {
                    _context.UpdateRange(relatedTasks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marked {count} tasks as urgent for PE {peNumber}",
                        relatedTasks.Count, plannedEvent.PeNumber);
                }
            }

            TempData["SuccessMessage"] = markAsUrgent
                ? "Planned Event marked as urgent. All related tasks have also been marked as urgent."
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

        private async Task<(int? workgroupId, bool canViewAll)> GetCurrentUserWorkGroupAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (null, false);

            // Extract the substring before the query (first 6 characters)
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .Include(u => u.WorkGroup)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);

            _logger.LogInformation("User {serviceId} workgroup: {workgroupId}",
                serviceIdShort, user?.WorkGroupId);

            bool canViewAll = user?.WorkGroup?.Name == "ALL-WORKGROUPS";
            return (user?.WorkGroupId, canViewAll);
        }

        private int GetCurrentUserId()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            var user = _context.Users.FirstOrDefault(u => u.ServiceId == serviceId);
            return user?.Id ?? 0;
        }

        public async Task<IActionResult> GlobalSearch(string searchType, string peNumber, string customer, string jobReference, string soNumber, int pageIndex = 1)
        {
            var query = _context.PlannedEvents.AsQueryable(); // No workgroup restriction

            if (!string.IsNullOrEmpty(searchType))
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
            }

            int pageSize = 20;
            var result = await PaginatedList<PlannedEvent>.CreateAsync(query.OrderByDescending(x => x.PECreatedDate), pageIndex, pageSize);

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
            PlannedEventId = i.PlannedEventId
        })
        .ToListAsync();

    var issueIds = allIssues.Select(i => i.OriginalIssueId ?? i.Id).Distinct().ToList();
    var resolutions = await _context.PEIssueResolutions
        .Where(r => issueIds.Contains(r.IssueId))
        .ToListAsync();

    ViewBag.ResolutionsByIssueId = resolutions.ToDictionary(r => r.IssueId);

            ViewData["SearchType"] = searchType;
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;



            return View(result);
        }

        private async Task<int> GetUrgentCount(int? workgroupId)
        {
            _logger.LogInformation("Getting urgent count for workgroup: {workgroupId}",
                workgroupId?.ToString() ?? "ALL");

            var query = _context.PlannedEvents.Where(p => p.PEStatus == "urgent"&&p.IsHold == false);

            if (workgroupId.HasValue)
            {
                var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                if (workgroup != null)
                {
                    query = query.Where(p => p.TaskWg != null &&
                        EF.Functions.Like(p.TaskWg, $"%{workgroup.Name}%"));
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("Urgent count: {count} for workgroup: {workgroupId}",
                count, workgroupId?.ToString() ?? "ALL");

            return count;
        }


        private async Task<int> GetOLAViolateCount(int? workgroupId)
        {
            _logger.LogInformation("Getting OLA violation count for workgroup: {workgroupId}",
                workgroupId?.ToString() ?? "ALL");

            var currentDate = DateTime.Today;
            var query = _context.PlannedEvents.Where(p =>
                p.ServiceRequiredDate.HasValue &&
                p.ServiceRequiredDate.Value < currentDate &&
                p.PEStatus != "completed");

            if (workgroupId.HasValue)
            {
                var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                if (workgroup != null)
                {
                    query = query.Where(p => p.TaskWg != null &&
                        EF.Functions.Like(p.TaskWg, $"%{workgroup.Name}%"));
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("OLA violation count: {count} for workgroup: {workgroupId}",
                count, workgroupId?.ToString() ?? "ALL");

            return count;
        }


        private async Task<int> GetHoldCount(int? workgroupId)
        {
            _logger.LogInformation("Getting hold count for workgroup: {workgroupId}",
                workgroupId?.ToString() ?? "ALL");

            // Use IsHold flag instead of just checking PEStatus
            var query = _context.PlannedEvents.Where(p => p.IsHold == true);

            if (workgroupId.HasValue)
            {
                var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                if (workgroup != null)
                {
                    query = query.Where(p => p.TaskWg != null &&
                        EF.Functions.Like(p.TaskWg, $"%{workgroup.Name}%"));
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("Hold count: {count} for workgroup: {workgroupId}",
                count, workgroupId?.ToString() ?? "ALL");

            return count;
        }


        private async Task<int> GetInProgressCount(int? workgroupId)
        {
            _logger.LogInformation("Getting in-progress count for workgroup: {workgroupId}",
                workgroupId?.ToString() ?? "ALL");

            var query = _context.PlannedEvents.Where(p => p.PEStatus == "ongoing"&&p.IsHold == false||p.PEStatus=="PENDING_URGENT_CONFIRMATION");

            if (workgroupId.HasValue)
            {
                var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                if (workgroup != null)
                {
                    query = query.Where(p => p.TaskWg != null &&
                        EF.Functions.Like(p.TaskWg, $"%{workgroup.Name}%"));
                }
            }

            var count = await query.CountAsync();
            _logger.LogInformation("In-progress count: {count} for workgroup: {workgroupId}",
                count, workgroupId?.ToString() ?? "ALL");

            return count;
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
    }
    
    
}

