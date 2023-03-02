using Microsoft.AspNetCore.Authorization;
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

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(IUserRepository userRepository, IEmailService emailService, RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            this._userRepository = userRepository;
            this._emailService = emailService;
            this._roleManager = roleManager;
            this._userManager = userManager;
        }

        // GET: UsersController
        public ActionResult Index()
        {
            List<SelectListItem> roles = new List<SelectListItem>();
            UserViewModel newUser = new UserViewModel();
            this._roleManager.Roles.ToList().ForEach(r =>
            {
                roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
            });

            ViewBag.rolesList = roles;
            ViewBag.genderList = newUser.GenderList;

            return View();
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

            userViewModel.DataInvalid = "true";

            List<SelectListItem> roles = new List<SelectListItem>();
            UserViewModel newUser = new UserViewModel();

            if (ModelState.IsValid)
            {
                userViewModel.DataInvalid = "";


                //create a record of the application user

                var applicationUser = new ApplicationUser
                {
                    FirstName = userViewModel.FirstName,
                    LastName = userViewModel.LastName,
                    Gender = userViewModel.Gender,
                    Email = userViewModel.Email,
                    PhoneNumber = userViewModel.PhoneNumber,
                    DateOfBirth = userViewModel.DateOfBirth,

                };

                //check if the user is already in the system
                var recordPresence = this._userRepository.Exists(applicationUser);

                if(recordPresence is not null)
                {
                    //repopulate roles

                    this._roleManager.Roles.ToList().ForEach(r =>
                    {
                        roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
                    });

                    userViewModel.DataInvalid = "true";
                    ViewBag.rolesList = roles;
                    ViewBag.genderList = newUser.GenderList;
                    ModelState.AddModelError(nameof(userViewModel.Email), "This email is already in used by another account");

                    return PartialView("_CreateUserPartial",userViewModel);

                }
                else
                {
                    //preactivate the account

                    applicationUser.EmailConfirmed= true;
                    //save the record to the database
                    var result = await this._userRepository.CreateUserAsync(applicationUser, "P@$$w0rd");

                    if(result.Succeeded)
                    {
                        //send account creation and confirmation emails

                        _emailService.SendMail(applicationUser.Email, "UCS SACCO ACCOUNT INFO", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");
                        string UserNameBody = "An account has been created on UCS SACCO. Your email is " + "<b>" + applicationUser.Email + " <br /> ";
                        string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";

                       

                        _emailService.SendMail(applicationUser.Email, "Login Details", UserNameBody);
                        _emailService.SendMail(applicationUser.Email, "Login Details", PasswordBody);
                        return Json(new { response = "User account created succefully" });
                    }
                    else
                    {
                        //repopulate roles

                        this._roleManager.Roles.ToList().ForEach(r =>
                        {
                            roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
                        });

                        userViewModel.DataInvalid = "true";

                        ViewBag.rolesList = roles;
                        ViewBag.genderList = newUser.GenderList;
                        //something is not right, could not save record to the database
                        return PartialView("_CreateUserPartial", userViewModel);
                    }



                }
            }
            else
            {
                //repopulate roles

                this._roleManager.Roles.ToList().ForEach(r =>
                {
                    roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
                });

                userViewModel.DataInvalid = "true";
                ViewBag.rolesList = roles;
                ViewBag.genderList = newUser.GenderList;
                //something is not right with the 
                return PartialView("_CreateUserPartial", userViewModel);
            }
            



        }

        // GET: UsersController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UsersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
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

                _emailService.SendMail(user.Email, "Account Changes", "Sorry but your account has been suspended from UCS SACCO. You can no longer access the appliaction. Contact support for more information and queries");

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

                resultTotal = await this._userRepository.TotalCount();
                var result = users = await this._userRepository.GetUsersWithRoles(CursorParameters);
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

              

            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }

        }
    }
}
