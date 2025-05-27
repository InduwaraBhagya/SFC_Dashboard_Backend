using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        #region General Issue Management

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

        // GET: Issues/Create - General issue creation form
        public async Task<IActionResult> Create()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, Name = $"{u.Name} ({u.ServiceId})" })
                .ToListAsync();
            ViewBag.Users = users;
            return View(new IssueCreateViewModel());
        }

        // POST: Issues/Create - General issue creation
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
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction("Index", "Home");
            }

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

            try
            {
                _context.PEIssues.Add(issue);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Issue created by user {UserName} ({UserId}) for receiver {ReceiverId}", 
                    currentUser.Name, currentUser.Id, model.ReceiverId);
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue for user {UserId}", currentUser.Id);
                ModelState.AddModelError("", "An error occurred while creating the issue.");
                await PopulateUsersAsync();
                return View(model);
            }
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
                .Where(i => i.ReceiverId == currentUser.Id)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id,
                    SenderId = i.SenderId,
                    SenderName = _context.Users
                        .Where(u => u.Id == i.SenderId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown",
                    ReceiverId = i.ReceiverId,
                    ReceiverName = currentUser.Name,
                    IssueText = i.IssueText,
                    AttachmentPath = i.AttachmentPath,
                    CreatedAt = i.CreatedAt,
                    PlannedEventId = i.PlannedEventId,
                    IsRead = i.IsRead,
                    IsReply = i.IsReply,
                    OriginalIssueId = i.OriginalIssueId,
                    IsResolved = i.IsResolved,
                    IsResolutionRequest = i.IsResolutionRequest,
                    PETaskId = i.PETaskId
                })
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
                .Where(i => i.SenderId == currentUser.Id)
                .Select(i => new PEIssueViewModel
                {
                    Id = i.Id,
                    SenderId = currentUser.Id,
                    SenderName = currentUser.Name,
                    ReceiverId = i.ReceiverId,
                    ReceiverName = _context.Users
                        .Where(u => u.Id == i.ReceiverId)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown",
                    IssueText = i.IssueText,
                    AttachmentPath = i.AttachmentPath,
                    CreatedAt = i.CreatedAt,
                    PlannedEventId = i.PlannedEventId,
                    IsRead = i.IsRead,
                    IsReply = i.IsReply,
                    OriginalIssueId = i.OriginalIssueId,
                    IsResolved = i.IsResolved,
                    IsResolutionRequest = i.IsResolutionRequest,
                    PETaskId = i.PETaskId
                })
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        // POST: Issues/ReplyToIssue
        [HttpPost]
        public async Task<IActionResult> ReplyToIssue(int issueId, string replyText)
        {
            // Get the original issue
            var originalIssue = await _context.PEIssues.FindAsync(issueId);
            if (originalIssue == null)
            {
                return NotFound();
            }

            // Get current user ID for sender
            var currentUserId = await GetCurrentUserIdAsync();
            
            // Create the reply
            var reply = new PEIssue
            {
                SenderId = currentUserId,                   // Current user is the sender
                ReceiverId = originalIssue.SenderId,        // Original sender becomes the receiver
                PlannedEventId = originalIssue.PlannedEventId,
                PETaskId = originalIssue.PETaskId,
                IssueText = replyText,
                CreatedAt = DateTime.Now,
                IsRead = false,
                IsReply = true,
                OriginalIssueId = issueId                   // Link to the original issue
            };
            
            _context.PEIssues.Add(reply);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Details", "PlannedEvents", new { id = originalIssue.PlannedEventId });
        }

        #endregion

        #region PE-Specific Issue Management

        // GET: Issues/CreateForPE - New Create action for PE Issues 
        public IActionResult CreateForPE(int plannedEventId, int? taskId = null)
        {
            var model = new PEIssue
            {
                PlannedEventId = plannedEventId,
                PETaskId = taskId ?? 0
            };
            
            return View(model);
        }
        
        // POST: Issues/CreateForPE - PE-specific issue creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForPE(PEIssue model, IFormFile Attachment)
        {
            try
            {
                _logger.LogInformation("CreateForPE called with model: {PE}, {Text}, {Receiver}, {Attachment}",
                    model.PlannedEventId, model.IssueText, model.ReceiverId,
                    Attachment != null ? Attachment.FileName : "No attachment");
                
                if (model.IssueText == null || string.IsNullOrWhiteSpace(model.IssueText))
                {
                    _logger.LogWarning("Issue text is empty or null");
                    TempData["ErrorMessage"] = "Issue text cannot be empty";
                    return RedirectToAction("Details", "PlannedEvents", new { id = model.PlannedEventId });
                }

                // Process attachment only if one was provided
                if (Attachment != null && Attachment.Length > 0)
                {
                    _logger.LogInformation("Processing attachment: {FileName}, Size: {Size}KB", 
                        Attachment.FileName, Attachment.Length / 1024);
                
                    try
                    {
                        string uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "issues", model.PlannedEventId.ToString());
                        Directory.CreateDirectory(uploadsDir);
                
                        var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(Attachment.FileName)}";
                        var filePath = Path.Combine(uploadsDir, uniqueFileName);
                
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await Attachment.CopyToAsync(stream);
                        }
                
                        model.AttachmentPath = $"/uploads/issues/{model.PlannedEventId}/{uniqueFileName}";
                        _logger.LogInformation("Attachment saved to: {Path}", model.AttachmentPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing attachment");
                        // Continue without attachment rather than failing the whole request
                    }
                }
        
                // Set creation time and user info
                model.CreatedAt = DateTime.Now;
        
                var currentUser = await GetCurrentUserAsync();
                if (currentUser != null)
                {
                    model.SenderId = currentUser.Id;
                }
        
                // Add the issue
                _context.PEIssues.Add(model);
        
                // Set the PE on hold
                var pe = await _context.PlannedEvents.FindAsync(model.PlannedEventId);
                if (pe != null)
                {
                    pe.IsHold = true;
                    _context.Update(pe);
                }

                await _context.SaveChangesAsync();
        
                TempData["SuccessMessage"] = "Issue reported successfully!";
                return RedirectToAction("Details", "PlannedEvents", new { id = model.PlannedEventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateForPE");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", "PlannedEvents", new { id = model.PlannedEventId });
            }
        }

        // For AJAX: Get issue suggestions for a PE/tasklist
        [HttpGet]
        public IActionResult GetIssueSuggestions(int peId, int taskListId)
        {
            var issues = _context.PEIssues
                .Where(i => i.PlannedEventId == peId && i.PETaskId == taskListId)
                .Select(i => new { text = i.IssueText })
                .Distinct()
                .ToList();
            return Json(issues);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return NotFound();
                }

                // Mark as read
                if (!issue.IsRead)
                {
                    issue.IsRead = true;
                    _context.Update(issue);
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int issueId, int plannedEventId, int receiverId, 
            string replyText, IFormFile attachment)
        {
            try
            {
                // Get the current user ID for the sender
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Unable to determine current user.";
                    return RedirectToAction("Details", "PlannedEvents", new { id = plannedEventId });
                }
                
                // Create the reply with the current user as the sender
                var reply = new PEIssue
                {
                    PlannedEventId = plannedEventId,
                    PETaskId = (await _context.PEIssues.FindAsync(issueId))?.PETaskId ?? 0,
                    SenderId = currentUser.Id, // Use the current user's ID
                    ReceiverId = receiverId,
                    IssueText = replyText,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsReply = true,
                    OriginalIssueId = issueId
                };

                // Handle attachment if provided
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues", plannedEventId.ToString());
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(attachment.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(stream);
                    }
                    
                    reply.AttachmentPath = $"/uploads/issues/{plannedEventId}/{uniqueFileName}";
                }

                _context.PEIssues.Add(reply);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Reply sent successfully.";
                return RedirectToAction("Details", "PlannedEvents", new { id = plannedEventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reply: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error sending reply: {ex.Message}";
                return RedirectToAction("Details", "PlannedEvents", new { id = plannedEventId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsFixed(int issueId, int plannedEventId, string resolutionDetails)
        {
            try
            {
                // Log the request for debugging
                _logger.LogInformation("MarkAsFixed called with issueId: {IssueId}, PE: {PlannedEventId}, details: {Details}", 
                    issueId, plannedEventId, resolutionDetails);
                    
                var issue = await _context.PEIssues.FindAsync(issueId);
                if (issue == null)
                {
                    TempData["ErrorMessage"] = "Issue not found.";
                    return RedirectToAction("Index", "PlannedEvents");
                }

                // Create a resolution confirmation request
                var resolutionRequest = new PEIssueResolution
                {
                    IssueId = issueId,
                    ResolutionDetails = resolutionDetails,
                    ResolutionDate = DateTime.Now,
                    IsConfirmed = false,
                    ConfirmationRequestedDate = DateTime.Now,
                    PlannedEventId = plannedEventId
                };

                _context.PEIssueResolutions.Add(resolutionRequest);

                // Send a notification to the original reporter
                var notification = new PEIssue
                {
                    PlannedEventId = plannedEventId,
                    PETaskId = issue.PETaskId,
                    SenderId = issue.ReceiverId, // Current user who fixed it
                    ReceiverId = issue.SenderId, // Original reporter
                    IssueText = $"RESOLUTION REQUEST: {resolutionDetails}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsReply = true,
                    OriginalIssueId = issueId,
                    IsResolutionRequest = true
                };

                _context.PEIssues.Add(notification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Issue marked as fixed. A confirmation request has been sent to the reporter.";
                
                // Return to the PE details page or index based on where the request came from
                if (Request.Headers["Referer"].ToString().Contains("Details"))
                {
                    return RedirectToAction("Details", "PlannedEvents", new { id = plannedEventId });
                }
                
                return RedirectToAction("Index", "PlannedEvents");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue as fixed: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error marking issue as fixed: {ex.Message}";
                return RedirectToAction("Index", "PlannedEvents");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmResolution(int resolutionId, bool isConfirmed)
        {
            _logger.LogInformation($"ConfirmResolution called with resolutionId: {resolutionId}, isConfirmed: {isConfirmed}");
            
            try
            {
                var resolution = await _context.PEIssueResolutions.FindAsync(resolutionId);
                if (resolution == null)
                {
                    _logger.LogWarning($"Resolution not found for id: {resolutionId}");
                    TempData["ErrorMessage"] = "Resolution not found.";
                    return RedirectToAction("Index", "PlannedEvents");
                }

                var issue = await _context.PEIssues.FindAsync(resolution.IssueId);
                if (issue == null)
                {
                    _logger.LogWarning($"Issue not found for resolution id: {resolutionId}");
                    TempData["ErrorMessage"] = "Issue not found.";
                    return RedirectToAction("Index", "PlannedEvents");
                }

                var pe = await _context.PlannedEvents.FindAsync(resolution.PlannedEventId);

                if (isConfirmed)
                {
                    // Update resolution status
                    resolution.IsConfirmed = true;
                    resolution.ConfirmedDate = DateTime.Now;
                    _context.Update(resolution);
                    
                    // Mark issue as resolved
                    if (issue != null)
                    {
                        issue.IsResolved = true;
                        _context.Update(issue);
                        _logger.LogInformation($"Issue {issue.Id} marked as resolved");
                    }
                        var resolutionRequestMessage = await _context.PEIssues
        .FirstOrDefaultAsync(i => i.IsResolutionRequest && 
                               i.OriginalIssueId == issue.Id);
                               
    if (resolutionRequestMessage != null)
    {
        // Instead of removing, mark it as hidden from inbox
        resolutionRequestMessage.IsHiddenFromInbox = true;
        _context.Update(resolutionRequestMessage);
        _logger.LogInformation($"Resolution request message {resolutionRequestMessage.Id} hidden from inbox");
    }
                    // Update planned event - ONLY if no other active issues remain
                    if (pe != null)
                    {
                        // Check if all issues are now resolved before removing hold status
                        var hasOtherActiveIssues = await _context.PEIssues
                            .AnyAsync(i => i.PlannedEventId == pe.Id && 
                                      !i.IsResolved && 
                                      i.OriginalIssueId == null && 
                                      i.Id != issue.Id);  // Exclude the current issue

                        if (!hasOtherActiveIssues)
                        {
                            pe.IsHold = false;  // Just set IsHold to false, don't change status
                            _context.Update(pe);
                            _logger.LogInformation($"PE {pe.Id} removed from hold status as all issues are resolved");
                        }
                        else
                        {
                            _logger.LogInformation($"PE {pe.Id} remains on hold due to other unresolved issues");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"PE not found for resolution id: {resolutionId}");
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Resolution confirmed and issue marked as resolved.";
                }
                else
                {
                    // If rejected, delete the resolution request and create a notification
                    _context.Remove(resolution);
                    
                    // Notify the user who attempted to fix the issue
                    var notification = new PEIssue
                    {
                        PlannedEventId = resolution.PlannedEventId,
                        PETaskId = issue.PETaskId,
                        SenderId = issue.SenderId, // Original reporter
                        ReceiverId = issue.ReceiverId, // User who tried to fix it
                        IssueText = $"RESOLUTION REJECTED: The fix was not accepted. Please try again.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        IsReply = true,
                        OriginalIssueId = issue.Id
                    };
                    
                    _context.PEIssues.Add(notification);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Resolution rejected. The responder has been notified.";
                }

                return RedirectToAction("Details", "PlannedEvents", new { id = resolution.PlannedEventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmResolution");
                TempData["ErrorMessage"] = $"Error processing resolution: {ex.Message}";
                return RedirectToAction("Index", "PlannedEvents");
            }
        }

        [HttpGet]
public async Task<IActionResult> GetResolution(int id)
{
    try
    {
        // Try by direct ID first
        var resolution = await _context.PEIssueResolutions
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsConfirmed);
        
        // If not found, try by issue ID
        if (resolution == null)
        {
            resolution = await _context.PEIssueResolutions
                .FirstOrDefaultAsync(r => r.IssueId == id && !r.IsConfirmed);
        }
        
        if (resolution == null)
        {
            _logger.LogWarning($"Resolution not found for ID or IssueID: {id}");
            return NotFound(new { error = "No resolution found for this issue" });
        }
        
        _logger.LogInformation($"Resolution found: ID={resolution.Id}, IssueID={resolution.IssueId}, Details={resolution.ResolutionDetails}");
        
        return Json(new { 
            id = resolution.Id,
            issueId = resolution.IssueId,
            details = resolution.ResolutionDetails,
            date = resolution.ResolutionDate
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error retrieving resolution for ID: {id}");
        return StatusCode(500, new { error = "An error occurred while retrieving the resolution" });
    }
}

        [HttpGet]
public async Task<IActionResult> GetResolutionById(int id)
{
    try
    {
        var resolution = await _context.PEIssueResolutions.FindAsync(id);
        
        if (resolution == null)
        {
            _logger.LogWarning($"Resolution not found for ID: {id}");
            return NotFound(new { error = "Resolution not found" });
        }
        
        return Json(new { 
            id = resolution.Id,
            issueId = resolution.IssueId,
            details = resolution.ResolutionDetails,
            date = resolution.ResolutionDate
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error retrieving resolution for ID: {id}");
        return StatusCode(500, new { error = "An error occurred while retrieving the resolution" });
    }
}

        #endregion

        #region Helper Methods

        private async Task<SystemUser?> GetCurrentUserAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(email));
            return user;
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Id ?? 0;
        }

        private async Task PopulateUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, Name = $"{u.Name} ({u.ServiceId})" })
                .ToListAsync();
            ViewBag.Users = users;
        }

        private string ExtractServiceId(string email)
        {
            // Extract service ID from email (whatever logic you're using)
            if (string.IsNullOrEmpty(email)) return string.Empty;
            
            // Assuming email format is name@domain.com or serviceId@domain.com
            return email.Split('@').FirstOrDefault() ?? string.Empty;
        }

        #endregion
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