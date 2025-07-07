using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class PETaskListsController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PETaskListsController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // GET: PETaskLists
        public async Task<IActionResult> Index()
        {
            return View(await _context.PETaskLists.ToListAsync());
        }

        // GET: PETaskLists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETaskList = await _context.PETaskLists
                .FirstOrDefaultAsync(m => m.Id == id);
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
                _context.Add(pETaskList);
                await _context.SaveChangesAsync();
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

            var pETaskList = await _context.PETaskLists.FindAsync(id);
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
                    _context.Update(pETaskList);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PETaskListExists(pETaskList.Id))
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

            var pETaskList = await _context.PETaskLists
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var pETaskList = await _context.PETaskLists.FindAsync(id);
            if (pETaskList != null)
            {
                _context.PETaskLists.Remove(pETaskList);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PETaskListExists(int id)
        {
            return _context.PETaskLists.Any(e => e.Id == id);
        }

        // GET: PETaskLists/GetForPE
        [HttpGet]
        public async Task<IActionResult> GetForPE(int peId)
        {
            try
            {
                // Get all available task lists (not PE-specific based on the model structure)
                var taskLists = await _context.PETaskLists
                    .OrderBy(tl => tl.TaskSeq)
                    .Select(tl => new { id = tl.Id, name = tl.Name })
                    .ToListAsync();

                return Json(taskLists);
            }
            catch (Exception)
            {
                // Log the error if you have a logger
                return Json(new List<object>());
            }
        }
    }
}
