using Microsoft.Extensions.Configuration;

namespace SFCDashboard.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateRequiredConfiguration(IConfiguration configuration, bool isDevelopment)
    {
        var missingKeys = new List<string>();

        // Validate Azure AD configuration only for non-development environments
        if (!isDevelopment)
        {
            var azureAdSection = configuration.GetSection("AzureAd");
            
            if (string.IsNullOrEmpty(azureAdSection["TenantId"]))
                missingKeys.Add("AZURE_AD_TENANT_ID");
            
            if (string.IsNullOrEmpty(azureAdSection["ClientId"]))
                missingKeys.Add("AZURE_AD_CLIENT_ID");
            
            if (string.IsNullOrEmpty(azureAdSection["ClientSecret"]))
                missingKeys.Add("AZURE_AD_CLIENT_SECRET");
            
            if (string.IsNullOrEmpty(azureAdSection["ApiClientId"]))
                missingKeys.Add("AZURE_AD_API_CLIENT_ID");
        }

        // Validate API Settings
        var apiSettings = configuration.GetSection("ApiSettings");
        if (string.IsNullOrEmpty(apiSettings["BaseUrl"]))
            missingKeys.Add("API_BASE_URL");

        if (missingKeys.Any())
        {
            var message = $"Missing required environment variables: {string.Join(", ", missingKeys)}. " +
                         "Please ensure these are set in your .env file or environment variables.";
            throw new InvalidOperationException(message);
        }
    }
}
