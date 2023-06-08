using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.DTOs.MemberAccount;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IMemberAccountRepository _memberAccountRepository;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;

        public HomeController(ITicketRepository ticketRepository, IMemberRepository memberRepository, IMemberAccountRepository memberAccountRepository,IMapper mapper, HttpClient httpClient)
        {
            _ticketRepository = ticketRepository;
            _memberRepository = memberRepository;
            _memberAccountRepository = memberAccountRepository;
            _mapper = mapper;
            _httpClient = httpClient;
        }

        public async Task<ActionResult> Index()
        {
            ViewBag.allTicketsCount = await this.CountAllMyTickets();
            ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
            ViewBag.openedTicketsCount = await this.CountTicketsByStatus("Open");
            ViewBag.waitingTicketsCount = await this.CountTicketsByStatus("New");

            //var accounts = await this.GetMemberAccountsAsync();

            var accounts = await Accounts();

            ViewBag.accounts = accounts;

            // ViewBag.memberAccounts = this._mapper.Map<List<ReadMemberAccountDTO>>(accounts);
           
            return View();
        }


        private async Task<int> CountAllMyTickets()
        {
            int count = 0;

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if (member != null)
            {
                int myTickets = await this._ticketRepository.TotalCountByMember(member.Id);

                if (myTickets > 0)
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

        private async Task<List<MemberAccount>?> GetMemberAccountsAsync()
        {
            List<MemberAccount> memberAccounts = new List<MemberAccount>();

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if (member != null)
            {
                 memberAccounts = await this._memberAccountRepository.GetMemberAccountsAsync(member.Id);


            }

            return memberAccounts;

        }



        public async Task<List<MemberAccount>> Accounts() 
        {

            List<MemberAccount> accountDTOs = new List<MemberAccount>();

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            var baseAccountResponse = await _httpClient.GetAsync($"http://41.77.8.30:8081/api/BaseAccount/{member.AccountNumber}");

            if (!baseAccountResponse.IsSuccessStatusCode)
            {
                return accountDTOs;
            }

            var json = await baseAccountResponse.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var status = document.RootElement.GetProperty("status").GetInt32();

            if (status == 404)
            {
                Json(new { error = "error", message = "failed to create the user account from the member" });

                return accountDTOs;
            }

            var baseAccountElement = document.RootElement.GetProperty("data").GetProperty("baseAccount");


            var relatedAccounts = baseAccountElement.GetProperty("related_accounts");

            if (relatedAccounts.ValueKind == JsonValueKind.Array)
            {
                foreach (var relatedAccount in relatedAccounts.EnumerateArray())
                {
                   accountDTOs.Add(new MemberAccount()
                    {
                      
                        AccountNumber = relatedAccount.GetProperty("accountNumber").GetString(),
                        AccountName = relatedAccount.GetProperty("accountName").GetString(),
                        Balance = relatedAccount.GetProperty("balance").GetDecimal()
                    });
                }
            }

     


            return accountDTOs;
        }

    }
}
