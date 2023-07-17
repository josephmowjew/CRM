using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Policy;
using System.Text.Json;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Data;
using UCS_CRM.Models;
using UCS_CRM.Persistence.Interfaces;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
 


        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ApplicationDbContext context,
            IMemberRepository memberRepository, IUnitOfWork unitOfWork, IEmailService emailService, HttpClient httpClient, IConfiguration config, IDepartmentRepository departmentRepository, IBranchRepository branchRepository)
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
        }

        public async Task<IActionResult> Create()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);

                if (findUserDb != null)
                {
                    var roles = _userManager.GetRolesAsync(findUserDb).Result.FirstOrDefault();

                    if (findUserDb.Department.Name.ToLower().Contains("Executive suite", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "SeniorManager" });
                    }

                    if (roles.Contains("Administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Admin" });
                    }
                    if (roles.Contains("Clerk", StringComparison.OrdinalIgnoreCase) || roles.Contains("Member Engagements officer", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Clerk" });
                    }
                    if (roles.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Manager" });
                    }
                   
                    if (roles.Contains("Member", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Member" });
                    }
                   
                    else
                    {

                        return RedirectToAction("Index", "Home", new { Area = "Clerk" });
                    }
                }
               
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCode(ConfirmPin confirmPin) 
        {
            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var userId = claimsIdentitifier.Value;

            //var findUserDb = await this._userRepository.GetUserWithRole(userId);

            var confirmedUser= await this._userRepository.ConfirmUserPin(userId, confirmPin.Pin);

            if (confirmedUser != null)
            {
                var roles = _userManager.GetRolesAsync(confirmedUser).Result.FirstOrDefault();

                confirmedUser.Pin = 0;


                await this._context.SaveChangesAsync();

                if (roles.Contains("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Admin" });
                }
                if (roles.Contains("Clerk", StringComparison.OrdinalIgnoreCase) || roles.Contains("Member Engagements officer", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Clerk" });
                }
                if (roles.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Manager" });
                }
                if (roles.Contains("Senior Manager", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "SeniorManager" });
                }
                if (roles.Contains("Member", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Member" });
                }
                else
                {

                    return RedirectToAction("Index", "Home", new { Area = "Member" });
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

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoginViewModel loginModel)
        {


            if (ModelState.IsValid)
            {
                //try to login the user with the credetials provided

                //check if the email belongs to an administrator before proceeding

                var findUserDb = await this._userRepository.GetUserWithRole(loginModel.Email);

                if (findUserDb != null)
                {
                    if (findUserDb.LastPasswordChangedDate < DateTime.Now)
                    {
                        var model = new ResetPassword { Token = "", Email = findUserDb.Email };

                        var token = await _userManager.GeneratePasswordResetTokenAsync(findUserDb);

                        return RedirectToActionPermanent("ResetPassword", new { token = token, email = findUserDb.Email });
                    }
                    else
                    {

                        var result = await _signInManager.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);

                        //get the result of the login attemp

                        if (result.Succeeded)
                        {
                            int pin = _memberRepository.RandomNumber();

                            var user = await this._userManager.FindByNameAsync(loginModel.Email);
                            var roles =  _userManager.GetRolesAsync(user).Result.ToList();

                            
                            if (user != null)
                            {
                                user.LastLogin = DateTime.Now;
                                user.Pin = pin;
                            }

                            await this._context.SaveChangesAsync();

                            string UserNameBody = "Your confirmation code is " + "<b>" + pin + " <br /> Enter this to login in";


                            //_emailService.SendMail(user.Email, "Login Details", UserNameBody);

                            TempData["response"] = $"Check your email for the code";
                            return RedirectToAction("Create", "Auth");


                        }
                        else
                        {
                            //flag an error message back the user

                            ModelState.AddModelError(String.Empty, "Invalid login credentials");

                            return View("Create", loginModel);
                        }
                    }
                   
                }
                else
                {
                    ModelState.AddModelError(String.Empty, "Invalid login credentials");

                    return View("Create", loginModel);
                }




            }
            return View("Create", loginModel);
        }

        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            //string hostPath = Configuration.GetSection("HostingSettings")["Host"];
            await _signInManager.SignOutAsync();

            //if (returnUrl != null)
            //{
            //    returnUrl = hostPath + returnUrl;
            //    //redirect to a specifc url if one was provided
            //    return LocalRedirect(returnUrl);
            //}

            //
            //return RedirectToAction("LogOut", "Home");
            return Redirect("/");//
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
            if (ModelState.IsValid)
            {
                //check if there is a member with the following national Id

               // Member? member = await this._memberRepository.GetMemberByNationalId(clientRegisterViewModel.NationalId);
              
                var protocol = _config.GetSection("APIURL")["link"];

                var response = await _httpClient.GetAsync(protocol + $"BioDataByNIN/{clientRegisterViewModel.NationalId}");

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "No member was found with the National Id that was provided");
                }

                var json = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(json);

                var status = document.RootElement.GetProperty("status").GetInt32();

                if (status == 404)
                {
                    ModelState.AddModelError("", "No member was found with the National Id that was provided");
                }

                //if (member == null)
                //{
                //    ModelState.AddModelError("", "No member was found with the National Id that was provided");
                //}
                else
                {
                    //check if there is an account with the email being provided

                    var baseAccountElement = document.RootElement.GetProperty("data");


                    var member = new Member()
                    {
                        Id = (baseAccountElement.GetProperty("id").GetInt32()),
                        // AccountNumber = baseAccountElement.GetProperty("accountNumber").GetString(),
                        Branch = baseAccountElement.GetProperty("branch").GetString(),
                        PhoneNumber = baseAccountElement.GetProperty("phoneNumber").GetString(),
                        FirstName = baseAccountElement.GetProperty("firstName").GetString(),
                        LastName = baseAccountElement.GetProperty("lastName").GetString(),
                        Address = baseAccountElement.GetProperty("employer").GetString(),
                        DateOfBirth = baseAccountElement.GetProperty("dateOfBirth").GetDateTime(),
                        NationalId = baseAccountElement.GetProperty("nationalID").GetString(),
                        EmailConfirmed = false,
                        Gender = baseAccountElement.GetProperty("nationalID").GetString()
                    };

                    var accountPresent = await this._userRepository.FindByEmailsync(clientRegisterViewModel.Email);

                    if(accountPresent != null)
                    {
                        ModelState.AddModelError(nameof(clientRegisterViewModel.Email), "An account already exist with this email");

                        return View("Register", clientRegisterViewModel);
                    }
                    //create a user account based on the member record

                    ApplicationUser? user = await this._memberRepository.CreateUserAccount(member, clientRegisterViewModel.Email,clientRegisterViewModel.Password);

                    if (user == null)
                    {
                        
                        ModelState.AddModelError("", "failed to create the user account from the member");

                        return View("Register", clientRegisterViewModel);
                    }

                    //sync changes 

                    await this._unitOfWork.SaveToDataStore();


                    //send emails

                    string UserNameBody = "An account has been created on UCS SACCO. Your email is " + "<b>" + user.Email + " <br /> ";
                   // string pin = "An account has been created on UCS SACCO. Your pin is " + "<b>" + user.Pin + " <br /> ";
                    string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if (user.DeletedDate != null)
                    {
                        _emailService.SendMail(user.Email, "Account Status", $"Good day, We are pleased to inform you that your account has been reactivated on the UCS SACCO. You may proceed to login using your previous credentials. ");

                    }
                    else
                    {
                        _emailService.SendMail(user.Email, "Login Details", UserNameBody);
                        //_emailService.SendMail(user.Email, "Login Details", pin);
                        _emailService.SendMail(user.Email, "Login Details", PasswordBody);
                        _emailService.SendMail(user.Email, "Account Details", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");


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

                _emailService.SendMail(user.Email, "Password reset details", $"Good day, please use the following link to reset your password\n <br/> {link}");

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


    }
}
