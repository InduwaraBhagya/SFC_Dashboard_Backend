using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;
using System.Text;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace SFCDashboard.Controllers
{
    public class ReportsController : BaseController
    {
    private readonly ILogger<ReportsController> _logger;
    private readonly IPlannedEventsApiClient _plannedEventsApiClient;

        public ReportsController(
            ILogger<ReportsController> logger,
            IPlannedEventsApiClient plannedEventsApiClient,
            IUsersApiClient usersApiClient)
            : base(usersApiClient)
        {
            _logger = logger;
            _plannedEventsApiClient = plannedEventsApiClient;
        }

        // GET: Reports/Generate
        public async Task<IActionResult> Generate()
        {
            try
            {
                _logger.LogInformation("Loading Planned Events for report generation");

                // Get all Planned Events from API
                var plannedEvents = (await _plannedEventsApiClient.GetPlannedEventsAsync()).ToList();

                // Get unique values for filter dropdowns
                ViewBag.Provinces = plannedEvents.Select(x => x.Province).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                ViewBag.Regions = plannedEvents.Select(x => x.Region).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                ViewBag.RTOMs = plannedEvents.Select(x => x.Rtom).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                ViewBag.ContractorNames = plannedEvents.Select(x => x.ContractorName).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                ViewBag.PENumbers = plannedEvents.Select(x => x.PeNumber).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                ViewBag.Customers = plannedEvents.Select(x => x.Customer).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();

                return View(plannedEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                TempData["Error"] = "An error occurred while loading the reports page.";
                return View(new List<PlannedEvent>());
            }
        }

        // POST: Reports/FilterRecords - AJAX endpoint for filtering
        [HttpPost]
        public async Task<IActionResult> FilterRecords(string province = "", string region = "", string rtom = "",
            string contractorName = "", string peNumber = "", string customer = "", bool urgentOnly = false)
        {
            try
            {
                var all = (await _plannedEventsApiClient.GetPlannedEventsAsync()).ToList();
                var filtered = ApplyPlannedEventFilters(all, province, region, rtom, contractorName, peNumber, customer, urgentOnly);
                return Json(new { success = true, data = filtered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering records");
                return Json(new { success = false, message = "An error occurred while filtering records." });
            }
        }

        // GET: Reports/ExportPDF
        public async Task<IActionResult> ExportPDF(string province = "", string region = "", string rtom = "",
            string contractorName = "", string peNumber = "", string customer = "", bool urgentOnly = false)
        {
            try
            {
                var filteredRecords = await GetFilteredPlannedEvents(province, region, rtom, contractorName, peNumber, customer, urgentOnly);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var title = new Paragraph("Planned Events Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Add metadata
                var metaFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var meta = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Total Records: {filteredRecords.Count}", metaFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(meta);

                // Create table
                var table = new PdfPTable(11)
                {
                    WidthPercentage = 100
                };

                // Set column widths
                table.SetWidths(new float[] { 5, 8, 8, 8, 12, 10, 12, 10, 15, 12, 10 });

                // Add headers
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
                var headers = new[] { "ID", "Province", "Region", "RTOM", "Contractor", "SO Number", "Customer", "PE Number", "PE Title", "Task Name", "Task Workgroup" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = new BaseColor(200, 200, 200),
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(cell);
                }

                // Add data
                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
                foreach (var record in filteredRecords)
                {
                    table.AddCell(new PdfPCell(new Phrase(record.Id.ToString(), dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.Province ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.Region ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.Rtom ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.ContractorName ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.SoNumber ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.Customer ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.PeNumber ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.PeTitle ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.TaskName ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.TaskWg ?? "", dataFont)) { Padding = 3 });
                }

                document.Add(table);
                document.Close();

                return File(stream.ToArray(), "application/pdf", $"Planned_Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PDF");
                TempData["Error"] = "An error occurred while exporting PDF.";
                return RedirectToAction("Generate");
            }
        }        // GET: Reports/ExportExcel
        public async Task<IActionResult> ExportExcel(string province = "", string region = "", string rtom = "",
            string contractorName = "", string peNumber = "", string customer = "", bool urgentOnly = false)
        {
            try
            {
                var filteredRecords = await GetFilteredPlannedEvents(province, region, rtom, contractorName, peNumber, customer, urgentOnly);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Planned Events Report");

                // Headers
                var headers = new[] { "ID", "Province", "Region", "RTOM", "Contractor Name", "SO Number", "Customer", "PE Number", "PE Title", "Task Name", "Task Workgroup" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Data
                for (int i = 0; i < filteredRecords.Count; i++)
                {
                    var record = filteredRecords[i];
                    worksheet.Cell(i + 2, 1).Value = record.Id;
                    worksheet.Cell(i + 2, 2).Value = record.Province ?? "";
                    worksheet.Cell(i + 2, 3).Value = record.Region ?? "";
                    worksheet.Cell(i + 2, 4).Value = record.Rtom ?? "";
                    worksheet.Cell(i + 2, 5).Value = record.ContractorName ?? "";
                    worksheet.Cell(i + 2, 6).Value = record.SoNumber ?? "";
                    worksheet.Cell(i + 2, 7).Value = record.Customer ?? "";
                    worksheet.Cell(i + 2, 8).Value = record.PeNumber ?? "";
                    worksheet.Cell(i + 2, 9).Value = record.PeTitle ?? "";
                    worksheet.Cell(i + 2, 10).Value = record.TaskName ?? "";
                    worksheet.Cell(i + 2, 11).Value = record.TaskWg ?? "";
                }

                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Planned_Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel");
                TempData["Error"] = "An error occurred while exporting Excel.";
                return RedirectToAction("Generate");
            }
        }

        // GET: Reports/ExportPNG - Export filtered table as PNG image
        public async Task<IActionResult> ExportPNG(string province = "", string region = "", string rtom = "",
            string contractorName = "", string peNumber = "", string customer = "", bool urgentOnly = false)
        {
            try
            {
                var filteredRecords = await GetFilteredPlannedEvents(province, region, rtom, contractorName, peNumber, customer, urgentOnly);

                // Calculate image dimensions
                const int headerHeight = 80;
                const int rowHeight = 25;
                const int padding = 20;
                const int columnWidth = 120;
                const int totalColumns = 11;

                int imageWidth = (totalColumns * columnWidth) + (padding * 2);
                int imageHeight = headerHeight + (filteredRecords.Count * rowHeight) + (padding * 2) + 60; // Extra space for title

                using var surface = SKSurface.Create(new SKImageInfo(imageWidth, imageHeight));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // Create paints
                var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                    IsAntialias = true
                };

                var headerPaint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 12,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                    IsAntialias = true
                };

                var dataPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 10,
                    Typeface = SKTypeface.FromFamilyName("Arial"),
                    IsAntialias = true
                };

                var headerBackgroundPaint = new SKPaint
                {
                    Color = SKColors.DarkBlue,
                    IsAntialias = true
                };

                var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    IsAntialias = true
                };

                // Draw title
                var title = "Planned Events Report";
                var titleBounds = new SKRect();
                titlePaint.MeasureText(title, ref titleBounds);
                canvas.DrawText(title, (imageWidth - titleBounds.Width) / 2, padding + titleBounds.Height, titlePaint);

                // Draw subtitle
                var subtitle = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Records: {filteredRecords.Count}";
                var subtitlePaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 12,
                    IsAntialias = true
                };
                var subtitleBounds = new SKRect();
                subtitlePaint.MeasureText(subtitle, ref subtitleBounds);
                canvas.DrawText(subtitle, (imageWidth - subtitleBounds.Width) / 2, padding + titleBounds.Height + 30, subtitlePaint);

                // Draw headers
                var headers = new[] { "ID", "Province", "Region", "RTOM", "Contractor", "SO Number", "Customer", "PE Number", "PE Title", "Task Name", "Task Workgroup" };
                var startY = padding + 60;

                for (int i = 0; i < headers.Length; i++)
                {
                    var rect = new SKRect(padding + (i * columnWidth), startY, padding + ((i + 1) * columnWidth), startY + headerHeight);
                    canvas.DrawRect(rect, headerBackgroundPaint);
                    canvas.DrawRect(rect, borderPaint);

                    var textBounds = new SKRect();
                    headerPaint.MeasureText(headers[i], ref textBounds);
                    canvas.DrawText(headers[i], rect.Left + 5, rect.Top + (headerHeight / 2) + (textBounds.Height / 2), headerPaint);
                }

                // Draw data rows
                for (int rowIndex = 0; rowIndex < filteredRecords.Count; rowIndex++)
                {
                    var record = filteredRecords[rowIndex];
                    var rowY = startY + headerHeight + (rowIndex * rowHeight);

                    var values = new[] {
                        record.Id.ToString(),
                        record.Province ?? "",
                        record.Region ?? "",
                        record.Rtom ?? "",
                        record.ContractorName ?? "",
                        record.SoNumber ?? "",
                        record.Customer ?? "",
                        record.PeNumber ?? "",
                        record.PeTitle ?? "",
                        record.TaskName ?? "",
                        record.TaskWg ?? ""
                    };

                    for (int colIndex = 0; colIndex < values.Length; colIndex++)
                    {
                        var rect = new SKRect(padding + (colIndex * columnWidth), rowY, padding + ((colIndex + 1) * columnWidth), rowY + rowHeight);
                        canvas.DrawRect(rect, borderPaint);

                        var text = values[colIndex];
                        if (text.Length > 15) text = text.Substring(0, 12) + "...";

                        canvas.DrawText(text, rect.Left + 5, rect.Top + (rowHeight / 2) + 5, dataPaint);
                    }
                }

                // Generate image
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream();
                data.SaveTo(stream);

                return File(stream.ToArray(), "image/png", $"Planned_Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PNG");
                TempData["Error"] = "An error occurred while exporting PNG.";
                return RedirectToAction("Generate");
            }
        }
        private async Task<List<PlannedEvent>> GetFilteredPlannedEvents(string province, string region, string rtom,
            string contractorName, string peNumber, string customer, bool urgentOnly)
        {
            var all = (await _plannedEventsApiClient.GetPlannedEventsAsync()).ToList();
            return ApplyPlannedEventFilters(all, province, region, rtom, contractorName, peNumber, customer, urgentOnly);
        }

        private static List<PlannedEvent> ApplyPlannedEventFilters(List<PlannedEvent> source, string province, string region, string rtom,
            string contractorName, string peNumber, string customer, bool urgentOnly)
        {
            IEnumerable<PlannedEvent> query = source;

            if (!string.IsNullOrWhiteSpace(province))
                query = query.Where(x => (x.Province ?? string.Empty).Contains(province, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(x => (x.Region ?? string.Empty).Contains(region, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(rtom))
                query = query.Where(x => (x.Rtom ?? string.Empty).Contains(rtom, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(contractorName))
                query = query.Where(x => (x.ContractorName ?? string.Empty).Contains(contractorName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(peNumber))
                query = query.Where(x => (x.PeNumber ?? string.Empty).Contains(peNumber, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(customer))
                query = query.Where(x => (x.Customer ?? string.Empty).Contains(customer, StringComparison.OrdinalIgnoreCase));
            if (urgentOnly)
                query = query.Where(x => string.Equals(x.PEStatus ?? string.Empty, "URGENT", StringComparison.OrdinalIgnoreCase));

            return query.ToList();
        }

        private string GenerateReportContent(List<PlannedEvent> records, string format)
        {
            var content = new StringBuilder();
            content.AppendLine($"PE Records Report - {format} Export");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Total Records: {records.Count}");
            content.AppendLine(new string('=', 80));
            content.AppendLine();

            foreach (var record in records)
            {
                content.AppendLine($"ID: {record.Id}");
                content.AppendLine($"Province: {record.Province}");
                content.AppendLine($"Region: {record.Region}");
                content.AppendLine($"RTOM: {record.Rtom}");
                content.AppendLine($"Contractor: {record.ContractorName}");
                content.AppendLine($"SO Number: {record.SoNumber}");
                content.AppendLine($"Customer: {record.Customer}");
                content.AppendLine($"PE Number: {record.PeNumber}");
                content.AppendLine($"PE Title: {record.PeTitle}");
                content.AppendLine(new string('-', 40));
            }

            return content.ToString();
        }
    }
}
