using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Controllers
{

    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMemberRepository _memberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private ApplicationDbContext _context;
        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ApplicationDbContext context, IMemberRepository memberRepository, IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _context = context;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<IActionResult> Create()
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


                    var result = await _signInManager.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);



                    //get the result of the login attemp

                    if (result.Succeeded)
                    {
                        var user = await this._userManager.FindByNameAsync(loginModel.Email);
                        var roles = _userManager.GetRolesAsync(user).Result.ToList();
                        if (user != null)
                        {
                            user.LastLogin = DateTime.Now;
                        }

                        await this._context.SaveChangesAsync();

                        if (roles.Contains("Administrator"))
                        {
                            return RedirectToAction("Index", "Home", new { Area = "Admin" });
                        }
                        if (roles.Contains("Clerk"))
                        {
                            return RedirectToAction("Index", "Home", new { Area = "Clerk" });
                        }
                        if (roles.Contains("Manager"))
                        {
                            return RedirectToAction("Index", "Home", new { Area = "Manager" });
                        }
                        if (roles.Contains("Senior Manager"))
                        {
                            return RedirectToAction("Index", "Home", new { Area = "SeniorManager" });
                        }
                        if (roles.Contains("Member"))
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
                        //flag an error message back the user

                        ModelState.AddModelError(String.Empty, "Invalid login credentials");

                        return View("Create", loginModel);
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

                Member? member = await this._memberRepository.GetMemberByNationalId(clientRegisterViewModel.NationalId);

                if (member == null)
                {
                    ModelState.AddModelError("", "No member was found with the National Id that was provided");
                }
                else
                {
                    //check if there is an account with the email being provided

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
                    string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if (user.DeletedDate != null)
                    {
                        _emailService.SendMail(user.Email, "Account Status", $"Good day, We are pleased to inform you that your account has been reactivated on the UCS SACCO. You may proceed to login using your previous credentials. ");

                    }
                    else
                    {
                        _emailService.SendMail(user.Email, "Login Details", UserNameBody);
                        _emailService.SendMail(user.Email, "Login Details", PasswordBody);
                        _emailService.SendMail(user.Email, "Account Details", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");


                    }

                    TempData["response"] = "New Account Created successfully, check your email account for more details";
                    return RedirectToAction("Create");
                }

            }

            return View("Register", clientRegisterViewModel);
        }
    }
}
