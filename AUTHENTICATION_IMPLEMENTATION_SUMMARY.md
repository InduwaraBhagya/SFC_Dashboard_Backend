# Azure Token-Based API Authentication Implementation Summary

## Overview
Successfully implemented a comprehensive Azure token-based authentication system for the SFC Dashboard application, consisting of:
- **SFCDashboard.Api** - Backend API with JWT Bearer authentication
- **SFCDashboard** - Frontend web application with token acquisition for API calls

## 🔧 Components Implemented

### 1. API Project (SFCDashboard.Api) Changes

#### A. Authentication Configuration
- **Production**: JWT Bearer authentication using Azure AD tokens
- **Development**: Dummy authentication scheme for local testing
- **Global Authorization**: All API endpoints require authentication (except /health)

#### B. New Files Created
- `DummyAuthenticationHandler.cs` - Development authentication handler

#### C. Configuration Updates
- Updated `appsettings.json` to include Azure AD audience
- Enhanced Swagger configuration with OAuth2 support for production

### 2. Web Application (SFCDashboard) Changes

#### A. Token Acquisition Service
- `TokenAcquisitionService.cs` - Service to acquire Azure AD tokens for API calls
- `DummyTokenAcquisitionService.cs` - Development version that skips token acquisition

#### B. Authentication Handler
- `AuthenticationDelegatingHandler.cs` - Automatically adds Bearer tokens to HttpClient requests

#### C. Enhanced Authentication Setup
- Production: Azure AD with downstream API token acquisition
- Development: Dummy scheme compatible with API's dummy authentication

## 🔑 Key Features

### Security Features
1. **JWT Bearer Token Validation** - API validates Azure AD tokens
2. **Automatic Token Acquisition** - Web app automatically gets tokens for API calls
3. **Token Caching** - In-memory token caching for performance
4. **Development Mode** - Bypasses authentication in development

### API Protection
1. **Global Authorization Policy** - All controllers require authentication
2. **Health Check Exception** - `/health` endpoint allows anonymous access
3. **Swagger Integration** - OAuth2 authentication in Swagger UI

### Token Management
1. **Automatic Header Injection** - Bearer tokens added to all API calls
2. **Error Handling** - Graceful handling of token acquisition failures
3. **Scope Configuration** - Proper Azure AD scope setup

## 🛠️ Configuration Required

### Azure AD App Registration Setup - DETAILED GUIDE

#### Step 1: Create App Registration
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
3. Fill in the details:
   - **Name**: `SFC Dashboard`
   - **Supported account types**: `Accounts in this organizational directory only (Single tenant)`
   - **Redirect URI**: 
     - Platform: `Web`
     - URL: `https://localhost:5001/signin-oidc` (for development)
     - Add production URL when deploying

#### Step 2: Configure Authentication
1. In your app registration, go to **Authentication**
2. Add redirect URIs:
   - `https://localhost:5001/signin-oidc` (Development)
   - `https://your-production-domain.com/signin-oidc` (Production)
3. Under **Front-channel logout URL**: 
   - `https://localhost:5001/signout-oidc`
4. Under **Implicit grant and hybrid flows**:
   - ✅ Check **ID tokens**
   - ✅ Check **Access tokens**

#### Step 3: Expose an API (Critical for API Authentication)
1. Go to **Expose an API**
2. Click **Set** next to Application ID URI
3. Accept the default: `api://[client ID]`
4. Click **Add a scope**:
   - **Scope name**: `access_as_user`
   - **Who can consent**: `Admins and users`
   - **Admin consent display name**: `Access SFC Dashboard API`
   - **Admin consent description**: `Allow the application to access SFC Dashboard API on behalf of the signed-in user`
   - **User consent display name**: `Access SFC Dashboard API`
   - **User consent description**: `Allow the application to access SFC Dashboard API on your behalf`
   - **State**: `Enabled`

#### Step 4: Configure API Permissions
1. Go to **API permissions**
2. Click **Add a permission**
3. Select **My APIs** tab
4. Find and select your app (SFC Dashboard)
5. Select **Delegated permissions**
6. Check ✅ `access_as_user`
7. Click **Add permissions**
8. Click **Grant admin consent for [Your Organization]** (Important!)

