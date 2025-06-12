using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;

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
        
        public async Task<IActionResult> GetUserEscalations()
        {
            // Get current user ID (implement according to your auth system)
            int userId = int.Parse(User.FindFirst("UserId").Value);
            
            var escalations = await _escalationService.GetEscalationsForUserAsync(userId);
            return Json(new { count = escalations.Count, escalations = escalations });
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
            // Get current user ID
            var userName = User.Identity?.Name;
            var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
                ? userName.Substring(0, 6)
                : null;
                
            var currentUser = !string.IsNullOrEmpty(serviceId)
                ? await _context.Users.FirstOrDefaultAsync(u => u.ServiceId == serviceId)
                : null;
                
            int? userId = currentUser?.Id;
            
            if (userId == null)
            {
                return Json(new { success = false });
            }
            
            // Get all unread escalations for this user
            var escalations = await _context.Escalations
                .Where(e => e.RecipientId == userId && !e.IsRead)
                .ToListAsync();
            
            // Mark them as read
            foreach (var escalation in escalations)
            {
                escalation.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }
    }
}