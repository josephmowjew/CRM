using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketRepository _ticketRepository;

        public HomeController(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.allTicketsCount = await this.CountAllMyTickets();
            ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
            ViewBag.openedTicketsCount = await this.CountTicketsByStatus("Open");
            ViewBag.waitingTicketsCount = await this.CountTicketsByStatus("New");
            return View();
        }

        private async Task<int> CountAllMyTickets()
        {
            int count = 0;

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);



            int myTickets = await this._ticketRepository.TotalCount();

            if (myTickets > 0)
            {
                count = myTickets;
            }



            return count;

        }
        private async Task<int> CountTicketsByStatus(string status)
        {
            int count = 0;

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            int myTickets = await this._ticketRepository.CountTicketsByStatus(status);

            if (myTickets > 0)
            {
                count = myTickets;
            }



            return count;

        }
    }
}
