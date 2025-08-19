using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class BOQController : BaseController
    {
        public BOQController(IUsersApiClient usersApiClient) : base(usersApiClient)
        {
        }

        public IActionResult BOQList()
        {
            ViewData["Title"] = "BOQ List";
            return View();
        }
    }
}
