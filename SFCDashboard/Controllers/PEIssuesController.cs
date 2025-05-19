using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using SFCDashboard.Data;

public class PEIssuesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PEIssuesController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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

            // Change PE status to Hold
            var pe = _context.PlannedEvents.FirstOrDefault(x => x.Id == model.PlannedEventId);
            if (pe != null)
            {
                pe.PEStatus = "Hold";
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
}