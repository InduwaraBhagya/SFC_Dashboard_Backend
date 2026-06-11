using SFCDashboard.ApiClients;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class WorkGroupsController : AdminControllerBase
    {
        private readonly IWorkGroupsApiClient _workGroupsApiClient;
        private readonly int _pageSize = 10;

        public WorkGroupsController(IWorkGroupsApiClient workGroupsApiClient, IUsersApiClient usersApiClient, IPermissionsApiClient permissionsApiClient, IRolePermissionsApiClient rolePermissionsApiClient)
            : base(permissionsApiClient, usersApiClient, rolePermissionsApiClient)
        {
            _workGroupsApiClient = workGroupsApiClient;
        }

        

        // GET: WorkGroups
        public async Task<IActionResult> Index(int? page, string searchTerm)
        {
            var allWorkGroupsList = (await _workGroupsApiClient.GetAllAsync()).ToList();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allWorkGroupsList = allWorkGroupsList.Where(w => w.Name != null && w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            var pageNumber = page ?? 1;
            var totalItems = allWorkGroupsList.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)_pageSize);
            var workGroups = allWorkGroupsList
                .OrderBy(w => w.Name)
                .Skip((pageNumber - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;
            ViewData["SearchTerm"] = searchTerm;

            return View(workGroups);
        }

        // GET: WorkGroups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workGroup = await _workGroupsApiClient.GetByIdAsync(id.Value);
            if (workGroup == null)
            {
                return NotFound();
            }

            return View(workGroup);
        }

        // GET: WorkGroups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: WorkGroups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] WorkGroup workGroup)
        {
            if (ModelState.IsValid)
            {
                await _workGroupsApiClient.CreateAsync(workGroup);
                return RedirectToAction(nameof(Index));
            }
            return View(workGroup);
        }
        public async Task<IActionResult> ImportFromBackendAsync()
        {
            string filePath = @"wwwroot\assets\WORK_GROUPS.xlsx";
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Message"] = "Excel file not found.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed();
                    List<WorkGroup> workGroups = new List<WorkGroup>();
                    foreach (var row in rows.Skip(1))
                    {
                        var workGroup = new WorkGroup
                        {
                            Name = row.Cell(1).GetValue<string>()
                        };
                        workGroups.Add(workGroup);
                    }
                    foreach (var wg in workGroups)
                    {
                        await _workGroupsApiClient.CreateAsync(wg);
                    }
                }
                TempData["Message"] = "Excel data imported successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
        // GET: WorkGroups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workGroup = await _workGroupsApiClient.GetByIdAsync(id.Value);
            if (workGroup == null)
            {
                return NotFound();
            }
            return View(workGroup);
        }

        // POST: WorkGroups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] WorkGroup workGroup)
        {
            if (id != workGroup.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _workGroupsApiClient.UpdateAsync(workGroup);
                }
                catch (Exception)
                {
                    if (!await WorkGroupExists(workGroup.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(workGroup);
        }

        // GET: WorkGroups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workGroup = await _workGroupsApiClient.GetByIdAsync(id.Value);
            if (workGroup == null)
            {
                return NotFound();
            }

            return View(workGroup);
        }

        // POST: WorkGroups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _workGroupsApiClient.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> WorkGroupExists(int id)
        {
            var wg = await _workGroupsApiClient.GetByIdAsync(id);
            return wg != null;
        }
    }
}
