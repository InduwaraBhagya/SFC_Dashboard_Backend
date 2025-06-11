using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Services;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    [Authorize]
    public class EscalationController : Controller
    {
        private readonly EscalationService _escalationService;
        
        public EscalationController(EscalationService escalationService)
        {
            _escalationService = escalationService;
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
    }
}