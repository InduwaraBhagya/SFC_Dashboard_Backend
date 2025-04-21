using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDB.Models;

namespace SFCDB.Controllers
{
    public class PERecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PERecordsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: PERecords
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, int pageIndex = 1)
        {
            var query = from r in _context.PERecords
                        select r;

            // Store current filters in ViewData
            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            // Determine the search string based on the search type
            string searchString = searchType switch
            {
                "customer" => customer,
                "jobReference" => jobReference,
                "soNumber" => soNumber,
                _ => peNumber
            };

            // Apply search filters based on type
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchType)
                {
                    case "customer":
                        query = query.Where(p => p.CUSTOMER != null && p.CUSTOMER.Contains(customer));
                        break;
                    case "jobReference":
                        query = query.Where(p => p.JOB_REFERENCE != null && p.JOB_REFERENCE.Contains(jobReference));
                        break;
                    case "soNumber":
                        query = query.Where(p => p.SO_NUMBER != null && p.SO_NUMBER.Contains(soNumber));
                        break;
                    default: // peNumber
                        query = query.Where(p => p.PE_NUMBER != null && p.PE_NUMBER.Contains(peNumber));
                        break;
                }
            }

            // Return empty list if no search criteria provided
            if (string.IsNullOrEmpty(peNumber) &&
                string.IsNullOrEmpty(customer) &&
                string.IsNullOrEmpty(jobReference) &&
                string.IsNullOrEmpty(soNumber))
            {
                return View(new PaginatedList<PERecord>(new List<PERecord>(), 0, pageIndex, 10));
            }

            // Order results
            query = query.OrderByDescending(p => p.WO_START_DATE)
                        .ThenBy(p => p.PE_NUMBER);

            int pageSize = 10;
            var paginatedList = await PaginatedList<PERecord>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

            // Log the number of records found
            Console.WriteLine($"Total records found: {paginatedList.Count}");

            // Pass search parameters back to the view for maintaining the search state
            ViewData["SearchType"] = searchType;
            ViewData["SearchString"] = searchString;

            return View(paginatedList);
        }

        // GET: PERecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords
                .FirstOrDefaultAsync(m => m.ID == id);
            if (pERecord == null)
            {
                return NotFound();
            }

            return View(pERecord);
        }

        // GET: PERecords/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PERecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PROVINCE,REGION,RTOM,RTOM_DESCRIPTION,JOB_REFERENCE,CONTRACTOR_NAME,PE_NUMBER,PE_ACTIVITY,PE_NATURE,PE_TITLE,PE_OBJECTIVE,PE_AREA,SO_NUMBER,TASK_SEQ,TASK_NAME,TASK_WG,WO_ACTUAL_START_DATE,REQUEST_REFERENCE_NO,SO_ID,REGION_1,PROVINCE_1,RTOM_1,LEA,CCT_ID,SERVICE_CATEGORY,SERVICE_TYPE,SO_CREATE_DATE,ORDER_TYPE,CRM_ORDER,WO_ID,PENDING_TASK_NAME,PENDING_WG,WO_STATUS,WO_START_DATE,SERVICE_SPEED,SERVICE_REQUIRED_DATE,FIBER_PE_NO,FIBER_SO_ID,PRODUCT_SO_ID,FIBER_PE_TASK_NAME,FIBER_PE_TASK_WG,PE_WO_COMMENTS,CUSTOMER,CUS_TYPE,ACCOUNT_MANAGER,SECTION_HANDLED_BY,LOCATION_A_ADDRESS,LOCATION_B_ADDRESS,NTU_TYPE,ACCESS_MEDIUM,ACCESS_MEDIUM_A_END,ACCESS_MEDIUM_B_END,WO_COMMENTS")] PERecord pERecord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pERecord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pERecord);
        }

        // GET: PERecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords.FindAsync(id);
            if (pERecord == null)
            {
                return NotFound();
            }
            return View(pERecord);
        }

        // POST: PERecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PROVINCE,REGION,RTOM,RTOM_DESCRIPTION,JOB_REFERENCE,CONTRACTOR_NAME,PE_NUMBER,PE_ACTIVITY,PE_NATURE,PE_TITLE,PE_OBJECTIVE,PE_AREA,SO_NUMBER,TASK_SEQ,TASK_NAME,TASK_WG,WO_ACTUAL_START_DATE,REQUEST_REFERENCE_NO,SO_ID,REGION_1,PROVINCE_1,RTOM_1,LEA,CCT_ID,SERVICE_CATEGORY,SERVICE_TYPE,SO_CREATE_DATE,ORDER_TYPE,CRM_ORDER,WO_ID,PENDING_TASK_NAME,PENDING_WG,WO_STATUS,WO_START_DATE,SERVICE_SPEED,SERVICE_REQUIRED_DATE,FIBER_PE_NO,FIBER_SO_ID,PRODUCT_SO_ID,FIBER_PE_TASK_NAME,FIBER_PE_TASK_WG,PE_WO_COMMENTS,CUSTOMER,CUS_TYPE,ACCOUNT_MANAGER,SECTION_HANDLED_BY,LOCATION_A_ADDRESS,LOCATION_B_ADDRESS,NTU_TYPE,ACCESS_MEDIUM,ACCESS_MEDIUM_A_END,ACCESS_MEDIUM_B_END,WO_COMMENTS")] PERecord pERecord)
        {
            if (id != pERecord.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pERecord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PERecordExists(pERecord.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pERecord);
        }

        // GET: PERecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords
                .FirstOrDefaultAsync(m => m.ID == id);
            if (pERecord == null)
            {
                return NotFound();
            }

            return View(pERecord);
        }

        // POST: PERecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pERecord = await _context.PERecords.FindAsync(id);
            if (pERecord != null)
            {
                _context.PERecords.Remove(pERecord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PERecords/ImportExcel
        public IActionResult ImportExcel()
        {
            return View();
        }

        // POST: PERecords/ImportExcel
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

            try
            {
                // Create temp file path
                var fileName = Path.GetFileName(excelFile.FileName);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "temp");

                // Ensure directory exists
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                var fullPath = Path.Combine(filePath, fileName);

                // Save the uploaded file temporarily
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await excelFile.CopyToAsync(fileStream);
                }

                List<PERecord> peRecords = new List<PERecord>();

                // Process the Excel file
                using (var workbook = new XLWorkbook(fullPath))
                {
                    var worksheet = workbook.Worksheet(1); // Read the first worksheet
                    var rows = worksheet.RowsUsed();

                    // Skip header row and process data
                    foreach (var row in rows.Skip(1))
                    {
                        var peRecord = new PERecord
                        {
                            PROVINCE = row.Cell(1).GetValue<string>(),
                            REGION = row.Cell(2).GetValue<string>(),
                            RTOM = row.Cell(3).GetValue<string>(),
                            RTOM_DESCRIPTION = row.Cell(4).GetValue<string>(),
                            JOB_REFERENCE = row.Cell(5).GetValue<string>(),
                            CONTRACTOR_NAME = row.Cell(6).GetValue<string>(),
                            PE_NUMBER = row.Cell(7).GetValue<string>(),
                            PE_ACTIVITY = row.Cell(8).GetValue<string>(),
                            PE_NATURE = row.Cell(9).GetValue<string>(),
                            PE_TITLE = row.Cell(10).GetValue<string>(),
                            PE_OBJECTIVE = row.Cell(11).GetValue<string>(),
                            PE_AREA = row.Cell(12).GetValue<string>(),
                            SO_NUMBER = row.Cell(13).GetValue<string>(),
                            TASK_SEQ = row.Cell(14).GetValue<int?>(),
                            TASK_NAME = row.Cell(15).GetValue<string>(),
                            TASK_WG = row.Cell(16).GetValue<string>(),
                            WO_ACTUAL_START_DATE = row.Cell(17).GetValue<string>(),
                            REQUEST_REFERENCE_NO = row.Cell(18).GetValue<string>(),
                            SO_ID = row.Cell(19).GetValue<string>(),
                            REGION_1 = row.Cell(20).GetValue<string>(),
                            PROVINCE_1 = row.Cell(21).GetValue<string>(),
                            RTOM_1 = row.Cell(22).GetValue<string>(),
                            LEA = row.Cell(23).GetValue<string>(),
                            CCT_ID = row.Cell(24).GetValue<string>(),
                            SERVICE_CATEGORY = row.Cell(25).GetValue<string>(),
                            SERVICE_TYPE = row.Cell(26).GetValue<string>(),
                            SO_CREATE_DATE = row.Cell(27).GetValue<DateTime?>(),
                            ORDER_TYPE = row.Cell(28).GetValue<string>(),
                            CRM_ORDER = row.Cell(29).GetValue<string>(),
                            WO_ID = row.Cell(30).GetValue<string>(),
                            PENDING_TASK_NAME = row.Cell(31).GetValue<string>(),
                            PENDING_WG = row.Cell(32).GetValue<string>(),
                            WO_STATUS = row.Cell(33).GetValue<string>(),
                            WO_START_DATE = row.Cell(34).GetValue<DateTime?>(),
                            SERVICE_SPEED = row.Cell(35).GetValue<string>(),
                            SERVICE_REQUIRED_DATE = row.Cell(36).GetValue<DateTime?>(),
                            FIBER_PE_NO = row.Cell(37).GetValue<string>(),
                            FIBER_SO_ID = row.Cell(38).GetValue<string>(),
                            PRODUCT_SO_ID = row.Cell(39).GetValue<string>(),
                            FIBER_PE_TASK_NAME = row.Cell(40).GetValue<string>(),
                            FIBER_PE_TASK_WG = row.Cell(41).GetValue<string>(),
                            PE_WO_COMMENTS = row.Cell(42).GetValue<string>(),
                            CUSTOMER = row.Cell(43).GetValue<string>(),
                            CUS_TYPE = row.Cell(44).GetValue<string>(),
                            ACCOUNT_MANAGER = row.Cell(45).GetValue<string>(),
                            SECTION_HANDLED_BY = row.Cell(46).GetValue<string>(),
                            LOCATION_A_ADDRESS = row.Cell(47).GetValue<string>(),
                            LOCATION_B_ADDRESS = row.Cell(48).GetValue<string>(),
                            NTU_TYPE = row.Cell(49).GetValue<string>(),
                            ACCESS_MEDIUM = row.Cell(50).GetValue<string>(),
                            ACCESS_MEDIUM_A_END = row.Cell(51).GetValue<string>(),
                            ACCESS_MEDIUM_B_END = row.Cell(52).GetValue<string>(),
                            WO_COMMENTS = row.Cell(53).GetValue<string>()
                        };

                        peRecords.Add(peRecord);
                    }
                }

                // Delete the temporary file
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Clear existing records and add new ones
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Delete all existing records
                        _context.PERecords.RemoveRange(_context.PERecords);
                        await _context.SaveChangesAsync();

                        // Add the new records
                        await _context.PERecords.AddRangeAsync(peRecords);
                        await _context.SaveChangesAsync();

                        // Commit the transaction
                        await transaction.CommitAsync();

                        TempData["Message"] = $"Successfully replaced all records with {peRecords.Count} new records from Excel.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction on error
                        await transaction.RollbackAsync();
                        throw; // Rethrow to be caught by outer catch block
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
                return RedirectToAction("ImportExcel");
            }
        }

        private bool PERecordExists(int id)
        {
            return _context.PERecords.Any(e => e.ID == id);
        }
    }
}