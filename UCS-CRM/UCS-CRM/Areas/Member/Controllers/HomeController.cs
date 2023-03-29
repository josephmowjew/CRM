using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IMemberRepository _memberRepository;

        public HomeController(ITicketRepository ticketRepository, IMemberRepository memberRepository)
        {
            _ticketRepository = ticketRepository;
            _memberRepository = memberRepository;
        }

        public async Task <ActionResult> Index()
        {
            ViewBag.allTicketsCount = await this.CountAllMyTickets();
            ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
            ViewBag.openedTicketsCount = await this.CountTicketsByStatus("Open");
            ViewBag.waitingTicketsCount = await this.CountTicketsByStatus("Waiting For Support");
            return View();
        }


        private async Task<int> CountAllMyTickets()
        {
            int count = 0;

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if(member != null)
            {
                int myTickets = await this._ticketRepository.TotalCountByMember(member.Id);

                if(myTickets > 0)
                {
                    count = myTickets;
                }

            }

            return count;

        }
        private async Task<int> CountTicketsByStatus(string status)
        {
            int count = 0;

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if (member != null)
            {
                int myTickets = await this._ticketRepository.CountTicketsByStatusMember(status, member.Id);

                if (myTickets > 0)
                {
                    count = myTickets;
                }

            }

            return count;

        }

    }
}
