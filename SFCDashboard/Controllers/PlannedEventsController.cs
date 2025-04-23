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
            var today = DateTime.Today;
            ViewData["InProgressCount"] = await _context.PlannedEvents
                .Where(p => p.PEStatus == "ongoing")
                .CountAsync();

            //ViewData["OLAViolateCount"] = await _context.PlannedEvents
            //    .Where(p => p.PECreatedDate.HasValue && p.PECreatedDate.Value.AddDays(30) < today)
            //    .CountAsync();

            ViewData["UrgentCount"] = await _context.PlannedEvents
                .Where(p => p.PEStatus == "URGENT")
                .CountAsync();

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

        //public async Task<IActionResult> OLAViolateRecords()
        //{
        //    var today = DateTime.Today;
        //    var OLAViolateRecords = await _context.PlannedEvents
        //        .Where(p => p.PECreatedDate.HasValue && p.PECreatedDate.Value.AddDays(30) < today)
        //        .ToListAsync();

        //    _logger.LogInformation($"Total OLA Violate records found: {OLAViolateRecords.Count}");

        //    return View(OLAViolateRecords);
        //}

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

        // POST: PlannedEvents/RequestUrgent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestUrgent(int id)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent == null || plannedEvent.PEStatus != "ongoing")
            {
                return NotFound();
            }

            plannedEvent.PEStatus = "PENDING_URGENT_CONFIRMATION";
            _context.Update(plannedEvent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Urgent request submitted for approval.";
            return RedirectToAction(nameof(InProgressRecords));
        }

        // GET: PlannedEvents/UrgentRequestsList
        public async Task<IActionResult> UrgentRequestsList()
        {
            var pendingRequests = await _context.PlannedEvents
                .Where(r => r.PEStatus == "PENDING_URGENT_CONFIRMATION")
                .ToListAsync();

            return View(pendingRequests);
        }

        // POST: PlannedEvents/ProcessUrgentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUrgentRequest(int id, string urgentReason)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);
            if (plannedEvent == null || plannedEvent.PEStatus != "PENDING_URGENT_CONFIRMATION")
            {
                return NotFound();
            }

            switch (urgentReason)
            {
                case "OpeningCeremony":
                    plannedEvent.PEStatus = "URGENT";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + " [URGENT: Opening Ceremony - Priority 1]";
                    break;

                case "CriticalCustomer":
                    plannedEvent.PEStatus = "URGENT";
                    plannedEvent.Priority = (plannedEvent.Priority ?? "") + " [URGENT: Critical Customer - Priority 2]";
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

            TempData["SuccessMessage"] = "Urgent request processed.";
            return RedirectToAction(nameof(UrgentRequestsList));
        }
    }
}
