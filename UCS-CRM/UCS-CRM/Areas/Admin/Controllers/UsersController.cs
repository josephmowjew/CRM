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
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDepartmentRepository _departmentRepository;
        private RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(IUserRepository userRepository, IEmailService emailService, RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IDepartmentRepository departmentRepository)
        {
            this._userRepository = userRepository;
            this._emailService = emailService;
            this._roleManager = roleManager;
            this._userManager = userManager;
            _unitOfWork = unitOfWork;
            _departmentRepository = departmentRepository;
        }

        // GET: UsersController
        public async Task<ActionResult> Index()
        {
            List<SelectListItem> roles = new List<SelectListItem>();
            UserViewModel newUser = new UserViewModel();

            this._roleManager.Roles.ToList().ForEach(r =>
            {
                roles.Add(new SelectListItem { Text = r.Name, Value = r.Name });
            });

            ViewBag.rolesList = roles;
            ViewBag.genderList = newUser.GenderList;
            ViewBag.departmentsList = await GetDepartments();

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
                    UserName= userViewModel.Email,
                    EmailConfirmed = true,
                    DepartmentId = userViewModel.DepartmentId,
                    LastPasswordChangedDate = DateTime.Now,

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
                    //pre-activate the account

                    applicationUser.EmailConfirmed= false;
                    //save the record to the database
                    var result = await this._userRepository.CreateUserAsync(applicationUser, "P@$$w0rd");

                   



                    if(result.Succeeded)
                    {
                        //associate user with a role

                        var roleResult = await this._userRepository.AddUserToRoleAsync(applicationUser, userViewModel.RoleName);

                        if(roleResult.Succeeded)
                        {
                            //send account creation and confirmation emails

                            //_emailService.SendMail(applicationUser.Email, "UCS SACCO ACCOUNT INFO", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");
                            string UserNameBody = "An account has been created on UCS SACCO. Your email is " + "<b>" + applicationUser.Email + " <br /> ";
                            //string pin = "An account has been created on UCS SACCO. Your pin is " + "<b>" + applicationUser.Pin + " <br /> ";
                            string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";

                            _emailService.SendMail(applicationUser.Email, "Login Details", UserNameBody);
                            //_emailService.SendMail(applicationUser.Email, "Login Details", pin);
                            _emailService.SendMail(applicationUser.Email, "Login Details", PasswordBody);
                            return Json(new { response = "User account created successfully" });
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
                DepartmentId = user.DepartmentId,
                Department = user.Department,
                Id = user.Id,

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

                    IdentityResult result = await this._userRepository.UpdateAsync(dbUser);


                    //update user role

                    var currentUserRoles = await this._userRepository.GetRolesAsync(dbUser.Id);

                    if (!currentUserRoles.Contains(applicationViewModel.RoleName))
                    {
                        //swap the roles
                        await _userRepository.RemoveFromRolesAsync(dbUser, currentUserRoles);

                        await this._userRepository.AddUserToRoleAsync(dbUser, applicationViewModel.RoleName);
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

                resultTotal = await this._userRepository.TotalCount();
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

                users = await this._userRepository.GetUnconfirmedUsersWithRoles(CursorParameters);

                try
                {


                    resultTotal = await this._userRepository.TotalUncomfirmedCount();
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

                _emailService.SendMail(user.Email, "Account Confirmation", "Congratulations!! your account has been confirmed  on UCS SACCO.");


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

                    resultTotal = await this._userRepository.TotalDeletedCount();
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

                _emailService.SendMail(user.Email, "Account Changes", "Congratulations!! your account has been reactivated on UCS SACCO.");


                return Json(new { status = "success", message = "user activated from the system successfully" });
            }

            return Json(new { status = "error", message = "user not found" });
        }
    
        private async Task<List<SelectListItem>> GetDepartments()
        {
            List<SelectListItem> departments = new() { new SelectListItem() { Text = "Select Department", Value=""} };

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
    }
}
