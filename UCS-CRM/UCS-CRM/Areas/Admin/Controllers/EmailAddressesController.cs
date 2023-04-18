using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.EmailAddress;
using UCS_CRM.Core.DTOs.TicketCategory;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class EmailAddressesController : Controller
    {
        private readonly IEmailAddressRepository _emailAddressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public EmailAddressesController(IEmailAddressRepository emailAddressRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _emailAddressRepository = emailAddressRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        // GET: EmailAddressesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: EmailAddressesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: EmailAddressesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: EmailAddressesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateEmailAddressDTO createEmailAddressDTO)
        {
            //check for model validity


            if (ModelState.IsValid)
            {

                createEmailAddressDTO.DataInvalid = "";


                //check for article title presence

                var mappedEmailAddress = this._mapper.Map<EmailAddress>(createEmailAddressDTO);

                var emailAddressPresence = this._emailAddressRepository.Exists(mappedEmailAddress.Email);
                if (emailAddressPresence != null)
                {
                    createEmailAddressDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createEmailAddressDTO.Email), $"Email Address exists with the name submitted'");

                    return PartialView("_CreateEmailAddressPartial", createEmailAddressDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedEmailAddress.CreatedById = claimsIdentitifier.Value;


                    this._emailAddressRepository.Add(mappedEmailAddress);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateEmailAddressPartial", createEmailAddressDTO);
                }
                catch (DbUpdateException ex)
                {
                    createEmailAddressDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateEmailAddressPartial", createEmailAddressDTO);
                }

                catch (Exception ex)
                {
                    createEmailAddressDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateEmailAddressPartial", createEmailAddressDTO);
                }




            }



            return PartialView("_CreateEmailAddressPartial", createEmailAddressDTO);
        }

        // GET: EmailAddressesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                EmailAddress? emailAddressDbRecord = await this._emailAddressRepository.GetEmailAddressAsync(id);

                if (emailAddressDbRecord is not null)
                {
                    //map the record 

                    ReadEmailAddressDTO mappedEmailAddress = this._mapper.Map<ReadEmailAddressDTO>(emailAddressDbRecord);

                    return Json(mappedEmailAddress);

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

        // POST: EmailAddressesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UpdateEmailAddressDTO editEmailAddressDTO)
        {
            editEmailAddressDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editEmailAddressDTO.DataInvalid = "";

                var ticketCategoryDB = await this._emailAddressRepository.GetEmailAddressAsync(id);

                if (ticketCategoryDB is null)
                {
                    editEmailAddressDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditEmailAddressPartial", editEmailAddressDTO);
                }
                //check if the role name isn't already taken

                var emailAddressExist = this._emailAddressRepository.Exists(editEmailAddressDTO.Email);



                bool isTaken = (emailAddressExist != null);
                if (isTaken)
                {

                    editEmailAddressDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editEmailAddressDTO.Email), $"The emailAddress {editEmailAddressDTO.Email} is already taken");


                    return PartialView("_EditEmailAddressPartial", editEmailAddressDTO);
                }



                this._mapper.Map(editEmailAddressDTO, ticketCategoryDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "Email Address details updated successfully" });
            }



            return PartialView("_EditEmailAddressPartial", editEmailAddressDTO);
        }

      
        // POST: EmailAddressesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var emailAddressDbRecord = await this._emailAddressRepository.GetEmailAddressAsync(id);

            if (emailAddressDbRecord != null)
            {
                this._emailAddressRepository.Remove(emailAddressDbRecord);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "emailAddress has been removed from the system successfully" });
            }

            return Json(new { status = "error", message = "emailAddress could not be found from the system" });
        }
        [HttpPost]
        public async Task<ActionResult> GetEmailAddresses()
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

            resultTotal = await this._emailAddressRepository.TotalActiveCount();
            var result = await this._emailAddressRepository.GetEmailAddresses(CursorParameters);
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
        }
    }
}
