namespace SFCDashboard.ApiClients
{
    public class PermissionsApiClient : IPermissionsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PermissionsApiClient> _logger;

        public PermissionsApiClient(HttpClient httpClient, ILogger<PermissionsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> IsUserAdminAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/is-admin/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var isAdmin) && isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return false; // Default to not admin on error for security
            }
        }

        public async Task<bool> HasPermissionAsync(string serviceId, string permissionName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/has-permission/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(permissionName)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var hasPermission) && hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {ServiceId}", permissionName, serviceId);
                return false; // Default to no permission on error for security
            }
        }
    }
}
