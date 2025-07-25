using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class SystemUsersController : BaseController
    {
        // Remove the redundant IUsersApiClient since it's inherited from BaseController
        private readonly IUserRolesApiClient _userRolesApiClient;
        private readonly IWorkGroupsApiClient _workGroupsApiClient;

        public SystemUsersController(
            IUsersApiClient usersApiClient,
            IUserRolesApiClient userRolesApiClient,
            IWorkGroupsApiClient workGroupsApiClient)
            : base(usersApiClient)
        {
            _userRolesApiClient = userRolesApiClient;
            _workGroupsApiClient = workGroupsApiClient;
        }

        // GET: SystemUsers
        public async Task<IActionResult> Index()
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            var users = await _usersApiClient.GetAllAsync();
            return View(users);
        }

        // GET: SystemUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var systemUser = await _usersApiClient.GetUserWithRoleAndWorkGroupsAsync(id.Value);
            if (systemUser == null)
            {
                return NotFound();
            }
            return View(systemUser);
        }

        // GET: SystemUsers/Create
        public async Task<IActionResult> CreateAsync()
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            var roles = await _userRolesApiClient.GetAllAsync();
            var workGroups = await _workGroupsApiClient.GetAllAsync();
            ViewData["UserRoleId"] = new SelectList(roles, "Id", "Name");
            ViewData["WorkGroups"] = new MultiSelectList(workGroups, "Id", "Name");
            return View(new SystemUserViewModel());
        }

        // POST: SystemUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemUserViewModel vm)
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            if (ModelState.IsValid)
            {
                var systemUser = new SystemUser
                {
                    Name = vm.Name,
                    ServiceId = vm.ServiceId,
                    UserRoleId = vm.UserRoleId
                };


                var createdUser = await _usersApiClient.CreateAsync(systemUser);
                if (createdUser != null && vm.WorkGroupIds != null && vm.WorkGroupIds.Any())
                {
                    await _usersApiClient.SetUserWorkGroupsAsync(createdUser.Id, vm.WorkGroupIds);
                }

                return RedirectToAction(nameof(Index));
            }
            var roles = await _userRolesApiClient.GetAllAsync();
            var workGroups = await _workGroupsApiClient.GetAllAsync();
            ViewData["UserRoleId"] = new SelectList(roles, "Id", "Name", vm.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(workGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // GET: SystemUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }

            var user = await _usersApiClient.GetUserWithRoleAndWorkGroupsAsync(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new SystemUserViewModel
            {
                Name = user.Name,
                ServiceId = user.ServiceId,
                UserRoleId = user.UserRoleId,
                WorkGroupIds = user.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>()
            };

            var roles = await _userRolesApiClient.GetAllAsync();
            var workGroups = await _workGroupsApiClient.GetAllAsync();
            ViewData["UserRoleId"] = new SelectList(roles, "Id", "Name", user.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(workGroups, "Id", "Name", vm.WorkGroupIds);

            return View(vm);
        }

        // POST: SystemUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SystemUserViewModel vm)
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            var existingUser = await _usersApiClient.GetUserWithRoleAndWorkGroupsAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingUser.Name = vm.Name;
                    existingUser.ServiceId = vm.ServiceId;
                    existingUser.UserRoleId = vm.UserRoleId;
                    await _usersApiClient.UpdateAsync(existingUser);
                    if (vm.WorkGroupIds != null)
                    {
                        await _usersApiClient.SetUserWorkGroupsAsync(existingUser.Id, vm.WorkGroupIds);
                    }
                }
                catch (Exception)
                {
                    // TODO: Add proper error handling for API update
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            var roles = await _userRolesApiClient.GetAllAsync();
            var workGroups = await _workGroupsApiClient.GetAllAsync();
            ViewData["UserRoleId"] = new SelectList(roles, "Id", "Name", vm.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(workGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // GET: SystemUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }
            var systemUser = await _usersApiClient.GetUserWithRoleAndWorkGroupsAsync(id.Value);
            if (systemUser == null)
            {
                return NotFound();
            }
            return View(systemUser);
        }

        // POST: SystemUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await HasAdminPermissionAsync())
                return RedirectToAction("Index", "PlannedEvents");

            await _usersApiClient.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        // Helper: API-based admin permission check using the new endpoint
        private async Task<bool> HasAdminPermissionAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
            var user = await _usersApiClient.GetByServiceIdAsync(serviceIdShort);
            if (user == null)
                return false;

            // Get API base URL from configuration
            var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var apiBaseUrl = config?["ApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                return false;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
                var response = await httpClient.GetAsync($"api/users/{user.Id}/has-admin-permission");
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    if (bool.TryParse(resultString, out var isAdmin))
                        return isAdmin;
                }
            }
            return false;
        }

        private async Task<bool> SystemUserExists(int id)
        {
            var user = await _usersApiClient.GetByIdAsync(id);
            return user != null;
        }

        [HttpGet]
        public async Task<JsonResult> SearchWorkgroups(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { });

            var workGroups = await _workGroupsApiClient.GetAllAsync();
            var filtered = workGroups
                .Where(w => w.Name.ToLower().Contains(term.ToLower()))
                .Select(w => new { id = w.Id, label = w.Name })
                .Take(20)
                .Distinct()
                .ToList();

            return Json(filtered);
        }

        [HttpGet]
        public async Task<JsonResult> GetWorkgroupsByIds(List<int> ids)
        {
            var workGroups = await _workGroupsApiClient.GetAllAsync();
            var filtered = workGroups
                .Where(w => ids.Contains(w.Id))
                .Select(w => new { id = w.Id, label = w.Name })
                .ToList();
            return Json(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _usersApiClient.GetAllAsync();
            var result = users.Select(u => new { id = u.Id, name = u.Name }).ToList();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Json(null);

            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
            var user = await _usersApiClient.GetByServiceIdAsync(serviceIdShort);
            if (user == null)
                return Json(null);
            return Json(new { id = user.Id, name = user.Name });
        }
    }
}