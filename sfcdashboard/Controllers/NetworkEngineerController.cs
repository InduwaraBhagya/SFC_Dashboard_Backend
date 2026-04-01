using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class NetworkEngineerController : BaseController
    {
        private readonly IAreaNetworkEngineersApiClient _areaNetworkEngineersApiClient;
        private readonly ILogger<NetworkEngineerController> _logger;

        public NetworkEngineerController(IAreaNetworkEngineersApiClient areaNetworkEngineersApiClient, IUsersApiClient usersApiClient, ILogger<NetworkEngineerController> logger) : base(usersApiClient)
        {
            _areaNetworkEngineersApiClient = areaNetworkEngineersApiClient;
            _logger = logger;
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

            try
            {
                _logger.LogInformation("Starting Excel import for Area Network Engineers");
                using (var stream = excelFile.OpenReadStream())
                {
                    var resultMessage = await _areaNetworkEngineersApiClient.ImportExcelAsync(stream, excelFile.FileName, HttpContext.RequestAborted);
                    _logger.LogInformation($"Import successful: {resultMessage}");
                    TempData["Message"] = $"Import successful! {resultMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                TempData["Message"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("ImportExcel");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve area network engineers list for Index view");
                var mappings = (await _areaNetworkEngineersApiClient.GetAllAsync()).ToList();
                _logger.LogInformation($"Successfully retrieved {mappings.Count} area network engineers for Index view");
                return View(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load network engineers from API for Index view");
                TempData["Error"] = $"Failed to load network engineers: {ex.Message}";
                return View(new List<AreaNetworkEngineer>());
            }
        }

        public IActionResult Create()
        {
            var model = new AreaNetworkEngineer();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AreaNetworkEngineer model)
        {
            if (ModelState.IsValid)
            {
                await _areaNetworkEngineersApiClient.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var mapping = await _areaNetworkEngineersApiClient.GetByIdAsync(id);
            if (mapping == null) return NotFound();
            return View(mapping);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AreaNetworkEngineer model)
        {
            if (ModelState.IsValid)
            {
                await _areaNetworkEngineersApiClient.UpdateAsync(model);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> List()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve area network engineers list");
                var mappings = (await _areaNetworkEngineersApiClient.GetAllAsync()).ToList();
                _logger.LogInformation($"Successfully retrieved {mappings.Count} area network engineers");
                return View(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load network engineers from API");
                TempData["Error"] = $"Failed to load network engineers: {ex.Message}";
                return View(new List<AreaNetworkEngineer>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ImportExcel()
        {
            try
            {
                _logger.LogInformation("Loading area network engineers for ImportExcel view");
                var mappings = (await _areaNetworkEngineersApiClient.GetAllAsync()).ToList();
                _logger.LogInformation($"Successfully loaded {mappings.Count} engineers for ImportExcel view");
                return View(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load network engineers for ImportExcel view");
                TempData["Error"] = $"Failed to load network engineers: {ex.Message}";
                return View(new List<AreaNetworkEngineer>());
            }
        }

        // Test endpoint to check API connectivity
        [HttpGet]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                _logger.LogInformation("Testing API connection for Area Network Engineers");
                var mappings = await _areaNetworkEngineersApiClient.GetAllAsync();
                var count = mappings?.Count() ?? 0;
                
                var result = new
                {
                    Success = true,
                    Message = $"API connection successful. Found {count} engineers.",
                    Count = count,
                    Engineers = mappings?.Take(5) // Show first 5 for debugging
                };
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API connection test failed");
                var result = new
                {
                    Success = false,
                    Message = $"API connection failed: {ex.Message}",
                    Count = 0
                };
                
                return Json(result);
            }
        }

        // Test endpoint to check Excel import API directly
        [HttpGet]
        public async Task<IActionResult> TestExcelImportEndpoint()
        {
            try
            {
                // Compose a client using the same pipeline (base address + auth handler)
                var clientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient(nameof(AreaNetworkEngineersApiClient));
                var request = new HttpRequestMessage(HttpMethod.Options, "api/areanetworkengineers/import-excel");
                var response = await client.SendAsync(request, HttpContext.RequestAborted);

                var result = new
                {
                    Success = response.IsSuccessStatusCode,
                    Message = $"Excel import endpoint test: {response.StatusCode}",
                    Url = client.BaseAddress is null ? "/api/areanetworkengineers/import-excel" : new Uri(client.BaseAddress, "api/areanetworkengineers/import-excel").ToString(),
                    ResponseHeaders = response.Headers.ToString()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel import endpoint test failed");
                var result = new
                {
                    Success = false,
                    Message = $"Excel import endpoint test failed: {ex.Message}"
                };
                
                return Json(result);
            }
        }
    }
}