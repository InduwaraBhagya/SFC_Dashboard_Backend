using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Services;
using SFCDashboard.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using SFCDashboard.Api.Configuration;
using DotNetEnv;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

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
azureAdSection["Audience"] = Environment.GetEnvironmentVariable("AZURE_AD_AUDIENCE") ?? azureAdSection["Audience"];

// Set Connection String from environment variables
var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING") ?? 
                      builder.Configuration.GetConnectionString("DefaultConnection");
builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"] = connectionString;

// Set API Settings from environment variables
var apiSettingsSection = builder.Configuration.GetSection("ApiSettings");
apiSettingsSection["BaseUrl"] = Environment.GetEnvironmentVariable("API_BASE_URL") ?? apiSettingsSection["BaseUrl"];

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Add global authorization policy to protect all API endpoints
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Add memory cache
builder.Services.AddMemoryCache();

// Enforce HTTPS: configure HSTS (sent only over HTTPS) and permanent HTTPS redirects
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365); // 1 year; consider 2 years if confident
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    // options.HttpsPort = 443; // uncomment and set if running on a non-standard port
});

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// Register services (background services are now handled by SFCDashboard.Background and SFCDashboard.EscalationService)
builder.Services.AddScoped<EscalationService>();

// Background services are now handled by separate services:
// - PERecordSyncService, OLAViolationService, HoldTaskReminderService -> SFCDashboard.Background
// - EscalationBackgroundService -> SFCDashboard.EscalationService

// Authentication configuration
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
    // Use JWT Bearer authentication for API in production
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdConfig);
}
else
{
    // Use a dummy authentication scheme in development
    builder.Services.AddAuthentication("DummyScheme")
        .AddScheme<AuthenticationSchemeOptions, SFCDashboard.Api.DummyAuthenticationHandler>("DummyScheme", options => { });
}

// Add authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                            ?? new[] { "http://localhost:3000", "https://localhost:3000" };
        
        corsBuilder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
    });
});

// Register API Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITaskQueueService, TaskQueueingService>();
// Note: PERecordsApiService temporarily disabled due to circular dependency
// builder.Services.AddHttpClient<PERecordsApiService>(client =>
// {
//     client.Timeout = TimeSpan.FromMinutes(10);
// });
// builder.Services.AddScoped<PERecordsApiService>();
builder.Services.AddScoped<IPlannedEventsApiService, PlannedEventsApiService>();
builder.Services.AddScoped<IProjectsApiService, ProjectsApiService>();
builder.Services.AddScoped<IUsersApiService, UsersApiService>();
builder.Services.AddScoped<IPETasksApiService, PETasksApiService>();
builder.Services.AddScoped<IPEIssuesApiService, PEIssuesApiService>();
builder.Services.AddScoped<IWorkGroupsApiService, WorkGroupsApiService>();
builder.Services.AddScoped<IAreaNetworkEngineersApiService, AreaNetworkEngineersApiService>();
builder.Services.AddScoped<IPETaskListsApiService, PETaskListsApiService>();
builder.Services.AddScoped<IEscalationsApiService, EscalationsApiService>();
builder.Services.AddScoped<IPEIssueResolutionsApiService, PEIssueResolutionsApiService>();
builder.Services.AddScoped<ICustomerUserAssignmentsApiService, CustomerUserAssignmentsApiService>();
builder.Services.AddScoped<IRolePermissionsApiService, RolePermissionsApiService>();
builder.Services.AddScoped<IUserRolesApiService, UserRolesApiService>();
builder.Services.AddScoped<IPermissionsApiService, PermissionsApiService>();
builder.Services.AddScoped<ISubTaskListsApiService, SubTaskListsApiService>();
builder.Services.AddScoped<INoticesService, NoticesService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SFC Dashboard API", 
        Version = "v1",
        Description = "API for SFC Dashboard backend operations"
    });
    
    if (!isDevelopment)
    {
        // Add Azure AD OAuth2 authentication to Swagger in production
        c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/oauth2/v2.0/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { $"api://{azureAdConfig["ClientId"]}/access_as_user", "Access API as user" }
                    }
                }
            }
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "oauth2"
                    }
                },
                new[] { $"api://{azureAdConfig["ClientId"]}/access_as_user" }
            }
        });
    }
    else
    {
        // Add Bearer token authentication to Swagger in development
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFC Dashboard API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Add global exception handler
app.UseGlobalExceptionHandler();

// Honor reverse-proxy headers (X-Forwarded-For/Proto) for correct scheme/hosts behind load balancers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    // Send the Strict-Transport-Security header to instruct browsers to always use HTTPS
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable static files
app.UseStaticFiles();

// Enable CORS
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();


// Map controllers
app.MapControllers();

// Health check endpoint - allow anonymous access
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

// Create uploads directory if it doesn't exist
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

