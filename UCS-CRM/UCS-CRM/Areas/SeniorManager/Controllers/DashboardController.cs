using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Areas.SeniorManager.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
