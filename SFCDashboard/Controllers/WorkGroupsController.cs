using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class WorkGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly int _pageSize = 10;  // Add this line

        public WorkGroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        

        // GET: WorkGroups
        public async Task<IActionResult> Index(int? page, string searchTerm)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            var query = _context.WorkGroups.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(w => w.Name.Contains(searchTerm));
            }

            var pageNumber = page ?? 1;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)_pageSize);

            var workGroups = await query
                .OrderBy(w => w.Name)
                .Skip((pageNumber - 1) * _pageSize)
                .Take(_pageSize)
                .ToListAsync();

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

            var workGroup = await _context.WorkGroups
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workGroup == null)
            {
                return NotFound();
            }

            return View(workGroup);
        }

        // GET: WorkGroups/Create
        public async Task<IActionResult> CreateAsync()
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            return View();
        }

        // POST: WorkGroups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] WorkGroup workGroup)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            if (ModelState.IsValid)
            {
                _context.Add(workGroup);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(workGroup);
        }
        public async Task<IActionResult> ImportFromBackendAsync()
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            string filePath = @"wwwroot\assets\WORK_GROUPS.xlsx"; // Change this to your file path


            if (!System.IO.File.Exists(filePath))
            {
                TempData["Message"] = "Excel file not found.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Read the first worksheet
                    var rows = worksheet.RowsUsed();

                    List<WorkGroup> workGroups = new List<WorkGroup>();

                    foreach (var row in rows.Skip(1)) // Skip header row
                    {
                        var workGroup = new WorkGroup
                        {
                            Name = row.Cell(1).GetValue<string>() // Column A: WorkGroup Name
                        };

                        workGroups.Add(workGroup);
                    }

                    _context.WorkGroups.AddRange(workGroups);
                    _context.SaveChanges();
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
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }

            var workGroup = await _context.WorkGroups.FindAsync(id);
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
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            if (id != workGroup.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workGroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkGroupExists(workGroup.Id))
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
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }

            var workGroup = await _context.WorkGroups
                .FirstOrDefaultAsync(m => m.Id == id);
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
            if (!await HttpContext.HasAdminPermissionAsync(_context))
            return RedirectToAction("Index", "PlannedEvents");

            var workGroup = await _context.WorkGroups.FindAsync(id);
            if (workGroup != null)
            {
                _context.WorkGroups.Remove(workGroup);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkGroupExists(int id)
        {
            return _context.WorkGroups.Any(e => e.Id == id);
        }
    }
}
