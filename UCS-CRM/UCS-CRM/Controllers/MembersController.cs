using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Controllers
{
    public class MembersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
