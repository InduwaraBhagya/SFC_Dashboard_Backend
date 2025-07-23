using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Services;
using SFCDashboard.Controllers;
using SFCDashboard.Middleware;
using SFCDashboard.ApiClients;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using OfficeOpenXml; // Add EPPlus namespace

var builder = WebApplication.CreateBuilder(args);

// Configure EPPlus license globally

builder.Services.AddControllersWithViews();

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())); // Add this line

// First register PERecordSyncService as a singleton so it can be retrieved
builder.Services.AddSingleton<PERecordSyncService>();
// Then register it as a hosted service using the same instance
builder.Services.AddHostedService(provider => provider.GetRequiredService<PERecordSyncService>());
builder.Services.AddHostedService<OLAViolationService>();
builder.Services.AddHostedService<HoldTaskReminderService>(); // Add this line
builder.Services.AddScoped<EscalationService>();
builder.Services.AddHostedService<EscalationBackgroundService>();
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
builder.Services.AddScoped<ITaskQueueService, TaskQueueingService>();

// Add CORS configuration for API endpoints
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register API Services
builder.Services.AddHttpClient<PERecordsApiService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for large Excel imports
});
builder.Services.AddScoped<PERecordsApiService>();
builder.Services.AddScoped<SFCDashboard.Controllers.Api.PERecordsApiController>();
builder.Services.AddScoped<IPlannedEventsApiService, PlannedEventsApiService>();
builder.Services.AddScoped<IUsersApiService, UsersApiService>();
builder.Services.AddScoped<IPETasksApiService, PETasksApiService>();
builder.Services.AddScoped<IPEIssuesApiService, PEIssuesApiService>();
builder.Services.AddScoped<IWorkGroupsApiService, WorkGroupsApiService>();
builder.Services.AddScoped<IAreaNetworkEngineersApiService, AreaNetworkEngineersApiService>();
builder.Services.AddScoped<IPETaskListsApiService, PETaskListsApiService>();
builder.Services.AddScoped<IEscalationsApiService, EscalationsApiService>();
builder.Services.AddScoped<IPEIssueResolutionsApiService, PEIssueResolutionsApiService>();
builder.Services.AddScoped<ICustomerUserAssignmentsApiService, CustomerUserAssignmentsApiService>();

var app = builder.Build();

// Configure middleware
if (!isDevelopment)
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add global exception handler for API endpoints
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable CORS
app.UseCors();

app.UseAuthentication(); // Must be called, even in development mode
app.UseAuthorization();
app.UseCors(); // Enable CORS

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