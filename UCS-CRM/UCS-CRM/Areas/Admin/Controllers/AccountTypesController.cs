using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountTypesController : Controller
    {
        private readonly IAccountTypeRepository _accountTypeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public AccountTypesController(IAccountTypeRepository accountTypeRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._accountTypeRepository = accountTypeRepository;
            this._mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: AccountTypesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AccountTypesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AccountTypesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountTypesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateAccountTypeDTO createAcccountTypeDTO)
        {
            //check for model validity

            createAcccountTypeDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createAcccountTypeDTO.DataInvalid = "";


                //check for article title presence

                var mappedAccountType = this._mapper.Map<AccountType>(createAcccountTypeDTO);

                var accountTypePresence = this._accountTypeRepository.Exists(mappedAccountType.Name);



                if (accountTypePresence != null)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createAcccountTypeDTO.Name), $"Another account type exists with the parameters submitted'");

                    return PartialView("_CreateAccountTypePartial", createAcccountTypeDTO);
                }


                //save to the database

                try
                {
                    //comment out this code
                    mappedAccountType.CreatedById = "1c9d8003-91b9-4eab-96a6-0bc90edd349b";

                    this._accountTypeRepository.Add(mappedAccountType);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateAccountTypePartial", createAcccountTypeDTO);
                }
                catch (DbUpdateException ex)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateAccountTypePartial", createAcccountTypeDTO);
                }

                catch (Exception ex)
                {

                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateAccountTypePartial", createAcccountTypeDTO);
                }




            }

           

            return PartialView("_CreateAccountTypePartial", createAcccountTypeDTO);
        }

        // GET: AccountTypesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                AccountType? accountTypeDbRecord = await this._accountTypeRepository.GetAccountType(id);

                if (accountTypeDbRecord is not null)
                {
                    //map the record 

                    ReadAccoutTypeDTO mappedAccountRecord = this._mapper.Map<ReadAccoutTypeDTO>(accountTypeDbRecord);

                    return Json(mappedAccountRecord);

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

           

            return View();
        }

        // POST: AccountTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditAccountTypeDTO editAccountTypeDTO)
        {
            editAccountTypeDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editAccountTypeDTO.DataInvalid = "";
                //check if the role name isn't already taken

                var accountTypeDB = await this._accountTypeRepository.GetAccountType(id);

                var accountTypePresent =  this._accountTypeRepository.Exists(editAccountTypeDTO.Name);



                bool isTaken = (accountTypePresent != null);

                if (isTaken)
                {

                    editAccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(editAccountTypeDTO.Name), $"The Account Type  {editAccountTypeDTO.Name} is already taken");


                    return PartialView("_EditAccountTypePartial", editAccountTypeDTO);
                }



                this._mapper.Map(editAccountTypeDTO, accountTypeDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(accountTypeDB);

            }



            return PartialView("_EditAccountTypePartial", editAccountTypeDTO);

        }

        // GET: AccountTypesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var accountTypeDb = await this._accountTypeRepository.GetAccountType(id);

            if (accountTypeDb != null)
            {
                this._accountTypeRepository.Remove(accountTypeDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "account type removed from the system successfully" });
            }

            return Json(new { status = "error", message = "account type could not be found from the system" });
        }

       

        [HttpPost]
        public async Task<ActionResult> GetAccountTypes()
        {

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

                List<AccountType>? repoAccountTypes = await this._accountTypeRepository.GetAccountTypes(CursorParameters);


                //get total records from the database
                resultTotal = await this._accountTypeRepository.TotalCount();
                var result = repoAccountTypes;
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }




            //fetch all roles from the system



            //return Json(identityRolesList.ToList());

        }
    }
}
