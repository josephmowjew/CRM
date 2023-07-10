using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.Feedback;
using UCS_CRM.Core.DTOs.TicketCategory;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize]
    public class FeedbacksController : Controller
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FeedbacksController(IFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _feedbackRepository = feedbackRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        // GET: FeedbacksController
        public ActionResult Index()
        {
            return View();
        }

        // GET: FeedbacksController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: FeedbacksController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: FeedbacksController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateFeedbackDTO createFeedbackDTO)
        {
            //check for model validity


            if (ModelState.IsValid)
            {

                createFeedbackDTO.DataInvalid = "";


                //check for article title presence

                var mappedFeedback = _mapper.Map<Feedback>(createFeedbackDTO);

                var feedbackPresence = _feedbackRepository.Exists(mappedFeedback.Description);
                if (feedbackPresence != null)
                {
                    createFeedbackDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createFeedbackDTO.Description), $"feedback exists with the name submitted'");

                    return PartialView("_CreateFeedbackPartial", createFeedbackDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedFeedback.CreatedById = claimsIdentitifier.Value;


                    _feedbackRepository.Add(mappedFeedback);
                    await _unitOfWork.SaveToDataStore();


                    return PartialView("_CreateFeedbackPartial", createFeedbackDTO);
                }
                catch (DbUpdateException ex)
                {
                    createFeedbackDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateFeedbackPartial", createFeedbackDTO);
                }

                catch (Exception ex)
                {
                    createFeedbackDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateFeedbackPartial", createFeedbackDTO);
                }




            }



            return PartialView("_CreateFeedbackPartial", createFeedbackDTO);
        }

        // GET: FeedbacksController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                Feedback? feedbackDbRecord = await _feedbackRepository.GetFeedbackAsync(id);

                if (feedbackDbRecord is not null)
                {
                    //map the record 

                    ReadFeedbackDTO mappedFeedback = _mapper.Map<ReadFeedbackDTO>(feedbackDbRecord);

                    return Json(mappedFeedback);

                }
                else
                {
                    return Json(new { status = "error", message = "record not found" });
                }
            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.Message });
            }
        }

        // POST: FeedbacksController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditFeedbackDTO editFeedbackDTO)
        {
            editFeedbackDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editFeedbackDTO.DataInvalid = "";

                var ticketCategoryDB = await _feedbackRepository.GetFeedbackAsync(id);

                if (ticketCategoryDB is null)
                {
                    editFeedbackDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditFeedbackPartial", editFeedbackDTO);
                }
                //check if the role name isn't already taken
                
                _mapper.Map(editFeedbackDTO, ticketCategoryDB);

                //save changes to data store

                await _unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "feedback details updated successfully" });
            }



            return PartialView("_EditFeedbackPartial", editFeedbackDTO);
        }


        // POST: FeedbacksController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var feedbackDbRecord = await _feedbackRepository.GetFeedbackAsync(id);

            if (feedbackDbRecord != null)
            {
                _feedbackRepository.Remove(feedbackDbRecord);

                await _unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "feedback has been removed from the system successfully" });
            }

            return Json(new { status = "error", message = "feedback could not be found from the system" });
        }
        [HttpPost]
        public async Task<ActionResult> GetFeedbacks()
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

            resultTotal = await _feedbackRepository.TotalActiveCount(User);
            var result = await _feedbackRepository.GetFeedbacks(CursorParameters, User);
            return Json(new { draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
        }
    }
}
