using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Data;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace SFCDashboard.Controllers
{
    public class NetworkEngineerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NetworkEngineerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length <= 0)
            {
                TempData["Message"] = "Please select an Excel file to upload.";
                return RedirectToAction("ImportExcel");
            }

            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Message"] = "Please select a valid Excel file (.xlsx).";
                return RedirectToAction("ImportExcel");
            }

            var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".xlsx";
            var fullPath = Path.Combine(tempFolder, fileName);

            try
            {
                // Save file to disk
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await excelFile.CopyToAsync(fileStream);
                }

                int imported = 0;
                using (var workbook = new XLWorkbook(fullPath))
                {
                    var worksheet = workbook.Worksheet(1); // First worksheet
                    var rows = worksheet.RowsUsed().Skip(1); // Skip header

                    foreach (var row in rows)
                    {
                        var area = row.Cell(1).GetValue<string>()?.Trim();
                        var engineer = row.Cell(2).GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(area) && !string.IsNullOrEmpty(engineer))
                        {
                            var mapping = new AreaNetworkEngineer { Area = area, EngineerName = engineer };
                            _context.AreaNetworkEngineers.Add(mapping);
                            imported++;
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Delete temp file
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                TempData["Message"] = $"Successfully imported {imported} records.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            return RedirectToAction("ImportExcel");
        }

        public IActionResult Index()
        {
            return RedirectToAction("ImportExcel");
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(AreaNetworkEngineer model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var mapping = await _context.AreaNetworkEngineers.FindAsync(id);
            if (mapping == null) return NotFound();
            return View(mapping);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AreaNetworkEngineer model)
        {
            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ImportExcel");
        }

        public async Task<IActionResult> List()
        {
            var mappings = await _context.AreaNetworkEngineers.ToListAsync();
            return View(mappings);
        }

        [HttpGet]
        public async Task<IActionResult> ImportExcel()
        {
            var mappings = await _context.AreaNetworkEngineers.ToListAsync();
            return View(mappings);
        }
    }
}