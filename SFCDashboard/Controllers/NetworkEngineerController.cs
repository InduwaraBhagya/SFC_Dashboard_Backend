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
                
                // Use the configured API base URL from HttpClient
                using (var httpClient = new HttpClient())
                using (var content = new MultipartFormDataContent())
                using (var stream = excelFile.OpenReadStream())
                {
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    content.Add(fileContent, "excelFile", excelFile.FileName);

                    // Get the API base URL from configuration
                    var apiBaseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ApiSettings:BaseUrl"];
                    if (string.IsNullOrEmpty(apiBaseUrl))
                    {
                        // Fallback to local API
                        apiBaseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                    }
                    
                    var apiUrl = $"{apiBaseUrl}/api/areanetworkengineers/import-excel";
                    _logger.LogInformation($"Posting to API URL: {apiUrl}");
                    
                    var response = await httpClient.PostAsync(apiUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Import successful: {responseContent}");
                        TempData["Message"] = $"Import successful! {responseContent}";
                    }
                    else
                    {
                        _logger.LogError($"Import failed with status {response.StatusCode}: {responseContent}");
                        TempData["Message"] = $"Import failed: {response.ReasonPhrase} - {responseContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                TempData["Message"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("ImportExcel");
        }

        public IActionResult Index()
        {
            return RedirectToAction("ImportExcel");
        }

        public IActionResult Create() => View();

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
            return RedirectToAction("ImportExcel");
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
                var apiBaseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ApiSettings:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    apiBaseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                }
                
                var apiUrl = $"{apiBaseUrl}/api/areanetworkengineers/import-excel";
                
                using (var httpClient = new HttpClient())
                {
                    // Test if the endpoint exists with a HEAD request
                    var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Options, apiUrl));
                    
                    var result = new
                    {
                        Success = response.IsSuccessStatusCode,
                        Message = $"Excel import endpoint test: {response.StatusCode}",
                        Url = apiUrl,
                        ResponseHeaders = response.Headers.ToString()
                    };
                    
                    return Json(result);
                }
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