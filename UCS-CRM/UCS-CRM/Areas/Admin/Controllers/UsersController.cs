﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Core.ViewModels;
using System.Linq.Dynamic;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using UCS_CRM.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using UCS_CRM.Persistence.SQLRepositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IBranchRepository _branchRepository;
        private RoleManager<Role> _roleManager;
        private readonly IRoleRepositorycs _roleRepositorycs;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HangfireJobEnqueuer _jobEnqueuer;

        private readonly IConfiguration Configuration;
        public UsersController(IUserRepository userRepository, IEmailService emailService, RoleManager<Role> roleManager,
            UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IDepartmentRepository departmentRepository, IRoleRepositorycs roleRepositorycs, IBranchRepository branchRepository, IConfiguration configuration, HangfireJobEnqueuer jobEnqueuer)
        {
            this._userRepository = userRepository;
            this._emailService = emailService;
            this._roleManager = roleManager;
            this._userManager = userManager;
            this._unitOfWork = unitOfWork;
            this._departmentRepository = departmentRepository;
            this._roleRepositorycs = roleRepositorycs;
            this._branchRepository = branchRepository;
            Configuration = configuration;
            this._jobEnqueuer = jobEnqueuer;
        }

        // GET: UsersController
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.rolesList = roles;
            await populateViewBags();
            await SetPendingUsersCount();
            return View();
        }

        private async Task SetPendingUsersCount()
        {
            var membersInRole = await _userRepository.GetUsersInRole("Member");
            ViewBag.PendingUsersCount = membersInRole.Count(u => !u.IsApproved);
        }

        [HttpGet]
        public async Task<IActionResult> PendingUsers()
        {
            await SetPendingUsersCount();
            await populateViewBags();
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Send approval notification email
                string approvalEmailBody = $@"
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
                        <h2>Account Approved</h2>
                        <div class='account-info'>
                            <p>Your UCS SACCO account has been approved by an administrator.</p>
                            <p>You can now log in to your account using your credentials.</p>
                        </div>
                        <p>
                            <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Login to Your Account</a>
                        </p>
                        <p class='footer'>Thank you for choosing UCS SACCO.</p>
                    </div>
                </body>
                </html>";

                EmailHelper.SendEmail(_jobEnqueuer, user.Email, "Account Approved", approvalEmailBody, user.SecondaryEmail);
                
                await SetPendingUsersCount();
                return Json(new { success = true, message = "User approved successfully" });
            }

            return Json(new { success = false, message = "Failed to approve user" });
        }

        private async Task<List<SelectListItem>> GetBranches()
        {
            List<SelectListItem> branches = new() { new SelectListItem() { Text = "---Select Branch---", Value = "" } };

            var branchesDb = await this._branchRepository.GetBranches();

            if (branchesDb != null)
            {
                branchesDb.ForEach(d =>
                {
                    branches.Add(new SelectListItem() { Text = d.Name, Value = d.Id.ToString() });
                });
            }

            return branches;
        }


        // GET: UsersController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsersController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserViewModel userViewModel)
        {
            if (!ModelState.IsValid)
            {
                return await HandleInvalidModelState(userViewModel);
            }

            var applicationUser = CreateApplicationUser(userViewModel);
            
            // Auto-approve if not a member
            applicationUser.IsApproved = userViewModel.RoleName != "Member";
            
            var deletedUser = await this._userRepository.FindDeletedUserByEmail(applicationUser.Email);

            if (deletedUser is not null)
            {
                ModelState.AddModelError(nameof(userViewModel.Email), "This account belongs to a deleted user, either restore the user or use a different email address");
                return await HandleInvalidModelState(userViewModel);
            }

            var recordPresence = this._userRepository.Exists(applicationUser);
            if (recordPresence is not null)
            {
                ModelState.AddModelError(nameof(userViewModel.Email), "This email is already in use by another account");
                return await HandleInvalidModelState(userViewModel);
            }

            var result = await this._userRepository.CreateUserAsync(applicationUser, "P@$$w0rd");
            if (!result.Succeeded)
            {
                return await HandleInvalidModelState(userViewModel);
            }

            var roleResult = await this._userRepository.AddUserToRoleAsync(applicationUser, userViewModel.RoleName);
            if (!roleResult.Succeeded)
            {
                return await HandleInvalidModelState(userViewModel);
            }

            await SendUserEmails(applicationUser);
            return Json(new { response = "User account created successfully" });
        }

        private async Task<ActionResult> HandleInvalidModelState(UserViewModel userViewModel)
        {
            userViewModel.DataInvalid = "true";
            await populateViewBags();
            return PartialView("_CreateUserPartial", userViewModel);
        }

        private ApplicationUser CreateApplicationUser(UserViewModel userViewModel)
        {
            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
            int pin = RandomNumber();

            return new ApplicationUser
            {
                FirstName = userViewModel.FirstName,
                LastName = userViewModel.LastName,
                Gender = userViewModel.Gender,
                Email = userViewModel.Email,
                PhoneNumber = userViewModel.PhoneNumber,
                UserName = userViewModel.Email,
                EmailConfirmed = false,
                DepartmentId = userViewModel.DepartmentId,
                LastPasswordChangedDate = DateTime.Now,
                BranchId = userViewModel.BranchId,
                CreatedById = claimsIdentitifier.Value,
                Pin = pin
            };
        }

        private async Task SendUserEmails(ApplicationUser applicationUser)
        {
            string UserNameBody = GenerateUserNameEmailBody(applicationUser);
            string PasswordBody = GeneratePasswordEmailBody();
            
            // Only send activation email for members who need approval
            if (!applicationUser.IsApproved)
            {
                string AccountActivationBody = GenerateAccountActivationEmailBody(applicationUser);
                EmailHelper.SendEmail(this._jobEnqueuer, applicationUser.Email, "Account Activation", AccountActivationBody, applicationUser.SecondaryEmail);
            }

            EmailHelper.SendEmail(this._jobEnqueuer, applicationUser.Email, "Login Details", UserNameBody, applicationUser.SecondaryEmail);
            EmailHelper.SendEmail(this._jobEnqueuer, applicationUser.Email, "Login Details", PasswordBody, applicationUser.SecondaryEmail);
        }

        private string GenerateUserNameEmailBody(ApplicationUser applicationUser)
        {
            return $@"
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
                    <h2>New Account Created</h2>
                    <div class='account-info'>
                        <p>An account has been created for you on UCS SACCO.</p>
                        <p>Your email address is: <strong>{applicationUser.Email}</strong></p>
                    </div>
                    <p class='footer'>Thank you for joining UCS SACCO.</p>
                </div>
            </body>
            </html>";
        }

        private string GeneratePasswordEmailBody()
        {
            return $@"
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
                    <p class='footer'>For security reasons, please do not share this password with anyone.</p>
                </div>
            </body>
            </html>";
        }

        private string GenerateAccountActivationEmailBody(ApplicationUser applicationUser)
        {
            var host = Configuration.GetSection("HostingSettings")["Host"];
            var protocol = Configuration.GetSection("HostingSettings")["Protocol"];
            return $@"
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
                        <p style='font-size: 24px; font-weight: bold; text-align: center;'>{applicationUser.Pin}</p>
                    </div>
                    <p style='text-align: center;'>
                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                    </p>
                    <p class='footer'>Please use this OTP to activate your account.</p>
                </div>
            </body>
            </html>";
        }

        // GET: UsersController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var user = await this._userRepository.FindByIdAsync(id);

            UserViewModel userView = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                DepartmentId = user.DepartmentId ?? 0, // Default to 0 if null
                Department = user.Department,
                BranchId = user.BranchId ?? 0, // Default to 0 if null
                Id = user.Id,
                SecondaryEmail = user.SecondaryEmail,
            };


            var userRoles = await this._userRepository.GetRolesAsync(user.Id);

            //only add role name if the user actually is assigned to some roles

            if (userRoles.Count > 0)
            {
                userView.RoleName = userRoles.First();
            }


            return Json(userView);
        }

        // POST: UsersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, UserViewModel applicationViewModel)
        {
            applicationViewModel.DataInvalid = "true";
            UserViewModel user = new UserViewModel();
            List<SelectListItem> roles = new List<SelectListItem>();
            List<SelectListItem> districts = new List<SelectListItem>();
         
            if (ModelState.IsValid)
            {
                applicationViewModel.DataInvalid = "";

                //find a user with the id provided
                var dbUser = await this._userRepository.FindByIdAsync(applicationViewModel.Id);

                if (dbUser != null)
                {
                    dbUser.FirstName = applicationViewModel.FirstName;
                    dbUser.LastName = applicationViewModel.LastName;
                    dbUser.Gender = applicationViewModel.Gender;
                    dbUser.Email = applicationViewModel.Email;
                    dbUser.PhoneNumber = applicationViewModel.PhoneNumber;
                    dbUser.DepartmentId = applicationViewModel.DepartmentId;
                    dbUser.UserName = applicationViewModel.Email;
                    dbUser.BranchId = applicationViewModel.BranchId;
                    dbUser.SecondaryEmail = applicationViewModel.SecondaryEmail;


                    IdentityResult result = await this._userRepository.UpdateAsync(dbUser);


                    //update user role
                    if (!applicationViewModel.RoleName.IsNullOrEmpty())
                    {


                        var currentUserRoles = await this._userRepository.GetRolesAsync(dbUser.Id);

                        if (!currentUserRoles.Contains(applicationViewModel.RoleName))
                        {
                            //swap the roles
                            await _userRepository.RemoveFromRolesAsync(dbUser, currentUserRoles);

                            await this._userRepository.AddUserToRoleAsync(dbUser, applicationViewModel.RoleName);
                        }
                    }
                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "user details updated successfully" });
                }



                return Json(new { status = "error", message = $"No user with the submited Id {applicationViewModel.Id} was found in the system" });
            }

            this._roleManager.Roles.ToList().ForEach(r =>
            {
                roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
            });

          
            ViewBag.rolesList = roles;

            ViewBag.genderList = user.GenderList;

          

            return PartialView("_EditUserPartial", applicationViewModel);
        }

       
        // POST: UsersController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            //find the user with the Id provided

            var user = await this._userRepository.FindByIdAsync(id);

            if (user != null)
            {
                user.DeletedDate = DateTime.Now;
                user.Status = Lambda.Deleted;

                //update the user

                await this._userRepository.UpdateAsync(user);

                await this._unitOfWork.SaveToDataStore();

               // _emailService.SendMail(user.Email, "Account Changes", "Sorry but your account has been suspended from UCS SACCO. You can no longer access the appliaction. Contact support for more information and queries");

                return Json(new { status = "success", message = "user deleted from the system" });
            }

            return Json(new { status = "error", message = "user not found" });
        }

        [HttpPost]

        public async Task<ActionResult> GetUsers()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            try
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

                resultTotal = await this._userRepository.TotalFilteredUsersCount(CursorParameters);
                var result = users = await this._userRepository.GetUsersWithRoles(CursorParameters);
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

              

            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }

        }

        [HttpGet]
        [Route("admin/unconfirmedUsers")]
        public async Task<ActionResult> UnconfirmedUsers() 
        {
            UserViewModel user = new UserViewModel();

            List<SelectListItem> roles = new List<SelectListItem>();
            List<SelectListItem> districts = new List<SelectListItem>();



            this._roleManager.Roles.ToList().ForEach(r =>
            {
                roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
            });


            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);


            ViewBag.currentUserId = claimsIdentitifier.Value;
            ViewBag.rolesList = roles;


            ViewBag.genderList = user.GenderList;
            return View();
        }
        public async Task<ActionResult> GetUnconfirmedUsers()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            try
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

              

                try
                {


                    resultTotal = await this._userRepository.TotalUncomfirmedCount(CursorParameters);
                    var result =  await this._userRepository.GetUnconfirmedUsersWithRoles(CursorParameters);
                    return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

                }
                catch (Exception)
                {

                    throw;
                }


            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }
        }

        // Confirm
        public async Task<ActionResult> ConfirmUser(string id)
        {

            //find the user with the Id provided

            var user = await this._userRepository.FindUnconfirmedUserByIdAsync(id);

            if (user != null)
            {
                user.DeletedDate = null;
                user.Status = Lambda.Active;
                user.EmailConfirmed = true;

                //update the user

                await this._userRepository.UpdateAsync(user);

                await this._unitOfWork.SaveToDataStore();

                 string emailBody = $@"
                 <html>
                 <head>
                     <style>
                         @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                         body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                         .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                         .logo {{ text-align: center; margin-bottom: 20px; }}
                         .logo img {{ max-width: 150px; }}
                         h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                         .confirmation-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                         .confirmation-info p {{ margin: 5px 0; }}
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
                         <h2>Account Confirmation</h2>
                         <div class='confirmation-info'>
                             <p>Congratulations! Your account has been confirmed on UCS SACCO.</p>
                         </div>
                         <p>
                             <a href='https://crm.ucssacco.com/' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                         </p>
                         <p class='footer'>Thank you for joining UCS SACCO.</p>
                     </div>
                 </body>
                 </html>";

                 EmailHelper.SendEmail(_jobEnqueuer, user.Email, "Account Confirmation", emailBody, user.SecondaryEmail);


                return Json(new { status = "success", message = "user confirmed from the system successfully" });
            }

            return Json(new { status = "error", message = "user not found" });
        }

        public ActionResult DeletedUsers()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> GetDeletedUsers()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            try
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

                users = await this._userRepository.GetDeletedUsers(CursorParameters);


                try
                {

                    resultTotal = await this._userRepository.TotalDeletedCount(CursorParameters);
                    var result = users;
                    return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

                }
                catch (Exception)
                {

                    throw;
                }


            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }
        }

        [Route("admin/users/reactivate/{id}")]
        public async Task<ActionResult> Reactivate(string id)
        {

            //find the user with the Id provided

            var user = await this._userRepository.FindByIdDeleteInclusiveAsync(id);

            if (user != null)
            {
                user.DeletedDate = null;
                user.Status = Lambda.Active;

                //update the user

                await this._userRepository.UpdateAsync(user);

                await this._unitOfWork.SaveToDataStore();

                string emailBody = $@"
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
                            <p>Congratulations! Your account has been reactivated on UCS SACCO.</p>
                        </div>
                        <p>
                            <a href='https://crm.ucssacco.com/' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                        </p>
                        <p class='footer'>Thank you for using our service.</p>
                    </div>
                </body>
                </html>";

                EmailHelper.SendEmail(_jobEnqueuer, user.Email, "Account Reactivation", emailBody, user.SecondaryEmail);

               
                return Json(new { status = "success", message = "user activated from the system successfully" });
            }

            return Json(new { status = "error", message = "user not found" });
        }
        public async Task<ActionResult> FetchRolesOnDepartment(int selectedValue)
        {
            var listOfRoles = await GetRoles(selectedValue);

            return Json(listOfRoles);
        }
        private async Task<List<SelectListItem>> GetDepartments()
        {
            List<SelectListItem> departments = new() { new SelectListItem() { Text = "---Select Department---", Value=""} };

            var departmentDb = await this._departmentRepository.GetDepartments();

            if(departmentDb != null)
            {
                departmentDb.ForEach(d =>
                {
                    departments.Add(new SelectListItem() { Text = d.Name, Value = d.Id.ToString() });
                });
            }

            return departments;

        }
        private async Task<List<SelectListItem>> GetRoles(int departmentId = 0)
        {
            List<SelectListItem> rolesList = new();

            //fetch positions from the datastore
            if(departmentId > 0)
            {
                var department = await this._departmentRepository.GetDepartment(departmentId);

                if (department != null)
                {
                    //fetch all positions associated with the department

                    department.Roles.ForEach(p =>
                    {
                        rolesList.Add(new SelectListItem() { Text = p.Name, Value = p.Name.ToString() });
                    });
                }
            }
            else
            {
                //get all positions if department wasn't specified
                var roles = await this._roleRepositorycs.GetRolesAsync();

                if (roles != null)
                {
                    roles.ForEach(p =>
                    {
                        rolesList.Add(new SelectListItem() { Text = p.Name, Value = p.Name.ToString() });
                    });
                }
            }



            return rolesList;
        }
        private async Task populateViewBags()
        {
            List<SelectListItem> roles = new List<SelectListItem>();
            UserViewModel newUser = new UserViewModel();

            this._roleManager.Roles.Where(r => r.Name.ToLower() != "member").ToList().ForEach(r =>
            {
                roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
            });

            ViewBag.rolesList = roles;
            ViewBag.genderList = newUser.GenderList;
            ViewBag.departmentsList = await GetDepartments();
            ViewBag.branchList = await GetBranches();

        }

        private int RandomNumber()
        {
            // generating a random number
            Random generator = new Random();
            int number = generator.Next(100000, 999999);

            return number;
        }

        [HttpPost]
        public async Task<JsonResult> GetPendingUsers()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                // Get users in Member role who are not approved
                var membersInRole = await _userRepository.GetUsersInRole("Member");
                var pendingMembers = membersInRole.Where(u => !u.IsApproved).AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    pendingMembers = pendingMembers.Where(u =>
                        u.FirstName.Contains(searchValue) ||
                        u.LastName.Contains(searchValue) ||
                        u.Email.Contains(searchValue) ||
                        u.PhoneNumber.Contains(searchValue) ||
                        u.Gender.Contains(searchValue)
                    );
                }

                // Apply sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    switch (sortColumn)
                    {
                        case "firstName":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.FirstName) : 
                                pendingMembers.OrderByDescending(u => u.FirstName);
                            break;
                        case "lastName":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.LastName) : 
                                pendingMembers.OrderByDescending(u => u.LastName);
                            break;
                        case "email":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.Email) : 
                                pendingMembers.OrderByDescending(u => u.Email);
                            break;
                        case "gender":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.Gender) : 
                                pendingMembers.OrderByDescending(u => u.Gender);
                            break;
                        case "phoneNumber":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.PhoneNumber) : 
                                pendingMembers.OrderByDescending(u => u.PhoneNumber);
                            break;
                        case "createdDate":
                            pendingMembers = sortColumnDirection == "asc" ? 
                                pendingMembers.OrderBy(u => u.CreatedDate) : 
                                pendingMembers.OrderByDescending(u => u.CreatedDate);
                            break;
                        default:
                            pendingMembers = pendingMembers.OrderByDescending(u => u.CreatedDate);
                            break;
                    }
                }

                var recordsTotal = pendingMembers.Count();
                var data = pendingMembers
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        formattedFirstName = u.FirstName,
                        formattedLastName = u.LastName,
                        formattedGender = u.Gender,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber,
                        formattedCreatedDate = u.CreatedDate.ToString("dd-MM-yyyy HH:mm")
                    })
                    .ToList();

                return Json(new
                {
                    draw = draw,
                    recordsFiltered = recordsTotal,
                    recordsTotal = recordsTotal,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Failed to load pending members data" });
            }
        }

    }
}
