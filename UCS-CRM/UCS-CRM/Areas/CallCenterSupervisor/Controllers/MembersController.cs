using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using UCS_CRM.Areas.Admin.ViewModels;
using UCS_CRM.Core.DTOs.Login;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.CallCenterSupervisor.Controllers
{
    [Area("CallCenterSupervisor")]
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly HttpClient _httpClient;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        public IConfiguration _configuration { get; }
        public MembersController(IMemberRepository memberRepository, IMapper mapper, IUnitOfWork unitOfWork, IEmailService emailService, HttpClient httpClient, IConfiguration configuration, HangfireJobEnqueuer jobEnqueuer)
        {
            _memberRepository = memberRepository;
            _emailService = emailService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _configuration = configuration;
            _jobEnqueuer = jobEnqueuer;
        }

        // GET: MemberController
        public ActionResult Index()
        {
            return View();
        }



        // GET: MemberController/Details/5
        public async Task<ActionResult> Detailso(int id)
        {
            var MemberDB = await this._memberRepository.GetMemberAsync(id);

            if (MemberDB == null)
            {
                return RedirectToAction("Index");
            }


            var mappedMember = this._mapper.Map<ReadMemberDTO>(MemberDB);

            return View(mappedMember);
        }


        public async Task<ActionResult> Details(int id)
        {

            //authenticate API
            string token = await ApiAuthenticate();

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index");
            }


            var member = await _memberRepository.GetMemberAsync(id);

            if (member == null)
            {
                return RedirectToAction("Index");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var baseAccountResponse = await _httpClient.GetAsync(_configuration["APIURL:link"] + $"BaseAccountAndRelatedAccounts?account_number={member.AccountNumber}");

            if (!baseAccountResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            var json = await baseAccountResponse.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var status = document.RootElement.GetProperty("status").GetInt32();

            

            if(document.RootElement.TryGetProperty("data", out JsonElement data))
            {
                if (data.ValueKind == JsonValueKind.Object)
                {
                    if (data.GetRawText() == "{}")
                    {
                        TempData["response"] = "Account number does not match any identification details";
                        return RedirectToAction("Index");
                    }
                   
                }
            }
            

            if (status == 404)
            {
                TempData["response"] = "Account number does not match any identification details";
                return RedirectToAction("Index");
            }

            var baseAccountElement = document.RootElement.GetProperty("data").GetProperty("base_account");

            decimal balance;
            if (decimal.TryParse(baseAccountElement.GetProperty("balance").GetString(), out balance))
            {
                balance = balance;
            }

            var baseAccount = new ReadMemberDTO()
            {
                // MemberId = int.Parse(baseAccountElement.GetProperty("account_number").GetString()),
                AccountNumber = baseAccountElement.GetProperty("account_number").GetString(),
                AccountName = baseAccountElement.GetProperty("account_name").GetString(),
                Balance = balance,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Address = member.Address,
                NationalId = member.NationalId
            };

            //var relatedAccounts = baseAccountElement.GetProperty("related_accounts");
            var relatedAccounts = document.RootElement.GetProperty("data").GetProperty("related_accounts");

            if (relatedAccounts.ValueKind == JsonValueKind.Array)
            {
                foreach (var relatedAccount in relatedAccounts.EnumerateArray())
                {
                    if (decimal.TryParse(relatedAccount.GetProperty("balance").GetString(), out balance))
                    {
                        balance = balance;
                    }

                    baseAccount.MemberAccounts.Add(new MemberAccount()
                    {
                        // MemberId = int.Parse(relatedAccount.GetProperty("member_id").GetString()),
                        AccountNumber = relatedAccount.GetProperty("account_number").GetString(),
                        AccountName = relatedAccount.GetProperty("account_name").GetString(),
                        Balance = balance
                    });
                }
            }

            var mappedMember = _mapper.Map<ReadMemberDTO>(baseAccount);


            return View(mappedMember);
        }



        // POST: MemberController/Edit/5
        [HttpPost]
        public async Task<ActionResult> CreateUserFromMember(UserFromMemberViewModel model)
        {
            try
            {
                //find the member with the Id provided

               UCS_CRM.Core.Models.Member? databaseMemberRecord = await this._memberRepository.GetMemberAsync(model.Id);

                if (databaseMemberRecord != null)
                {
                    ApplicationUser? user =  await this._memberRepository.CreateUserAccount(databaseMemberRecord, model.Email);

                    if (user == null)
                    {
                        return Json(new { error = "error", message = "failed to create the user account from the member" });
                    }

                    //sync changes 

                    await this._unitOfWork.SaveToDataStore();


                    //send emails

                    string UserNameBody = $@"
                    <html>
                    <head>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                            body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                            .logo {{ text-align: center; margin-bottom: 20px; }}
                            .logo img {{ max-width: 150px; }}
                            h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                            .account-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                            .account-info p {{ margin: 5px 0; }}
                            .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                            .cta-button:hover {{ background-color: #003d82; }}
                            .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='logo'>
                                <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                            </div>
                            <h2>Account Created</h2>
                            <div class='account-info'>
                                <p>An account has been created for you on UCS SACCO.</p>
                                <p>Your email address is: <strong>{user.Email}</strong></p>
                            </div>
                            <p>
                                <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>Login to Your Account</a>
                            </p>
                            <p class='footer'>Thank you for joining UCS SACCO.</p>
                        </div>
                    </body>
                    </html>";
                    string PasswordBody = $@"
                    <html>
                    <head>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                            body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                            .logo {{ text-align: center; margin-bottom: 20px; }}
                            .logo img {{ max-width: 150px; }}
                            h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                            .password-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                            .password-info p {{ margin: 5px 0; }}
                            .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                            .cta-button:hover {{ background-color: #003d82; }}
                            .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='logo'>
                                <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                            </div>
                            <h2>Account Password</h2>
                            <div class='password-info'>
                                <p>An account has been created for you on UCS SACCO App.</p>
                                <p>Your temporary password is: <strong>P@$$w0rd</strong></p>
                                <p>Please change this password upon your first login.</p>
                            </div>
                            <p>
                                <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>Login to Your Account</a>
                            </p>
                            <p class='footer'>For security reasons, please change your password immediately after logging in.</p>
                        </div>
                    </body>
                    </html>";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if(user.DeletedDate != null)
                    {
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Account Status", $"Good day, We are pleased to inform you that your account has been reactivated on the UCS SACCO. You may proceed to login using your previous credentials. ", user.SecondaryEmail);
                    }
                    else
                    {
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Login Details", UserNameBody, user.SecondaryEmail);
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Login Details", PasswordBody, user.SecondaryEmail);
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Account Details", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the USC SACCO.\r\nPlease visit XXXXXXXXXXXXXXXXXXXXXXXX to register with Gravatar. ", user.SecondaryEmail);                       
                    }


                    return Json(new { status = "success", message = "user account created successfully" });
                }
                else
                {
                    return Json(new { error = "error", message = "failed to create the user account from the member" });

                }

            }
            catch (Exception)
            {

                return Json(new { error = "error", message = "failed to create the user account from the member" });
            }
        }

        // GET: MemberController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MemberController/Delete/5
        [HttpPost]
        public async Task<ActionResult> DeleteUserAccount(int id)
        {
            //get member id

            try
            {
                UCS_CRM.Core.Models.Member databaseMemberRecord = await this._memberRepository.GetMemberAsync(id);

                if (databaseMemberRecord != null)
                {
                    this._memberRepository.DeleteUser(databaseMemberRecord);

                    //sync changes 

                   await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "account deleted successfully" });
                }
                else
                {
                    return Json(new { error = "error", message = "failed to delete the account" });

                }


            }
            catch (Exception ex)
            {

                return Json(new {error = "error", message = "failed to delete the message"});
            }
            
        }
        [HttpPost]
        public async Task<ActionResult> GetMembers()
        {
            //datatable stuff
            var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
            var start = HttpContext.Request.Form["start"].FirstOrDefault();
            var length = HttpContext.Request.Form["length"].FirstOrDefault();

            var sortColumn = HttpContext.Request.Form["columns[" + HttpContext.Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnAscDesc = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int resultTotal = 0;

            //create a cursor params based on the data coming from the datatable
            CursorParams CursorParameters = new CursorParams() { SearchTerm = searchValue.Trim(), Skip = skip, SortColum = sortColumn, SortDirection = sortColumnAscDesc, Take = pageSize };

            resultTotal = await this._memberRepository.TotalFilteredMembersCount(CursorParameters);
            var result = await this._memberRepository.GetMembers(CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadMemberDTO>>(result);

            var cleanListOfMemberReadDTO = new List<ReadMemberDTO>();

            mappedResult.ForEach(m =>
            {

              

                if(m?.User != null)
                {
                    m.User.Member = null;
                }

                cleanListOfMemberReadDTO.Add(m);
            });
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = cleanListOfMemberReadDTO });

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

            var status = document.RootElement.GetProperty("status").GetInt32();

            if (status == 404)
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
