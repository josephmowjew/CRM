using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Areas.Admin.ViewModels;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        public MembersController(IMemberRepository memberRepository, IMapper mapper, IUnitOfWork unitOfWork, IEmailService emailService, IUserRepository userRepository, HangfireJobEnqueuer jobEnqueuer)
        {
            _memberRepository = memberRepository;
            _emailService = emailService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _jobEnqueuer = jobEnqueuer;
        }

        // GET: MemberController
        public ActionResult Index()
        {
            UserViewModel newUser = new UserViewModel();
            ViewBag.genderList = newUser.GenderList;
            return View();
        }

      

       

        // POST: MemberController/Edit/5
        [HttpPost]
        public async Task<ActionResult> CreateUserFromMember(UserFromMemberViewModel model)
        {
            try
            {
                //find the member with the Id provided

                UCS_CRM.Core.Models.Member? databaseMemberRecord = await this._memberRepository.GetMemberAsync(model.Id);

                int pin = _memberRepository.RandomNumber();

                if (databaseMemberRecord != null)
                {

                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    //update the gender
                    databaseMemberRecord.Gender = model.Gender;


                    ApplicationUser? user =  await this._memberRepository.CreateUserAccount(databaseMemberRecord, model.Email, "",claimsIdentitifier.Value);

                    user.Pin = pin;

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
                            <p class='footer'>Thank you for choosing UCS SACCO.</p>
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
                            <h2>Account Created</h2>
                            <div class='password-info'>
                                <p>An account has been created for you on UCS SACCO App.</p>
                                <p>Your temporary password is: <strong>P@$$w0rd</strong></p>
                                <p>Please change this password upon your first login.</p>
                            </div>
                            <p>
                                <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                            </p>
                            <p class='footer'>Thank you for choosing UCS SACCO.</p>
                        </div>
                    </body>
                    </html>";
                    //send pin to email

                    string AccountActivationBody = $@"
                    <html>
                    <head>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                            body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                            .logo {{ text-align: center; margin-bottom: 20px; }}
                            .logo img {{ max-width: 150px; }}
                            h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                            .otp-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                            .otp-info p {{ margin: 5px 0; }}
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
                            <h2>Account Activation</h2>
                            <div class='otp-info'>
                                <p>Here is the One Time Pin (OTP) for your account on UCS:</p>
                                <p><strong>{user.Pin}</strong></p>
                            </div>
                            <p>
                                <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                            </p>
                            <p class='footer'>Thank you for choosing UCS SACCO.</p>
                        </div>
                    </body>
                    </html>";

                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if (user.DeletedDate != null)
                    {
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Account Reactivation", $@"
                        <html>
                        <head>
                            <style>
                                @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                                body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                                .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                                .logo {{ text-align: center; margin-bottom: 20px; }}
                                .logo img {{ max-width: 150px; }}
                                h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                                .info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
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
                                <h2>Account Reactivation</h2>
                                <div class='info'>
                                    <p>Good day,</p>
                                    <p>We are pleased to inform you that your account has been reactivated on the UCS SACCO.</p>
                                    <p>You may proceed to login using your previous credentials.</p>
                                </div>
                                <p>
                                    <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                                </p>
                                <p class='footer'>Thank you for choosing UCS SACCO.</p>
                            </div>
                        </body>
                        </html>");

                    }
                    else
                    {
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", UserNameBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", PasswordBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Account Activation", AccountActivationBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Account Password", $@"
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
                            <h2>Account Password</h2>
                            <div class='account-info'>
                                <p>An account has been created for you on UCS SACCO App.</p>
                                <p><strong>Your temporary password:</strong> P@$$w0rd</p>
                                <p>Please change this password upon your first login.</p>
                            </div>
                            <p>
                                <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Login to Your Account</a>
                            </p>
                            <p class='footer'>Thank you for choosing UCS SACCO.</p>
                        </div>
                    </body>
                    </html>");

                    string gravatorEmailBody = $@"
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
                                <h2>Set Up Your Profile Picture</h2>
                                <div class='account-info'>
                                    <p>Good day,</p>
                                    <p>To enhance your profile in the UCS SACCO application, we recommend setting up a profile picture using Gravatar.</p>
                                    <p>Gravatar allows you to associate an avatar with your email address, which will be displayed on your profile in our application.</p>
                                </div>
                                <p>
                                    <a href='https://en.gravatar.com/' class='cta-button' style='color: #ffffff;'>Register with Gravatar</a>
                                </p>
                                <p class='footer'>Thank you for being a part of UCS SACCO.</p>
                            </div>
                        </body>
                        </html>";
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Set Up Your Profile Picture", gravatorEmailBody);

                      

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
            CursorParams CursorParameters = new CursorParams() { SearchTerm = searchValue, Skip = skip, SortColum = sortColumn, SortDirection = sortColumnAscDesc, Take = pageSize };

            resultTotal = await this._memberRepository.TotalCount();
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


    }
}
