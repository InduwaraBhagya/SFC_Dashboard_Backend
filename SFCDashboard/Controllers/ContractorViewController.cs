using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SFCDashboard.Controllers
{
    public class ContractorViewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContractorViewController> _logger;

        public ContractorViewController(
            ApplicationDbContext context,
            ILogger<ContractorViewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string contractorName)
        {
            if (string.IsNullOrEmpty(contractorName))
            {
                // Use a default contractor name or redirect to a selection page
                return RedirectToAction("SelectContractor");
            }

            // Get summary info for dashboard
            // Get count of records by status
            var urgentCount = await _context.PlannedEvents
                .Where(pe => pe.ContractorName == contractorName &&
                        pe.PEStatus != null &&
                        pe.PEStatus.ToLower() == "urgent")
                .CountAsync();
                
            var onHoldCount = await _context.PlannedEvents
                .Where(pe => pe.ContractorName == contractorName &&
                        pe.IsHold == true)
                .CountAsync();
                
            var totalCount = await _context.PlannedEvents
                .Where(pe => pe.ContractorName == contractorName)
                .CountAsync();
                
            var ongoingCount = totalCount - urgentCount - onHoldCount;

            // Get most recent projects (top 5)
            var recentProjects = await _context.PlannedEvents
                .Where(pe => pe.ContractorName == contractorName)
                .OrderByDescending(pe => pe.ServiceRequiredDate)
                .Take(5)
                .ToListAsync();

            _logger.LogInformation($"Loading dashboard for contractor {contractorName}");
            
            ViewData["ContractorName"] = contractorName;
            ViewData["UrgentCount"] = urgentCount;
            ViewData["OnHoldCount"] = onHoldCount;
            ViewData["OngoingCount"] = ongoingCount;
            ViewData["TotalCount"] = totalCount;
            
            return View(recentProjects);
        }

        public async Task<IActionResult> SelectContractor()
        {
            // Get all distinct contractor names from PlannedEvents
            var contractors = await _context.PlannedEvents
                .Where(pe => !string.IsNullOrEmpty(pe.ContractorName))
                .Select(pe => pe.ContractorName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
                
            return View(contractors);
        }

        public async Task<IActionResult> ContractorRecords(string contractorName, string peNumber, string reference, string customer)
        {
            if (string.IsNullOrEmpty(contractorName))
            {
                return RedirectToAction("SelectContractor");
            }

            // Store search parameters in ViewData for maintaining state
            ViewData["SearchPeNumber"] = peNumber;
            ViewData["SearchReference"] = reference;
            ViewData["SearchCustomer"] = customer;
            ViewData["ContractorName"] = contractorName;

            // Get all PlannedEvents filtered by contractor name
            var query = _context.PlannedEvents.AsQueryable();
            
            // Apply ContractorName filter - using exact match instead of Contains
            query = query.Where(pe => pe.ContractorName == contractorName);
            
            // Apply additional filters if provided
            if (!string.IsNullOrEmpty(peNumber))
            {
                query = query.Where(pe => pe.PeNumber != null && pe.PeNumber.Contains(peNumber));
            }
            
            if (!string.IsNullOrEmpty(reference))
            {
                query = query.Where(pe => pe.JobReference != null && pe.JobReference.Contains(reference));
            }
            
            if (!string.IsNullOrEmpty(customer))
            {
                query = query.Where(pe => pe.Customer != null && pe.Customer.Contains(customer));
            }
            
            // Order by service required date
            var events = await query
                .OrderByDescending(pe => pe.ServiceRequiredDate)
                .ToListAsync();

            _logger.LogInformation($"Found {events.Count} events for contractor {contractorName}");
            
            return View(events);
        }

        public async Task<IActionResult> Details(string peNumber, string contractorName, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(peNumber))
            {
                return BadRequest("PE Number is required");
            }

            var plannedEvent = await _context.PlannedEvents
                .FirstOrDefaultAsync(pe => pe.PeNumber == peNumber);
            
            if (plannedEvent == null)
            {
                return NotFound($"Planned Event with PE Number {peNumber} not found");
            }
            
            // Verify this event belongs to the contractor (exact match)
            if (plannedEvent.ContractorName != contractorName)
            {
                return Forbid();
            }

            // Get related PE tasks for this event
            var peTasks = await _context.PETasks
                .Where(t => t.PENumber == plannedEvent.PeNumber)
                .OrderBy(t => t.TaskSeq)
                .ToListAsync();
            
            ViewBag.PETasks = peTasks;
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("ContractorRecords", new { contractorName });
            ViewBag.ContractorName = contractorName;
            
            return View(plannedEvent);
        }

        // Helper method to get survey tasks for a PE
        public async Task<IActionResult> SurveyTaskDetails(string peNumber, string contractorName)
        {
            if (string.IsNullOrEmpty(peNumber))
            {
                return BadRequest("PE Number is required");
            }

            // Verify the PE belongs to this contractor
            var plannedEvent = await _context.PlannedEvents
                .FirstOrDefaultAsync(pe => pe.PeNumber == peNumber && pe.ContractorName == contractorName);
                
            if (plannedEvent == null)
            {
                return NotFound("Planned Event not found or does not belong to this contractor");
            }

            // Check if this PE has survey tasks
            var hasSurveyTask = await _context.PETasks
                .AnyAsync(t => t.PENumber == peNumber && t.Task == "SURVEY FIBER ROUTE");
                
            if (!hasSurveyTask)
            {
                TempData["Error"] = "No survey tasks found for this PE";
                return RedirectToAction("Details", new { peNumber, contractorName });
            }

            // Redirect to the SurveyTasks controller using PE number
            return RedirectToAction("Details", "SurveyTasks", new { peNumber });
        }

        // Helper method to check if a PE has survey tasks
        public async Task<JsonResult> HasSurveyTasks(string peNumber)
        {
            if (string.IsNullOrEmpty(peNumber))
            {
                return Json(new { hasSurveyTasks = false });
            }

            var hasSurveyTask = await _context.PETasks
                .AnyAsync(t => t.PENumber == peNumber && t.Task == "SURVEY FIBER ROUTE");
                
            return Json(new { hasSurveyTasks = hasSurveyTask });
        }
    }
}