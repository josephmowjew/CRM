﻿using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;

        public HomeController(ITicketRepository ticketRepository, IUserRepository userRepository)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.allTicketsCount = await this.CountTicketsByStatus("");
            ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
            ViewBag.archivedTicketsCount = await this.CountTicketsByStatus("Archived");
            ViewBag.newTicketsCount = await this.CountTicketsByStatus("New");
            ViewBag.resolvedTicketsCount = await this.CountTicketsByStatus("Resolved");
            ViewBag.reopenedTicketsCount = await this.CountTicketsByStatus("Re-opened");
            return View();
        }

        private async Task<int> CountAllMyTickets()
        {
            int count = 0;
            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
            var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);

            // Check if user is in Executive Suite Department
            bool isExecutive = findUserDb?.Department?.Name?.Trim().ToUpper() == "EXECUTIVE SUITE";

            CursorParams CursorParameters = new CursorParams() { Take = 10 };

            int myTickets = await this._ticketRepository.GetTicketsTotalFilteredAsync(
                CursorParameters, 
                isExecutive ? null : findUserDb.Department, // Pass null department for executives
                ""
            );

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
            var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);

            // Check if user is in Executive Suite Department
            bool isExecutive = findUserDb?.Department?.Name?.Trim().ToUpper() == "EXECUTIVE SUITE";

            CursorParams CursorParameters = new CursorParams() { Take = 10 };

            int myTickets = await this._ticketRepository.GetTicketsTotalFilteredAsync(
                CursorParameters, 
                isExecutive ? null : findUserDb.Department, // Pass null department for executives
                status
            );

            if (myTickets > 0)
            {
                count = myTickets;
            }

            return count;
        }
    }
}
