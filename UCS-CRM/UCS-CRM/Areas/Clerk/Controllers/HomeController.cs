using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Areas.Client.Controllers
{
    [Area("Clerk")]
    [Authorize]
    public class ClerkHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
