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

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ApplicationDbContext context,
            IMemberRepository memberRepository, IUnitOfWork unitOfWork, IEmailService emailService, HttpClient httpClient, IConfiguration config, IDepartmentRepository departmentRepository, IBranchRepository branchRepository, IConfiguration configuration, HangfireJobEnqueuer hangfireJobEnqueuer)
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
        }

        public async Task<IActionResult> Create()
        {
            if (_signInManager.IsSignedIn(User))
            {
                TempData["response"] = "";

                var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);

                if (findUserDb != null)
                {
                    var roles = _userManager.GetRolesAsync(findUserDb).Result.FirstOrDefault();


                    if (roles.Equals("Member", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Member" });
                    }
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
                        return RedirectToAction("Index", "Home", new { Area = "officer" });
                    }
                    if (roles.Contains("Teller", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Teller" });
                    }
                    if (roles.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Manager" });
                    }
                    if (roles.Contains("Call center officer", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "CallCenterOfficer" });
                    }


                    else
                    {

                        return RedirectToAction("Index", "Home", new { Area = "officer" });
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
                    return RedirectToAction("Index", "Home", new { Area = "officer" });
                }
                 if (roles.Contains("Teller", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Teller" });
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
                if (roles.Contains("Call center officer", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { Area = "CallCenterOfficer" });
                }
                else
                {

                    return RedirectToAction("Index", "Home", new { Area = "officer" });
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

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAccount(ConfirmPin confirmPin)
        {
            //find the account with this pin

            ApplicationUser userDb = await this._userRepository.FindUserByPin(confirmPin.Pin,confirmPin.Email);

            if(userDb !=  null)
            {
                this._userRepository.ConfirmUserAccount(userDb);

                //save changes to the database

                await this._unitOfWork.SaveToDataStore();

                TempData["response"] = "Your account has been activated successfully";

                return RedirectToActionPermanent("Create");
            }

            TempData["errorResponse"] = "no account was found to activate";

            return View();
            
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoginViewModel loginModel)
        {
            if (!ModelState.IsValid)
                return View("Create", loginModel);

            // Retrieve the user with the provided email and roles
           

            var initialSignInResult = await _signInManager.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);

           
            if(initialSignInResult.Succeeded)
            {
                //logout the user

                await _signInManager.SignOutAsync();

                 TempData["errorResponse"] = "";

                var findUserDb = await _userRepository.GetUserWithRole(loginModel.Email, false);



                // Check if the user needs to reset their password
                if (findUserDb.LastPasswordChangedDate < DateTime.Now)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(findUserDb);
                    return RedirectToActionPermanent("ResetPassword", new { token, email = findUserDb.Email });
                }

                // Attempt to sign in the user
                var result = await _signInManager.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Update the user's login details
                    int pin = _memberRepository.RandomNumber();
                    var user = await _userManager.FindByNameAsync(loginModel.Email);
                    var roles = await _userManager.GetRolesAsync(user);

                    if (user != null)
                    {
                        user.LastLogin = DateTime.Now;
                        user.Pin = pin;
                        user.EmailConfirmed = false;
                    }

                    await _context.SaveChangesAsync();

                    string userNameBody = $"Your confirmation code is <b>{pin}</b> <br /> Enter this to login in";
                     this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", userNameBody);
                   


                    TempData["response"] = $"Check your email for the confirmation code";

                  


                    return RedirectToAction("ConfirmAccount", "Auth", new { email = loginModel.Email });
                }
            }

            var userFromDb = await _userRepository.GetUserWithRole(loginModel.Email, false);

            if (userFromDb == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials");
                return View("Create", loginModel);
            }

            // Check if the user's email is confirmed
            if (!userFromDb.EmailConfirmed)
            {
                // Send a one-time pin (OTP) to the user's email
                string userNameBody = $"Here is the One time Pin (OTP) for your account on UCS: <strong>{userFromDb.Pin}</strong> <br />";

                //throw this process to the background 
               
                this._jobEnqueuer.EnqueueEmailJob(loginModel.Email, "Login Details", userNameBody);
               


                TempData["response"] = $"Check your email for the confirmation code";

               
               
                return RedirectToActionPermanent("ConfirmAccount", new { email = userFromDb.Email });
            }
            // Invalid login credentials
            ModelState.AddModelError(string.Empty, "Invalid login credentials");
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
            TempData["response"] = "";
           
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
            UserViewModel newUser = new UserViewModel();
            ViewBag.genderList = newUser.GenderList;

            if (ModelState.IsValid)
            {
                //check if there is a member with the following national Id

                Member? dbmember = await this._memberRepository.GetMemberByNationalId(clientRegisterViewModel.NationalId);

               
               //check if the member record already has an associted user account

                if(dbmember.User != null)
                {
                    ModelState.AddModelError("", "There is a user account that is already associated with the ID, kindly login");

                    return View("Register", clientRegisterViewModel);
                }


                if (dbmember == null)
                {
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

                    string UserNameBody = "An account has been created on UCS SACCO. Your email is " + "<b>" + user.Email + " <br /> ";
                   // string pin = "An account has been created on UCS SACCO. Your pin is " + "<b>" + user.Pin + " <br /> ";
                    string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if (user.DeletedDate != null)
                    {
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Account Status", $"Good day, We are pleased to inform you that your account has been reactivated on the UCS SACCO. You may proceed to login using your previous credentials.");
                        

                    }
                    else
                    {
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", UserNameBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", PasswordBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Login Details", PasswordBody);
                        this._jobEnqueuer.EnqueueEmailJob(user.Email, "Account Details", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");
                       
                       


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

                this._jobEnqueuer.EnqueueEmailJob(user.Email, "Password reset details", $"Good day, please use the following link to reset your password\n <br/> {link}");
                

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
