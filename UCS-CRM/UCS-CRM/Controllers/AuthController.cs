using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Controllers
{

    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserRepository _userRepository;
        private ApplicationDbContext _context;
        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _context = context;
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
                            return RedirectToAction("Index", "Users", new { Area = "Admin" });
                        }
                        if (roles.Contains("Clerk"))
                        {
                            return RedirectToAction("Index", "ClerkTickets", new { Area = "Clerk" });
                        }
                        if (roles.Contains("Manager"))
                        {
                            return RedirectToAction("Index", "Users", new { Area = "Admin" });
                        }
                        if (roles.Contains("Senior Manager"))
                        {
                            return RedirectToAction("Index", "Dashboard", new { Area = "Admin" });
                        }
                        if (roles.Contains("Client"))
                        {
                            return RedirectToAction("Index", "Tickets", new { Area = "Client" });
                        }
                        else
                        {

                            return RedirectToAction("Index", "Tickets", new { Area = "Client" });
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
    }
}
