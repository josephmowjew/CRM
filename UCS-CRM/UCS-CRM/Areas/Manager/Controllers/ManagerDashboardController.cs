using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Areas.Manager.Controllers
{
    public class ManagerDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