#### Step 5: Create Client Secret
1. Go to **Certificates & secrets**
2. Click **New client secret**
3. **Description**: `SFC Dashboard Secret`
4. **Expires**: Choose appropriate duration (24 months recommended)
5. Click **Add**
6. **IMPORTANT**: Copy the secret value immediately (you won't see it again!)

#### Step 6: Note Important Values
Copy these values for your configuration:
- **Application (client) ID**: `example`
- **Directory (tenant) ID**: `example`
- **Client Secret**: `[The value you just copied]`
- **Application ID URI**: `api://[client ID]`

### Required Azure AD Configuration Summary:

1. **API Permissions**:
   ```
   api://[client ID]/access_as_user
   ```

2. **Expose an API**:
   - Application ID URI: `api://[client ID]`
   - Scope: `access_as_user`

3. **Client Secret**: Update both appsettings files with the actual client secret

### Configuration Files Updated

#### Option 1: Using User Secrets (Recommended for Development)
For security, use .NET User Secrets to store the client secret:

1. **Set up User Secrets for API project**:
   ```bash
   cd SFCDashboard.Api
   dotnet user-secrets init
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_ACTUAL_CLIENT_SECRET_HERE"
   ```

2. **Set up User Secrets for Web project**:
   ```bash
   cd SFCDashboard
   dotnet user-secrets init
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_ACTUAL_CLIENT_SECRET_HERE"
   ```

#### Option 2: Update appsettings.json (Less Secure)
⚠️ **Warning**: Only use this for development, never commit real secrets to source control!


```

## 🚀 How It Works

### Production Flow
1. User logs into web application via Azure AD
2. Web app acquires token with API scope when making API calls
3. AuthenticationDelegatingHandler adds Bearer token to HttpClient requests
4. API validates JWT token and processes request

### Development Flow
1. Web app uses dummy authentication
2. API calls skip token acquisition (DummyTokenAcquisitionService)
3. API accepts all requests with dummy authentication handler

## 📋 Next Steps

### To Complete Implementation

1. **✅ Update Remaining HttpClients**: ~~Add `.AddHttpMessageHandler<AuthenticationDelegatingHandler>()` to all HttpClient registrations in SFCDashboard/Program.cs~~ **COMPLETED**

2. **Configure Azure AD**: Set up proper API permissions and scopes in Azure portal

3. **Update Client Secret**: Replace placeholder client secret with actual value

4. **Test Authentication**: 
   - Test API endpoints with valid tokens
   - Verify token acquisition in web application
   - Test Swagger UI OAuth2 flow

### ✅ HttpClient Updates Completed
All HttpClient configurations in the web application now include the `AuthenticationDelegatingHandler`, which means:
- **Automatic token acquisition** for all API calls
- **Bearer token injection** in all HTTP requests to the API
- **Seamless authentication** across all API client services

## 🔍 Testing

### Step-by-Step Testing Guide

#### 1. Test Azure AD Configuration
Before testing the application, verify your Azure AD setup:

1. **Verify App Registration**:
   - Go to Azure Portal > Azure AD > App registrations
   - Find your "SFC Dashboard" app
   - Check that all redirect URIs are configured
   - Verify the API scope `access_as_user` exists under "Expose an API"
   - Confirm API permissions include your app's `access_as_user` scope
   - Ensure admin consent has been granted

2. **Test Authentication URL**:
   Visit this URL to test basic Azure AD authentication:
   ```
   https://login.microsoftonline.com/[tenent ID]/oauth2/v2.0/authorize?client_id=[client ID]&response_type=code&redirect_uri=https://localhost:5001/signin-oidc&scope=openid%20profile%20email
   ```

#### 2. Test Development Mode
1. **Start both projects**:
   ```bash
   # Terminal 1 - Start API
   cd SFCDashboard.Api
   dotnet run
   
   # Terminal 2 - Start Web App
   cd SFCDashboard
   dotnet run
   ```

2. **Test API Health Check**:
   - Navigate to: `https://localhost:7081/health`
   - Should return: `{"Status":"Healthy","Timestamp":"..."}`

3. **Test Web Application**:
   - Navigate to: `https://localhost:5001`
   - Should load without authentication errors
   - Check browser console for any errors

#### 3. Test Production Mode Authentication
1. **Update appsettings to force production mode** (temporarily):
   ```json
   // In both appsettings.json files, ensure AzureAd section has ClientSecret
   ```

2. **Test with real Azure AD**:
   - Navigate to web application
   - Should redirect to Azure AD login
   - Login with your organizational account
   - Should redirect back to application

#### 4. API Testing
- Use Swagger UI at `https://localhost:7081` (API project)
- Authenticate using OAuth2 flow in Swagger
- Test protected endpoints

### Web Application Testing
- Navigate to web application
- Login with Azure AD credentials
- Verify API calls work seamlessly

## 🐛 Troubleshooting

### Common Issues and Solutions

#### 1. "AADSTS50011: The reply URL specified in the request does not match..."
**Solution**: 
- Check redirect URIs in Azure AD app registration
- Ensure `https://localhost:5001/signin-oidc` is added
- Case-sensitive URL matching

#### 2. "AADSTS65001: The user or administrator has not consented..."
**Solution**:
- Go to Azure AD > App registrations > Your app > API permissions
- Click "Grant admin consent for [Organization]"
- Ensure `access_as_user` scope is properly configured

#### 3. "401 Unauthorized" from API endpoints
**Solution**:
- Verify API permissions include your app's scope
- Check that `Audience` in API appsettings.json matches App ID URI
- Ensure client secret is correctly configured

#### 4. Token acquisition fails in web application
**Solution**:
- Check that `EnableTokenAcquisitionToCallDownstreamApi()` is called
- Verify API scope configuration: `api://[client-id]/access_as_user`
- Check user consent for API permissions

#### 5. Development mode not working
**Solution**:
- Ensure `IsDevelopment()` returns true
- Check that DummyAuthenticationHandler is registered
- Verify dummy services are being used

### Debugging Tips

1. **Enable detailed logging**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore.Authentication": "Debug",
         "Microsoft.Identity.Web": "Debug"
       }
     }
   }
   ```

2. **Check browser network tab** for 401/403 responses

3. **Use Fiddler or similar** to inspect HTTP requests and token headers

4. **Azure AD logs**: Check sign-in logs in Azure Portal for authentication failures

## 🛡️ Security Considerations

1. **Token Validation**: API properly validates Azure AD tokens
2. **Scope Isolation**: API only accepts tokens with correct scope
3. **Development Security**: Dummy authentication only active in development
4. **Error Handling**: Token failures are logged but don't expose sensitive information

This implementation provides a robust, production-ready authentication system while maintaining development workflow efficiency.
