using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SFCDashboard.ApiClients;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class RegisterController : BaseController
    {
        private readonly IWorkGroupsApiClient _workGroupsApiClient;

        public RegisterController(IUsersApiClient usersApiClient, IWorkGroupsApiClient workGroupsApiClient)
            : base(usersApiClient)
        {
            _workGroupsApiClient = workGroupsApiClient;
        }

        // GET
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            // Get existing user details from API
            var existingUser = await _usersApiClient.GetUserByServiceIdAsync(serviceId);

            if (existingUser == null)
            {
                // If user doesn't exist, create new user object
                var name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                existingUser = new SystemUser
                {
                    Name = name,
                    ServiceId = serviceId,
                    UserWorkGroups = new List<UserWorkGroup>()
                };
            }

            var vm = new SystemUserViewModel
            {
                Name = existingUser?.Name ?? "",
                ServiceId = existingUser?.ServiceId ?? "",
                WorkGroupIds = existingUser?.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>()
            };

            var allWorkGroups = (await _workGroupsApiClient.GetAllAsync()).ToList();
            ViewData["WorkGroupIds"] = new MultiSelectList(allWorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemUserViewModel vm)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            // Get existing user
            var existingUser = await _usersApiClient.GetUserByServiceIdAsync(serviceId);

            if (existingUser != null)
            {
                // Update existing user's name
                existingUser.Name = vm.Name;
                // Update user (if needed, implement UpdateUserAsync in IUsersApiClient)
                // await _usersApiClient.UpdateUserAsync(existingUser); // Uncomment if implemented

                // Update user-workgroup relations via API
                await _usersApiClient.SetUserWorkGroupsAsync(existingUser.Id, vm.WorkGroupIds);
            }
            else
            {
                // Create new user if doesn't exist (if needed, implement CreateUserAsync in IUsersApiClient)
                var user = new SystemUser
                {
                    ServiceId = serviceId,
                    Name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty,
                    UserWorkGroups = new List<UserWorkGroup>()
                };
                // var createdUser = await _usersApiClient.CreateUserAsync(user); // Uncomment if implemented
                // if (createdUser != null)
                //     await _usersApiClient.SetUserWorkGroupsAsync(createdUser.Id, vm.WorkGroupIds);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // All changes are done via API, so just redirect
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }

            var allWorkGroups = (await _workGroupsApiClient.GetAllAsync()).ToList();
            ViewData["WorkGroupIds"] = new MultiSelectList(allWorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }
    }
}