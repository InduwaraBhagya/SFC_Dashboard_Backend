using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFCDashboard.Controllers
{
    [Authorize]
    public class EscalationController : Controller
    {
        private readonly EscalationService _escalationService;
        private readonly ApplicationDbContext _context;
        
        public EscalationController(EscalationService escalationService, ApplicationDbContext context)
        {
            _escalationService = escalationService;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUserEscalations(string filter = "unread")
        {
            // Get current user ID using the provided method
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get the current user with role information
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (currentUser == null || currentUser.UserRoleId == 0)
                return Json(new { count = 0, escalations = new List<object>() });
            
            // Get escalations based on filter - COMPARING USER ROLE ID
            List<Escalation> escalations;
            if (filter == "all")
            {
                // Get all escalations for this user's role (read and unread, but not ignored)
                escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Include(e => e.Recipient)
                    .Where(e => e.Recipient.UserRoleId == currentUser.UserRoleId && !e.IsIgnored)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // Get only unread escalations for this user's role
                escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Include(e => e.Recipient)
                    .Where(e => e.Recipient.UserRoleId == currentUser.UserRoleId && !e.IsRead && !e.IsIgnored)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            
            // Project to anonymous objects with all required fields
            var result = escalations.Select(e => new {
                id = e.Id,
                taskId = e.TaskId,
                taskWorkGroup = e.PETask?.TaskWorkGroup, // Include TaskWorkGroup from PETask
                level = (int)e.Level,
                createdAt = e.CreatedAt,
                isRead = e.IsRead
            }).ToList();
            
            return Json(new { count = result.Count, escalations = result });
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var escalation = await _escalationService.GetEscalationDetailsAsync(id);
            
            if (escalation == null)
            {
                return NotFound();
            }
            
            // Mark as read
            await _escalationService.MarkAsReadAsync(id);
            
            return View(escalation);
        }
        
        [HttpPost]
        public async Task<IActionResult> Ignore(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest("Reason is required");
            }
            
            // Get current user ID (implement according to your auth system)
            int userId = int.Parse(User.FindFirst("UserId").Value);
            
            await _escalationService.IgnoreEscalationAsync(id, reason, userId);
            
            return RedirectToAction("Index", "Home");
        }
        
        // POST: Escalation/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = await GetCurrentUserIdAsync();
            if (userId == 0)
                return Json(new { success = false });
            
            // Get all unread escalations for this user
            var unreadEscalations = await _context.Escalations
                .Where(e => e.RecipientId == userId && !e.IsRead)
                .ToListAsync();
            
            // Mark them all as read
            foreach (var escalation in unreadEscalations)
            {
                escalation.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        // The provided method for getting current user ID
        private async Task<int> GetCurrentUserIdAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return 0;

            // Extract the substring before the query
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);
            return user?.Id ?? 0;
        }
    }
}