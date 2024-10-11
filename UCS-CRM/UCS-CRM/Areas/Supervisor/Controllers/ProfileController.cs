using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;
using AutoMapper;
using UCS_CRM.Core.Services;

namespace UCS_CRM.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserRepository userRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when accessing Profile Index");
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when updating profile");
                return NotFound();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _unitOfWork.SaveToDataStoreSync();
                _logger.LogInformation($"User {user.UserName} updated their profile");
                TempData["StatusMessage"] = "Your profile has been updated";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (!ModelState.IsValid)
            {
                return View("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when changing password");
                return NotFound();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "The new password and confirmation password do not match.");
                return View("Index", user);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Index", user);
            }

            await _signInManager.RefreshSignInAsync(user);
            await _unitOfWork.SaveToDataStoreSync();

            // Send email notification
            await _emailService.SendMailWithKeyVarReturn(user.Email, "Password Changed", 
            $@"
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
                        <h2>Password Changed</h2>
                        <div class='info'>
                            <p>Your password has been successfully changed on UCS SACCO.</p>
                        </div>
                        <p>
                            <a href='https://crm.ucssacco.com/' class='cta-button' style='color: #ffffff;'>Access UCS CRM</a>
                        </p>
                        <p class='footer'>If you did not make this change, please contact support immediately.</p>
                    </div>
                </body>
                </html>");

            _logger.LogInformation($"User {user.UserName} changed their password");
            TempData["StatusMessage"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }
    }
}