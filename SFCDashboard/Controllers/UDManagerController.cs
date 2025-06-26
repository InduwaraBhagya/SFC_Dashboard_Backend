using Microsoft.AspNetCore.Mvc;
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

        public UDManagerController(
            ApplicationDbContext context,
            ILogger<UDManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UDManager
        public async Task<IActionResult> Index()
        {
            var viewModel = new UDManagerViewModel
            {
                Categories = await _context.UDCategories.ToListAsync(),
                SubCategories = await _context.UDSubCategories
                    .Include(s => s.Category)
                    .ToListAsync(),
                UDNames = await _context.UDNames
                    .Include(u => u.Category)
                    .Include(u => u.SubCategory)
                    .ToListAsync()
            };

            return View(viewModel);
        }
        
        // POST: UDManager/AddCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([Bind("Category")] UDCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.UDCategories.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New UD Category added: {category.Category}");
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: UDManager/AddSubCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubCategory([Bind("CategoryId,SubCategory")] UDSubCategory subCategory)
        {
            if (ModelState.IsValid)
            {
                _context.UDSubCategories.Add(subCategory);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New UD SubCategory added: {subCategory.SubCategory}");
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: UDManager/AddUDName
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUDName([Bind("CategoryId,SubCategoryId,Name,Unit,UnitPrice")] UDName udName)
        {
            if (ModelState.IsValid)
            {
                _context.UDNames.Add(udName);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New UDName added: {udName.Name}");
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: UDManager/GetSubCategories
        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var subCategories = await _context.UDSubCategories
                .Where(s => s.CategoryId == categoryId)
                .Select(s => new { id = s.Id, name = s.SubCategory })
                .ToListAsync();
                
            return Json(subCategories);
        }

        // DELETE: UDManager/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.UDCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            // Check if the category is in use
            bool hasSubCategories = await _context.UDSubCategories.AnyAsync(s => s.CategoryId == id);
            bool hasUDNames = await _context.UDNames.AnyAsync(u => u.CategoryId == id);
            bool hasBoqItems = await _context.BOQs.AnyAsync(b => b.CategoryId == id);

            if (hasSubCategories || hasUDNames || hasBoqItems)
            {
                return Json(new { 
                    success = false, 
                    message = "Cannot delete this category because it is being used by subcategories, UD names, or BOQ items." 
                });
            }

            _context.UDCategories.Remove(category);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        // DELETE: UDManager/DeleteSubCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var subCategory = await _context.UDSubCategories.FindAsync(id);
            if (subCategory == null)
            {
                return Json(new { success = false, message = "SubCategory not found." });
            }

            // Check if the subcategory is in use
            bool hasUDNames = await _context.UDNames.AnyAsync(u => u.SubCategoryId == id);
            bool hasBoqItems = await _context.BOQs.AnyAsync(b => b.SubCategoryId == id);

            if (hasUDNames || hasBoqItems)
            {
                return Json(new { 
                    success = false, 
                    message = "Cannot delete this subcategory because it is being used by UD names or BOQ items." 
                });
            }

            _context.UDSubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        // DELETE: UDManager/DeleteUDName/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUDName(int id)
        {
            var udName = await _context.UDNames.FindAsync(id);
            if (udName == null)
            {
                return Json(new { success = false, message = "UD Name not found." });
            }

            // Check if the UD name is in use
            bool hasBoqItems = await _context.BOQs.AnyAsync(b => b.UDNameId == id);

            if (hasBoqItems)
            {
                return Json(new { 
                    success = false, 
                    message = "Cannot delete this UD name because it is being used by BOQ items." 
                });
            }

            _context.UDNames.Remove(udName);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        // POST: UDManager/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory([Bind("Id,Category")] UDCategory category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.UDCategories.FindAsync(category.Id);
                    if (existingCategory == null)
                    {
                        return Json(new { success = false, message = "Category not found." });
                    }
                    
                    existingCategory.Category = category.Category;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Category {category.Id} updated successfully");
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating category {category.Id}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid data submitted." });
        }

        // POST: UDManager/EditSubCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubCategory([Bind("Id,CategoryId,SubCategory")] UDSubCategory subCategory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingSubCategory = await _context.UDSubCategories.FindAsync(subCategory.Id);
                    if (existingSubCategory == null)
                    {
                        return Json(new { success = false, message = "Sub-category not found." });
                    }
                    
                    existingSubCategory.CategoryId = subCategory.CategoryId;
                    existingSubCategory.SubCategory = subCategory.SubCategory;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"SubCategory {subCategory.Id} updated successfully");
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating subcategory {subCategory.Id}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid data submitted." });
        }

        // POST: UDManager/EditUDName
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUDName([Bind("Id,CategoryId,SubCategoryId,Name,Unit,UnitPrice")] UDName udName)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUDName = await _context.UDNames.FindAsync(udName.Id);
                    if (existingUDName == null)
                    {
                        return Json(new { success = false, message = "UD Name not found." });
                    }
                    
                    // Preserve the IsActive status
                    bool isActive = existingUDName.IsActive;
                    
                    // Update the properties
                    existingUDName.CategoryId = udName.CategoryId;
                    existingUDName.SubCategoryId = udName.SubCategoryId;
                    existingUDName.Name = udName.Name;
                    existingUDName.Unit = udName.Unit;
                    existingUDName.UnitPrice = udName.UnitPrice;
                    existingUDName.IsActive = isActive; // Keep the original active state
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"UDName {udName.Id} updated successfully");
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating UDName {udName.Id}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid data submitted." });
        }

        // POST: UDManager/ToggleUDNameActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUDNameActive(int id, bool isActive)
        {
            try
            {
                var udName = await _context.UDNames.FindAsync(id);
                if (udName == null)
                {
                    return Json(new { success = false, message = "UD Name not found." });
                }

                udName.IsActive = isActive;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"UD Name {id} active state changed to {isActive}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling UD Name {id} active state");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}