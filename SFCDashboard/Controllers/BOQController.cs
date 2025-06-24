using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    public class BOQController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BOQController> _logger;

        public BOQController(ApplicationDbContext context, ILogger<BOQController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: BOQ/GetSubCategories/5
        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            try
            {
                _logger.LogInformation($"GetSubCategories called with categoryId: {categoryId}");
                
                var subCategories = await _context.UDSubCategories
                    .Where(s => s.CategoryId == categoryId)
                    .Select(s => new { id = s.Id, name = s.SubCategory })
                    .ToListAsync();
                
                _logger.LogInformation($"Found {subCategories.Count} sub-categories");
                
                return Json(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSubCategories for categoryId: {categoryId}");
                return Json(new { error = ex.Message });
            }
        }

        // GET: BOQ/GetUDNames
        [HttpGet]
        public async Task<IActionResult> GetUDNames(int categoryId, int subCategoryId)
        {
            try
            {
                _logger.LogInformation($"GetUDNames called with categoryId: {categoryId}, subCategoryId: {subCategoryId}");
        
                var udNames = await _context.UDNames
                    .Where(u => u.CategoryId == categoryId && u.SubCategoryId == subCategoryId)
                    .Select(u => new { 
                        id = u.Id, 
                        name = u.Name,
                        unit = u.Unit,
                        unitPrice = u.UnitPrice 
                    })
                    .ToListAsync();
        
                _logger.LogInformation($"Found {udNames.Count} UD names for categoryId: {categoryId}, subCategoryId: {subCategoryId}");
        
                return Json(udNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetUDNames for categoryId: {categoryId}, subCategoryId: {subCategoryId}");
                return Json(new { error = ex.Message });
            }
        }

        // GET: BOQ/GetCategories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                _logger.LogInformation("GetCategories called");
                
                var categories = await _context.UDCategories
                    .Select(c => new { id = c.Id, category = c.Category })
                    .ToListAsync();
                
                _logger.LogInformation($"Found {categories.Count} categories");
                
                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories");
                return Json(new { error = ex.Message });
            }
        }

        // GET: BOQ/Create/5 (5 is the taskId)
        public async Task<IActionResult> Create(int taskId)
        {
            // Check if the task exists and is a survey route task
            var task = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.Task == "SURVEY FIBER ROUTE");

            if (task == null)
            {
                return NotFound();
            }

            ViewBag.TaskId = taskId;
            ViewBag.Categories = await _context.UDCategories.ToListAsync();
            ViewBag.RTOM = task.PlannedEvent.Rtom;

            return View(new BOQViewModel { TaskId = taskId });
        }

        // POST: BOQ/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BOQViewModel model)
        {
            try 
            {
                _logger.LogInformation($"Create POST received with TaskId: {model.TaskId}");
                
                // Log all form values for debugging
                _logger.LogInformation($"Received values: TaskId={model.TaskId}, " +
                    $"CategoryId={model.CategoryId}, SubCategoryId={model.SubCategoryId}, " +
                    $"UDNameId={model.UDNameId}, Quantity={model.Quantity}, " + 
                    $"Unit={model.Unit ?? "null"}, UnitPrice={model.UnitPrice}");

                // Clear any existing ModelState errors for these fields
                ModelState.Remove("UDNameValue");
                ModelState.Remove("CategoryName");
                ModelState.Remove("SubCategoryName");
                
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                        
                    _logger.LogWarning($"Model validation failed: {errors}");
                    return Json(new { success = false, message = $"Invalid data: {errors}" });
                }

                // Additional validation with null checks
                if (model.UDNameId <= 0)
                {
                    return Json(new { success = false, message = "UD Name selection is required." });
                }
                
                if (model.CategoryId <= 0)
                {
                    return Json(new { success = false, message = "Category selection is required." });
                }
                
                if (model.SubCategoryId <= 0)
                {
                    return Json(new { success = false, message = "Sub-Category selection is required." });
                }

                // Validate and ensure non-null values
                if (model.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than zero." });
                }
                
                if (string.IsNullOrEmpty(model.Unit))
                {
                    return Json(new { success = false, message = "Unit is required." });
                }
                
                // Get the UDName to ensure we have a valid unit price
                var udName = await _context.UDNames.FindAsync(model.UDNameId);
                if (udName == null)
                {
                    return Json(new { success = false, message = "Selected UD Name not found." });
                }
                
                // Use the UDName's unit and price if model values are missing or zero
                if (string.IsNullOrEmpty(model.Unit))
                {
                    model.Unit = udName.Unit ?? "each";
                }
                
                if (model.UnitPrice <= 0)
                {
                    model.UnitPrice = udName.UnitPrice > 0 ? udName.UnitPrice : 0.01m;
                }

                // Get the current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated or not found." });
                }

                // Get the task to access its PlannedEvent's RTOM
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId);

                if (task == null)
                {
                    return Json(new { success = false, message = "Task not found." });
                }

                // Get the weight for the RTOM with null safety
                var rtom = task.PlannedEvent?.Rtom ?? "DEFAULT";
                var weight = await _context.RTOMWeights
                    .FirstOrDefaultAsync(w => w.RTOM == rtom);

                decimal weightValue = weight?.Weight ?? 1.0m;
                
                // Calculate with null-safety
                decimal unitPrice = model.UnitPrice;
                decimal quantity = model.Quantity;
                decimal adjustedUnitPrice = unitPrice * weightValue;
                decimal amount = adjustedUnitPrice * quantity;
                
                _logger.LogInformation($"Calculated values: UnitPrice={unitPrice}, " +
                    $"Weight={weightValue}, AdjustedPrice={adjustedUnitPrice}, " +
                    $"Quantity={quantity}, Amount={amount}");
                
                // Create the BOQ entity
                var boq = new BOQ
                {
                    TaskId = model.TaskId,
                    CategoryId = model.CategoryId,
                    SubCategoryId = model.SubCategoryId,
                    UDNameId = model.UDNameId,
                    Quantity = quantity,
                    Unit = model.Unit,
                    UnitPrice = unitPrice,
                    AdjustedUnitPrice = adjustedUnitPrice, 
                    Amount = amount,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = currentUser.Id
                };

                _context.BOQs.Add(boq);
                
                // Add activity to SurveyTaskActivities
                // var activity = new SurveyTaskActivity
                // {
                //     PETaskId = model.TaskId,
                //     Description = "Added new BOQ item",
                //     FilePath="",
                //     FileName="",
                //     SystemUserId = currentUser.Id,
                //     CreatedAt = DateTime.Now
                // };

                // _context.SurveyTaskActivities.Add(activity);
                
                // await _context.SaveChangesAsync();

                _logger.LogInformation($"BOQ created successfully for TaskId: {model.TaskId}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating BOQ for TaskId: {model.TaskId}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: BOQ/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var boq = await _context.BOQs
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.UDName)
                .Include(b => b.Task)
                .Include(b => b.Task.PlannedEvent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (boq == null)
            {
                return NotFound();
            }

            var viewModel = new BOQViewModel
            {
                Id = boq.Id,
                TaskId = boq.TaskId,
                CategoryId = boq.CategoryId,
                SubCategoryId = boq.SubCategoryId,
                UDNameId = boq.UDNameId,
                Quantity = boq.Quantity,
                Unit = boq.Unit,
                UnitPrice = boq.UnitPrice,
                AdjustedUnitPrice = boq.AdjustedUnitPrice,
                Amount = boq.Amount,
                CategoryName = boq.Category?.Category,
                SubCategoryName = boq.SubCategory?.SubCategory,
                UDNameValue = boq.UDName?.Name
            };

            ViewBag.TaskId = boq.TaskId;
            ViewBag.Categories = await _context.UDCategories.ToListAsync();
            ViewBag.SubCategories = await _context.UDSubCategories
                .Where(s => s.CategoryId == boq.CategoryId)
                .ToListAsync();
            ViewBag.UDNames = await _context.UDNames
                .Where(u => u.CategoryId == boq.CategoryId && u.SubCategoryId == boq.SubCategoryId)
                .ToListAsync();
            ViewBag.RTOM = boq.Task.PlannedEvent.Rtom;

            return View(viewModel);
        }

        // POST: BOQ/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BOQViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TaskId = model.TaskId;
                ViewBag.Categories = await _context.UDCategories.ToListAsync();
                ViewBag.SubCategories = await _context.UDSubCategories
                    .Where(s => s.CategoryId == model.CategoryId)
                    .ToListAsync();
                ViewBag.UDNames = await _context.UDNames
                    .Where(u => u.CategoryId == model.CategoryId && u.SubCategoryId == model.SubCategoryId)
                    .ToListAsync();
                return View(model);
            }

            // Get the current user
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                ModelState.AddModelError("", "User not authenticated or not found.");
                return View(model);
            }

            // Get the original BOQ entity
            var boq = await _context.BOQs
                .Include(b => b.Task)
                .Include(b => b.Task.PlannedEvent)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (boq == null)
            {
                return NotFound();
            }

            // Get the weight for the RTOM
            var weight = await _context.RTOMWeights
                .FirstOrDefaultAsync(w => w.RTOM == boq.Task.PlannedEvent.Rtom);

            decimal weightValue = weight?.Weight ?? 1.0m;

            // Calculate the adjusted unit price and amount
            var adjustedUnitPrice = model.UnitPrice * weightValue;
            var amount = adjustedUnitPrice * model.Quantity;

            // Update the BOQ entity
            boq.CategoryId = model.CategoryId;
            boq.SubCategoryId = model.SubCategoryId;
            boq.UDNameId = model.UDNameId;
            boq.Quantity = model.Quantity;
            boq.Unit = model.Unit;
            boq.UnitPrice = model.UnitPrice;
            boq.AdjustedUnitPrice = adjustedUnitPrice;
            boq.Amount = amount;

            try
            {
                _context.Update(boq);
                
                // Add activity to SurveyTaskActivities
                var activity = new SurveyTaskActivity
                {
                    PETaskId = boq.TaskId,
                    Description = $"Updated BOQ item #{boq.Id}",
                    SystemUserId = currentUser.Id,
                    CreatedAt = DateTime.Now
                };

                _context.SurveyTaskActivities.Add(activity);
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BOQExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction("Details", "SurveyTasks", new { id = boq.TaskId });
        }

        // POST: BOQ/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var boq = await _context.BOQs.FindAsync(id);
            if (boq == null)
            {
                return NotFound();
            }

            // Get the current user
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            int taskId = boq.TaskId;
            _context.BOQs.Remove(boq);
            
            // Add activity to SurveyTaskActivities
            var activity = new SurveyTaskActivity
            {
                PETaskId = taskId,
                Description = $"Deleted BOQ item #{id}",
                SystemUserId = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            _context.SurveyTaskActivities.Add(activity);
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        private bool BOQExists(int id)
        {
            return _context.BOQs.Any(e => e.Id == id);
        }
        
        private async Task<SystemUser> GetCurrentUserAsync()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return null;
            }

            var serviceId = ExtractServiceId(userName);
            return await _context.Users.FirstOrDefaultAsync(u => u.ServiceId == serviceId);
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            if (email.Contains('@'))
            {
                return email.Split('@').FirstOrDefault();
            }
            return email;
        }
    }
}