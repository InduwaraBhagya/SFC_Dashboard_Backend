# Azure AD Configuration Quick Reference

## 🎯 **Quick Setup Checklist**

### Azure Portal Configuration
- [ ] App Registration created: "SFC Dashboard"
- [ ] Redirect URIs added: `https://localhost:5001/signin-oidc`
- [ ] API exposed with scope: `access_as_user`
- [ ] API permissions granted: `api://[client ID]/access_as_user`
- [ ] Admin consent granted
- [ ] Client secret created and copied

### Application Configuration
- [ ] User secrets configured with client secret
- [ ] Both appsettings.json files updated
- [ ] Audience configured in API project
- [ ] ApiClientId configured in web project

## 🔧 **Essential Azure AD Values**

```
Application (Client) ID: [client ID]
Directory (Tenant) ID: [tenent ID]
Application ID URI: api://[client ID]
Scope: access_as_user
```

## ⚡ **Quick Commands**

### Set User Secrets (Run in project directories):
```bash
# For API project
cd SFCDashboard.Api
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET_HERE"

# For Web project  
cd SFCDashboard
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET_HERE"
```

### Test URLs:
- **API Health**: https://localhost:7081/health
- **API Swagger**: https://localhost:7081/swagger
- **Web App**: https://localhost:5001

## 🚨 **Critical Steps - Don't Skip!**

1. **Expose an API** - Without this, token validation will fail
2. **Grant Admin Consent** - Required for API permissions
3. **Copy Client Secret** - You only see it once!
4. **Add Redirect URIs** - Authentication will fail without these

## 🔍 **Verification Steps**

1. **Check App Registration**:
   - Azure Portal > Azure AD > App registrations > SFC Dashboard
   - Verify all sections are configured

2. **Test Authentication Flow**:
   ```
   https://login.microsoftonline.com/[tenent ID]/oauth2/v2.0/authorize?client_id=[client ID]&response_type=code&redirect_uri=https://localhost:5001/signin-oidc&scope=openid%20profile%20email
   ```

3. **Test API Access**:
   - Start both applications
   - Login to web app
   - Check if API calls work (no 401 errors)

## 🛠️ **Production Deployment Notes**

When deploying to production:
1. Add production redirect URIs to Azure AD
2. Update CORS settings in API
3. Use Azure Key Vault for client secrets
4. Configure proper SSL certificates
5. Update allowed origins in appsettings

---
*This guide covers the essential Azure AD configuration for the SFC Dashboard authentication system.*
