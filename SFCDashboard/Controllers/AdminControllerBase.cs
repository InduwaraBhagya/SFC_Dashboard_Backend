using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFCDashboard.Data;

namespace SFCDashboard.Controllers
{
    public class AdminControllerBase : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminControllerBase(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var isAdmin = await HttpContext.HasAdminPermissionAsync(_context);
            
            if (!isAdmin)
            {
                context.Result = RedirectToAction("Index", "PlannedEvents");
                return;
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}