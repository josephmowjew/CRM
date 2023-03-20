using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.Client.Controllers
{
    [Area("Clerk")]
    [Authorize]
    public class ClerkTicketsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _env;
        public ClerkTicketsController(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork, ITicketCategoryRepository ticketCategoryRepository, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository, IWebHostEnvironment env)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketCategoryRepository = ticketCategoryRepository;
            _stateRepository = stateRepository;
            _priorityRepository = priorityRepository;
            _env = env;
        }

        // GET: TicketsController
        public async Task<ActionResult> Index()
        {
            await populateViewBags();

            return View();
        }


        // POST: TicketsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketDTO createTicketDTO)
        {


            createTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createTicketDTO.DataInvalid = "";

                //search for the default state

                var defaultState = this._stateRepository.DefaultState(Lambda.State);

                if (defaultState == null)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError("", "Sorry but the application failed to log your ticket because of a missing state, please contact administrator for assistance");

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }
                else
                {
                    createTicketDTO.StateId = defaultState.Id;
                }


                //check for article title presence

                var mappedTicket = this._mapper.Map<Ticket>(createTicketDTO);

                var statePresence = this._ticketRepository.Exists(mappedTicket);

                if (statePresence != null)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createTicketDTO.Title), $"title exists with the name submitted'");

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicket.CreatedById = claimsIdentitifier.Value;

                    //get the last ticket

                    Ticket lastTicket = await this._ticketRepository.LastTicket();


                    //generate ticket number
                    var lastTicketId = lastTicket == null ? 0 : lastTicket.Id;

                    string ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);

                    //assign ticket number to the mapped record

                    mappedTicket.TicketNumber = ticketNumber;


                    this._ticketRepository.Add(mappedTicket);




                    //save ticket to the data store

                    await this._unitOfWork.SaveToDataStore();

                    if (createTicketDTO.Attachments.Count > 0)
                    {
                        var attachments = createTicketDTO.Attachments.Select(async attachment =>
                        {
                            string fileUrl = await UploadFile(attachment);
                            return new TicketAttachment()
                            {
                                FileName = attachment.FileName,
                                TicketId = mappedTicket.Id,
                                Url = fileUrl
                            };
                        });

                        var mappedAttachments = await Task.WhenAll(attachments);

                        mappedTicket.TicketAttachments.AddRange(mappedAttachments);

                        await this._unitOfWork.SaveToDataStore();
                    }



                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }
                catch (DbUpdateException ex)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }

                catch (Exception ex)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }




            }



            return PartialView("_CreateTicketPartial", createTicketDTO);
        }

        // GET: TicketsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var identityRole = await _ticketRepository.GetTicket(id);

            return Json(identityRole);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditTicketDTO editTicketDTO)
        {
            editTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editTicketDTO.DataInvalid = "";

                var ticketDB = await this._ticketRepository.GetTicket(id);

                if (ticketDB is null)
                {
                    editTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditTicketPartial", editTicketDTO);
                }
                //check if the role name isn't already taken
                var mappedTicket = this._mapper.Map<Ticket>(editTicketDTO);

                var ticketExist = this._ticketRepository.Exists(mappedTicket);



                bool isTaken = (ticketExist != null);
                if (isTaken)
                {

                    editTicketDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editTicketDTO.Title), $"The title {editTicketDTO.Title} is already taken");


                    return PartialView("_EditTicketPartial", editTicketDTO);
                }



                this._mapper.Map(editTicketDTO, ticketDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(ticketDB);
            }



            return PartialView("_EditTicketPartial", editTicketDTO);
        }


        // POST: TicketsController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var ticketRecordDb = await this._ticketRepository.GetTicket(id);

            if (ticketRecordDb != null)
            {
                //only execute remove if the state is not pending

                if (ticketRecordDb.State.Name.ToLower() != Lambda.Pending.ToLower())
                {
                    return Json(new { status = "error", message = "ticket could not be found from the system at the moment as it has been responded to, consider closing it instead" });
                }
                else
                {
                    this._ticketRepository.Remove(ticketRecordDb);

                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "ticket has been removed from the system successfully" });
                }

            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }

        [HttpPost]
        public async Task<ActionResult> GetTickets()
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

            resultTotal = await this._ticketRepository.TotalCount();
            var result = await this._ticketRepository.GetTickets(CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            var cleanResult = new List<ReadTicketDTO>();

            //mappedResult.ForEach(record =>
            //{
            //    record.State.Tickets = null;
            //    record.TicketAttachments.Select(r => r.Ticket = null);

            //    cleanResult.Add(record);

            //});



            return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });



        }

        private async Task<List<SelectListItem>> GetTicketCategories()
        {
            var ticketCategories = await this._ticketCategoryRepository.GetTicketCategories();

            var ticketCategoriesList = new List<SelectListItem>();

            ticketCategories.ForEach(category =>
            {
                ticketCategoriesList.Add(new SelectListItem() { Text = category.Name, Value = category.Id.ToString() });
            });

            return ticketCategoriesList;

        }

        private async Task<List<SelectListItem>> GetTicketPriorities()
        {
            var ticketPriorities = await this._priorityRepository.GetTicketPriorities();

            var ticketPrioritiesList = new List<SelectListItem>();

            ticketPriorities.ForEach(priority =>
            {
                ticketPrioritiesList.Add(new SelectListItem() { Text = priority.Name, Value = priority.Id.ToString() });
            });

            return ticketPrioritiesList;

        }

        private async Task populateViewBags()
        {
            ViewBag.priorities = await GetTicketPriorities();
            ViewBag.categories = await GetTicketCategories();
        }

        private async Task<string> UploadFile(IFormFile file)
        {
            string fileName = string.Empty;
            string complete_file_name = string.Empty;
            try
            {
                // Get the extension of the file
                var extension = Path.GetExtension(file.FileName);
                // Generate a file name on the spot
                fileName = Path.GetRandomFileName() + extension;
                // Generate a possible path to the file
                var pathBuilt = Path.Combine(this._env.WebRootPath, "TicketAttachments");

                if (!Directory.Exists(pathBuilt))
                {
                    // Create the directory
                    await Task.Run(() => Directory.CreateDirectory(pathBuilt));
                }

                var path = Path.Combine(pathBuilt, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    // Copy the file to the path
                    await file.CopyToAsync(stream);
                }

                complete_file_name = Path.Combine("/", "TicketAttachments", fileName);

                return complete_file_name;
            }
            catch (Exception ex)
            {
                return $"{complete_file_name} {ex}";
            }
        }

    }
}
