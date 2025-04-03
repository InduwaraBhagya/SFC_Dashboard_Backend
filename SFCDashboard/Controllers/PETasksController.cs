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
    public class PETasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PETasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PETasks
        public async Task<IActionResult> Index()
        {
            return View(await _context.PETask.ToListAsync());
        }

        // GET: PETasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETask = await _context.PETask
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pETask == null)
            {
                return NotFound();
            }

            return View(pETask);
        }

        // GET: PETasks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PETasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TaskSeq,PlannedEvent,OLA_Parameters")] PETask pETask)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pETask);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pETask);
        }

        // GET: PETasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETask = await _context.PETask.FindAsync(id);
            if (pETask == null)
            {
                return NotFound();
            }
            return View(pETask);
        }

        // POST: PETasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TaskSeq,PlannedEvent,OLA_Parameters")] PETask pETask)
        {
            if (id != pETask.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pETask);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PETaskExists(pETask.Id))
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
            return View(pETask);
        }

        // GET: PETasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pETask = await _context.PETask
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pETask == null)
            {
                return NotFound();
            }

            return View(pETask);
        }

        // POST: PETasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pETask = await _context.PETask.FindAsync(id);
            if (pETask != null)
            {
                _context.PETask.Remove(pETask);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PETaskExists(int id)
        {
            return _context.PETask.Any(e => e.Id == id);
        }
    }
}
