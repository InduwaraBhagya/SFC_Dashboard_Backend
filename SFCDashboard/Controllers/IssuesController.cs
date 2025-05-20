using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace SFCDashboard.Controllers
{
    public class IssuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<IssuesController> _logger;

        public IssuesController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<IssuesController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: Issues
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return RedirectToAction("Index", "Home");

            var received = await _context.PEIssues
                .Include(i => i.SenderId)
                .Where(i => i.ReceiverId == currentUser.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var sent = await _context.PEIssues
                .Include(i => i.ReceiverId)
                .Where(i => i.SenderId == currentUser.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.ReceivedIssues = received;
            ViewBag.SentIssues = sent;
            ViewBag.UnreadCount = received.Count(i => !i.IsRead);

            return View();
        }

        // GET: Issues/Create
        public async Task<IActionResult> Create()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, Name = $"{u.Name} ({u.ServiceId})" })
                .ToListAsync();
            ViewBag.Users = users;
            return View(new IssueCreateViewModel());
        }

        // POST: Issues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IssueCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUsersAsync();
                return View(model);
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return RedirectToAction("Index", "Home");

            var issue = new PEIssue
            {
                PlannedEventId = model.PlannedEventId,
                PETaskId = model.PETaskId,
                SenderId = currentUser.Id,
                ReceiverId = model.ReceiverId,
                IssueText = model.IssueText,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues", issue.PlannedEventId.ToString());
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(model.Attachment.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }
                issue.AttachmentPath = $"/uploads/issues/{issue.PlannedEventId}/{uniqueFileName}";
            }

            _context.PEIssues.Add(issue);
            await _context.SaveChangesAsync();

            // Optional: Notification logic here

            return RedirectToAction(nameof(Index));
        }

        // GET: Issues/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.PEIssues
                .Include(i => i.SenderId)
                .Include(i => i.ReceiverId)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();

            // Mark as read if receiver is viewing
            var currentUser = await GetCurrentUserAsync();
            if (currentUser != null && issue.ReceiverId == currentUser.Id && !issue.IsRead)
            {
                issue.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(issue);
        }

        // GET: Issues/MyInbox
        public async Task<IActionResult> MyInbox()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return RedirectToAction("Index", "Home");

            var issues = await _context.PEIssues
                .Include(i => i.SenderId)
                .Where(i => i.ReceiverId == currentUser.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        // GET: Issues/MySent
        public async Task<IActionResult> MySent()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return RedirectToAction("Index", "Home");

            var issues = await _context.PEIssues
                .Include(i => i.ReceiverId)
                .Where(i => i.SenderId == currentUser.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        // Utility: Get current user from context
        private async Task<SystemUser?> GetCurrentUserAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return null;

            // Extract the first 6 characters of the service ID
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .Include(u => u.WorkGroup) // Include WorkGroup if needed
                .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);

            if (user == null)
            {
                // Log the failed attempt to find user
                _logger.LogWarning("User not found for ServiceId: {ServiceId}", serviceIdShort);
                return null;
            }

            // Log successful user lookup
            _logger.LogInformation("Found user: {UserId} - {UserName} ({ServiceId})", 
                user.Id, user.Name, user.ServiceId);

            return user;
        }

        private async Task PopulateUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, Name = $"{u.Name} ({u.ServiceId})" })
                .ToListAsync();
            ViewBag.Users = users;
        }
    }

    public class IssueCreateViewModel
    {
        [Required]
        public int PlannedEventId { get; set; }

        [Required]
        public int PETaskId { get; set; }

        [Required]
        [Display(Name = "Send To")]
        public int ReceiverId { get; set; }

        [Required]
        [Display(Name = "Issue Description")]
        public string IssueText { get; set; }

        [Display(Name = "Attachment")]
        [FileExtensions(Extensions = ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png")]
        public IFormFile? Attachment { get; set; }
    }
}