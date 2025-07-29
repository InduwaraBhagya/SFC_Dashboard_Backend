using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class PETaskListsController : AdminControllerBase
    {
        private readonly IPETaskListsApiClient _peTaskListsApi;
        private readonly ILogger<PETaskListsController> _logger;

        public PETaskListsController(IPETaskListsApiClient peTaskListsApi, IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient, IRolePermissionsApiClient rolePermissionsApi, ILogger<PETaskListsController> logger) : base(permissionsApi, usersApiClient, rolePermissionsApi)
        {
            _peTaskListsApi = peTaskListsApi;
            _logger = logger;
        }

        // GET: PETaskLists
        public async Task<IActionResult> Index()
        {
            var taskLists = await _peTaskListsApi.GetAllAsync();
            return View(taskLists);
        }

        // GET: PETaskLists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETaskList = await _peTaskListsApi.GetByIdAsync(id.Value);
            if (pETaskList == null)
            {
                return NotFound();
            }

            return View(pETaskList);
        }

        // GET: PETaskLists/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PETaskLists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TaskSeq,Name,OLA_Parameters")] PETaskList pETaskList)
        {
            if (ModelState.IsValid)
            {
                await _peTaskListsApi.CreateAsync(pETaskList);
                return RedirectToAction(nameof(Index));
            }
            return View(pETaskList);
        }

        // GET: PETaskLists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETaskList = await _peTaskListsApi.GetByIdAsync(id.Value);
            if (pETaskList == null)
            {
                return NotFound();
            }
            return View(pETaskList);
        }

        // POST: PETaskLists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TaskSeq,Name,OLA_Parameters")] PETaskList pETaskList)
        {
            if (id != pETaskList.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _peTaskListsApi.UpdateAsync(pETaskList);
                }
                catch (Exception)
                {
                    if (!(await PETaskListExists(pETaskList.Id)))
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
            return View(pETaskList);
        }

        // GET: PETaskLists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETaskList = await _peTaskListsApi.GetByIdAsync(id.Value);
            if (pETaskList == null)
            {
                return NotFound();
            }

            return View(pETaskList);
        }

        // POST: PETaskLists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _peTaskListsApi.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> PETaskListExists(int id)
        {
            return await _peTaskListsApi.ExistsAsync(id);
        }

        // GET: PETaskLists/GetForPE
        [HttpGet]
        public async Task<IActionResult> GetForPE(int peId)
        {
            try
            {
                // Get all available task lists (not PE-specific based on the model structure)
                var taskLists = await _peTaskListsApi.GetAllAsync();
                var result = taskLists
                    .OrderBy(tl => tl.TaskSeq)
                    .Select(tl => new { id = tl.Id, name = tl.Name })
                    .ToList();

                return Json(result);
            }
            catch (Exception)
            {
                // Log the error if you have a logger
                return Json(new List<object>());
            }
        }
    }
}
