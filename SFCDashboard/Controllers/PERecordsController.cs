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
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class PERecordsController : BaseController
    {
        private readonly ILogger<PERecordsController> _logger;
        private readonly IPERecordsApiClient _apiClient;
        public PERecordsController(
            ILogger<PERecordsController> logger,
            IPERecordsApiClient apiClient,
            IUsersApiClient usersApiClient)
            : base(usersApiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        // GET: PERecords
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving all PE Records via API client");
            var response = await _apiClient.GetPERecordsAsync();
            if (response.Success && response.Data != null)
            {
                return View(response.Data.Items);
            }
            TempData["Message"] = response.Message ?? "Failed to retrieve PE Records.";
            return View(new List<PERecord>()); // Return empty list on failure
        }

        // GET: PERecords/ImportExcel
        public async Task<IActionResult> ImportExcel()
        {
            try
            {
                // Get diagnostic info from API
                var statsResponse = await _apiClient.GetDatabaseStatsAsync();
                if (statsResponse.Success && statsResponse.Data != null)
                {
                    var stats = statsResponse.Data;
                    ViewBag.PERecordsCount = stats.PeRecordsCount;
                    ViewBag.PlannedEventsCount = stats.PlannedEventsCount;
                    ViewBag.PETasksCount = stats.PeTasksCount;
                    ViewBag.TaskTemplatesCount = stats.TaskTemplatesCount;
                }
                else
                {
                    ViewBag.PERecordsCount = 0;
                    ViewBag.PlannedEventsCount = 0;
                    ViewBag.PETasksCount = 0;
                    ViewBag.TaskTemplatesCount = 0;
                }
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
                
                // Call the API client to import the Excel file
                var result = await _apiClient.ImportPERecordsAsync(excelFile);
                
                if (result.Success)
                {
                    TempData["Message"] = result.Message;
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