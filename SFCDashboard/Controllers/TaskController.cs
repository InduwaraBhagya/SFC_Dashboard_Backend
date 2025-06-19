using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SFCDashboard.Controllers
{
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the PE Task
            var peTask = await _context.PlannedEvents
                .FirstOrDefaultAsync(m => m.Id == id);

            if (peTask == null)
            {
                return NotFound();
            }

            // Redirect to the PlannedEvents Details action with the task ID
            return RedirectToAction("Details", "PlannedEvents", new { id = peTask.Id });
        }
    }
}