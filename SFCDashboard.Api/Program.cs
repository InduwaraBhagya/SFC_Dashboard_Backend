using Microsoft.AspNetCore.Authentication;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add memory cache
builder.Services.AddMemoryCache();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// Register background services
builder.Services.AddSingleton<PERecordSyncService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PERecordSyncService>());
builder.Services.AddHostedService<OLAViolationService>();
builder.Services.AddHostedService<HoldTaskReminderService>();
builder.Services.AddScoped<EscalationService>();
builder.Services.AddHostedService<EscalationBackgroundService>();

// Authentication configuration
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
    // Use a dummy authentication scheme in development
    builder.Services.AddAuthentication("DummyScheme")
        .AddScheme<AuthenticationSchemeOptions, DummyAuthenticationHandler>("DummyScheme", options => { });
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
    
    // Add JWT authentication to Swagger
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

app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseUserRegistration();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

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

