using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Areas.Manager.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
