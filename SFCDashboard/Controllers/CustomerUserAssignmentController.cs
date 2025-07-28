using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Controllers;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class CustomerUserAssignmentController : AdminControllerBase
    {
        private readonly ICustomerUserAssignmentsApiClient _customerUserAssignmentsApi;
        private readonly IPlannedEventsApiClient _plannedEventsApi;

        public CustomerUserAssignmentController(
            ICustomerUserAssignmentsApiClient customerUserAssignmentsApi,
            IPlannedEventsApiClient plannedEventsApi,
            IPermissionsApiClient permissionsApi,
            IUsersApiClient usersApiClient,
            IRolePermissionsApiClient rolePermissionsApi,
            ILogger<CustomerUserAssignmentController> logger) : base(permissionsApi, usersApiClient, rolePermissionsApi)
        {
            _customerUserAssignmentsApi = customerUserAssignmentsApi;
            _plannedEventsApi = plannedEventsApi;
        }

        // GET: CustomerUserAssignment
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            try
            {
                var viewModel = new CustomerUserAssignmentManageViewModel();

                // Get all assignments with user details
                IEnumerable<CustomerUserAssignment> assignments;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    assignments = await _customerUserAssignmentsApi.SearchAssignmentsAsync(searchTerm);
                    viewModel.SearchTerm = searchTerm;
                }
                else
                {
                    assignments = await _customerUserAssignmentsApi.GetAssignmentsWithUsersAsync();
                }

                // Convert to view model
                var assignmentViewModels = assignments.Select(c => new CustomerUserAssignmentViewModel
                {
                    Id = c.Id,
                    Customer = c.Customer,
                    UserId = c.UserId,
                    UserName = c.User?.Name ?? "",
                    UserServiceId = c.User?.ServiceId ?? "",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).OrderBy(c => c.Customer).ToList();

                viewModel.Assignments = assignmentViewModels;

                // Get all unique customers from PlannedEvents that don't have assignments yet
                var assignedCustomers = assignmentViewModels.Select(a => a.Customer).ToList();
                var availableCustomers = await _customerUserAssignmentsApi.GetAvailableCustomersAsync(assignedCustomers);

                viewModel.AvailableCustomers = availableCustomers.ToList();

                // Get users with SALES workgroup
                var salesUsers = await _usersApiClient.GetSalesUsersAsync();

                viewModel.SalesUsers = salesUsers;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error and return an error view or redirect
                TempData["ErrorMessage"] = $"Error loading customer assignments: {ex.Message}";
                return View(new CustomerUserAssignmentManageViewModel());
            }
        }

        // POST: CustomerUserAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerUserAssignmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if customer already has an assignment
                var existingAssignment = await _customerUserAssignmentsApi.GetExistingAssignmentByCustomerAsync(model.Customer);

                if (existingAssignment != null)
                {
                    TempData["ErrorMessage"] = $"Customer '{model.Customer}' is already assigned to a user.";
                    return RedirectToAction(nameof(Index));
                }

                var assignment = new CustomerUserAssignment
                {
                    Customer = model.Customer,
                    UserId = model.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _customerUserAssignmentsApi.CreateAsync(assignment);

                TempData["SuccessMessage"] = $"Customer '{model.Customer}' has been successfully assigned.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Please check the form for errors.";
            return RedirectToAction(nameof(Index));
        }

        // POST: CustomerUserAssignment/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int userId)
        {
            var assignment = await _customerUserAssignmentsApi.GetByIdAsync(id);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            assignment.UserId = userId;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _customerUserAssignmentsApi.UpdateAsync(assignment);

            TempData["SuccessMessage"] = $"Assignment for customer '{assignment.Customer}' has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: CustomerUserAssignment/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _customerUserAssignmentsApi.GetByIdAsync(id);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            await _customerUserAssignmentsApi.DeleteAsync(id);

            TempData["SuccessMessage"] = $"Assignment for customer '{assignment.Customer}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: API endpoint to get sales users for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetSalesUsers()
        {
            var salesUsers = await _usersApiClient.GetSalesUsersAsync();

            var result = salesUsers
                .Select(u => new { u.Id, u.Name, u.ServiceId })
                .OrderBy(u => u.Name)
                .ToList();

            return Json(result);
        }
    }
}
