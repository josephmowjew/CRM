using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public MembersController(IMemberRepository memberRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _memberRepository = memberRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: MemberController
        public ActionResult Index()
        {
            return View();
        }

        // GET: MemberController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: MemberController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MemberController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
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

        // GET: MemberController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: MemberController/Edit/5
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

        // GET: MemberController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MemberController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteUserAccount(int id)
        {
            //get member id

            try
            {
                Member databaseMemberRecord = await this._memberRepository.GetMemberAsync(id);

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
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = mappedResult });

        }


    }
}
