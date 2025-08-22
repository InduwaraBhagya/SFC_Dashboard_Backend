using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class AreaNetworkEngineersApiClient : IAreaNetworkEngineersApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AreaNetworkEngineersApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<AreaNetworkEngineer>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/areanetworkengineers");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                    return new List<AreaNetworkEngineer>();
                    
                return JsonSerializer.Deserialize<IEnumerable<AreaNetworkEngineer>>(json, _jsonOptions) ?? new List<AreaNetworkEngineer>();
            }
            catch (JsonException)
            {
                return new List<AreaNetworkEngineer>();
            }
            catch (HttpRequestException)
            {
                return new List<AreaNetworkEngineer>();
            }
        }

        public async Task<AreaNetworkEngineer?> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/areanetworkengineers/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                    
                return JsonSerializer.Deserialize<AreaNetworkEngineer>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<AreaNetworkEngineer> CreateAsync(AreaNetworkEngineer areaNetworkEngineer)
        {
            try
            {
                var json = JsonSerializer.Serialize(areaNetworkEngineer, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/areanetworkengineers", content);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseJson))
                    throw new InvalidOperationException("Empty response from server");
                    
                return JsonSerializer.Deserialize<AreaNetworkEngineer>(responseJson, _jsonOptions)!;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize server response", ex);
            }
        }

        public async Task<AreaNetworkEngineer> UpdateAsync(AreaNetworkEngineer areaNetworkEngineer)
        {
            try
            {
                var json = JsonSerializer.Serialize(areaNetworkEngineer, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/areanetworkengineers/{areaNetworkEngineer.Id}", content);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseJson))
                    throw new InvalidOperationException("Empty response from server");
                    
                return JsonSerializer.Deserialize<AreaNetworkEngineer>(responseJson, _jsonOptions)!;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize server response", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/areanetworkengineers/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to delete engineer with ID {id}", ex);
            }
        }

        public async Task<string?> GetEngineerNameByAreaAsync(string area)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/areanetworkengineers/by-area/{area}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                    
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                return result?.GetValueOrDefault("engineerName");
            }
            catch (JsonException)
            {
                return null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<string> ImportExcelAsync(Stream excelStream, string fileName, CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(excelStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContent, "excelFile", fileName);

            var response = await _httpClient.PostAsync("api/areanetworkengineers/import-excel", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Import failed: {(int)response.StatusCode} {response.ReasonPhrase} - {responseContent}");
            }
            return responseContent;
        }
    }
}
