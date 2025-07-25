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
        private readonly ILogger<ApiTestController> _logger;

        public ApiTestController(
            IPlannedEventsApiClient plannedEventsApi,
            IPETasksApiClient peTasksApi,
            IUsersApiClient usersApi,
            ILogger<ApiTestController> logger) : base(usersApi)
        {
            _plannedEventsApi = plannedEventsApi;
            _peTasksApi = peTasksApi;
            _usersApi = usersApi;
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
    }
}
