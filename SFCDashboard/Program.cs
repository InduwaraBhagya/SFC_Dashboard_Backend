using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Controllers;
using SFCDashboard.Middleware;
using SFCDashboard.ApiClients;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using OfficeOpenXml; // Add EPPlus namespace

var builder = WebApplication.CreateBuilder(args);

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
});

builder.Services.AddHttpClient<IPETasksApiClient, PETasksApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IUsersApiClient, UsersApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IPermissionsApiClient, PermissionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IPERecordsApiClient, PERecordsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IEscalationsApiClient, EscalationsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IPETaskListsApiClient, PETaskListsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IPEIssuesApiClient, PEIssuesApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IPEIssueResolutionsApiClient, PEIssueResolutionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IWorkGroupsApiClient, WorkGroupsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IAreaNetworkEngineersApiClient, AreaNetworkEngineersApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<ICustomerUserAssignmentsApiClient, CustomerUserAssignmentsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<ITaskQueueApiClient, TaskQueueApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Load Azure AD Configuration
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
var isDevelopment = builder.Environment.IsDevelopment();

if (!isDevelopment && !string.IsNullOrEmpty(azureAdConfig["ClientId"]))
{
    // Use Azure AD authentication in production
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(azureAdConfig);
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

app.UseAuthentication(); // Must be called, even in development mode
app.UseAuthorization();

app.UseUserRegistration();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")  // Changed from PlannedEvents to Home
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