using Bogus;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Configuration;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UCS_CRM.Core.DTOs.Login;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Data;
using UCS_CRM.Models;
using UCS_CRM.Persistence.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UCS_CRM.Core.Helpers;
using System.Security.Authentication;

namespace UCS_CRM.Controllers
{

    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMemberRepository _memberRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
 
        public IConfiguration _configuration { get; }
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        private readonly ILogger<AuthController> _logger;
        private readonly IFailedRegistrationRepository _failedRegistrationRepository;


       public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ApplicationDbContext context,
            IMemberRepository memberRepository, IUnitOfWork unitOfWork, IEmailService emailService, HttpClient httpClient, IConfiguration config, IDepartmentRepository departmentRepository, IBranchRepository branchRepository, IConfiguration configuration, HangfireJobEnqueuer hangfireJobEnqueuer, ILogger<AuthController> logger, IFailedRegistrationRepository failedRegistrationRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _context = context;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _httpClient = httpClient;
            _config = config;
            _departmentRepository = departmentRepository;
            _branchRepository = branchRepository;
            _configuration = configuration;
            _jobEnqueuer = hangfireJobEnqueuer;
            _logger = logger;
            _failedRegistrationRepository = failedRegistrationRepository;
        }

        public async Task<IActionResult> Create()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return await RedirectLoggedInUser(User.Identity.Name);
            }
            
            // If there's an error message, it will be displayed in the view
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoginViewModel loginModel)
        {
            if (!ModelState.IsValid)
                return View("Create", loginModel);

            var user = await _userManager.FindByEmailAsync(loginModel.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials");
                return View("Create", loginModel);
            }

            if (!user.IsApproved && !await _userManager.IsInRoleAsync(user, "Administrator"))
            {
                ModelState.AddModelError(string.Empty, "Your account is pending administrator approval");
                return View("Create", loginModel);
            }

            if (user.LastPasswordChangedDate < DateTime.Now.AddDays(-90))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return RedirectToAction("ResetPassword", new { token, email = user.Email });
            }

             if (!user.EmailConfirmed)
                {
                    // Generate and send OTP
                    int pin = _memberRepository.RandomNumber();
                    user.Pin = pin;
                    user.UpdatedDate = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    string userNameBody = $@"
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
                            .pin {{ font-size: 24px; font-weight: bold; text-align: center; color: #0056b3; }}
                            .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='logo'>
                                <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                            </div>
                            <h2>Confirmation Code</h2>
                            <div class='confirmation-info'>
                                <p>Your confirmation code is:</p>
                                <p class='pin'>{pin}</p>
                                <p>Enter this code to log in to your account.</p>
                            </div>
                            <p class='footer'>Thank you for using our service.</p>
                        </div>
                    </body>
                    </html>";
                    EmailHelper.SendEmail(_jobEnqueuer, user.Email, "Login Details", userNameBody, user.SecondaryEmail);
                    TempData["response"] = $"Check your email for the confirmation code";
                    return RedirectToAction("ConfirmAccount", "Auth", new { email = loginModel.Email });
                }

            var result = await _signInManager.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                

            
                // Generate and send OTP
                int pin = _memberRepository.RandomNumber();
                user.Pin = pin;
                user.UpdatedDate = DateTime.Now;
                await _userManager.UpdateAsync(user);

                string userNameBody = $@"
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
                        .pin {{ font-size: 24px; font-weight: bold; text-align: center; color: #0056b3; }}
                        .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='logo'>
                            <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                        </div>
                        <h2>Confirmation Code</h2>
                        <div class='confirmation-info'>
                            <p>Your confirmation code is:</p>
                            <p class='pin'>{pin}</p>
                            <p>Enter this code to log in to your account.</p>
                        </div>
                        <p class='footer'>Thank you for using our service.</p>
                    </div>
                </body>
                </html>";
                
                EmailHelper.SendEmail(_jobEnqueuer, user.Email, "Login Details", userNameBody, user.SecondaryEmail);
                TempData["response"] = $"Check your email for the confirmation code";
                return RedirectToAction("ConfirmAccount", "Auth", new { email = loginModel.Email });
                

                // User is authenticated and email is confirmed
                HttpContext.Session.SetString("LastUserActivity",DateTime.Now.ToString());
                return await RedirectLoggedInUser(user.Email);
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials");
            return View("Create", loginModel);
        }

       [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAccount(ConfirmPin confirmPin)
        {
            //clear temp messages 
            TempData["response"] = "";
            TempData["errorResponse"] = "";
            
            var user = await _userManager.FindByEmailAsync(confirmPin.Email);
            if (user == null)
            {
                TempData["errorResponse"] = "No account was found to activate";
                return View();
            }

            if (user.Pin != confirmPin.Pin)
            {
                ModelState.AddModelError("", "Invalid confirmation code");
                return View(confirmPin);
            }

            if (user.UpdatedDate <= DateTime.Now.AddMinutes(-5))
            {
              
               
                TempData["error"] = "Confirmation code has expired";

                return RedirectToAction("Create", "Auth");
            }

                user.EmailConfirmed = true;
                user.Pin = 0; // Reset the pin after successful confirmation
                await _userManager.UpdateAsync(user);

                // Sign out any existing authentication
                await _signInManager.SignOutAsync();

                // Sign in the user and create the authentication cookie
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Generate claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email)
                };


                   // Add roles to claims
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Create ClaimsIdentity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create ClaimsPrincipal
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in the user and create the authentication cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties 
                    { 
                        IsPersistent = false,
                        ExpiresUtc = DateTime.Now.AddMinutes(30) // Set an expiration time as needed
                    }
                );

                HttpContext.Session.SetString("LastUserActivity", DateTime.Now.ToString());
                TempData["response"] = "Your account has been activated successfully";

            // Redirect to the appropriate area
            return await RedirectLoggedInUser(user.Email);
        }

        private async Task<IActionResult> RedirectLoggedInUser(string email)
        {
            var user = await _userRepository.GetUserWithRole(email);

            TempData["response"] = "";
            if (user == null)
            {
                
                return RedirectToAction("Index", "Home");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (user.Department?.Name.Contains("Executive suite", StringComparison.OrdinalIgnoreCase) == true)
            {
               
                return RedirectToAction("Index", "Home", new { Area = "SeniorManager" });
            }

                if (string.IsNullOrEmpty(role))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Officer" });
                }

                string roleLower = role.ToLower();
                if (roleLower.Contains("manager"))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Manager" });
                }

                if (roleLower.Contains("supervisor"))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Supervisor" });
                }

                switch (roleLower)
                {
                    case "administrator":
                        return RedirectToAction("Index", "Home", new { Area = "Admin" });
                    case "member":
                        return RedirectToAction("Index", "Home", new { Area = "Member" });
                    case "teller":
                        return RedirectToAction("Index", "Home", new { Area = "Teller" });
                    case "call center agent":
                        return RedirectToAction("Index", "Home", new { Area = "CallCenterOfficer" });
                    case "ict officer":
                        return RedirectToAction("Index", "Home", new { Area = "ICTOfficer" });
                    default:
                        return RedirectToAction("Index", "Home", new { Area = "Officer" });
                }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCode(ConfirmPin confirmPin)
        {
            if (!ModelState.IsValid)
            {
                return View("MFA", confirmPin);
            }

            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
            
            if (claimsIdentifier == null)
            {
                return RedirectToAction("Create", "Auth");
            }

            var userId = claimsIdentifier.Value;
            var confirmedUser = await this._userRepository.ConfirmUserPin(userId, confirmPin.Pin);

            if (confirmedUser != null)
            {
                var roles = await _userManager.GetRolesAsync(confirmedUser);
                var role = roles.FirstOrDefault();

                confirmedUser.Pin = 0;
                await this._context.SaveChangesAsync();

                // Set the last activity time for the idle timeout feature
                HttpContext.Session.SetString("LastUserActivity",DateTime.Now.ToString());


                if (string.IsNullOrEmpty(role))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Officer" });
                }



                string roleLower = role.ToLower();

                 if (confirmedUser?.Department?.Name.Contains("Executive suite", StringComparison.OrdinalIgnoreCase) == true)
                {
                
                    return RedirectToAction("Index", "Home", new { Area = "SeniorManager" });
                }
                
                if (roleLower.Contains("manager"))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Manager" });
                }

                if (roleLower.Contains("supervisor"))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Supervisor" });
                }

                switch (roleLower)
                {
                    case "administrator":
                        return RedirectToAction("Index", "Home", new { Area = "Admin" });
                    case "member":
                        return RedirectToAction("Index", "Home", new { Area = "Member" });
                    case "teller":
                        return RedirectToAction("Index", "Home", new { Area = "Teller" });
                    case "call center agent":
                        return RedirectToAction("Index", "Home", new { Area = "CallCenterOfficer" });
                    case "ict officer":
                        return RedirectToAction("Index", "Home", new { Area = "ICTOfficer" });
                    default:
                        return RedirectToAction("Index", "Home", new { Area = "Officer" });
                }

            }
            else
            {
                ModelState.AddModelError("", "Wrong pin");
                return View("MFA", confirmPin);
            }
        }        
        public ActionResult MFA()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ConfirmAccount(string email)
        {
            ViewBag.response = $"Check your email for the pin";

            ViewBag.email = email;
            return View();
        }

         public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["response"] = "";
            return Redirect("/");
        }


        [HttpGet]
        public IActionResult Register()
        {
            UserViewModel newUser = new UserViewModel();          
            ViewBag.genderList = newUser.GenderList;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ClientRegisterViewModel clientRegisterViewModel)
        {
            UserViewModel newUser = new UserViewModel();
            ViewBag.genderList = newUser.GenderList;

            if (ModelState.IsValid)
            {
                //check if there is a member with the following national Id

                Member? dbmember = await this._memberRepository.GetUnregisteredMemberByNationalId(clientRegisterViewModel.NationalId);

               
               //check if the member record already has an associted user account

                if(dbmember?.User != null)
                {
                    ModelState.AddModelError("", "There is a user account that is already associated with the ID, kindly login");

                    return View("Register", clientRegisterViewModel);
                }


                if (dbmember == null)
                {
                    // Track failed registration
                    var failedRegistration = new FailedRegistration
                    {
                        NationalId = clientRegisterViewModel.NationalId,
                        Email = clientRegisterViewModel.Email,
                        //PhoneNumber = clientRegisterViewModel.PhoneNumber,
                        AttemptedAt = DateTime.UtcNow
                    };
                    
                    await _failedRegistrationRepository.AddAsync(failedRegistration);
                    await _unitOfWork.SaveToDataStore();

                    ModelState.AddModelError("", "No member was found with the National Id that was provided");
                    return View("Register", clientRegisterViewModel);
                }
                
                else
                {

                    int pin = _memberRepository.RandomNumber();

                  
                   
                    //create a user account based on the member record

                    ApplicationUser? user = await this._memberRepository.CreateUserAccount(dbmember, clientRegisterViewModel.Email,clientRegisterViewModel.Password);

                    user.Pin = pin;

                    if (user == null)
                    {
                        
                        ModelState.AddModelError("", "failed to create the user account from the member");

                        return View("Register", clientRegisterViewModel);
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
                                <p><strong>Your email:</strong> {user.Email}</p>
                                <p>Your account is pending administrator approval. You will receive another email once your account has been approved.</p>
                            </div>
                            <p class='footer'>Thank you for choosing UCS SACCO.</p>
                        </div>
                    </body>
                    </html>";
                   // string pin = "An account has been created on UCS SACCO. Your pin is " + "<b>" + user.Pin + " <br /> ";
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
                    </html>";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if (user.DeletedDate != null)
                    {
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
                                <h2>Account Reactivation</h2>
                                <div class='account-info'>
                                    <p>Good day,</p>
                                    <p>We are pleased to inform you that your account has been reactivated on the UCS SACCO.</p>
                                    <p>You may proceed to login using your previous credentials.</p>
                                    <p><strong>Your PIN:</strong> {user.Pin}</p>
                                </div>
                                <p>
                                    <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>Login to Your Account</a>
                                </p>
                                <p class='footer'>Thank you for choosing UCS SACCO.</p>
                            </div>
                        </body>
                        </html>";

                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Account Reactivation", emailBody, user.SecondaryEmail);

                    }
                    else
                    {
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Login Details", UserNameBody,user.SecondaryEmail);
                        EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Login Details", PasswordBody,user.SecondaryEmail);
                        // string gravatorEmailBody = $@"
                        // <html>
                        // <head>
                        //     <style>
                        //         @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                        //         body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                        //         .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                        //         .logo {{ text-align: center; margin-bottom: 20px; }}
                        //         .logo img {{ max-width: 150px; }}
                        //         h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                        //         .account-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                        //         .account-info p {{ margin: 5px 0; }}
                        //         .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                        //         .cta-button:hover {{ background-color: #003d82; }}
                        //         .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        //     </style>
                        // </head>
                        // <body>
                        //     <div class='container'>
                        //         <div class='logo'>
                        //             <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                        //         </div>
                        //         <h2>Set Up Your Profile Picture</h2>
                        //         <div class='account-info'>
                        //             <p>Good day,</p>
                        //             <p>To enhance your profile in the UCS SACCO application, we recommend setting up a profile picture using Gravatar.</p>
                        //             <p>Gravatar allows you to associate an avatar with your email address, which will be displayed on your profile in our application.</p>
                        //         </div>
                        //         <p>
                        //             <a href='https://en.gravatar.com/' class='cta-button' style='color: #ffffff;'>Register with Gravatar</a>
                        //         </p>
                        //         <p class='footer'>Thank you for being a part of UCS SACCO.</p>
                        //     </div>
                        // </body>
                        // </html>";
                        // EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Set Up Your Profile Picture", gravatorEmailBody, user.SecondaryEmail);


                    }

                    TempData["response"] = "New Account Created successfully, check your email account for more details";
                    return RedirectToAction("Create");
                }

            }

            return View("Register", clientRegisterViewModel);
        }
        [HttpGet]
        [AllowAnonymous]
        public  IActionResult ForgotPassword()
        {
            return View();

        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([System.ComponentModel.DataAnnotations.Required] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var link = Url.Action(nameof(ResetPassword), "Auth", new { token, email = email }, Request.Scheme);

                //send email

                EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Password Reset", $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                            <h2 style='color: #333;'>Password Reset Request</h2>
                            <p>Hello,</p>
                            <p>We received a request to reset your password. If you didn't make this request, you can ignore this email.</p>
                            <p>To reset your password, please click the button below:</p>
                            <p style='text-align: center;'>
                                <a href='{link}' style='display: inline-block; padding: 10px 20px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 5px;'>Reset Password</a>
                            </p>
                            <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
                            <p>{link}</p>
                            <p>This link will expire in 24 hours for security reasons.</p>
                            <p>If you need any assistance, please contact our support team.</p>
                            <p>Best regards,<br>UCS SACCO Team</p>
                        </div>
                    </body>
                    </html>", user.SecondaryEmail);

                TempData["response"] = $"Password change request has been sent to your email {email}. Please open your email";
                return RedirectToAction("Create");
            }
            else
            {
                TempData["response"] = $"Could not send link to email. Please try again later";
                return RedirectToAction("Create");
            }
        }
        [HttpGet("reset-password")]
        public ActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPassword { Token= token, Email= email};

            return View(model);

        }
    

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPassword resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);

            if(user != null)
            {

                //reset the password

                var resetPasswordResult = await this._userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);

                if(resetPasswordResult.Succeeded)
                {
                    user.LastPasswordChangedDate = DateTime.Now.AddDays(90);
                    await this._context.SaveChangesAsync();

                    TempData["response"] = $"Your account password has been reset successfully, You can try loging in using your new credentials";
                    return RedirectToAction("Create");
                }
                else
                {

                    TempData["response"] = $"Failed to reset the password";
                    return RedirectToAction("Create");
                }
            }
            else
            {

                TempData["response"] = $"Failed to reset the password";
                return RedirectToAction("Create");
            }

           

        }


        public async Task<IActionResult> GenerateUserData()
        {
            List<Department>? departments = await this._departmentRepository.GetDepartments();
            List<Branch>? branches  = await this._branchRepository.GetBranches();
            List<ApplicationUser> users = new List<ApplicationUser>();
            List<ApplicationUser> addedUsers = new();
            string[] gender = { "Male", "Female" };



            var testUsers = new Faker<ApplicationUser>()
                    .RuleFor(u => u.Gender, f => f.PickRandom<string>(gender))
                    .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                    .RuleFor(u => u.LastName, (f, u) => f.Name.LastName())
                    .RuleFor(u => u.PhoneNumber, (f, u) => f.Phone.PhoneNumber())
                    .RuleFor(u => u.Email, (f, u) => f.Internet.Email())
                    .RuleFor(u => u.UserName, (f, u) => u.Email)
                    .RuleFor(u => u.Status, (f, u) => "Active")
                    .RuleFor(u => u.DepartmentId, 6)
                    .RuleFor(u => u.BranchId, (f, u) => f.PickRandom<Branch>(branches).Id);

          var generatedUser = testUsers.Generate(50);

          //create a random class 
          Random rnd = new Random();


            Department departmentFullRecord = await this._departmentRepository.GetDepartment(6);

            var departmentRoles = departmentFullRecord.Roles;
            for (int i = 0; i < generatedUser.Count; i++)
            {

                //try adding a new user

                var result = await this._userRepository.CreateUserAsync(generatedUser[i], "P@$$w0rd");

                try
                {
                    if (result.Succeeded)
                    {

                        //pick a random role in the department


                        int randomRoleIndex = rnd.Next(departmentRoles.Count);

                        string roleName = departmentRoles[randomRoleIndex].Name;
                        //associate user to role
                        var dbResult = await this._userRepository.AddUserToRoleAsync(generatedUser[i], roleName);

                        addedUsers.Add(generatedUser[i]);
                    }

                    await this._unitOfWork.SaveToDataStore();
                }
                catch (Exception ex)
                {

                    throw;
                }

              
            }

            return Json(generatedUser);
                   



        }

        public async Task<IActionResult> ApiAuthenticate()
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

                // Send POST request        
                var tokenResponse = await _httpClient.PostAsync(_configuration["APIURL:link"] + $"Token", stringContent);

                var json = await tokenResponse.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(json);

                var status = document.RootElement.GetProperty("status").GetInt32();

                if (status == 404)
                {
                    TempData["errorResponse"] = "Failed to authenticate with the API. Please try again later or contact support.";
                    return RedirectToAction("Create");
                }

                var token = document.RootElement.GetProperty("token").GetString();
                return Json(new { token });
            }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
            {
                // Log the error
                _logger.LogError(ex, "SSL Certificate validation failed when connecting to MHub API");

                // Send email to support
                await NotifySupportAboutSSLIssue(ex);

                // Redirect user with an error message
                TempData["errorResponse"] = "There was a security issue connecting to our services. Our support team has been notified and is working on resolving it. Please try again later.";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "An unexpected error occurred when authenticating with MHub API");

                // Redirect user with a generic error message
                TempData["errorResponse"] = "An unexpected error occurred. Please try again later or contact support if the issue persists.";
                return RedirectToAction("Create");
            }
        }

        private async Task NotifySupportAboutSSLIssue(Exception ex)
        {
            string subject = "SSL Certificate Issue with MHub API";
            string body = $"An SSL certificate validation error occurred when connecting to the MHub API. Details:\n\n{ex}";
            
             EmailHelper.SendEmail(_jobEnqueuer, _configuration["SupportEmail"], subject, body);
        }

    }
}
