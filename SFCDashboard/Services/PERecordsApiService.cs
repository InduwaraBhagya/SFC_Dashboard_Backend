using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Controllers.Api;

namespace SFCDashboard.Services
{
    public class PERecordsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PERecordsApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly PERecordsApiController _apiController;
        private readonly bool _useInternalApi;

        public PERecordsApiService(
            HttpClient httpClient, 
            ILogger<PERecordsApiService> logger, 
            IConfiguration configuration,
            PERecordsApiController apiController)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiController = apiController;
            _useInternalApi = _configuration.GetValue<bool>("ApiSettings:UseInternalApi", true);
        }

        /// <summary>
        /// Import PE Records via API
        /// </summary>
        /// <param name="excelFile">Excel file to import</param>
        /// <returns>API response result</returns>
        public async Task<ApiResponse> ImportPERecordsAsync(IFormFile excelFile)
        {
            try
            {
                if (_useInternalApi)
                {
                    // Call the API controller directly (internal call)
                    _logger.LogInformation("Using internal API call for import");
                    var result = await _apiController.ImportPERecords(excelFile);
                    
                    if (result is OkObjectResult okResult)
                    {
                        var response = okResult.Value;
                        return ParseApiResponse(response ?? new { success = false, message = "No response data" });
                    }
                    else if (result is BadRequestObjectResult badResult)
                    {
                        var response = badResult.Value;
                        return ParseApiResponse(response ?? new { success = false, message = "Bad request" });
                    }
                    else if (result is ObjectResult objectResult)
                    {
                        var response = objectResult.Value;
                        return ParseApiResponse(response ?? new { success = false, message = "Unknown response" });
                    }
                    else
                    {
                        return new ApiResponse { Success = false, Message = "Unknown error occurred" };
                    }
                }
                else
                {
                    // Call external API via HTTP
                    return await CallExternalApiAsync(excelFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling import API");
                return new ApiResponse { Success = false, Message = $"Error calling API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Get database statistics via API
        /// </summary>
        /// <returns>Database statistics</returns>
        public async Task<DatabaseStats> GetDatabaseStatsAsync()
        {
            try
            {
                if (_useInternalApi)
                {
                    // Call the API controller directly (internal call)
                    _logger.LogInformation("Using internal API call for stats");
                    var result = await _apiController.GetDatabaseStats();
                    
                    if (result is OkObjectResult okResult)
                    {
                        var response = okResult.Value;
                        return ParseStatsResponse(response ?? new { success = false, data = new DatabaseStats() });
                    }
                    else
                    {
                        _logger.LogError("Internal API call failed");
                        return new DatabaseStats();
                    }
                }
                else
                {
                    // Call external API via HTTP
                    return await CallExternalStatsApiAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling stats API");
                return new DatabaseStats();
            }
        }

        private async Task<ApiResponse> CallExternalApiAsync(IFormFile excelFile)
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
            
            // Set a longer timeout for large file uploads
            var timeoutMinutes = _configuration.GetValue<int>("ApiSettings:TimeoutMinutes", 10);
            _httpClient.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
            
            using var content = new MultipartFormDataContent();
            using var fileStream = excelFile.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(streamContent, "excelFile", excelFile.FileName);

            try
            {
                var response = await _httpClient.PostAsync($"{baseUrl}/api/perecordsapi/import", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return result ?? new ApiResponse { Success = false, Message = "Invalid response format" };
                }
                else
                {
                    _logger.LogError("External API call failed with status code: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return errorResult ?? new ApiResponse { Success = false, Message = "Unknown error occurred" };
                    }
                    catch
                    {
                        return new ApiResponse { Success = false, Message = $"API call failed: {response.StatusCode}" };
                    }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "API call timed out after {TimeoutMinutes} minutes", timeoutMinutes);
                return new ApiResponse { Success = false, Message = $"Import timed out after {timeoutMinutes} minutes. Please try with a smaller file or contact support." };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "API call was cancelled");
                return new ApiResponse { Success = false, Message = "Import was cancelled. Please try again." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during API call");
                return new ApiResponse { Success = false, Message = $"Unexpected error: {ex.Message}" };
            }
        }

        private async Task<DatabaseStats> CallExternalStatsApiAsync()
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
            
            var response = await _httpClient.GetAsync($"{baseUrl}/api/perecordsapi/stats");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiStatsResponse>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result?.Data ?? new DatabaseStats();
            }
            else
            {
                _logger.LogError("External API call failed with status code: {StatusCode}", response.StatusCode);
                return new DatabaseStats();
            }
        }

        private ApiResponse ParseApiResponse(object response)
        {
            try
            {
                if (response == null) return new ApiResponse { Success = false, Message = "No response received" };
                
                var json = JsonSerializer.Serialize(response);
                var result = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result ?? new ApiResponse { Success = false, Message = "Invalid response format" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing API response");
                return new ApiResponse { Success = false, Message = "Error parsing response" };
            }
        }

        private DatabaseStats ParseStatsResponse(object response)
        {
            try
            {
                if (response == null) return new DatabaseStats();
                
                var json = JsonSerializer.Serialize(response);
                var result = JsonSerializer.Deserialize<ApiStatsResponse>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result?.Data ?? new DatabaseStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing stats response");
                return new DatabaseStats();
            }
        }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public int PlannedEventCount { get; set; }
        public int PeTaskCount { get; set; }
        public bool SyncCompleted { get; set; }
        public string? SyncError { get; set; }
    }

    public class ApiStatsResponse
    {
        public bool Success { get; set; }
        public DatabaseStats Data { get; set; } = new();
    }

    public class DatabaseStats
    {
        public int PeRecordsCount { get; set; }
        public int PlannedEventsCount { get; set; }
        public int PeTasksCount { get; set; }
        public int TaskTemplatesCount { get; set; }
    }
}
