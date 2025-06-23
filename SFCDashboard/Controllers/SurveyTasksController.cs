using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class SurveyTasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostEnvironment _env;
        private readonly ILogger<SurveyTasksController> _logger;

        public SurveyTasksController(
            ApplicationDbContext context,
            IHostEnvironment env,
            ILogger<SurveyTasksController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: SurveyTasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return Content("ID is null");
            }

            // First check if the task exists at all
            var taskExists = await _context.PETasks.AnyAsync(t => t.Id == id);
            if (!taskExists)
            {
                return Content($"Task with ID {id} does not exist in database");
            }

            // Then check if it matches the filter condition
            var taskMatchesFilter = await _context.PETasks.AnyAsync(t => t.Id == id && t.Task == "SURVEY FIBER ROUTE");
            if (!taskMatchesFilter)
            {
                var actualTask = await _context.PETasks.FirstOrDefaultAsync(t => t.Id == id);
                return Content($"Task with ID {id} exists but Task value is '{actualTask?.Task ?? "null"}' instead of 'SURVEY FIBER ROUTE'");
            }

            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == id && t.Task == "SURVEY FIBER ROUTE");

            if (task == null)
            {
                return NotFound();
            }

            var activities = await _context.SurveyTaskActivities
                .Include(a => a.User)
                .Where(a => a.PETaskId == id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // Check for null values in the activities collection and fix them
            foreach (var activity in activities)
            {
                // Make sure User is not null
                if (activity.User == null)
                {
                    activity.User = new SystemUser { Name = "Unknown", ServiceId = "SYSTEM" };
                }
                
                // Handle null values for other properties
                activity.Description = activity.Description ?? string.Empty;
                activity.FileName = activity.FileName ?? string.Empty;
                activity.FilePath = activity.FilePath ?? string.Empty;
            }

            var viewModel = new SurveyTaskViewModel
            {
                Task = task,
                Activities = activities,
                NewActivity = new SurveyTaskActivityViewModel { TaskId = task.Id }
            };

            return View(viewModel);
        }

        // POST: SurveyTasks/AddActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddActivity(SurveyTaskActivityViewModel model)
        {
            // Clear any model errors related to File if it's null
            if (model.File == null && ModelState.ContainsKey("File"))
            {
                ModelState.Remove("File");
            }

            if (!ModelState.IsValid)
            {
                // Return to details view with validation errors
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId && t.Task == "SURVEY FIBER ROUTE");

                if (task == null)
                {
                    return NotFound();
                }

                var activities = await _context.SurveyTaskActivities
                    .Include(a => a.User)
                    .Where(a => a.PETaskId == model.TaskId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var viewModel = new SurveyTaskViewModel
                {
                    Task = task,
                    Activities = activities,
                    NewActivity = model
                };

                return View("Details", viewModel);
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                ModelState.AddModelError("", "User not authenticated or not found.");
                return RedirectToAction(nameof(Details), new { id = model.TaskId });
            }

            // Create the activity entity with non-null values
            var activity = new SurveyTaskActivity
            {
                PETaskId = model.TaskId,
                User = currentUser,
                Description = model.Description ?? string.Empty,  // Replace null with empty string
                CreatedAt = DateTime.Now,
                FileName = string.Empty,  // Initialize with empty string by default
                FilePath = string.Empty   // Initialize with empty string by default
            };

            // Only process file if one was provided
            if (model.File != null && model.File.Length > 0)
            {
                try
                {
                    // Create directory if it doesn't exist
                    var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "SurveyTasks");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Create a unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(fileStream);
                    }

                    // Update activity with file information
                    activity.FilePath = filePath;
                    activity.FileName = model.File.FileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file for survey task activity");
                    ModelState.AddModelError("", "Error uploading file. Please try again.");
                    return RedirectToAction(nameof(Details), new { id = model.TaskId });
                }
            }

            // Save the activity to database
            _context.SurveyTaskActivities.Add(activity);
            await _context.SaveChangesAsync();

            // Redirect back to the details page to show the newly added activity
            return RedirectToAction(nameof(Details), new { id = model.TaskId });
        }

        // GET: SurveyTasks/DownloadFile/5
        public async Task<IActionResult> DownloadFile(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.SurveyTaskActivities
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null || string.IsNullOrEmpty(activity.FilePath) || string.IsNullOrEmpty(activity.FileName))
            {
                return NotFound();
            }

            // Get the file path on server
            var filePath = activity.FilePath;
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Return the file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", activity.FileName);
        }

        private async Task<SystemUser> GetCurrentUserAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            return await _context.Users.FirstOrDefaultAsync(u => u.ServiceId == serviceId);
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email[..Math.Min(email.Length, 6)];
        }
        
    }
}