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
    [Route("SurveyTasks")]
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

        // GET: SurveyTasks/Details/{peNumber}
        [HttpGet("Details/{peNumber}")]
        public async Task<IActionResult> Details(string peNumber)
        {
            if (string.IsNullOrEmpty(peNumber))
            {
                return BadRequest("PE Number is required");
            }

            // First check if the task exists at all
            var taskExists = await _context.PETasks.AnyAsync(t => t.PENumber == peNumber);
            if (!taskExists)
            {
                return Content($"Task with PE Number {peNumber} does not exist in database");
            }

            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.PENumber == peNumber && t.Task == "SURVEY FIBER ROUTE");

            if (task == null)
            {
                return NotFound();
            }

            var activities = await _context.SurveyTaskActivities
                .Include(a => a.User)
                .Where(a => a.PETaskId == task.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // Get BOQ items for this task
            var boqItems = await _context.BOQs
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.UDName)
                .Include(b => b.CreatedBy)
                .Where(b => b.TaskId == task.Id)
                .OrderBy(b => b.CategoryId)
                .ThenBy(b => b.SubCategoryId)
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

            // Check if BOQ has been submitted before
            var boqSubmission = await _context.SurveyTaskActivities
                .Where(a => a.PETaskId == task.Id && a.Description.Contains("Submitted BOQ with total amount"))
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
                
            ViewBag.BOQSubmitted = boqSubmission != null;
            ViewBag.BOQSubmissionDate = boqSubmission?.CreatedAt;

            var viewModel = new SurveyTaskViewModel
            {
                Task = task,
                Activities = activities,
                NewActivity = new SurveyTaskActivityViewModel { TaskId = task.Id, PENumber = task.PENumber },
                BOQItems = boqItems
            };

            return View(viewModel);
        }

        // POST: SurveyTasks/AddActivity
        [HttpPost("AddActivity")]
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
                return RedirectToAction(nameof(Details), new { peNumber = model.PENumber });
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
                    return RedirectToAction(nameof(Details), new { peNumber = model.PENumber });
                }
            }

            // Save the activity to database
            _context.SurveyTaskActivities.Add(activity);
            await _context.SaveChangesAsync();

            // Redirect back to the details page to show the newly added activity
            return RedirectToAction(nameof(Details), new { peNumber = model.PENumber });
        }

        // GET: SurveyTasks/DownloadFile/{id}
        [HttpGet("DownloadFile/{id:int}")]
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

        // POST: SurveyTasks/SubmitBoqTotal
        [HttpPost("SubmitBoqTotal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBoqTotal(int taskId, string peNumber, decimal totalAmount)
        {
            try
            {
                _logger.LogInformation($"SubmitBoqTotal called for TaskId: {taskId}, Total: {totalAmount}");
                
                // Verify that the task exists
                var task = await _context.PETasks
                    .FirstOrDefaultAsync(t => t.Id == taskId);
                    
                if (task == null)
                {
                    _logger.LogWarning($"Task not found for ID: {taskId}");
                    return Json(new { success = false, message = "Task not found" });
                }
                
                // Get the current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated or not found");
                    return Json(new { success = false, message = "User not authenticated or not found" });
                }
                
                // Verify that there are actually BOQ items for this task
                var boqCount = await _context.BOQs.CountAsync(b => b.TaskId == taskId);
                if (boqCount == 0)
                {
                    _logger.LogWarning($"No BOQ items found for TaskId: {taskId}");
                    return Json(new { success = false, message = "No BOQ items found for this task" });
                }
                
                // Recalculate the total amount to verify it matches what was sent
                var calculatedTotal = await _context.BOQs
                    .Where(b => b.TaskId == taskId)
                    .SumAsync(b => b.Amount);
                    
                if (Math.Abs(calculatedTotal - totalAmount) > 0.01m)
                {
                    _logger.LogWarning($"Total amount mismatch: Sent={totalAmount}, Calculated={calculatedTotal}");
                    return Json(new { success = false, message = "Total amount mismatch. Please refresh the page and try again." });
                }
                
                // Create activity record for the BOQ submission
                var activity = new SurveyTaskActivity
                {
                    PETaskId = taskId,
                    Description = $"Submitted BOQ with total amount of {totalAmount:N2}",
                    FilePath = "",
                    FileName = "",
                    SystemUserId = currentUser.Id,
                    CreatedAt = DateTime.Now
                };
                
                _context.SurveyTaskActivities.Add(activity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"BOQ submission successful for TaskId: {taskId}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting BOQ total for TaskId: {taskId}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // POST: SurveyTasks/SubmitBoqTotalByPE
        [HttpPost("SubmitBoqTotalByPE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBoqTotalByPE(string peNumber, decimal totalAmount)
        {
            try
            {
                _logger.LogInformation($"SubmitBoqTotalByPE called for PE Number: {peNumber}, Total: {totalAmount}");
                
                // Find the task by PE number
                var task = await _context.PETasks
                    .FirstOrDefaultAsync(t => t.PENumber == peNumber && t.Task == "SURVEY FIBER ROUTE");
                    
                if (task == null)
                {
                    _logger.LogWarning($"Task not found for PE Number: {peNumber}");
                    return Json(new { success = false, message = "Task not found" });
                }
                
                // Get the current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated or not found");
                    return Json(new { success = false, message = "User not authenticated or not found" });
                }
                
                // Verify that there are actually BOQ items for this task
                var boqCount = await _context.BOQs.CountAsync(b => b.TaskId == task.Id);
                if (boqCount == 0)
                {
                    _logger.LogWarning($"No BOQ items found for PE Number: {peNumber}");
                    return Json(new { success = false, message = "No BOQ items found for this task" });
                }
                
                // Recalculate the total amount to verify it matches what was sent
                var calculatedTotal = await _context.BOQs
                    .Where(b => b.TaskId == task.Id)
                    .SumAsync(b => b.Amount);
                    
                if (Math.Abs(calculatedTotal - totalAmount) > 0.01m)
                {
                    _logger.LogWarning($"Total amount mismatch: Sent={totalAmount}, Calculated={calculatedTotal}");
                    return Json(new { success = false, message = "Total amount mismatch. Please refresh the page and try again." });
                }
                
                // Create activity record for the BOQ submission
                var activity = new SurveyTaskActivity
                {
                    PETaskId = task.Id,
                    Description = $"Submitted BOQ with total amount of {totalAmount:N2}",
                    FilePath = "",
                    FileName = "",
                    SystemUserId = currentUser.Id,
                    CreatedAt = DateTime.Now
                };
                
                _context.SurveyTaskActivities.Add(activity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"BOQ submission successful for PE Number: {peNumber}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting BOQ total for PE Number: {peNumber}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: SurveyTasks/GetBOQDetails/{id}
        [HttpGet("GetBOQDetails/{id:int}")]
        public async Task<IActionResult> GetBOQDetails(int id)
        {
            try
            {
                var boq = await _context.BOQs
                    .Include(b => b.Category)
                    .Include(b => b.SubCategory)
                    .Include(b => b.UDName)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (boq == null)
                {
                    return NotFound();
                }

                var result = new
                {
                    id = boq.Id,
                    taskId = boq.TaskId,
                    categoryId = boq.CategoryId,
                    subCategoryId = boq.SubCategoryId,
                    udNameId = boq.UDNameId,
                    quantity = boq.Quantity,
                    unit = boq.Unit,
                    unitPrice = boq.UnitPrice,
                    adjustedUnitPrice = boq.AdjustedUnitPrice,
                    amount = boq.Amount,
                    categoryName = boq.Category?.Category,
                    subCategoryName = boq.SubCategory?.SubCategory,
                    udNameValue = boq.UDName?.Name
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting BOQ details for id: {id}");
                return Json(null);
            }
        }

        // GET: SurveyTasks/GetBOQDetailsByPE/{peNumber}
        [HttpGet("GetBOQDetailsByPE/{peNumber}")]
        public async Task<IActionResult> GetBOQDetailsByPE(string peNumber)
        {
            try
            {
                // First find the task by PE number
                var task = await _context.PETasks
                    .FirstOrDefaultAsync(t => t.PENumber == peNumber && t.Task == "SURVEY FIBER ROUTE");

                if (task == null)
                {
                    return NotFound();
                }

                // Get BOQ items for this task
                var boqItems = await _context.BOQs
                    .Include(b => b.Category)
                    .Include(b => b.SubCategory)
                    .Include(b => b.UDName)
                    .Where(b => b.TaskId == task.Id)
                    .Select(boq => new
                    {
                        id = boq.Id,
                        taskId = boq.TaskId,
                        categoryId = boq.CategoryId,
                        subCategoryId = boq.SubCategoryId,
                        udNameId = boq.UDNameId,
                        quantity = boq.Quantity,
                        unit = boq.Unit,
                        unitPrice = boq.UnitPrice,
                        adjustedUnitPrice = boq.AdjustedUnitPrice,
                        amount = boq.Amount,
                        categoryName = boq.Category!.Category,
                        subCategoryName = boq.SubCategory!.SubCategory,
                        udNameValue = boq.UDName!.Name
                    })
                    .ToListAsync();

                return Json(boqItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting BOQ details for PE number: {peNumber}");
                return Json(null);
            }
        }

        // POST: SurveyTasks/EditInline
        [HttpPost("EditInline")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInline([FromForm] BOQViewModel model)
        {
            try
            {
                _logger.LogInformation($"EditInline received for BOQ ID: {model.Id}");
                
                // Find the BOQ entry
                var boq = await _context.BOQs
                    .Include(b => b.Task)
                    .Include(b => b.Task.PlannedEvent)
                    .FirstOrDefaultAsync(b => b.Id == model.Id);

                if (boq == null)
                {
                    return Json(new { success = false, message = "BOQ item not found." });
                }

                // Validate inputs
                if (model.UDNameId <= 0 || model.CategoryId <= 0 || model.SubCategoryId <= 0 || model.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Please fill in all required fields." });
                }

                // Get the UDName for unit and price info
                var udName = await _context.UDNames.FindAsync(model.UDNameId);
                if (udName == null)
                {
                    return Json(new { success = false, message = "Selected UD Name not found." });
                }
                
                // Get the weight for the RTOM
                var rtom = boq.Task.PlannedEvent?.Rtom ?? "DEFAULT";
                var weight = await _context.RTOMWeights
                    .FirstOrDefaultAsync(w => w.RTOM == rtom);

                decimal weightValue = weight?.Weight ?? 1.0m;
                
                // Calculate with null-safety
                decimal unitPrice = udName.UnitPrice;
                decimal quantity = model.Quantity;
                decimal adjustedUnitPrice = unitPrice * weightValue;
                decimal amount = adjustedUnitPrice * quantity;
                
                // Get the current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated or not found." });
                }

                // Update the BOQ entity
                boq.CategoryId = model.CategoryId;
                boq.SubCategoryId = model.SubCategoryId;
                boq.UDNameId = model.UDNameId;
                boq.Quantity = quantity;
                boq.Unit = udName.Unit ?? "each";
                boq.UnitPrice = unitPrice;
                boq.AdjustedUnitPrice = adjustedUnitPrice;
                boq.Amount = amount;

                // Add activity to SurveyTaskActivities
                var activity = new SurveyTaskActivity
                {
                    PETaskId = boq.TaskId,
                    Description = $"Updated BOQ item #{boq.Id}",
                    SystemUserId = currentUser.Id,
                    CreatedAt = DateTime.Now,
                    FilePath = "",
                    FileName =""
                };

                _context.SurveyTaskActivities.Add(activity);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"BOQ item #{boq.Id} updated successfully");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating BOQ item ID: {model.Id}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // POST: SurveyTasks/EditInlineByPE
        [HttpPost("EditInlineByPE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInlineByPE([FromForm] BOQViewModel model, string peNumber)
        {
            try
            {
                _logger.LogInformation($"EditInlineByPE received for BOQ ID: {model.Id}, PE Number: {peNumber}");
                
                // Find the BOQ entry
                var boq = await _context.BOQs
                    .Include(b => b.Task)
                    .Include(b => b.Task.PlannedEvent)
                    .FirstOrDefaultAsync(b => b.Id == model.Id);

                if (boq == null)
                {
                    return Json(new { success = false, message = "BOQ item not found." });
                }

                // Verify that the BOQ belongs to the correct PE
                if (boq.Task.PENumber != peNumber)
                {
                    return Json(new { success = false, message = "BOQ item does not belong to the specified PE." });
                }

                // Validate inputs
                if (model.UDNameId <= 0 || model.CategoryId <= 0 || model.SubCategoryId <= 0 || model.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Please fill in all required fields." });
                }

                // Get the UDName for unit and price info
                var udName = await _context.UDNames.FindAsync(model.UDNameId);
                if (udName == null)
                {
                    return Json(new { success = false, message = "Selected UD Name not found." });
                }
                
                // Get the weight for the RTOM
                var rtom = boq.Task.PlannedEvent?.Rtom ?? "DEFAULT";
                var weight = await _context.RTOMWeights
                    .FirstOrDefaultAsync(w => w.RTOM == rtom);

                decimal weightValue = weight?.Weight ?? 1.0m;
                
                // Calculate with null-safety
                decimal unitPrice = udName.UnitPrice;
                decimal quantity = model.Quantity;
                decimal adjustedUnitPrice = unitPrice * weightValue;
                decimal amount = adjustedUnitPrice * quantity;
                
                // Get the current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated or not found." });
                }

                // Update the BOQ entity
                boq.CategoryId = model.CategoryId;
                boq.SubCategoryId = model.SubCategoryId;
                boq.UDNameId = model.UDNameId;
                boq.Quantity = quantity;
                boq.Unit = udName.Unit ?? "each";
                boq.UnitPrice = unitPrice;
                boq.AdjustedUnitPrice = adjustedUnitPrice;
                boq.Amount = amount;

                // Add activity to SurveyTaskActivities
                var activity = new SurveyTaskActivity
                {
                    PETaskId = boq.TaskId,
                    Description = $"Updated BOQ item #{boq.Id}",
                    SystemUserId = currentUser.Id,
                    CreatedAt = DateTime.Now,
                    FilePath = "",
                    FileName = ""
                };

                _context.SurveyTaskActivities.Add(activity);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"BOQ item #{boq.Id} updated successfully for PE Number: {peNumber}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating BOQ item ID: {model.Id} for PE Number: {peNumber}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        private async Task<SystemUser?> GetCurrentUserAsync()
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