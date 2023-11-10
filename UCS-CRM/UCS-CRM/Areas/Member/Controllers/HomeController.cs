using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UCS_CRM.Core.DTOs.Login;
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
        public IConfiguration _configuration { get; }


        public HomeController(ITicketRepository ticketRepository, IMemberRepository memberRepository, IMemberAccountRepository memberAccountRepository,IMapper mapper, 
            HttpClient httpClient, IConfiguration configuration)
        {
            _ticketRepository = ticketRepository;
            _memberRepository = memberRepository;
            _memberAccountRepository = memberAccountRepository;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ActionResult> Index()
        {
            ViewBag.allTicketsCount = await this.CountAllMyTickets();
            ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
            ViewBag.archivedTicketsCount = await this.CountTicketsByStatus("Archived");
            ViewBag.newTicketsCount = await this.CountTicketsByStatus("New");
            ViewBag.resolvedTicketsCount = await this.CountTicketsByStatus("Resolved");
            ViewBag.reopenedTicketsCount = await this.CountTicketsByStatus("Re-opened");

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
            //authenticate API
            string token = await ApiAuthenticate();

            if (string.IsNullOrEmpty(token))
            {
                return new List<MemberAccount>();
            }         


            List<MemberAccount> accountDTOs = new List<MemberAccount>();

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var baseAccountResponse = await _httpClient.GetAsync(_configuration["APIURL:link"] + $"BaseAccountAndRelatedAccounts?account_number={member.AccountNumber}");

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

            var baseAccountElement = document.RootElement.GetProperty("data").GetProperty("base_account");

            var relatedAccounts = document.RootElement.GetProperty("data").GetProperty("related_accounts");

            if (relatedAccounts.ValueKind == JsonValueKind.Array)
            {
                foreach (var relatedAccount in relatedAccounts.EnumerateArray())
                {
                    decimal balance;
                    if (decimal.TryParse(relatedAccount.GetProperty("balance").GetString(), out balance))
                    {
                        balance = balance;
                    }
                    accountDTOs.Add(new MemberAccount()
                    {
                      
                        AccountNumber = relatedAccount.GetProperty("account_number").GetString(),
                        AccountName = relatedAccount.GetProperty("account_name").GetString(),
                        Balance = balance
                    });
                }
            }

     


            return accountDTOs;
        }


        public async Task<string> ApiAuthenticate()
        {

           // APIToken token = new APIToken();

            var username = _configuration["APICredentials:Username"];
            var password = _configuration["APICredentials:Password"];

            APILogin apiLogin = new APILogin()
            {
                Username = username,
                Password = password,
            };

        

            var jsonContent = JsonConvert.SerializeObject(apiLogin);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send POST request        
            var tokenResponse = await _httpClient.PostAsync(_configuration["APIURL:link"] + $"Token", stringContent);

            var json = await tokenResponse.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var status = document.RootElement.GetProperty("status").GetString();

            if (status == "404")
            {
              
                //
                Console.WriteLine("Failed to login");
                return "Failed to login";
            }

            var token = document.RootElement.GetProperty("token").GetString();

            return token;
        }
    }
}
