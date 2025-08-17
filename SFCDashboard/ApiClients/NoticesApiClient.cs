using System.Text;
using System.Text.Json;
using SFCDB.Models;

namespace SFCDashboard.ApiClients
{
    public class NoticesApiClient : INoticesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NoticesApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public NoticesApiClient(HttpClient httpClient, ILogger<NoticesApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<ApiResponse<List<Notice>>> GetNoticesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/NoticesApi");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var notices = JsonSerializer.Deserialize<List<Notice>>(dataProp.GetRawText(), _jsonOptions);
                            return new ApiResponse<List<Notice>> { Success = true, Message = "Success", Data = notices };
                        }
                    }

                    return new ApiResponse<List<Notice>> { Success = false, Message = "Invalid response format" };
                }

                _logger.LogError("Failed to get notices. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                return new ApiResponse<List<Notice>> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notices");
                return new ApiResponse<List<Notice>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<Notice>> GetNoticeAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/NoticesApi/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var notice = JsonSerializer.Deserialize<Notice>(dataProp.GetRawText(), _jsonOptions);
                            return new ApiResponse<Notice> { Success = true, Message = "Success", Data = notice };
                        }
                    }

                    return new ApiResponse<Notice> { Success = false, Message = "Invalid response format" };
                }

                _logger.LogError("Failed to get notice {NoticeId}. Status: {StatusCode}, Response: {Response}",
                    id, response.StatusCode, responseContent);
                return new ApiResponse<Notice> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notice {NoticeId}", id);
                return new ApiResponse<Notice> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<Notice>> CreateNoticeAsync(string description, int createdBy, string createdUserName, bool isPinned = false)
        {
            try
            {
                var request = new NoticeCreateRequest
                {
                    Description = description,
                    CreatedBy = createdBy,
                    CreatedUserName = createdUserName,
                    IsPinned = isPinned
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/NoticesApi", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var notice = JsonSerializer.Deserialize<Notice>(dataProp.GetRawText(), _jsonOptions);
                            return new ApiResponse<Notice> { Success = true, Message = "Success", Data = notice };
                        }
                    }

                    return new ApiResponse<Notice> { Success = false, Message = "Invalid response format" };
                }

                _logger.LogError("Failed to create notice. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                return new ApiResponse<Notice> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notice");
                return new ApiResponse<Notice> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<Notice>> UpdateNoticeAsync(int id, string description, int updatedBy, string updatedUserName)
        {
            try
            {
                var request = new NoticeUpdateRequest
                {
                    Description = description,
                    UpdatedBy = updatedBy,
                    UpdatedUserName = updatedUserName
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/NoticesApi/{id}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var notice = JsonSerializer.Deserialize<Notice>(dataProp.GetRawText(), _jsonOptions);
                            return new ApiResponse<Notice> { Success = true, Message = "Success", Data = notice };
                        }
                    }

                    return new ApiResponse<Notice> { Success = false, Message = "Invalid response format" };
                }

                _logger.LogError("Failed to update notice {NoticeId}. Status: {StatusCode}, Response: {Response}",
                    id, response.StatusCode, responseContent);
                return new ApiResponse<Notice> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notice {NoticeId}", id);
                return new ApiResponse<Notice> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<Notice>> TogglePinNoticeAsync(int id, bool isPinned, int updatedBy, string updatedUserName)
        {
            try
            {
                var request = new NoticePinRequest
                {
                    IsPinned = isPinned,
                    UpdatedBy = updatedBy,
                    UpdatedUserName = updatedUserName
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"api/NoticesApi/{id}/pin", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var notice = JsonSerializer.Deserialize<Notice>(dataProp.GetRawText(), _jsonOptions);
                            return new ApiResponse<Notice> { Success = true, Message = "Success", Data = notice };
                        }
                    }

                    return new ApiResponse<Notice> { Success = false, Message = "Invalid response format" };
                }

                _logger.LogError("Failed to toggle pin notice {NoticeId}. Status: {StatusCode}, Response: {Response}",
                    id, response.StatusCode, responseContent);
                return new ApiResponse<Notice> { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin notice {NoticeId}", id);
                return new ApiResponse<Notice> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> DeleteNoticeAsync(int id, int updatedBy, string updatedUserName)
        {
            try
            {
                var request = new NoticeDeleteRequest
                {
                    UpdatedBy = updatedBy,
                    UpdatedUserName = updatedUserName
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/NoticesApi/{id}")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        return new ApiResponse { Success = true, Message = "Success" };
                    }

                    var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Invalid response format";
                    return new ApiResponse { Success = false, Message = message ?? "Invalid response format" };
                }

                _logger.LogError("Failed to delete notice {NoticeId}. Status: {StatusCode}, Response: {Response}",
                    id, response.StatusCode, responseContent);
                return new ApiResponse { Success = false, Message = $"API call failed with status {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notice {NoticeId}", id);
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
