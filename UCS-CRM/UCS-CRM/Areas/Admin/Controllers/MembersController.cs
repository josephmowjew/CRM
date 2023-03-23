using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Areas.Admin.ViewModels;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        public MembersController(IMemberRepository memberRepository, IMapper mapper, IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _memberRepository = memberRepository;
            _emailService = emailService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: MemberController
        public ActionResult Index()
        {
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

                if (databaseMemberRecord != null)
                {
                    ApplicationUser? user =  await this._memberRepository.CreateUserAccount(databaseMemberRecord, model.Email);

                    if (user == null)
                    {
                        return Json(new { error = "error", message = "failed to create the user account from the member" });
                    }

                    //sync changes 

                    await this._unitOfWork.SaveToDataStore();


                    //send emails

                    string UserNameBody = "An account has been created on UCS SACCO. Your email is " + "<b>" + user.Email + " <br /> ";
                    string PasswordBody = "An account has been created on UCS SACCO App. Your password is " + "<b> P@$$w0rd <br />";


                    //check if this is a new user or not (old users will have a deleted date field set to an actual date)
                    if(user.DeletedDate != null)
                    {
                        _emailService.SendMail(user.Email, "Account Status", $"Good day, We are pleased to inform you that your account has been reactivated on the UCS SACCO. You may proceed to login using your previous credentials. ");

                    }
                    else
                    {
                        _emailService.SendMail(user.Email, "Login Details", UserNameBody);
                        _emailService.SendMail(user.Email, "Login Details", PasswordBody);
                        _emailService.SendMail(user.Email, "Account Details", $"Good day, for those who have not yet registered with Gravator, please do so so that you may upload an avatar of yourself that can be associated with your email address and displayed on your profile in the Mental Lab application.\r\nPlease visit https://en.gravatar.com/ to register with Gravatar. ");


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
