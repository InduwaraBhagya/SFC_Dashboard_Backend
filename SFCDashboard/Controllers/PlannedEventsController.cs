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

        // GET: PlannedEvents/Index
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer, 
            string jobReference, string soNumber, int pageIndex = 1)
        {
            var query = from r in _context.PlannedEvents
                        select r;

            // Store current filters in ViewData
            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            // Determine the search string based on the search type
            string searchString = searchType switch
            {
                "customer" => customer,
                "jobReference" => jobReference,
                "soNumber" => soNumber,
                _ => peNumber
            };

            // Calculate counts for dashboard boxes
            ViewData["UrgentCount"] = await _context.PlannedEvents
                .Where(p => p.PEStatus == "urgent")
                .CountAsync();
                
            ViewData["InProgressCount"] = await _context.PlannedEvents
                .Where(p => p.PEStatus == "ongoing")
                .CountAsync();
                
            // Count OLA violations
            var currentDate = DateTime.Today;
            ViewData["OLAViolateCount"] = await _context.PETasks
                .Where(t => t.TaskStatus != "COMPLETED" && 
                           t.TaskCompleteDate.Date < currentDate)
                .CountAsync();
            
            // Get top OLA violations - first get data
            var violationsData = await _context.PETasks
                .Where(t => t.TaskStatus != "COMPLETED" && 
                           t.TaskCompleteDate.Date < currentDate)
                .OrderBy(t => t.TaskCompleteDate)
                .Take(5)
                .Include(t => t.PlannedEvent)
                .ToListAsync();
            
            // Then transform it in memory
            var topViolations = violationsData
                .Select(t => new {
                    PENumber = t.PENumber,
                    TaskName = t.Task,
                    DueDate = t.TaskCompleteDate,
                    DaysOverdue = (currentDate - t.TaskCompleteDate.Date).Days,
                    PlannedEventId = t.PlannedEvent?.Id
                })
                .ToList();
            
            ViewData["TopOLAViolations"] = topViolations;

            // Apply search filters based on type
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchType)
                {
                    case "customer":
                        query = query.Where(p => p.Customer != null && p.Customer.Contains(customer));
                        break;
                    case "jobReference":
                        query = query.Where(p => p.JobReference != null && p.JobReference.Contains(jobReference));
                        break;
                    case "soNumber":
                        query = query.Where(p => p.SoNumber != null && p.SoNumber.Contains(soNumber));
                        break;
                    default: // peNumber
                        query = query.Where(p => p.PeNumber != null && p.PeNumber.Contains(peNumber));
                        break;
                }
            }

            // Return empty list if no search criteria provided
            if (string.IsNullOrEmpty(peNumber) &&
                string.IsNullOrEmpty(customer) &&
                string.IsNullOrEmpty(jobReference) &&
                string.IsNullOrEmpty(soNumber))
            {
                return View(new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, 10));
            }

            // Order results
            query = query.OrderByDescending(p => p.PECreatedDate)
                        .ThenBy(p => p.PeNumber);

            int pageSize = 10;
            var paginatedList = await PaginatedList<PlannedEvent>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

            // Log the number of records found
            _logger.LogInformation($"Total records found: {paginatedList.Count}");

            // Pass search parameters back to the view for maintaining the search state
            ViewData["SearchType"] = searchType;
            ViewData["SearchString"] = searchString;

            // Get pending urgent requests for the message inbox
            var pendingUrgentRequests = await _context.PlannedEvents
                .Where(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                .OrderByDescending(p => p.PECreatedDate)
                .Take(10)
                .ToListAsync();
                
            ViewData["PendingUrgentRequests"] = pendingUrgentRequests;
            
            // Get pending task urgent requests as well
            var pendingTaskRequests = await _context.PETasks
                .Where(t => t.UrgentRequested && !t.IsUrgent)
                .Include(t => t.PlannedEvent)
                .OrderByDescending(t => t.TaskCreatedDate)
                .Take(5)
                .ToListAsync();
                
            ViewData["PendingTaskRequests"] = pendingTaskRequests;
            
            // Total message count for the badge - remove the +5 since we're removing the static messages
            ViewData["TotalMessages"] = pendingUrgentRequests.Count + pendingTaskRequests.Count;
            
            return View(paginatedList);
        }

// GET: PlannedEvents/Details/5
public async Task<IActionResult> Details(int? id)
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

    // Get related PE tasks for this event
    var peTasks = await _context.PETasks
        .Where(t => t.PENumber == plannedEvent.PeNumber)
        .OrderBy(t => t.TaskSeq)
        .ToListAsync();
        
    ViewBag.PETasks = peTasks;

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

        public async Task<IActionResult> InProgressRecords()
        {
            var inProgressRecords = await _context.PlannedEvents
                .Where(p => p.PEStatus == "ongoing")
                .ToListAsync();

            _logger.LogInformation("Total INPROGRESS records found: {Count}", inProgressRecords.Count);

            return View(inProgressRecords);
        }

        // GET: PlannedEvents/OLAViolateRecords
        public async Task<IActionResult> OLAViolateRecords()
        {
            try
            {
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
                        g => new {
                            TasksCount = g.Count(),
                            MaxDaysOverdue = g.Max(t => (currentDate - t.TaskCompleteDate.Date).Days),
                            OldestViolation = g.OrderBy(t => t.TaskCompleteDate).FirstOrDefault()?.TaskCompleteDate
                        }
                    );

                ViewBag.ViolationDetails = violationDetails;
                
                _logger.LogInformation("Retrieved {count} OLA violated records", olaViolateRecords.Count);
                
                return View(olaViolateRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OLA violated records");
                TempData["ErrorMessage"] = "An error occurred while retrieving OLA violated records.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult HoldRecords()
        {
            // Placeholder action for HoldRecords
            return View();
        }

        public async Task<IActionResult> UrgentRecords()
        {
            var urgentRecords = await _context.PlannedEvents
                .Where(p => p.PEStatus == "URGENT")
                .ToListAsync();

            _logger.LogInformation("Total URGENT records found: {Count}", urgentRecords.Count);

            return View(urgentRecords);
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
                    plannedEvent.PEStatus = "IN_PROGRESS";
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

    }
}
