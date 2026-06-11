using SFCDashboard.Services;

namespace SFCDashboard.Handlers
{
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly ITokenAcquisitionService _tokenService;
        private readonly ILogger<AuthenticationDelegatingHandler> _logger;

        public AuthenticationDelegatingHandler(
            ITokenAcquisitionService tokenService,
            ILogger<AuthenticationDelegatingHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Add authorization header if not already present
                if (request.Headers.Authorization == null)
                {
                    var accessToken = await _tokenService.GetAccessTokenAsync();
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add authentication header to request");
                // Continue without auth header in case of failure
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
