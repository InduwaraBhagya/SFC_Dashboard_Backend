using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Controllers;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class CustomerUserAssignmentController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomerUserAssignmentController(ApplicationDbContext context, IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient) : base(permissionsApi, usersApiClient)
        {
            _context = context;
        }

        // GET: CustomerUserAssignment
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var viewModel = new CustomerUserAssignmentManageViewModel();

            // Get all assignments with user details
            var assignmentsQuery = _context.CustomerUserAssignments
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                assignmentsQuery = assignmentsQuery.Where(c => 
                    c.Customer.Contains(searchTerm) || 
                    c.User.Name.Contains(searchTerm) ||
                    c.User.ServiceId.Contains(searchTerm));
                viewModel.SearchTerm = searchTerm;
            }

            var assignments = await assignmentsQuery
                .OrderBy(c => c.Customer)
                .Select(c => new CustomerUserAssignmentViewModel
                {
                    Id = c.Id,
                    Customer = c.Customer,
                    UserId = c.UserId,
                    UserName = c.User.Name,
                    UserServiceId = c.User.ServiceId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            viewModel.Assignments = assignments;

            // Get all unique customers from PlannedEvents that don't have assignments yet
            var assignedCustomers = assignments.Select(a => a.Customer).ToList();
            var availableCustomers = await _context.PlannedEvents
                .Where(p => !string.IsNullOrEmpty(p.Customer) && !assignedCustomers.Contains(p.Customer))
                .Select(p => p.Customer!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            viewModel.AvailableCustomers = availableCustomers;

            // Get users with SALES workgroup
            var salesUsers = await _context.Users
                .Include(u => u.UserWorkGroups)
                .ThenInclude(uwg => uwg.WorkGroup)
                .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name.Contains("SALES")))
                .OrderBy(u => u.Name)
                .ToListAsync();

            viewModel.SalesUsers = salesUsers;

            return View(viewModel);
        }

        // POST: CustomerUserAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerUserAssignmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if customer already has an assignment
                var existingAssignment = await _context.CustomerUserAssignments
                    .FirstOrDefaultAsync(c => c.Customer == model.Customer);

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

                _context.CustomerUserAssignments.Add(assignment);
                await _context.SaveChangesAsync();

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
            var assignment = await _context.CustomerUserAssignments.FindAsync(id);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            assignment.UserId = userId;
            assignment.UpdatedAt = DateTime.UtcNow;

            _context.Update(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Assignment for customer '{assignment.Customer}' has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: CustomerUserAssignment/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _context.CustomerUserAssignments.FindAsync(id);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.CustomerUserAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Assignment for customer '{assignment.Customer}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: API endpoint to get sales users for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetSalesUsers()
        {
            var salesUsers = await _context.Users
                .Include(u => u.UserWorkGroups)
                .ThenInclude(uwg => uwg.WorkGroup)
                .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name.Contains("SALES")))
                .Select(u => new { u.Id, u.Name, u.ServiceId })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Json(salesUsers);
        }
    }
}
