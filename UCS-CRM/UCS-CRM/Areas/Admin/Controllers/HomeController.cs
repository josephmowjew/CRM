using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IStateRepository _stateRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IFailedRegistrationRepository _failedRegistrationRepository;
        

        public HomeController(ITicketRepository ticketRepository,
                              IUserRepository userRepository,
                              IDepartmentRepository departmentRepository,
                              IStateRepository stateRepository,
                              IBranchRepository branchRepository,
                              IMemberRepository memberRepository,
                              IFailedRegistrationRepository failedRegistrationRepository)
        {
            this._ticketRepository = ticketRepository;
            this._userRepository = userRepository;
            this._departmentRepository = departmentRepository;
            this._stateRepository = stateRepository;
            this._branchRepository = branchRepository;
            this._memberRepository = memberRepository;
            this._failedRegistrationRepository = failedRegistrationRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.departmentsCounts = await this.CountDepartmentsAvailable();
            ViewBag.statesCount = await this.CountStatesAvailable();
            ViewBag.branchesCount = await this.CountBranchesAvailable();
            ViewBag.usersCount = await this.CountUsersAvailable();
            ViewBag.membersCount = await this.CountMembersAvailable();
            ViewBag.failedRegistrationsCount = await _failedRegistrationRepository.GetUnresolvedCountAsync();
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

        private async Task<int> CountUsersAvailable()
        {
            int count = 0;

            count = await this._userRepository.TotalCount();

            return count;
        }

        private async Task<int> CountDepartmentsAvailable()
        {
            int count = 0;

            count = await this._departmentRepository.TotalCount();

            return count;
        }

        private async Task<int> CountBranchesAvailable()
        {
            int count = 0;

            count = await this._branchRepository.TotalCount();

            return count;
        }

        private async Task<int> CountStatesAvailable()
        {
            int count = 0;

            count = await this._stateRepository.TotalActiveCount();

            return count;
        }

        private async Task<int> CountMembersAvailable()
        {
            int count = 0;

            count = await this._memberRepository.TotalCount();

            return count;
        }
    }
}