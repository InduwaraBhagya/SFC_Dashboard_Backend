using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDB.Models;
using SFCDashboard.Services;

namespace SFCDB.Controllers
{
    public class PERecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PERecordsController> _logger;
        private readonly PERecordsApiService _apiService;

        public PERecordsController(
            ApplicationDbContext context,
            ILogger<PERecordsController> logger,
            PERecordsApiService apiService)
        {
            _context = context;
            _logger = logger;
            _apiService = apiService;
        }

        // GET: PERecords
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving all PE Records");
            return View(await _context.PERecords.ToListAsync());
        }

        // GET: PERecords/ImportExcel
        public async Task<IActionResult> ImportExcel()
        {
            try
            {
                // Get diagnostic info from API
                var stats = await _apiService.GetDatabaseStatsAsync();
                ViewBag.PERecordsCount = stats.PeRecordsCount;
                ViewBag.PlannedEventsCount = stats.PlannedEventsCount;
                ViewBag.PETasksCount = stats.PeTasksCount;
                ViewBag.TaskTemplatesCount = stats.TaskTemplatesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database statistics");
                // Set default values if API call fails
                ViewBag.PERecordsCount = 0;
                ViewBag.PlannedEventsCount = 0;
                ViewBag.PETasksCount = 0;
                ViewBag.TaskTemplatesCount = 0;
            }
            
            return View();
        }

        // POST: PERecords/ImportExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length <= 0)
            {
                TempData["Message"] = "Please select an Excel file to upload.";
                return RedirectToAction("ImportExcel");
            }

            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Message"] = "Please select a valid Excel file (.xlsx).";
                return RedirectToAction("ImportExcel");
            }

            try
            {
                _logger.LogInformation("Starting import via API for Excel file: {fileName}", excelFile.FileName);
                
                // Call the API service to import the Excel file
                var result = await _apiService.ImportPERecordsAsync(excelFile);
                
                if (result.Success)
                {
                    if (result.SyncCompleted)
                    {
                        TempData["Message"] = $"Successfully replaced all records with {result.RecordCount} new records from Excel and synced to all related tables. " +
                            $"PlannedEvents: {result.PlannedEventCount}, PETasks: {result.PeTaskCount}";
                    }
                    else
                    {
                        TempData["Message"] = result.Message;
                    }
                }
                else
                {
                    TempData["Message"] = $"Error: {result.Message}";
                }
                
                return RedirectToAction("ImportExcel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportExcel action");
                TempData["Message"] = $"Error: {ex.Message}";
                return RedirectToAction("ImportExcel");
            }
        }
        
    }
}