using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace SFCDashboard.Services
{
    public interface ITokenAcquisitionService
    {
        Task<string> GetAccessTokenAsync();
        Task AddAuthorizationHeaderAsync(HttpClient httpClient);
    }

    public class TokenAcquisitionService : ITokenAcquisitionService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenAcquisitionService> _logger;

        public TokenAcquisitionService(
            ITokenAcquisition tokenAcquisition, 
            IConfiguration configuration,
            ILogger<TokenAcquisitionService> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var apiClientId = _configuration["AzureAd:ApiClientId"] ?? _configuration["AzureAd:ClientId"];
                var scopes = new[] { $"api://{apiClientId}/access_as_user" };
                
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire access token");
                throw;
            }
        }

        public async Task AddAuthorizationHeaderAsync(HttpClient httpClient)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add authorization header");
                throw;
            }
        }
    }

    // Dummy implementation for development
    public class DummyTokenAcquisitionService : ITokenAcquisitionService
    {
        public Task<string> GetAccessTokenAsync()
        {
            // Return a dummy token for development
            return Task.FromResult("dummy-development-token");
        }

        public Task AddAuthorizationHeaderAsync(HttpClient httpClient)
        {
            // In development, don't add any authorization header since API uses dummy auth
            return Task.CompletedTask;
        }
    }
}
