# Environment Configuration

This application uses environment variables to manage sensitive configuration values like Azure AD credentials, connection strings, and API endpoints.

## Setup Instructions

### 1. Copy Environment Files

Copy the example environment files and update them with your actual values:

```bash
# For SFCDashboard project
cp SFCDashboard\.env.example SFCDashboard\.env

# For SFCDashboard.Api project
cp SFCDashboard.Api\.env.example SFCDashboard.Api\.env
```

### 2. Configure Environment Variables

#### SFCDashboard Project (.env)

```bash
# Azure AD Configuration
AZURE_AD_TENANT_ID=your-tenant-id
AZURE_AD_CLIENT_ID=your-client-id
AZURE_AD_CLIENT_SECRET=your-client-secret
AZURE_AD_API_CLIENT_ID=your-api-client-id

# API Settings
API_BASE_URL=http://localhost:5292

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

#### SFCDashboard.Api Project (.env)

```bash
# Azure AD Configuration
AZURE_AD_TENANT_ID=your-tenant-id
AZURE_AD_CLIENT_ID=your-client-id
AZURE_AD_CLIENT_SECRET=your-client-secret
AZURE_AD_AUDIENCE=api://your-api-client-id

# Database Connection
DEFAULT_CONNECTION_STRING=Server=your-server;Database=your-database;Trusted_Connection=True;TrustServerCertificate=True;

# API Settings
API_BASE_URL=https://localhost:7081

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

### 3. Azure AD Configuration Values

You can find these values in the Azure Portal:

1. **Tenant ID**: Azure Active Directory > Properties > Directory ID
2. **Client ID**: App registrations > Your App > Application (client) ID
3. **Client Secret**: App registrations > Your App > Certificates & secrets
4. **Audience**: Usually in the format `api://your-client-id`

### 4. Security Notes

- **Never commit .env files to version control** - they are already added to .gitignore
- The .env.example files serve as templates and should be committed
- Use Azure Key Vault or similar services for production environments
- Consider using User Secrets for development environments as an alternative

### 5. Production Deployment

For production environments, set these environment variables directly in your hosting environment:

- Azure App Service: Configuration > Application settings
- Docker: Docker environment variables
- IIS: Environment variables in web.config or application settings
- Linux: Export statements in shell profiles

### 6. Validation

The application will validate that all required environment variables are set during startup. If any are missing, the application will display an error message and exit.

### 7. Development vs Production

- In development mode, Azure AD authentication can be bypassed using a dummy authentication handler
- In production mode, all Azure AD configuration values are required
- Connection strings and API URLs should be environment-specific

## Troubleshooting

### Common Issues

1. **Missing Environment Variables**: Check that your .env file exists and contains all required values
2. **Azure AD Authentication Failures**: Verify your client ID, tenant ID, and client secret are correct
3. **Database Connection Issues**: Ensure your connection string format is correct and the database is accessible

### Debugging

To debug configuration issues, you can temporarily add logging to see which values are being loaded:

```csharp
Console.WriteLine($"Tenant ID: {Environment.GetEnvironmentVariable("AZURE_AD_TENANT_ID")}");
Console.WriteLine($"Client ID: {Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID")}");
// Don't log secrets in production!
```

## Migration from appsettings.json

If you're migrating from hardcoded values in appsettings.json:

1. Copy the values from your appsettings.json to the appropriate .env file
2. Clear the values in appsettings.json (they should be empty strings)
3. Test that the application starts correctly with the new configuration
4. Remove any hardcoded sensitive values from your codebase
