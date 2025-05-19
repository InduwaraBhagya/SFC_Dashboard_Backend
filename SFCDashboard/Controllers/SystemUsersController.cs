using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;

namespace SFCDashboard.Controllers
{
    public class SystemUsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SystemUsers
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Users.Include(s => s.UserRole).Include(s => s.WorkGroup);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: SystemUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemUser = await _context.Users
                .Include(s => s.UserRole)
                .Include(s => s.WorkGroup)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemUser == null)
            {
                return NotFound();
            }

            return View(systemUser);
        }

        // GET: SystemUsers/Create
        public IActionResult Create()
        {
            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name");
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name");
            return View();
        }

        // POST: SystemUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ServiceId,UserRoleId,WorkGroupId")] SystemUser systemUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(systemUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name", systemUser.UserRoleId);
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", systemUser.WorkGroupId);
            return View(systemUser);
        }

        // GET: SystemUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemUser = await _context.Users.FindAsync(id);
            if (systemUser == null)
            {
                return NotFound();
            }
            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name", systemUser.UserRoleId);
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", systemUser.WorkGroupId);
            return View(systemUser);
        }

        // POST: SystemUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ServiceId,UserRoleId,WorkGroupId")] SystemUser systemUser)
        {
            if (id != systemUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Update only the properties you want to allow editing
                    existingUser.Name = systemUser.Name;
                    existingUser.ServiceId = systemUser.ServiceId;
                    existingUser.UserRoleId = systemUser.UserRoleId;
                    existingUser.WorkGroupId = systemUser.WorkGroupId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SystemUserExists(systemUser.Id))
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
            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name", systemUser.UserRoleId);
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", systemUser.WorkGroupId);
            return View(systemUser);
        }

        // GET: SystemUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemUser = await _context.Users
                .Include(s => s.UserRole)
                .Include(s => s.WorkGroup)
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var systemUser = await _context.Users.FindAsync(id);
            if (systemUser != null)
            {
                _context.Users.Remove(systemUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SystemUserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
        [HttpGet]
        public JsonResult SearchWorkgroups(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { });

            var workgroups = _context.WorkGroups
                .Where(w => w.Name.ToLower().Contains(term.ToLower()))
                .Select(w => new { id = w.Id, label = w.Name })
                .Take(20)
                .Distinct()
                .ToList();

            return Json(workgroups);
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.Users.Select(u => new { id = u.Id, name = u.Name }).ToList();
            return Json(users);
        }

    }
}
