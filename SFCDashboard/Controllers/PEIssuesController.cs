using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using SFCDashboard.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class PEIssuesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PEIssuesController> _logger;

    public PEIssuesController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<PEIssuesController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PEIssue model, IFormFile Attachment)
    {
        if (ModelState.IsValid)
        {
            // Save attachment if present
            if (Attachment != null && Attachment.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "issues", model.PlannedEventId.ToString());
                Directory.CreateDirectory(uploads);
                var filePath = Path.Combine(uploads, Attachment.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }
                model.AttachmentPath = $"/uploads/issues/{model.PlannedEventId}/{Attachment.FileName}";
            }

            // Set sender (current user)
            model.CreatedAt = DateTime.Now;
            _context.PEIssues.Add(model);

            // Change IsHold flag instead of directly changing status
            var pe = _context.PlannedEvents.FirstOrDefault(x => x.Id == model.PlannedEventId);
            if (pe != null)
            {
                pe.IsHold = true;
                // Keep PEStatus as "Hold" for backward compatibility
                
            }

            await _context.SaveChangesAsync();
            // Redirect or return success
            return RedirectToAction("Details", "PlannedEvents", new { id = model.PlannedEventId });
        }
        // If invalid, return to details
        return RedirectToAction("Details", "PlannedEvents", new { id = model.PlannedEventId });
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
    public async Task<IActionResult> Reply(int issueId, int plannedEventId, int senderId, int receiverId, 
        string replyText, IFormFile attachment)
    {
        try
        {
            var reply = new PEIssue
            {
                PlannedEventId = plannedEventId,
                PETaskId = (await _context.PEIssues.FindAsync(issueId))?.PETaskId ?? 0,
                SenderId = senderId,
                ReceiverId = receiverId,
                IssueText = replyText,
                CreatedAt = DateTime.Now,
                IsRead = false,
                IsReply = true,
                OriginalIssueId = issueId
            };

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
            TempData["ErrorMessage"] = $"Error sending reply: {ex.Message}";
            return RedirectToAction("Index", "PlannedEvents");
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsFixed(int issueId, int plannedEventId, string resolutionDetails)
    {
        try
        {
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
            return RedirectToAction("Details", "PlannedEvents", new { id = plannedEventId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error marking issue as fixed: {ex.Message}";
            return RedirectToAction("Index", "PlannedEvents");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmResolution(int resolutionId, bool isConfirmed)
    {
        _logger.LogWarning($"ConfirmResolution called with resolutionId: {resolutionId}, isConfirmed: {isConfirmed}");
        
        try
        {
            var resolution = await _context.PEIssueResolutions.FindAsync(resolutionId);
            if (resolution == null)
            {
                _logger.LogWarning($"Resolution not found for id: {resolutionId}");
                TempData["ErrorMessage"] = "Resolution not found.";
                return RedirectToAction("Index", "PlannedEvents");
            }

            resolution.IsConfirmed = isConfirmed;
            resolution.ConfirmedDate = DateTime.Now;
            _context.Update(resolution);

            if (isConfirmed)
            {
                // Update the parent Planned Event
                var pe = await _context.PlannedEvents.FindAsync(resolution.PlannedEventId);
                if (pe != null)
                {
                    _logger.LogWarning($"Changing PE status from hold to '{pe.PEStatus}'  for PE ID {pe.Id}");
                    
                    pe.IsHold = false; // Clear the hold flag
                    _context.Update(pe);
                    
                    _logger.LogWarning($"About to save PE {pe.Id} status change");
                }
                else
                {
                    _logger.LogWarning("PlannedEvent not found for ID: {peId}", resolution.PlannedEventId);
                }

                // Mark the original issue as resolved
                var issue = await _context.PEIssues.FindAsync(resolution.IssueId);
                if (issue != null)
                {
                    issue.IsResolved = true;
                    _context.Update(issue);
                }

                TempData["SuccessMessage"] = "Resolution confirmed. PE record status updated to ongoing.";
            }
            else
            {
                TempData["WarningMessage"] = "Resolution rejected. The issue will be sent back for further attention.";
            }

            _logger.LogWarning("About to call SaveChangesAsync");
            await _context.SaveChangesAsync();
            _logger.LogWarning("SaveChangesAsync completed");
            
            return RedirectToAction("Details", "PlannedEvents", new { id = resolution.PlannedEventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming resolution");
            TempData["ErrorMessage"] = $"Error confirming resolution: {ex.Message}";
            return RedirectToAction("Index", "PlannedEvents");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetResolution(int id)
    {
        try
        {
            var resolution = await _context.PEIssueResolutions
                .FirstOrDefaultAsync(r => r.IssueId == id && !r.IsConfirmed);
                
            if (resolution == null)
            {
                return Json(new { });
            }
            
            return Json(new { 
                id = resolution.Id,
                details = resolution.ResolutionDetails,
                date = resolution.ResolutionDate.ToString("yyyy-MM-dd HH:mm")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> TestSetOngoing(int peId)
    {
        var pe = await _context.PlannedEvents.FindAsync(peId);
        if (pe != null)
        {
            pe.PEStatus = "ongoing";
            await _context.SaveChangesAsync();
            return Content("Updated to ongoing");
        }
        return Content("Not found");
    }
}