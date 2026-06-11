using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class ApiTestController : BaseController
    {
        private readonly IPlannedEventsApiClient _plannedEventsApi;
        private readonly IPETasksApiClient _peTasksApi;
        private readonly IUsersApiClient _usersApi;
        private readonly IPermissionsApiClient _permissionsApi;
        private readonly ILogger<ApiTestController> _logger;

        public ApiTestController(
            IPlannedEventsApiClient plannedEventsApi,
            IPETasksApiClient peTasksApi,
            IUsersApiClient usersApi,
            IPermissionsApiClient permissionsApi,
            ILogger<ApiTestController> logger) : base(usersApi)
        {
            _plannedEventsApi = plannedEventsApi;
            _peTasksApi = peTasksApi;
            _usersApi = usersApi;
            _permissionsApi = permissionsApi;
            _logger = logger;
        }

        // GET: /ApiTest
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.Message = "Testing API Backend Integration";
                
                // Test API calls
                var plannedEvents = await _plannedEventsApi.GetAllAsync();
                var users = await _usersApi.GetAllAsync();
                var tasks = await _peTasksApi.GetAllAsync();

                ViewBag.PlannedEventsCount = plannedEvents?.Count() ?? 0;
                ViewBag.UsersCount = users?.Count() ?? 0;
                ViewBag.TasksCount = tasks?.Count() ?? 0;
                ViewBag.ApiStatus = "✅ API Backend Connected Successfully";

                return View(plannedEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API backend");
                ViewBag.Message = "API Backend Test";
                ViewBag.ApiStatus = $"❌ API Backend Connection Failed: {ex.Message}";
                ViewBag.PlannedEventsCount = 0;
                ViewBag.UsersCount = 0;
                ViewBag.TasksCount = 0;
                
                return View(new List<PlannedEvent>());
            }
        }

        // GET: /ApiTest/PlannedEvents
        public async Task<IActionResult> PlannedEvents()
        {
            try
            {
                var events = await _plannedEventsApi.GetAllAsync();
                ViewBag.Title = "Planned Events from API Backend";
                return View("PlannedEventsList", events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching planned events from API");
                ViewBag.Error = $"Error: {ex.Message}";
                return View("PlannedEventsList", new List<PlannedEvent>());
            }
        }

        // GET: /ApiTest/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _usersApi.GetAllAsync();
                ViewBag.Title = "Users from API Backend";
                return View("UsersList", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users from API");
                ViewBag.Error = $"Error: {ex.Message}";
                return View("UsersList", new List<SystemUser>());
            }
        }

        // GET: /ApiTest/Tasks
        public async Task<IActionResult> Tasks()
        {
            try
            {
                var tasks = await _peTasksApi.GetAllAsync();
                ViewBag.Title = "PE Tasks from API Backend";
                return View("TasksList", tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks from API");
                ViewBag.Error = $"Error: {ex.Message}";
                return View("TasksList", new List<PETask>());
            }
        }

        // GET: /ApiTest/Permissions
        public async Task<IActionResult> Permissions()
        {
            var results = new List<string>();
            
            try
            {
                // Get current user's service ID (first 6 characters)
                var serviceId = User.Identity?.Name;
                if (string.IsNullOrEmpty(serviceId))
                {
                    results.Add("❌ No user logged in");
                    ViewBag.TestResults = results;
                    ViewBag.Title = "Permissions API Test Results";
                    return View("TestResults");
                }

                var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
                results.Add($"✅ User Service ID: {serviceId} (Short: {serviceIdShort})");

                // Test 1: Check if user is admin
                results.Add("");
                results.Add("--- Testing IsUserAdminAsync ---");
                var isAdmin = await _permissionsApi.IsUserAdminAsync(serviceIdShort);
                results.Add($"✅ IsUserAdminAsync result: {isAdmin}");

                // Test 2: Check specific permission
                results.Add("");
                results.Add("--- Testing HasPermissionAsync ---");
                var hasAdminPerm = await _permissionsApi.HasPermissionAsync(serviceIdShort, "Admin");
                results.Add($"✅ HasPermissionAsync('Admin') result: {hasAdminPerm}");

                // Test 3: Get all permissions
                results.Add("");
                results.Add("--- Testing GetAllPermissionsAsync ---");
                var allPermissions = await _permissionsApi.GetAllPermissionsAsync();
                results.Add($"✅ GetAllPermissionsAsync count: {allPermissions.Count}");
                
                foreach (var perm in allPermissions.Take(5)) // Show first 5
                {
                    results.Add($"   - {perm.Name}: {perm.Description}");
                }
                
                if (allPermissions.Count > 5)
                {
                    results.Add($"   ... and {allPermissions.Count - 5} more permissions");
                }

                // Test 4: API connectivity check
                results.Add("");
                results.Add("--- API Connectivity Status ---");
                results.Add("✅ All API calls completed successfully");

            }
            catch (HttpRequestException ex)
            {
                results.Add($"❌ HTTP Request Error: {ex.Message}");
                results.Add("🔍 Check if the API project is running on the configured port");
                _logger.LogError(ex, "HTTP error during permissions API test");
            }
            catch (Exception ex)
            {
                results.Add($"❌ General Error: {ex.Message}");
                _logger.LogError(ex, "Error during permissions API test");
            }

            ViewBag.TestResults = results;
            ViewBag.Title = "Permissions API Test Results";
            return View("TestResults");
        }

        // GET: /ApiTest/HealthCheck
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Try to get all permissions to test basic connectivity
                var permissions = await _permissionsApi.GetAllPermissionsAsync();
                
                return Json(new { 
                    success = true, 
                    message = "Permissions API is healthy", 
                    permissionCount = permissions.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permissions API health check failed");
                
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}
