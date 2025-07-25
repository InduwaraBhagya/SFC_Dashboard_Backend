using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Data;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class NetworkEngineerController : BaseController
    {
        private readonly IAreaNetworkEngineersApiClient _areaNetworkEngineersApiClient;

        public NetworkEngineerController(IAreaNetworkEngineersApiClient areaNetworkEngineersApiClient, IUsersApiClient usersApiClient) : base(usersApiClient)
        {
            _areaNetworkEngineersApiClient = areaNetworkEngineersApiClient;
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
                using (var httpClient = new HttpClient())
                using (var content = new MultipartFormDataContent())
                using (var stream = excelFile.OpenReadStream())
                {
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    content.Add(fileContent, "excelFile", excelFile.FileName);

                    // Adjust the API URL as needed
                    var apiUrl = "/api/areanetworkengineers/import-excel";
                    var baseUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    var response = await httpClient.PostAsync(baseUrl + apiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        TempData["Message"] = $"Import successful: {result}";
                    }
                    else
                    {
                        TempData["Message"] = $"Import failed: {response.ReasonPhrase}";
                    }
                }
            }
            catch (Exception ex)
            {
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
                var mappings = (await _areaNetworkEngineersApiClient.GetAllAsync()).ToList();
                return View(mappings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load network engineers: {ex.Message}";
                return View(new List<AreaNetworkEngineer>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ImportExcel()
        {
            try
            {
                var mappings = (await _areaNetworkEngineersApiClient.GetAllAsync()).ToList();
                return View(mappings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load network engineers: {ex.Message}";
                return View(new List<AreaNetworkEngineer>());
            }
        }
    }
}