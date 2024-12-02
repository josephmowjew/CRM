using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
using System.Security.Authentication;
using UCS_CRM.Core.Services;

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
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly HangfireJobEnqueuer _jobEnqueuer;

        public HomeController(
            ITicketRepository ticketRepository,
            IMemberRepository memberRepository,
            IMemberAccountRepository memberAccountRepository,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<HomeController> logger,
            HangfireJobEnqueuer jobEnqueuer)
        {
            _ticketRepository = ticketRepository;
            _memberRepository = memberRepository;
            _memberAccountRepository = memberAccountRepository;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                ViewBag.allTicketsCount = await this.CountAllMyTickets();
                ViewBag.closedTicketsCount = await this.CountTicketsByStatus("Closed");
                ViewBag.archivedTicketsCount = await this.CountTicketsByStatus("Archived");
                ViewBag.newTicketsCount = await this.CountTicketsByStatus("New");
                ViewBag.resolvedTicketsCount = await this.CountTicketsByStatus("Resolved");
                ViewBag.reopenedTicketsCount = await this.CountTicketsByStatus("Re-opened");
                var memberId = await GetMemberId();
                ViewBag.MemberId = memberId;
                var accounts = await Accounts();
                ViewBag.accounts = accounts;

                return View();
            }
            catch (Exception ex)
            {
                TempData["errorResponse"] = ex.Message;
                return RedirectToAction("Create", "Auth", new { area = "" });
            }
        }

        private async Task<int> CountAllMyTickets()
        {
            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if (member != null)
            {
                return await this._ticketRepository.TotalCountByMember(member.Id);
            }

            return 0;
        }

        private async Task<int> CountTicketsByStatus(string status)
        {
            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
            var member = await this._memberRepository.GetMemberByUserId(claimsIdentitifier.Value);

            if (member != null)
            {
                return await this._ticketRepository.CountTicketsByStatusMember(status, member.Id);
            }

            return 0;
        }

        public async Task<List<MemberAccount>> Accounts()
        {
            try
            {
                string token = await ApiAuthenticate();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("API authentication token is null or empty");
                    return new List<MemberAccount>();
                }

                var userClaims = User.Identity as ClaimsIdentity;
                if (userClaims == null)
                {
                    _logger.LogWarning("User claims identity is null");
                    return new List<MemberAccount>();
                }

                var claimsIdentifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
                if (claimsIdentifier == null)
                {
                    _logger.LogWarning("NameIdentifier claim not found");
                    return new List<MemberAccount>();
                }

                var member = await _memberRepository.GetMemberByUserId(claimsIdentifier.Value);
                if (member == null)
                {
                    _logger.LogWarning($"Member not found for user ID: {claimsIdentifier.Value}");
                    return new List<MemberAccount>();
                }

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var baseAccountResponse = await _httpClient.GetAsync(_configuration["APIURL:link"] + $"BaseAccountAndRelatedAccounts?account_number={member.AccountNumber}");

                if (!baseAccountResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"API request failed with status code: {baseAccountResponse.StatusCode}");
                    return new List<MemberAccount>();
                }

                var json = await baseAccountResponse.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(json);

                var status = document.RootElement.GetProperty("status").GetInt32();
                var message = document.RootElement.GetProperty("message").GetString();

                if (message.Equals("Account Number Does Not Match any Identification Details", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"No matching account found for account number: {member.AccountNumber}");
                    return new List<MemberAccount>();
                }

                if (status == 404)
                {
                    _logger.LogWarning("API returned a 404 status");
                    return new List<MemberAccount>();
                }

                var accountDTOs = new List<MemberAccount>();
                var relatedAccounts = document.RootElement.GetProperty("data").GetProperty("related_accounts");

                if (relatedAccounts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var relatedAccount in relatedAccounts.EnumerateArray())
                    {
                        if (decimal.TryParse(relatedAccount.GetProperty("balance").GetString(), out decimal balance))
                        {
                            accountDTOs.Add(new MemberAccount()
                            {
                                AccountNumber = relatedAccount.GetProperty("account_number").GetString(),
                                AccountName = relatedAccount.GetProperty("account_name").GetString(),
                                Balance = balance
                            });
                        }
                    }
                }

                return accountDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching accounts");
                return new List<MemberAccount>();
            }
        }

        private async Task<string> ApiAuthenticate()
        {
            try
            {
                var username = _configuration["APICredentials:Username"];
                var password = _configuration["APICredentials:Password"];

                APILogin apiLogin = new APILogin()
                {
                    Username = username,
                    Password = password,
                };

                var jsonContent = JsonConvert.SerializeObject(apiLogin);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var tokenResponse = await _httpClient.PostAsync(_configuration["APIURL:link"] + $"Token", stringContent);

                var json = await tokenResponse.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(json);

                var status = document.RootElement.GetProperty("status").GetInt32();

                if (status == 404)
                {
                    throw new Exception("Failed to authenticate with the API.");
                }

                return document.RootElement.GetProperty("token").GetString();
            }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
            {
                _logger.LogError(ex, "SSL Certificate validation failed when connecting to MHub API");
                await NotifySupportAboutSSLIssue(ex);
                throw new Exception("There was a security issue connecting to our services. Our support team has been notified.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred when authenticating with MHub API");
                throw new Exception("An unexpected error occurred. Please try again later or contact support.");
            }
        }

       private async Task NotifySupportAboutSSLIssue(Exception ex)
        {
            string subject = "SSL Certificate Issue with MHub API";
            
            string userFriendlyMessage = "An SSL certificate validation error occurred when connecting to the MHub API. This likely indicates an issue with the API server's SSL certificate.";
            
            string timeOfOccurrence = $"Time of occurrence: {DateTime.Now:yyyy-MM-dd HH:mm:ss GMT+2}";
            
            if (ex is HttpRequestException httpEx && httpEx.InnerException is AuthenticationException authEx)
            {
                userFriendlyMessage += $"\n\nSpecific issue: {authEx.Message}";
            }
            
            string body = $"{userFriendlyMessage}\n\n{timeOfOccurrence}\n\nPlease investigate and update the SSL certificate if necessary.";
            
            EmailHelper.SendEmail(_jobEnqueuer, _configuration["SupportEmail"], subject, body);
        }

        private async Task<int?> GetMemberId()
        {
            var userClaims = (ClaimsIdentity?)User.Identity;

            var claimsIdentitifier = userClaims?.FindFirst(ClaimTypes.NameIdentifier);

            if (claimsIdentitifier != null)
            {

                var currentUserId = claimsIdentitifier.Value;


                var member = await this._memberRepository.GetMemberByUserId(currentUserId);

                return member?.Id;

            }

            return null;
        }
    }
}
