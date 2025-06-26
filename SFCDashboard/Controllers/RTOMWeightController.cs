using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    [Authorize]
    public class RTOMWeightController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RTOMWeightController> _logger;

        public RTOMWeightController(ApplicationDbContext context, ILogger<RTOMWeightController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: RTOMWeight
        public async Task<IActionResult> Index()
        {
            try
            {
                var rtomWeights = await _context.RTOMWeights.ToListAsync();
                return View(rtomWeights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading RTOM Weights");
                TempData["Error"] = "An error occurred while loading RTOM Weights.";
                return View(Array.Empty<RTOMWeight>());
            }
        }

        // POST: RTOMWeight/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RTOM,Weight")] RTOMWeight rtomWeight)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate RTOM
                    var existing = await _context.RTOMWeights
                        .FirstOrDefaultAsync(r => r.RTOM.ToLower() == rtomWeight.RTOM.ToLower());
                    
                    if (existing != null)
                    {
                        TempData["Error"] = $"RTOM '{rtomWeight.RTOM}' already exists.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Add(rtomWeight);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created new RTOM Weight: {rtomWeight.RTOM}");
                    TempData["Success"] = "RTOM Weight created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating RTOM Weight for {rtomWeight.RTOM}");
                    TempData["Error"] = "Failed to create RTOM Weight.";
                }
            }
            else
            {
                TempData["Error"] = "Please check the form for errors.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: RTOMWeight/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,RTOM,Weight")] RTOMWeight rtomWeight)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate RTOM (excluding the current one)
                    var existing = await _context.RTOMWeights
                        .FirstOrDefaultAsync(r => r.RTOM.ToLower() == rtomWeight.RTOM.ToLower() && r.Id != rtomWeight.Id);
                    
                    if (existing != null)
                    {
                        TempData["Error"] = $"RTOM '{rtomWeight.RTOM}' already exists.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Update(rtomWeight);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated RTOM Weight: {rtomWeight.RTOM}");
                    TempData["Success"] = "RTOM Weight updated successfully.";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await _context.RTOMWeights.AnyAsync(e => e.Id == rtomWeight.Id))
                    {
                        _logger.LogWarning($"RTOM Weight ID {rtomWeight.Id} not found for update");
                        TempData["Error"] = "RTOM Weight not found.";
                    }
                    else
                    {
                        _logger.LogError(ex, $"Concurrency error updating RTOM Weight {rtomWeight.Id}");
                        TempData["Error"] = "The record was modified by another user. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating RTOM Weight {rtomWeight.Id}");
                    TempData["Error"] = "Failed to update RTOM Weight.";
                }
            }
            else
            {
                TempData["Error"] = "Please check the form for errors.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: RTOMWeight/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var rtomWeight = await _context.RTOMWeights.FindAsync(id);
                if (rtomWeight == null)
                {
                    TempData["Error"] = "RTOM Weight not found.";
                    return RedirectToAction(nameof(Index));
                }

                _context.RTOMWeights.Remove(rtomWeight);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted RTOM Weight: {rtomWeight.RTOM}");
                TempData["Success"] = "RTOM Weight deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting RTOM Weight {id}");
                TempData["Error"] = "Failed to delete RTOM Weight.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}