using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    public class UDManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UDManagerController> _logger;

        public UDManagerController(ApplicationDbContext context, ILogger<UDManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UDManager
        public async Task<IActionResult> Index(string searchTerm = null)
        {
            var viewModel = new UDManagerViewModel();
            
            // Load categories
            viewModel.Categories = await _context.UDCategories.ToListAsync();
            
            // Load subcategories
            viewModel.SubCategories = await _context.UDSubCategories
                .Include(s => s.Category)
                .ToListAsync();
            
            // Load UD names with search filter
            var query = _context.UDNames
                .Include(u => u.Category)
                .Include(u => u.SubCategory)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => 
                    u.Name.ToLower().Contains(searchTerm) ||
                    u.Category.Category.ToLower().Contains(searchTerm) ||
                    u.SubCategory.SubCategory.ToLower().Contains(searchTerm));
            }
            
            viewModel.UDNames = await query.ToListAsync();
            
            ViewBag.SearchTerm = searchTerm;
            
            return View(viewModel);
        }

        // POST: UDManager/AddCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(UDCategory newCategory)
        {
            if (!string.IsNullOrEmpty(newCategory.Category))
            {
                try
                {
                    _context.UDCategories.Add(newCategory);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Category '{newCategory.Category}' added successfully");
                    TempData["SuccessMessage"] = $"Category '{newCategory.Category}' added successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding category");
                    ModelState.AddModelError("", "Unable to add category. " + ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError("NewCategory.Category", "Category name is required");
            }
            
            // If we got this far, something failed, redisplay form
            // Create a new view model to render the Index view with
            var viewModel = new UDManagerViewModel
            {
                Categories = await _context.UDCategories.ToListAsync(),
                SubCategories = await _context.UDSubCategories.Include(s => s.Category).ToListAsync(),
                UDNames = await _context.UDNames
                    .Include(u => u.Category)
                    .Include(u => u.SubCategory)
                    .ToListAsync(),
                NewCategory = newCategory
            };
            
            return View("Index", viewModel);
        }

        // POST: UDManager/AddSubCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubCategory(UDSubCategory newSubCategory)
        {
            if (newSubCategory.CategoryId > 0 && !string.IsNullOrEmpty(newSubCategory.SubCategory))
            {
                try
                {
                    _context.UDSubCategories.Add(newSubCategory);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"SubCategory '{newSubCategory.SubCategory}' added successfully");
                    TempData["SuccessMessage"] = $"SubCategory '{newSubCategory.SubCategory}' added successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding subcategory");
                    ModelState.AddModelError("", "Unable to add subcategory. " + ex.Message);
                }
            }
            else
            {
                if (newSubCategory.CategoryId <= 0)
                    ModelState.AddModelError("NewSubCategory.CategoryId", "Please select a category");
                
                if (string.IsNullOrEmpty(newSubCategory.SubCategory))
                    ModelState.AddModelError("NewSubCategory.SubCategory", "Sub-category name is required");
            }
            
            // If we got this far, something failed, redisplay form
            var viewModel = new UDManagerViewModel
            {
                Categories = await _context.UDCategories.ToListAsync(),
                SubCategories = await _context.UDSubCategories.Include(s => s.Category).ToListAsync(),
                UDNames = await _context.UDNames
                    .Include(u => u.Category)
                    .Include(u => u.SubCategory)
                    .ToListAsync(),
                NewSubCategory = newSubCategory
            };
            
            return View("Index", viewModel);
        }

        // POST: UDManager/AddUDName
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUDName(UDName newUDName)
        {
            if (newUDName.CategoryId > 0 && newUDName.SubCategoryId > 0 && 
                !string.IsNullOrEmpty(newUDName.Name) && !string.IsNullOrEmpty(newUDName.Unit))
            {
                try
                {
                    _context.UDNames.Add(newUDName);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"UD Name '{newUDName.Name}' added successfully");
                    TempData["SuccessMessage"] = $"UD Name '{newUDName.Name}' added successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding UD name");
                    ModelState.AddModelError("", "Unable to add UD name. " + ex.Message);
                }
            }
            else
            {
                if (newUDName.CategoryId <= 0)
                    ModelState.AddModelError("NewUDName.CategoryId", "Please select a category");
                
                if (newUDName.SubCategoryId <= 0)
                    ModelState.AddModelError("NewUDName.SubCategoryId", "Please select a sub-category");
                
                if (string.IsNullOrEmpty(newUDName.Name))
                    ModelState.AddModelError("NewUDName.Name", "UD name is required");
                
                if (string.IsNullOrEmpty(newUDName.Unit))
                    ModelState.AddModelError("NewUDName.Unit", "Unit is required");
            }
            
            // If we got this far, something failed, redisplay form
            var viewModel = new UDManagerViewModel
            {
                Categories = await _context.UDCategories.ToListAsync(),
                SubCategories = await _context.UDSubCategories.Include(s => s.Category).ToListAsync(),
                UDNames = await _context.UDNames
                    .Include(u => u.Category)
                    .Include(u => u.SubCategory)
                    .ToListAsync(),
                NewUDName = newUDName
            };
            
            return View("Index", viewModel);
        }

        // POST: UDManager/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var udName = await _context.UDNames.FindAsync(id);
            if (udName == null)
            {
                return NotFound();
            }

            udName.IsActive = !udName.IsActive;
            
            try
            {
                _context.Update(udName);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"UD Name ID {id} status changed to {(udName.IsActive ? "Active" : "Inactive")}");
                return Json(new { success = true, isActive = udName.IsActive });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error toggling UD name status");
                return Json(new { success = false, message = "Failed to update status." });
            }
        }
        
        // GET: UDManager/GetSubCategories/5
        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var subCategories = await _context.UDSubCategories
                .Where(s => s.CategoryId == categoryId)
                .Select(s => new { id = s.Id, name = s.SubCategory })
                .ToListAsync();
                
            return Json(subCategories);
        }

        // Add this new method to get UD name details for editing
        [HttpGet]
        public async Task<IActionResult> GetUDNameDetails(int id)
        {
            var udName = await _context.UDNames
                .Include(u => u.Category)
                .Include(u => u.SubCategory)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (udName == null)
            {
                return NotFound();
            }
            
            var result = new {
                id = udName.Id,
                categoryId = udName.CategoryId,
                subCategoryId = udName.SubCategoryId,
                name = udName.Name,
                unit = udName.Unit,
                unitPrice = udName.UnitPrice,
                isActive = udName.IsActive
            };
            
            return Json(result);
        }

        // Add this new method to update UD name
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUDName(UDName updateModel)
        {
            if (updateModel.Id <= 0)
            {
                return BadRequest("Invalid ID");
            }
            
            var udName = await _context.UDNames.FindAsync(updateModel.Id);
            
            if (udName == null)
            {
                return NotFound();
            }
            
            if (updateModel.CategoryId > 0 && updateModel.SubCategoryId > 0 && 
                !string.IsNullOrEmpty(updateModel.Name) && !string.IsNullOrEmpty(updateModel.Unit))
            {
                try
                {
                    // Update properties
                    udName.CategoryId = updateModel.CategoryId;
                    udName.SubCategoryId = updateModel.SubCategoryId;
                    udName.Name = updateModel.Name;
                    udName.Unit = updateModel.Unit;
                    udName.UnitPrice = updateModel.UnitPrice;
                    udName.IsActive = updateModel.IsActive;
                    
                    _context.Update(udName);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"UD Name '{udName.Name}' updated successfully");
                    
                    return Json(new { success = true, message = $"UD Name '{udName.Name}' updated successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating UD name");
                    return Json(new { success = false, message = "Unable to update UD name. " + ex.Message });
                }
            }
            else
            {
                var errors = new List<string>();
                
                if (updateModel.CategoryId <= 0)
                    errors.Add("Please select a category");
                
                if (updateModel.SubCategoryId <= 0)
                    errors.Add("Please select a sub-category");
                
                if (string.IsNullOrEmpty(updateModel.Name))
                    errors.Add("UD name is required");
                
                if (string.IsNullOrEmpty(updateModel.Unit))
                    errors.Add("Unit is required");
                
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }
        }
    }
}