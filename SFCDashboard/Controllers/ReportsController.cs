using Microsoft.AspNetCore.Mvc;
using SFCDB.Models;
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
        private readonly IPERecordsApiClient _peRecordsApiClient;

        public ReportsController(
            ILogger<ReportsController> logger,
            IPERecordsApiClient peRecordsApiClient,
            IUsersApiClient usersApiClient)
            : base(usersApiClient)
        {
            _logger = logger;
            _peRecordsApiClient = peRecordsApiClient;
        }

        // GET: Reports/Generate
        public async Task<IActionResult> Generate()
        {
            try
            {
                _logger.LogInformation("Loading PE Records for report generation");

                // Get PE Records from API
                var response = await _peRecordsApiClient.GetPERecordsAsync(1, 10000); // Get large number for filtering

                if (response.Success && response.Data != null)
                {
                    var peRecords = response.Data.Items;

                    // Get unique values for filter dropdowns
                    ViewBag.Provinces = peRecords.Select(x => x.PROVINCE).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                    ViewBag.Regions = peRecords.Select(x => x.REGION).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                    ViewBag.RTOMs = peRecords.Select(x => x.RTOM).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                    ViewBag.ContractorNames = peRecords.Select(x => x.CONTRACTOR_NAME).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                    ViewBag.SONumbers = peRecords.Select(x => x.SO_NUMBER).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                    ViewBag.Customers = peRecords.Select(x => x.CUSTOMER).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();

                    return View(peRecords);
                }
                else
                {
                    TempData["Error"] = response.Message ?? "Failed to retrieve PE Records.";
                    return View(new List<PERecord>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                TempData["Error"] = "An error occurred while loading the reports page.";
                return View(new List<PERecord>());
            }
        }

        // POST: Reports/FilterRecords - AJAX endpoint for filtering
        [HttpPost]
        public async Task<IActionResult> FilterRecords(string province = "", string region = "", string rtom = "",
            string contractorName = "", string soNumber = "", string customer = "")
        {
            try
            {
                var response = await _peRecordsApiClient.GetFilteredPERecordsAsync(
                    string.IsNullOrWhiteSpace(province) ? null : province,
                    string.IsNullOrWhiteSpace(region) ? null : region,
                    string.IsNullOrWhiteSpace(rtom) ? null : rtom,
                    string.IsNullOrWhiteSpace(contractorName) ? null : contractorName,
                    string.IsNullOrWhiteSpace(soNumber) ? null : soNumber,
                    string.IsNullOrWhiteSpace(customer) ? null : customer
                );

                if (response.Success && response.Data != null)
                {
                    return Json(new { success = true, data = response.Data });
                }

                return Json(new { success = false, message = response.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering records");
                return Json(new { success = false, message = "An error occurred while filtering records." });
            }
        }

        // GET: Reports/ExportPDF
        public async Task<IActionResult> ExportPDF(string province = "", string region = "", string rtom = "",
            string contractorName = "", string soNumber = "", string customer = "")
        {
            try
            {
                var filteredRecords = await GetFilteredRecords(province, region, rtom, contractorName, soNumber, customer);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var title = new Paragraph("PE Records Report", titleFont)
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
                    table.AddCell(new PdfPCell(new Phrase(record.ID.ToString(), dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.PROVINCE ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.REGION ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.RTOM ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.CONTRACTOR_NAME ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.SO_NUMBER ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.CUSTOMER ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.PE_NUMBER ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.PE_TITLE ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.TASK_NAME ?? "", dataFont)) { Padding = 3 });
                    table.AddCell(new PdfPCell(new Phrase(record.TASK_WG ?? "", dataFont)) { Padding = 3 });
                }

                document.Add(table);
                document.Close();

                return File(stream.ToArray(), "application/pdf", $"PE_Records_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PDF");
                TempData["Error"] = "An error occurred while exporting PDF.";
                return RedirectToAction("Generate");
            }
        }        // GET: Reports/ExportExcel
        public async Task<IActionResult> ExportExcel(string province = "", string region = "", string rtom = "",
            string contractorName = "", string soNumber = "", string customer = "")
        {
            try
            {
                var filteredRecords = await GetFilteredRecords(province, region, rtom, contractorName, soNumber, customer);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("PE Records Report");

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
                    worksheet.Cell(i + 2, 1).Value = record.ID;
                    worksheet.Cell(i + 2, 2).Value = record.PROVINCE ?? "";
                    worksheet.Cell(i + 2, 3).Value = record.REGION ?? "";
                    worksheet.Cell(i + 2, 4).Value = record.RTOM ?? "";
                    worksheet.Cell(i + 2, 5).Value = record.CONTRACTOR_NAME ?? "";
                    worksheet.Cell(i + 2, 6).Value = record.SO_NUMBER ?? "";
                    worksheet.Cell(i + 2, 7).Value = record.CUSTOMER ?? "";
                    worksheet.Cell(i + 2, 8).Value = record.PE_NUMBER ?? "";
                    worksheet.Cell(i + 2, 9).Value = record.PE_TITLE ?? "";
                    worksheet.Cell(i + 2, 10).Value = record.TASK_NAME ?? "";
                    worksheet.Cell(i + 2, 11).Value = record.TASK_WG ?? "";
                }

                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PE_Records_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
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
            string contractorName = "", string soNumber = "", string customer = "")
        {
            try
            {
                var filteredRecords = await GetFilteredRecords(province, region, rtom, contractorName, soNumber, customer);

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
                var title = "PE Records Report";
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
                        record.ID.ToString(),
                        record.PROVINCE ?? "",
                        record.REGION ?? "",
                        record.RTOM ?? "",
                        record.CONTRACTOR_NAME ?? "",
                        record.SO_NUMBER ?? "",
                        record.CUSTOMER ?? "",
                        record.PE_NUMBER ?? "",
                        record.PE_TITLE ?? "",
                        record.TASK_NAME ?? "",
                        record.TASK_WG ?? ""
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

                return File(stream.ToArray(), "image/png", $"PE_Records_Report_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PNG");
                TempData["Error"] = "An error occurred while exporting PNG.";
                return RedirectToAction("Generate");
            }
        }
        private async Task<List<PERecord>> GetFilteredRecords(string province, string region, string rtom,
            string contractorName, string soNumber, string customer)
        {
            var response = await _peRecordsApiClient.GetFilteredPERecordsAsync(
                string.IsNullOrWhiteSpace(province) ? null : province,
                string.IsNullOrWhiteSpace(region) ? null : region,
                string.IsNullOrWhiteSpace(rtom) ? null : rtom,
                string.IsNullOrWhiteSpace(contractorName) ? null : contractorName,
                string.IsNullOrWhiteSpace(soNumber) ? null : soNumber,
                string.IsNullOrWhiteSpace(customer) ? null : customer
            );

            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            return new List<PERecord>();
        }

        private string GenerateReportContent(List<PERecord> records, string format)
        {
            var content = new StringBuilder();
            content.AppendLine($"PE Records Report - {format} Export");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Total Records: {records.Count}");
            content.AppendLine(new string('=', 80));
            content.AppendLine();

            foreach (var record in records)
            {
                content.AppendLine($"ID: {record.ID}");
                content.AppendLine($"Province: {record.PROVINCE}");
                content.AppendLine($"Region: {record.REGION}");
                content.AppendLine($"RTOM: {record.RTOM}");
                content.AppendLine($"Contractor: {record.CONTRACTOR_NAME}");
                content.AppendLine($"SO Number: {record.SO_NUMBER}");
                content.AppendLine($"Customer: {record.CUSTOMER}");
                content.AppendLine($"PE Number: {record.PE_NUMBER}");
                content.AppendLine($"PE Title: {record.PE_TITLE}");
                content.AppendLine(new string('-', 40));
            }

            return content.ToString();
        }
    }
}
