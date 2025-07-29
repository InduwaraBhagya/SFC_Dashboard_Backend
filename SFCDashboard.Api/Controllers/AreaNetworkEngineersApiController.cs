using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Services;
using SFCDashboard.Models;
using ClosedXML.Excel;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/areanetworkengineers")]
    public class AreaNetworkEngineersApiController : ControllerBase
    {
        private readonly IAreaNetworkEngineersApiService _service;
        public AreaNetworkEngineersApiController(IAreaNetworkEngineersApiService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AreaNetworkEngineer>>> GetAll()
        {
            var result = await _service.GetAreaNetworkEngineersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AreaNetworkEngineer>> GetById(int id)
        {
            var result = await _service.GetAreaNetworkEngineerAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-area/{area}")]
        public async Task<ActionResult<object>> GetByArea(string area)
        {
            var engineerName = await _service.GetEngineerNameByAreaAsync(area);
            if (string.IsNullOrEmpty(engineerName)) return NotFound();
            return Ok(new { engineerName });
        }

        [HttpPost]
        public async Task<ActionResult<AreaNetworkEngineer>> Create([FromBody] AreaNetworkEngineer engineer)
        {
            var created = await _service.CreateAreaNetworkEngineerAsync(engineer);
            if (created == null) return BadRequest();
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AreaNetworkEngineer>> Update(int id, [FromBody] AreaNetworkEngineer engineer)
        {
            if (id != engineer.Id) return BadRequest();
            var updated = await _service.UpdateAreaNetworkEngineerAsync(engineer);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAreaNetworkEngineerAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length <= 0)
                return BadRequest("No file uploaded.");
            
            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file type. Only .xlsx is supported.");

            var imported = 0;
            var errors = new List<string>();
            
            try
            {
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0;
                    
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1); // Skip header
                        
                        foreach (var row in rows)
                        {
                            try
                            {
                                var area = row.Cell(1).GetValue<string>()?.Trim();
                                var engineer = row.Cell(2).GetValue<string>()?.Trim();
                                
                                if (string.IsNullOrEmpty(area) || string.IsNullOrEmpty(engineer))
                                {
                                    errors.Add($"Row {row.RowNumber()}: Missing area or engineer name");
                                    continue;
                                }

                                // Check if mapping already exists
                                var existing = await _service.GetAreaNetworkEngineerByAreaAsync(area);
                                if (existing != null)
                                {
                                    // Update existing mapping
                                    existing.EngineerName = engineer;
                                    await _service.UpdateAreaNetworkEngineerAsync(existing);
                                }
                                else
                                {
                                    // Create new mapping
                                    var mapping = new AreaNetworkEngineer { Area = area, EngineerName = engineer };
                                    await _service.CreateAreaNetworkEngineerAsync(mapping);
                                }
                                imported++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }
                
                var result = new { 
                    imported, 
                    errors = errors.Count > 0 ? errors : null,
                    message = $"Successfully imported {imported} records" + (errors.Count > 0 ? $" with {errors.Count} errors" : "")
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error importing Excel: {ex.Message}");
            }
        }
    }
}
