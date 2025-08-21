using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;
using SFCDashboard.Controllers;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Globalization;


public class ProjectController : BaseController
{
    private readonly IProjectsApiClient _projectsApiClient;

    public ProjectController(IProjectsApiClient projectsApiClient, IUsersApiClient usersApiClient) : base(usersApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectsApiClient.GetAllProjectsAsync();

        // Get current user permissions from backend
        var userName = User?.Identity?.Name;
        var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
            ? userName.Substring(0, 6)
            : string.Empty;

        var permissions = await _usersApiClient.GetProjectUserPermissionsAsync(serviceId);

        ViewBag.CanManageProjects = permissions?.CanManageProjects ?? false;
        ViewBag.CurrentUser = permissions?.CurrentUser;

        return View(projects);
    }

    public async Task<IActionResult> Search(string searchTerm, int projectId)
    {
        var results = await _projectsApiClient.SearchPlannedEventsAsync(searchTerm, projectId);
        return Json(new { items = results });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (ModelState.IsValid)
        {
            var success = await _projectsApiClient.CreateProjectAsync(project);
            if (success)
                return RedirectToAction(nameof(Index));
        }
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> AssignPEToProject(int plannedEventId, int projectId)
    {
        var success = await _projectsApiClient.AssignPEToProjectAsync(plannedEventId, projectId);
        if (!success)
            return BadRequest("Assignment failed");
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AssignMultiplePEsToProject([FromBody] MultipleAssignmentModel model)
    {
        if (model.PlannedEventIds == null || !model.PlannedEventIds.Any())
        {
            return BadRequest("No PEs selected");
        }
        var success = await _projectsApiClient.AssignMultiplePEsToProjectAsync(model.ProjectId, model.PlannedEventIds);
        if (!success)
            return BadRequest("Assignment failed");
        return Json(new { success = true });
    }

    public class MultipleAssignmentModel
    {
        public int ProjectId { get; set; }
        public List<int>? PlannedEventIds { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var projectDetails = await _projectsApiClient.GetProjectDetailsAsync(id);
        if (projectDetails == null)
        {
            return NotFound();
        }

        // Get current user permissions from backend
        var userName = User?.Identity?.Name;
        var serviceId = !string.IsNullOrEmpty(userName) && userName.Length >= 6
            ? userName.Substring(0, 6)
            : string.Empty;

        var permissions = await _usersApiClient.GetProjectUserPermissionsAsync(serviceId);

        ViewBag.CanManageProjects = permissions?.CanManageProjects ?? false;
        ViewBag.CurrentUser = permissions?.CurrentUser;

        var vm = new ProjectDetailsViewModel
        {
            Id = projectDetails.Id,
            ProjectName = projectDetails.ProjectName,
            CreatedDate = projectDetails.CreatedDate,
            ProjectPEs = projectDetails.ProjectPEs.Select(pe => new ProjectPEViewModel
            {
                Id = pe.Id,
                PlannedEventId = pe.PlannedEventId,
                PlannedEvent = pe.PlannedEvent,
                CurrentTask = pe.CurrentTask
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> RemovePEFromProject(int plannedEventId, int projectId)
    {
        var success = await _projectsApiClient.RemovePEFromProjectAsync(plannedEventId, projectId);
        if (!success)
            return NotFound("Mapping not found");
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _projectsApiClient.DeleteProjectAsync(id);
        if (!success)
            return NotFound("Project not found");
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> ExportToExcel(int id)
    {
        var projectDetails = await _projectsApiClient.GetProjectDetailsAsync(id);
        if (projectDetails == null)
        {
            return NotFound();
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Assigned PEs");

        // Set up headers
        worksheet.Cell(1, 1).Value = "Status";
        worksheet.Cell(1, 2).Value = "PE Number";
        worksheet.Cell(1, 3).Value = "Customer";
        worksheet.Cell(1, 4).Value = "Job Reference";
        worksheet.Cell(1, 5).Value = "Required Date";
        worksheet.Cell(1, 6).Value = "Current Task";
        worksheet.Cell(1, 7).Value = "Current WG";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

        // Add data
        var row = 2;
        foreach (var pe in projectDetails.ProjectPEs)
        {
            // Create view model to get status
            var viewModel = new ProjectPEViewModel
            {
                Id = pe.Id,
                PlannedEventId = pe.PlannedEventId,
                PlannedEvent = pe.PlannedEvent,
                CurrentTask = pe.CurrentTask
            };

            worksheet.Cell(row, 1).Value = viewModel.StatusDisplayText;
            worksheet.Cell(row, 2).Value = pe.PlannedEvent.PeNumber;
            worksheet.Cell(row, 3).Value = pe.PlannedEvent.Customer ?? "";
            worksheet.Cell(row, 4).Value = pe.PlannedEvent.JobReference ?? "";
            worksheet.Cell(row, 5).Value = pe.PlannedEvent.ServiceRequiredDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 6).Value = pe.CurrentTask ?? "No Active Task";
            worksheet.Cell(row, 7).Value = pe.PlannedEvent.TaskWg ?? "Not Assigned";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add project info at the top
        worksheet.Row(1).InsertRowsAbove(2);
        worksheet.Cell(1, 1).Value = $"Project: {projectDetails.ProjectName}";
        worksheet.Cell(2, 1).Value = $"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        // Create memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"{projectDetails.ProjectName}_AssignedPEs_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportToPdf(int id)
    {
        var projectDetails = await _projectsApiClient.GetProjectDetailsAsync(id);
        if (projectDetails == null)
        {
            return NotFound();
        }

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
        var writer = PdfWriter.GetInstance(document, stream);

        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

        document.Add(new Paragraph($"Project: {projectDetails.ProjectName}", titleFont));
        document.Add(new Paragraph($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}", normalFont));
        document.Add(new Paragraph(" ")); // Empty line

        // Create table
        var table = new PdfPTable(7);
        table.WidthPercentage = 100;
        table.SetWidths(new float[] { 12, 15, 23, 18, 13, 13, 6 });

        // Add headers
        table.AddCell(new PdfPCell(new Phrase("Status", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("PE Number", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("Customer", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("Job Reference", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("Required Date", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("Current Task", headerFont)) { BackgroundColor = BaseColor.LightGray });
        table.AddCell(new PdfPCell(new Phrase("Current WG", headerFont)) { BackgroundColor = BaseColor.LightGray });

        // Add data
        foreach (var pe in projectDetails.ProjectPEs)
        {
            // Create view model to get status
            var viewModel = new ProjectPEViewModel
            {
                Id = pe.Id,
                PlannedEventId = pe.PlannedEventId,
                PlannedEvent = pe.PlannedEvent,
                CurrentTask = pe.CurrentTask
            };

            table.AddCell(new Phrase(viewModel.StatusDisplayText, normalFont));
            table.AddCell(new Phrase(pe.PlannedEvent.PeNumber, normalFont));
            table.AddCell(new Phrase(pe.PlannedEvent.Customer ?? "", normalFont));
            table.AddCell(new Phrase(pe.PlannedEvent.JobReference ?? "", normalFont));
            table.AddCell(new Phrase(pe.PlannedEvent.ServiceRequiredDate?.ToString("yyyy-MM-dd") ?? "", normalFont));
            table.AddCell(new Phrase(pe.CurrentTask ?? "No Active Task", normalFont));
            table.AddCell(new Phrase(pe.PlannedEvent.TaskWg ?? "Not Assigned", normalFont));
        }

        document.Add(table);
        document.Close();

        var fileName = $"{projectDetails.ProjectName}_AssignedPEs_{DateTime.Now:yyyyMMdd}.pdf";
        return File(stream.ToArray(), "application/pdf", fileName);
    }
}