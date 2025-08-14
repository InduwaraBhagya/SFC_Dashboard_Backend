using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SFCDashboard.Middleware;
using SFCDashboard.ApiClients;
using SFCDashboard.Services;
using SFCDashboard.Handlers;
using SFCDashboard.Configuration;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();


var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use settings from appsettings.json (including HTTPS certificate)
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

// Configure configuration to read from environment variables
builder.Configuration.AddEnvironmentVariables();

// Set Azure AD configuration from environment variables
var azureAdSection = builder.Configuration.GetSection("AzureAd");
azureAdSection["TenantId"] = Environment.GetEnvironmentVariable("AZURE_AD_TENANT_ID") ?? azureAdSection["TenantId"];
azureAdSection["ClientId"] = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID") ?? azureAdSection["ClientId"];
azureAdSection["ClientSecret"] = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_SECRET") ?? azureAdSection["ClientSecret"];
azureAdSection["ApiClientId"] = Environment.GetEnvironmentVariable("AZURE_AD_API_CLIENT_ID") ?? azureAdSection["ApiClientId"];

// Set API Settings from environment variables
var apiSettingsSection = builder.Configuration.GetSection("ApiSettings");
apiSettingsSection["BaseUrl"] = Environment.GetEnvironmentVariable("API_BASE_URL") ?? apiSettingsSection["BaseUrl"];

// Configure EPPlus license globally

builder.Services.AddControllersWithViews();

// Add memory cache
builder.Services.AddMemoryCache();

// Configure HttpClient for API calls



var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5292";

builder.Services.AddHttpClient<IPlannedEventsApiClient, PlannedEventsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IProjectsApiClient, ProjectsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();


builder.Services.AddHttpClient<IRolePermissionsApiClient, RolePermissionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IUserRolesApiClient, UserRolesApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPETasksApiClient, PETasksApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IUsersApiClient, UsersApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPermissionsApiClient, PermissionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPERecordsApiClient, PERecordsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IEscalationsApiClient, EscalationsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPETaskListsApiClient, PETaskListsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPEIssuesApiClient, PEIssuesApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IPEIssueResolutionsApiClient, PEIssueResolutionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IWorkGroupsApiClient, WorkGroupsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<IAreaNetworkEngineersApiClient, AreaNetworkEngineersApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<ICustomerUserAssignmentsApiClient, CustomerUserAssignmentsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<ITaskQueueApiClient, TaskQueueApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<ISubTaskListsApiClient, SubTaskListsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// Load Azure AD Configuration
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
var isDevelopment = builder.Environment.IsDevelopment();

// Validate required configuration
try
{
    ConfigurationValidator.ValidateRequiredConfiguration(builder.Configuration, isDevelopment);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Configuration Error: {ex.Message}");
    Environment.Exit(1);
}

if (!isDevelopment && !string.IsNullOrEmpty(azureAdConfig["ClientId"]))
{
    // Use Azure AD authentication in production
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(azureAdConfig)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();
}
else
{
    // Use a dummy authentication scheme in development to prevent errors
    builder.Services.AddAuthentication("DummyScheme")
        .AddScheme<AuthenticationSchemeOptions, DummyAuthenticationHandler>("DummyScheme", options => { });
}

// Add MVC Controllers with Conditional Authentication
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Add Razor Pages
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
builder.Services.AddHttpContextAccessor();

// Register token acquisition service
if (!isDevelopment)
{
    builder.Services.AddScoped<ITokenAcquisitionService, TokenAcquisitionService>();
    builder.Services.AddTransient<AuthenticationDelegatingHandler>();
}
else
{
    // In development, use a dummy token service that doesn't require authentication
    builder.Services.AddScoped<ITokenAcquisitionService, DummyTokenAcquisitionService>();
    builder.Services.AddTransient<AuthenticationDelegatingHandler>();
}

var app = builder.Build();

// Configure middleware
if (!isDevelopment)
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add cookie clearing middleware before authentication
app.UseCookieClearing(); // Server-side cookie clearing enabled

app.UseAuthentication(); // Must be called, even in development mode
app.UseAuthorization();

app.UseUserRegistration();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PlannedEvents}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// This ensures the upload directory exists when the application starts
app.Use(async (context, next) =>
{
    var uploadsDirectory = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "surveys");
    if (!Directory.Exists(uploadsDirectory))
    {
        Directory.CreateDirectory(uploadsDirectory);
    }

    await next();
});

app.Run();