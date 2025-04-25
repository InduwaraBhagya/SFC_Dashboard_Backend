using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace YourNamespace.Controllers
{
    public class IssuesController : Controller
    {
        private readonly DbContext _context;
        private readonly IWebHostEnvironment _environment;

        public IssuesController(DbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Issues
        public async Task<IActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();

            // Get unread count for the Inbox button
            var unreadCount = await _context.Issues
                .Where(i => i.ReceiverId == currentUserId && i.Status == "Pending")
                .CountAsync();
            ViewBag.UnreadCount = unreadCount;

            // Get recent received issues (top 5)
            var recentReceivedIssues = await _context.Issues
                .Include(i => i.Sender)
                .Where(i => i.ReceiverId == currentUserId)
                .OrderByDescending(i => i.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentReceivedIssues = recentReceivedIssues;

            // Get recent sent issues (top 5)
            var recentSentIssues = await _context.Issues
                .Include(i => i.Receiver)
                .Where(i => i.SenderId == currentUserId)
                .OrderByDescending(i => i.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentSentIssues = recentSentIssues;

            return View();
        }

        // GET: Issues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Issues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateIssueViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Get the current user ID (you would get this from your auth system)
                int currentUserId = GetCurrentUserId();

                var issue = new Issue
                {
                    SenderId = currentUserId,
                    ReceiverId = model.ReceiverId,
                    IssueText = model.IssueText,
                    Remarks = model.Remarks,
                    CreatedDate = DateTime.Now,
                    Status = "Pending"
                };

                _context.Issues.Add(issue);
                await _context.SaveChangesAsync();

                // Process attachments if any
                if (model.Attachments != null && model.Attachments.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "issues", issue.IssueId.ToString());
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in model.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            string filePath = Path.Combine(uploadsFolder, file.FileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var attachment = new IssueAttachment
                            {
                                IssueId = issue.IssueId,
                                FileName = file.FileName,
                                FilePath = $"/uploads/issues/{issue.IssueId}/{file.FileName}",
                                FileSize = (int)file.Length,
                                UploadDate = DateTime.Now
                            };

                            _context.IssueAttachments.Add(attachment);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Issues/Inbox
        public async Task<IActionResult> Inbox()
        {
            int currentUserId = GetCurrentUserId();

            var issues = await _context.Issues
                .Include(i => i.Sender)
                .Include(i => i.Attachments)
                .Where(i => i.ReceiverId == currentUserId)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            var unreadCount = issues.Count(i => i.Status == "Pending");

            var viewModel = new IssueInboxViewModel
            {
                ReceivedIssues = issues,
                UnreadCount = unreadCount
            };

            return View(viewModel);
        }

        // GET: Issues/Sent
        public async Task<IActionResult> Sent()
        {
            int currentUserId = GetCurrentUserId();

            var issues = await _context.Issues
                .Include(i => i.Receiver)
                .Include(i => i.Attachments)
                .Where(i => i.SenderId == currentUserId)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            return View(issues);
        }

        // GET: Issues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(m => m.IssueId == id);

            if (issue == null)
            {
                return NotFound();
            }

            // If current user is the receiver, mark as read
            int currentUserId = GetCurrentUserId();
            if (issue.ReceiverId == currentUserId && issue.Status == "Pending")
            {
                issue.Status = "Read";
                await _context.SaveChangesAsync();
            }

            return View(issue);
        }

        // GET: Issues/GetUsers
        [HttpGet]
        public async Task<IActionResult> GetUsers(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return Json(new List<object>());
            }

            var users = await _context.Users
                .Where(u => u.Name.Contains(searchTerm))
                .Select(u => new { id = u.Id, text = u.Name })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        // GET: Issues/Download/5
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attachment = await _context.IssueAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == id);

            if (attachment == null)
            {
                return NotFound();
            }

            // Get the physical file path
            string filePath = Path.Combine(_environment.WebRootPath, "uploads", "issues",
                attachment.IssueId.ToString(), attachment.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Read the file
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // Return the file
            return File(memory, GetContentType(attachment.FileName), attachment.FileName);
        }

        // Helper method to get MIME type
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".pdf": return "application/pdf";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".txt": return "text/plain";
                case ".csv": return "text/csv";
                default: return "application/octet-stream";
            }
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            // This is just a placeholder - you would implement this based on your authentication system
            // For example, using claims:
            // return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // If you're using ASP.NET Core Identity:
            // var userId = _userManager.GetUserId(User);
            // return int.Parse(userId);

            return 1; // Placeholder - replace with actual implementation
        }
    }

    // View Models
    public class CreateIssueViewModel
    {
        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string ReceiverName { get; set; }

        [Required]
        [Display(Name = "Issue")]
        public string IssueText { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        public List<IFormFile> Attachments { get; set; }
    }

    public class IssueInboxViewModel
    {
        public List<Issue> ReceivedIssues { get; set; }
        public int UnreadCount { get; set; }
    }
    // Update the `Users` property in the `DbContext` class to be strongly typed to a collection of `User` objects.
    // This ensures that the `.Name` property can be accessed without errors.

    public class DbContext
    {
        public DbSet<Issue> Issues { get; set; }
        public DbSet<IssueAttachment> IssueAttachments { get; set; }
        public DbSet<User> Users { get; set; } // Changed from IEnumerable<object> to DbSet<User>
        internal void OnModelCreating(ModelBuilder modelBuilder) { }
        internal async Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }

    // Ensure the `Issue` class is defined with the required properties.
    public class Issue
    {
        public int IssueId { get; set; }
        public int ReceiverId { get; set; }
        public int SenderId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? IssueText { get; set; }
        public string Remarks { get; set; }
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public ICollection<IssueAttachment> Attachments { get; set; }
    }

    // Ensure the `IssueAttachment` class is defined.
    public class IssueAttachment
    {
        public int AttachmentId { get; set; }
        public int IssueId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int FileSize { get; set; }
        public DateTime UploadDate { get; set; }
    }

    // Ensure the `User` class is defined.
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}