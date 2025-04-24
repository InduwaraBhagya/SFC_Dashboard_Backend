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
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDB.Models;
using Microsoft.Extensions.DependencyInjection;
using SFCDashboard.Services;

namespace SFCDB.Controllers
{
    public class PERecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<PERecordsController> _logger;

        public PERecordsController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            ILogger<PERecordsController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: PERecords
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving all PE Records");
            return View(await _context.PERecords.ToListAsync());
        }

        // GET: PERecords/ImportExcel
        public IActionResult ImportExcel()
        {
            // Prepare diagnostic info for the view
            ViewBag.PERecordsCount = _context.PERecords.Count();
            ViewBag.PlannedEventsCount = _context.PlannedEvents.Count();
            ViewBag.PETasksCount = _context.PETasks.Count();
            ViewBag.TaskTemplatesCount = _context.PETaskLists.Count();
            
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
                _logger.LogInformation("Starting import of Excel file: {fileName}", excelFile.FileName);
                
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

                _logger.LogInformation("Excel file saved temporarily at: {path}", fullPath);
                
                List<PERecord> peRecords = new List<PERecord>();

                // Process the Excel file
                using (var workbook = new XLWorkbook(fullPath))
                {
                    var worksheet = workbook.Worksheet(1); // Read the first worksheet
                    var rows = worksheet.RowsUsed();
                    
                    _logger.LogInformation("Processing {count} rows from Excel", rows.Count());

                    // Skip header row and process data
                    foreach (var row in rows.Skip(1))
                    {
                        try 
                        {
                            var peNumber = row.Cell(7).GetValue<string>();
                            
                            // Skip rows with empty PE_NUMBER as they're essential
                            if (string.IsNullOrWhiteSpace(peNumber))
                            {
                                _logger.LogWarning("Skipping row with empty PE_NUMBER at row {rowNumber}", row.RowNumber());
                                continue;
                            }
                            
                            var peRecord = new PERecord
                            {
                                PROVINCE = GetCellValueSafely<string>(row, 1),
                                REGION = GetCellValueSafely<string>(row, 2),
                                RTOM = GetCellValueSafely<string>(row, 3),
                                RTOM_DESCRIPTION = GetCellValueSafely<string>(row, 4),
                                JOB_REFERENCE = GetCellValueSafely<string>(row, 5),
                                CONTRACTOR_NAME = GetCellValueSafely<string>(row, 6),
                                PE_NUMBER = peNumber,
                                PE_ACTIVITY = GetCellValueSafely<string>(row, 8),
                                PE_NATURE = GetCellValueSafely<string>(row, 9),
                                PE_TITLE = GetCellValueSafely<string>(row, 10),
                                PE_OBJECTIVE = GetCellValueSafely<string>(row, 11),
                                PE_AREA = GetCellValueSafely<string>(row, 12),
                                SO_NUMBER = GetCellValueSafely<string>(row, 13),
                                TASK_SEQ = GetCellValueSafely<int?>(row, 14),
                                TASK_NAME = GetCellValueSafely<string>(row, 15),
                                TASK_WG = GetCellValueSafely<string>(row, 16),
                                WO_ACTUAL_START_DATE = GetCellValueSafely<string>(row, 17),
                                REQUEST_REFERENCE_NO = GetCellValueSafely<string>(row, 18),
                                SO_ID = GetCellValueSafely<string>(row, 19),
                                REGION_1 = GetCellValueSafely<string>(row, 20),
                                PROVINCE_1 = GetCellValueSafely<string>(row, 21),
                                RTOM_1 = GetCellValueSafely<string>(row, 22),
                                LEA = GetCellValueSafely<string>(row, 23),
                                CCT_ID = GetCellValueSafely<string>(row, 24),
                                SERVICE_CATEGORY = GetCellValueSafely<string>(row, 25),
                                SERVICE_TYPE = GetCellValueSafely<string>(row, 26),
                                SO_CREATE_DATE = GetCellValueSafely<DateTime?>(row, 27),
                                ORDER_TYPE = GetCellValueSafely<string>(row, 28),
                                CRM_ORDER = GetCellValueSafely<string>(row, 29),
                                WO_ID = GetCellValueSafely<string>(row, 30),
                                PENDING_TASK_NAME = GetCellValueSafely<string>(row, 31),
                                PENDING_WG = GetCellValueSafely<string>(row, 32),
                                WO_STATUS = GetCellValueSafely<string>(row, 33),
                                WO_START_DATE = GetCellValueSafely<DateTime?>(row, 34),
                                SERVICE_SPEED = GetCellValueSafely<string>(row, 35),
                                SERVICE_REQUIRED_DATE = GetCellValueSafely<DateTime?>(row, 36),
                                FIBER_PE_NO = GetCellValueSafely<string>(row, 37),
                                FIBER_SO_ID = GetCellValueSafely<string>(row, 38),
                                PRODUCT_SO_ID = GetCellValueSafely<string>(row, 39),
                                FIBER_PE_TASK_NAME = GetCellValueSafely<string>(row, 40),
                                FIBER_PE_TASK_WG = GetCellValueSafely<string>(row, 41),
                                PE_WO_COMMENTS = GetCellValueSafely<string>(row, 42),
                                CUSTOMER = GetCellValueSafely<string>(row, 43),
                                CUS_TYPE = GetCellValueSafely<string>(row, 44),
                                ACCOUNT_MANAGER = GetCellValueSafely<string>(row, 45),
                                SECTION_HANDLED_BY = GetCellValueSafely<string>(row, 46),
                                LOCATION_A_ADDRESS = GetCellValueSafely<string>(row, 47),
                                LOCATION_B_ADDRESS = GetCellValueSafely<string>(row, 48),
                                NTU_TYPE = GetCellValueSafely<string>(row, 49),
                                ACCESS_MEDIUM = GetCellValueSafely<string>(row, 50),
                                ACCESS_MEDIUM_A_END = GetCellValueSafely<string>(row, 51),
                                ACCESS_MEDIUM_B_END = GetCellValueSafely<string>(row, 52),
                                WO_COMMENTS = GetCellValueSafely<string>(row, 53)
                            };

                            peRecords.Add(peRecord);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing row {rowNumber}", row.RowNumber());
                        }
                    }
                }

                _logger.LogInformation("Successfully processed {count} valid records from Excel", peRecords.Count);
                
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
                        _logger.LogInformation("Removing existing PE records");
                        // Delete all existing records
                        _context.PERecords.RemoveRange(_context.PERecords);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Adding {count} new PE records", peRecords.Count);
                        // Add the new records
                        await _context.PERecords.AddRangeAsync(peRecords);
                        await _context.SaveChangesAsync();

                        // Commit the transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during database transaction");
                        // Rollback the transaction on error
                        await transaction.RollbackAsync();
                        throw; // Rethrow to be caught by outer catch block
                    }
                }

                // First check if task templates exist
                var templateCount = await _context.PETaskLists.CountAsync();
                if (templateCount == 0)
                {
                    TempData["Message"] = "Warning: PETaskList templates are missing. Please add task templates before syncing.";
                    return RedirectToAction("ImportExcel");
                }

                try
                {
                    // Get the syncService from the DI container
                    _logger.LogInformation("Starting PE record synchronization");
                    var syncService = HttpContext.RequestServices.GetRequiredService<PERecordSyncService>();
                    
                    // Try the simplified direct test first
                    await TestDirectPlannedEventCreation(syncService);
                    
                    // Then run the full sync
                    await syncService.SyncPERecordsAsync();
                    
                    // Check counts after sync
                    var peCount = await _context.PERecords.CountAsync();
                    var plannedEventCount = await _context.PlannedEvents.CountAsync();
                    var peTaskCount = await _context.PETasks.CountAsync();
                    
                    _logger.LogInformation("Sync completed. PERecords: {peCount}, PlannedEvents: {plannedEventCount}, PETasks: {peTaskCount}", 
                        peCount, plannedEventCount, peTaskCount);

                    TempData["Message"] = $"Successfully replaced all records with {peRecords.Count} new records from Excel and synced to all related tables. " +
                        $"PlannedEvents: {plannedEventCount}, PETasks: {peTaskCount}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronization");
                    TempData["Message"] = $"Records imported but sync failed: {ex.Message}. Inner error: {ex.InnerException?.Message}";
                }
                
                return RedirectToAction("ImportExcel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportExcel action");
                TempData["Message"] = $"Error: {ex.Message}. Inner error: {ex.InnerException?.Message}";
                return RedirectToAction("ImportExcel");
            }
        }
        
        private async Task TestDirectPlannedEventCreation(PERecordSyncService syncService)
        {
            try
            {
                _logger.LogInformation("Testing direct PlannedEvent creation");
                
                // Create a test PlannedEvent directly
                var testEvent = new SFCDashboard.Models.PlannedEvent
                {
                    PeNumber = "TEST-" + DateTime.Now.Ticks,
                    PeTitle = "Test Event",
                    Province = "Test Province",
                    Region = "Test Region",
                    PEStatus = "ongoing",
                    PECreatedDate = DateTime.UtcNow
                };
                
                _context.PlannedEvents.Add(testEvent);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Test PlannedEvent created successfully with ID: {id}", testEvent.Id);
                
                // Create a task for the test event
                var testTask = new SFCDashboard.Models.PETask
                {
                    PENumber = testEvent.PeNumber,
                    TaskSeq = 1,
                    Task = "Test Task",
                    OLA = "1",
                    TaskStatus = "INPROGRESS",
                    TaskPhase = "ONGOING",
                    TaskCreatedDate = DateTime.UtcNow,
                    TaskCompleteDate = DateTime.UtcNow.AddDays(1),
                    TaskWorkGroup = "TEST"
                };
                
                _context.PETasks.Add(testTask);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Test PETask created successfully with ID: {id}", testTask.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestDirectPlannedEventCreation");
                throw; // Rethrow so the calling method can handle it
            }
        }

        // Helper method to safely get cell values
        private T GetCellValueSafely<T>(IXLRow row, int columnIndex)
        {
            try
            {
                var cell = row.Cell(columnIndex);
                
                // For strings, handle empty cells as empty strings
                if (typeof(T) == typeof(string))
                {
                    if (cell.IsEmpty())
                        return (T)(object)string.Empty;
                    
                    return cell.GetValue<T>();
                }
                
                // For other types, return default if cell is empty
                if (cell.IsEmpty())
                    return default(T);
                
                return cell.GetValue<T>();
            }
            catch
            {
                // If there's any error, return default value for that type
                return default(T);
            }
        }

        // GET: PERecords/Diagnostics
        public async Task<IActionResult> Diagnostics()
        {
            var viewModel = new Dictionary<string, object>();
            
            // Record counts
            viewModel["PERecord Count"] = await _context.PERecords.CountAsync();
            viewModel["PlannedEvent Count"] = await _context.PlannedEvents.CountAsync();
            viewModel["PETask Count"] = await _context.PETasks.CountAsync();
            viewModel["PETaskList Count"] = await _context.PETaskLists.CountAsync();
            
            // Sample records
            viewModel["Sample PERecord"] = await _context.PERecords.FirstOrDefaultAsync();
            viewModel["Sample PlannedEvent"] = await _context.PlannedEvents.FirstOrDefaultAsync();
            viewModel["Sample PETask"] = await _context.PETasks.FirstOrDefaultAsync();
            viewModel["Sample PETaskList"] = await _context.PETaskLists.FirstOrDefaultAsync();
            
            // Database connection info
            viewModel["Connection String"] = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***");
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> TriggerSync()
        {
            try
            {
                _logger.LogInformation("Manually triggering PE record synchronization");
                var syncService = HttpContext.RequestServices.GetRequiredService<PERecordSyncService>();
                await syncService.SyncPERecordsAsync();
                
                TempData["Message"] = "Synchronization completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual synchronization");
                TempData["Message"] = $"Synchronization error: {ex.Message}";
            }
            
            return RedirectToAction("Diagnostics");
        }
        
        private bool PERecordExists(int id)
        {
            return _context.PERecords.Any(e => e.ID == id);
        }
    }
}