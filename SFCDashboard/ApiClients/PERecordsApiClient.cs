using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SFCDB.Models;

namespace SFCDashboard.ApiClients
{
    public class PERecordsApiClient : IPERecordsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PERecordsApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PERecordsApiClient(HttpClient httpClient, ILogger<PERecordsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<ApiResponse> ImportPERecordsAsync(IFormFile excelFile)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(excelFile.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(excelFile.ContentType);
                content.Add(fileContent, "excelFile", excelFile.FileName);

                var response = await _httpClient.PostAsync("api/perecords/import", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                    return result ?? new ApiResponse { Success = false, Message = "Failed to deserialize response" };
                }

                _logger.LogError("Failed to import PE records. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new ApiResponse { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing PE records");
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> ImportPERecordsFromJsonAsync(List<PERecord> peRecords)
        {
            try
            {
                var json = JsonSerializer.Serialize(peRecords, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/perecords/import-json", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                    return result ?? new ApiResponse { Success = false, Message = "Failed to deserialize response" };
                }

                _logger.LogError("Failed to import PE records from JSON. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new ApiResponse { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing PE records from JSON");
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<PaginatedResult<PERecord>>> GetPERecordsAsync(int page = 1, int pageSize = 1000)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/perecords?page={page}&pageSize={pageSize}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PaginatedResult<PERecord>>>(responseContent, _jsonOptions);
                    return result ?? new ApiResponse<PaginatedResult<PERecord>> { Success = false, Message = "Failed to deserialize response" };
                }

                _logger.LogError("Failed to get PE records. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new ApiResponse<PaginatedResult<PERecord>> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE records");
                return new ApiResponse<PaginatedResult<PERecord>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<DatabaseStats>> GetDatabaseStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/perecords/stats");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DatabaseStats>>(responseContent, _jsonOptions);
                    return result ?? new ApiResponse<DatabaseStats> { Success = false, Message = "Failed to deserialize response" };
                }

                _logger.LogError("Failed to get database stats. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new ApiResponse<DatabaseStats> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database stats");
                return new ApiResponse<DatabaseStats> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> SyncPERecordsAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/perecords/sync", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                    return result ?? new ApiResponse { Success = false, Message = "Failed to deserialize response" };
                }

                _logger.LogError("Failed to sync PE records. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new ApiResponse { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing PE records");
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
