using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;

namespace SFCDashboard.Controllers.Api
{
    /// <summary>
    /// API Controller for PE Records management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class PERecordsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PERecordsApiController> _logger;
        private readonly PERecordSyncService _syncService;

        public PERecordsApiController(
            ApplicationDbContext context,
            ILogger<PERecordsApiController> logger,
            PERecordSyncService syncService)
        {
            _context = context;
            _logger = logger;
            _syncService = syncService;
        }

        /// <summary>
        /// Import PE Records from Excel file
        /// </summary>
        /// <param name="excelFile">Excel file containing PE Records (.xlsx format)</param>
        /// <returns>Import result with success status and message</returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
        public async Task<IActionResult> ImportPERecords(IFormFile excelFile)
        {
            try
            {
                // Validate file
                if (excelFile == null || excelFile.Length <= 0)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Please provide an Excel file to upload." 
                    });
                }

                // Validate file size (additional check)
                const long maxFileSize = 100 * 1024 * 1024; // 100MB
                if (excelFile.Length > maxFileSize)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds maximum limit of 100MB." 
                    });
                }

                // Validate file type more strictly
                var allowedExtensions = new[] { ".xlsx" };
                var fileExtension = Path.GetExtension(excelFile.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Only .xlsx files are supported." 
                    });
                }

                // Validate content type
                var allowedContentTypes = new[] { 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/octet-stream" // Some browsers send this for xlsx files
                };
                if (!allowedContentTypes.Contains(excelFile.ContentType))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid file type. Only Excel files (.xlsx) are allowed." 
                    });
                }

                if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Please provide a valid Excel file (.xlsx)." 
                    });
                }

                _logger.LogInformation("Starting import of Excel file: {fileName}", excelFile.FileName);

                // Process Excel file directly from stream (no need to save temporarily)
                List<PERecord> peRecords = new List<PERecord>();

                using (var stream = excelFile.OpenReadStream())
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed();
                    var totalRows = rows.Count();

                    _logger.LogInformation("Processing {count} rows from Excel", totalRows);

                    // Skip header row and process data
                    var processedRows = 0;
                    foreach (var row in rows.Skip(1))
                    {
                        try
                        {
                            processedRows++;
                            
                            // Log progress for large files
                            if (processedRows % 1000 == 0)
                            {
                                _logger.LogInformation("Processed {processed} of {total} rows", processedRows, totalRows - 1);
                            }

                            // Validate row has minimum required columns
                            if (row.CellsUsed().Count() < 7)
                            {
                                _logger.LogWarning("Skipping row {rowNumber} - insufficient columns", row.RowNumber());
                                continue;
                            }

                            var peNumber = row.Cell(7).GetValue<string>();

                            // Skip rows with empty PE_NUMBER as they're essential
                            if (string.IsNullOrWhiteSpace(peNumber))
                            {
                                _logger.LogWarning("Skipping row with empty PE_NUMBER at row {rowNumber}", row.RowNumber());
                                continue;
                            }

                            // Validate PE_NUMBER format (basic validation)
                            if (peNumber.Length > 50) // Assuming max length of 50
                            {
                                _logger.LogWarning("Skipping row {rowNumber} - PE_NUMBER too long: {peNumber}", row.RowNumber(), peNumber);
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

                // Clear existing records and add new ones
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _logger.LogInformation("Removing existing PE records");
                        _context.PERecords.RemoveRange(_context.PERecords);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Adding {count} new PE records in batches", peRecords.Count);
                        
                        // Process in batches to avoid memory issues with large datasets
                        const int batchSize = 1000;
                        for (int i = 0; i < peRecords.Count; i += batchSize)
                        {
                            var batch = peRecords.Skip(i).Take(batchSize).ToList();
                            await _context.PERecords.AddRangeAsync(batch);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Processed batch {batchNumber}/{totalBatches}", 
                                (i / batchSize) + 1, (peRecords.Count + batchSize - 1) / batchSize);
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during database transaction");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // Check if task templates exist
                var templateCount = await _context.PETaskLists.CountAsync();
                if (templateCount == 0)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Records imported successfully, but PETaskList templates are missing. Please add task templates before syncing.",
                        recordCount = peRecords.Count,
                        syncCompleted = false
                    });
                }

                try
                {
                    // Use the injected syncService
                    _logger.LogInformation("Starting PE record synchronization");
                    await _syncService.SyncPERecordsAsync();
                    
                    // Check counts after sync
                    var peCount = await _context.PERecords.CountAsync();
                    var plannedEventCount = await _context.PlannedEvents.CountAsync();
                    var peTaskCount = await _context.PETasks.CountAsync();
                    
                    _logger.LogInformation("Sync completed. PERecords: {peCount}, PlannedEvents: {plannedEventCount}, PETasks: {peTaskCount}", 
                        peCount, plannedEventCount, peTaskCount);

                    return Ok(new { 
                        success = true, 
                        message = $"Successfully replaced all records with {peRecords.Count} new records from Excel and synced to all related tables.",
                        recordCount = peRecords.Count,
                        plannedEventCount = plannedEventCount,
                        peTaskCount = peTaskCount,
                        syncCompleted = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronization");
                    return Ok(new { 
                        success = true, 
                        message = $"Records imported but sync failed: {ex.Message}",
                        recordCount = peRecords.Count,
                        syncCompleted = false,
                        syncError = ex.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportPERecords API");
                return StatusCode(500, new { 
                    success = false, 
                    message = "An internal server error occurred. Please try again later." 
                });
            }
        }

        /// <summary>
        /// Import PE Records from JSON data
        /// </summary>
        /// <param name="peRecords">List of PE Records to import</param>
        /// <returns>Import result with success status and message</returns>
        [HttpPost("import-json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportPERecordsFromJson([FromBody] List<PERecord> peRecords)
        {
            try
            {
                if (peRecords == null || !peRecords.Any())
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "No PE records provided." 
                    });
                }

                // Validate input size
                const int maxRecordCount = 100000; // 100k records limit
                if (peRecords.Count > maxRecordCount)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Maximum {maxRecordCount} records allowed per import." 
                    });
                }

                _logger.LogInformation("Starting import of {count} PE records from JSON", peRecords.Count);

                // Validate required fields
                var invalidRecords = peRecords.Where(r => string.IsNullOrWhiteSpace(r.PE_NUMBER)).ToList();
                if (invalidRecords.Any())
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Found {invalidRecords.Count} records with missing PE_NUMBER." 
                    });
                }

                // Validate PE_NUMBER lengths
                var invalidLengthRecords = peRecords.Where(r => !string.IsNullOrWhiteSpace(r.PE_NUMBER) && r.PE_NUMBER.Length > 50).ToList();
                if (invalidLengthRecords.Any())
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Found {invalidLengthRecords.Count} records with PE_NUMBER longer than 50 characters." 
                    });
                }

                // Clear existing records and add new ones
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _logger.LogInformation("Removing existing PE records");
                        _context.PERecords.RemoveRange(_context.PERecords);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Adding {count} new PE records in batches", peRecords.Count);
                        
                        // Process in batches to avoid memory issues with large datasets
                        const int batchSize = 1000;
                        for (int i = 0; i < peRecords.Count; i += batchSize)
                        {
                            var batch = peRecords.Skip(i).Take(batchSize).ToList();
                            await _context.PERecords.AddRangeAsync(batch);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Processed batch {batchNumber}/{totalBatches}", 
                                (i / batchSize) + 1, (peRecords.Count + batchSize - 1) / batchSize);
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during database transaction");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // Check if task templates exist
                var templateCount = await _context.PETaskLists.CountAsync();
                if (templateCount == 0)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Records imported successfully, but PETaskList templates are missing. Please add task templates before syncing.",
                        recordCount = peRecords.Count,
                        syncCompleted = false
                    });
                }

                try
                {
                    // Use the injected syncService
                    _logger.LogInformation("Starting PE record synchronization");
                    await _syncService.SyncPERecordsAsync();
                    
                    // Check counts after sync
                    var peCount = await _context.PERecords.CountAsync();
                    var plannedEventCount = await _context.PlannedEvents.CountAsync();
                    var peTaskCount = await _context.PETasks.CountAsync();
                    
                    _logger.LogInformation("Sync completed. PERecords: {peCount}, PlannedEvents: {plannedEventCount}, PETasks: {peTaskCount}", 
                        peCount, plannedEventCount, peTaskCount);

                    return Ok(new { 
                        success = true, 
                        message = $"Successfully replaced all records with {peRecords.Count} new records and synced to all related tables.",
                        recordCount = peRecords.Count,
                        plannedEventCount = plannedEventCount,
                        peTaskCount = peTaskCount,
                        syncCompleted = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronization");
                    return Ok(new { 
                        success = true, 
                        message = $"Records imported but sync failed: {ex.Message}",
                        recordCount = peRecords.Count,
                        syncCompleted = false,
                        syncError = ex.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportPERecordsFromJson API");
                return StatusCode(500, new { 
                    success = false, 
                    message = "An internal server error occurred. Please try again later." 
                });
            }
        }

        /// <summary>
        /// Get all PE Records with pagination support
        /// </summary>
        /// <param name="request">Pagination request parameters</param>
        /// <returns>Paginated list of PE Records</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPERecords([FromQuery] PERecordsRequest request)
        {
            try
            {
                // Validate pagination parameters
                if (request.Page < 1)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Page number must be greater than 0." 
                    });
                }

                if (request.PageSize < 1 || request.PageSize > 10000)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Page size must be between 1 and 10000." 
                    });
                }

                var totalRecords = await _context.PERecords.CountAsync();
                var records = await _context.PERecords
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

                return Ok(new { 
                    success = true, 
                    data = records,
                    pagination = new
                    {
                        currentPage = request.Page,
                        pageSize = request.PageSize,
                        totalRecords = totalRecords,
                        totalPages = totalPages,
                        hasNextPage = request.Page < totalPages,
                        hasPreviousPage = request.Page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE records");
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving PE records." 
                });
            }
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        /// <returns>Database statistics including record counts</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                var stats = new
                {
                    peRecordsCount = await _context.PERecords.CountAsync(),
                    plannedEventsCount = await _context.PlannedEvents.CountAsync(),
                    peTasksCount = await _context.PETasks.CountAsync(),
                    taskTemplatesCount = await _context.PETaskLists.CountAsync()
                };

                return Ok(new { 
                    success = true, 
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database statistics");
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving database statistics." 
                });
            }
        }

        /// <summary>
        /// Safely get cell value with proper error handling and type conversion
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="row">Excel row</param>
        /// <param name="columnIndex">Column index (1-based)</param>
        /// <returns>Converted value or default</returns>
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
                    return default(T)!;
                
                return cell.GetValue<T>();
            }
            catch
            {
                // If there's any error, return default value for that type
                return default(T)!;
            }
        }
    }

    /// <summary>
    /// Request model for paginated PE Records retrieval
    /// </summary>
    public class PERecordsRequest
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of records per page
        /// </summary>
        [Range(1, 10000, ErrorMessage = "Page size must be between 1 and 10000")]
        public int PageSize { get; set; } = 1000;
    }

    /// <summary>
    /// Response model for API operations
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Response data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Pagination information (if applicable)
        /// </summary>
        public PaginationInfo? Pagination { get; set; }
    }

    /// <summary>
    /// Pagination information
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
