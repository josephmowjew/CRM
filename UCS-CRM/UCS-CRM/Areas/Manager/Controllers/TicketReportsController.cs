﻿using AutoMapper;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UCS_CRM.Persistence.Interfaces;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.Helpers;
using UCS_CRM.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace UCS_CRM.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize]
    public class TicketReportsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _env;

        public TicketReportsController(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork,
            ITicketCategoryRepository ticketCategoryRepository, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository,
            IWebHostEnvironment env, ITicketCommentRepository ticketCommentRepository, IUserRepository userRepository, IMemberRepository memberRepository, ITicketEscalationRepository ticketEscalationRepository, IBranchRepository branchRepository)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketCategoryRepository = ticketCategoryRepository;
            _stateRepository = stateRepository;
            _priorityRepository = priorityRepository;
            _env = env;
            _ticketCommentRepository = ticketCommentRepository;
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _ticketEscalationRepository = ticketEscalationRepository;
            this._branchRepository = branchRepository;
        }

        public async Task<ActionResult> Index()
        {
            await populateViewBags();

            return View();
        }

        //
        public async Task<IActionResult> TicketReport(DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0, int stateId = 0)
        {
            ViewBag.stateId = stateId;
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;
            ViewBag.branch = branch;
            ViewBag.categoryId = categoryId;

            await populateViewBags();
            return View();
        }
        public async Task<IActionResult> Perfomance(DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0, int stateId = 0)
        {

            ViewBag.stateId = stateId;
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;
            ViewBag.branch = branch;
            ViewBag.categoryId = categoryId;
            int departmentId = 0;

            await populateViewBags();

            var user = await this._userRepository.FindByEmailsync(User.Identity.Name);

            if (user != null)
            {
                branch = user.Branch.Name;
                departmentId = user.Department.Id;
            }

            var tickets = await _ticketRepository.GetMemberEngagementOfficerReport(startDate, endDate, branch , stateId, categoryId,departmentId);

            List<ApplicationUser> memberEngagementOfficers = new();
            List<UserTickets> userTickets = new();

            tickets.ForEach(ticket =>
            {
            if (!memberEngagementOfficers.Contains(ticket.AssignedTo))
            {
                memberEngagementOfficers.Add(ticket.AssignedTo);
                }
            });


            memberEngagementOfficers.ForEach(user =>
            {
               int openTickets =  tickets.Where(t => t.State.Name != Lambda.Closed && t.AssignedToId == user.Id).Count();
               int closedTickets = tickets.Where(t => t.State.Name == Lambda.Closed && t.AssignedToId == user.Id).Count();

                userTickets.Add(new UserTickets{ UserName = user.FullName, OpenTickets = openTickets, ClosedTickets = closedTickets });

            });




            ViewBag.userTickets = userTickets;

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Escalated(DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0, int stateId = 0)
        {
            ViewBag.stateId = stateId;
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;
            ViewBag.branch = branch;
            ViewBag.categoryId = categoryId;

            await populateViewBags();
            return View();

        }

        // run report based on the parameters 
        [HttpPost]
        public async Task<ActionResult> GetEscalateReportData(DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0, int stateId = 0)
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

            resultTotal = await this._ticketRepository.GetEscalatedTicketsDataCountAsync(CursorParameters, startDate, endDate, branch, categoryId);
            var result = await this._ticketRepository.GetEscalatedTicketsData(CursorParameters, startDate, endDate, branch, categoryId);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = mappedResult });

        }


        // run report based on the parameters 
        [HttpPost]
        public async Task<ActionResult> GetTickets(DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0, int stateId = 0)
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

            resultTotal = await this._ticketRepository.GetTicketReportsCount(CursorParameters, startDate, endDate, branch, stateId, categoryId);
            var result = await this._ticketRepository.GetTicketReports(CursorParameters, startDate, endDate, branch, stateId, categoryId);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = mappedResult });

        }

        //Categories

        private async Task<List<SelectListItem>> GetTicketCategories()
        {
            var ticketCategories = await this._ticketCategoryRepository.GetTicketCategories();

            var ticketCategoriesList = new List<SelectListItem>();

            ticketCategoriesList.Add(new SelectListItem() { Text = "------ Select Category ------", Value = "" });

            ticketCategories.ForEach(category =>
            {
                ticketCategoriesList.Add(new SelectListItem() { Text = category.Name, Value = category.Id.ToString() });
            });

            return ticketCategoriesList;

        }

        // Status
        private async Task<List<SelectListItem>> GetTicketStates()
        {
            var ticketStates = await this._stateRepository.GetStates();

            //filter closed state

            ticketStates = ticketStates.ToList();

            var ticketStatesList = new List<SelectListItem>();

            ticketStatesList.Add(new SelectListItem() { Text = "------ Select State ------", Value = "" });

            ticketStates.ForEach(state =>
            {
                ticketStatesList.Add(new SelectListItem() { Text = state.Name, Value = state.Id.ToString() });
            });

            return ticketStatesList;

        }

        // Status
        private async Task<List<SelectListItem>> GetBranches()
        {
            var branches = await this._branchRepository.GetBranches();

            //filter closed state

            branches = branches.Where(ts => ts.Name.Trim().ToLower() != Lambda.Closed.Trim().ToLower()).ToList();

            var branchList = new List<SelectListItem>();

            branchList.Add(new SelectListItem() { Text = "------ Select Branch ------", Value = "" });

            branches.ForEach(branch =>
            {
                branchList.Add(new SelectListItem() { Text = branch.Name, Value = branch.Name.ToString() });
            });

            return branchList;

        }



        private async Task populateViewBags()
        {

            ViewBag.categories = await GetTicketCategories();
            ViewBag.states = await GetTicketStates();
            ViewBag.branches = await GetBranches();
        }


        //ignore
        public IActionResult Ignore(string format)
        {
            var reportModel = new Ticket()
            {
                Title = "Sample Report",
                Description = "This is a sample report."
                // Set other properties as needed
            };

            format = "word";
            //var viewResult = System.Web.Mvc.ViewEngines.Engines.FindView(ControllerContext, "ReportView", null);
            //var stringWriter = new StringWriter();
            //var viewContext = new System.Web.Mvc.ViewContext(ControllerContext, viewResult.View, new System.Web.Mvc.ViewDataDictionary(reportModel), new System.Web.Mvc.TempDataDictionary(), stringWriter);
            //viewResult.View.Render(viewContext, stringWriter);
            //viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
            //var htmlContent = stringWriter.ToString();

            byte[] reportBytes;
            string mimeType;
            string fileName;
            switch (format.ToLower())
            {
                case "pdf":
                    reportBytes = ConvertHtmlToPdf(reportModel.ToString());
                    mimeType = "application/pdf";
                    fileName = "report.pdf";
                    break;
                case "excel":
                    reportBytes = GenerateExcelReport();
                    mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = "report.xlsx";
                    break;
                case "word":
                    reportBytes = GenerateWordReport(reportModel.ToString());
                    mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileName = "report.docx";
                    break;
                default:
                    return View("sds");
            }

            return File(reportBytes, mimeType, fileName);
        }

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document();
                var writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
                var htmlWorker = new HTMLWorker(document);
                htmlWorker.Parse(new StringReader(htmlContent));
                document.Close();
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateExcelReport()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Report");
                worksheet.Cells["A1"].Value = "Sample Report";
                worksheet.Cells["A2"].Value = "Title";
                worksheet.Cells["B2"].Value = "Description";
                // Set cell values based on your data

                using (var range = worksheet.Cells["A1:B1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                return package.GetAsByteArray();
            }
        }

        private byte[] GenerateWordReport(string htmlContent)
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document();
                var writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
                var htmlWorker = new HTMLWorker(document);
                htmlWorker.Parse(new StringReader(htmlContent));
                document.Close();
                return memoryStream.ToArray();
            }
        }

        public class ReportModel
        {
            public string Title { get; set; }
            public string Description { get; set; }
            // Add other properties as needed for your report
        }

        public async Task<IActionResult> CustomReport()
        {
            await populateViewBags();
            return View(new CustomTicketReport());
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCustomReport([FromForm] CustomTicketReport model)
        {
            CursorParams cursorParams = new CursorParams { Take = int.MaxValue };
            var tickets = await _ticketRepository.GetTicketReports(cursorParams, model.StartDate, model.EndDate, model.Branch ?? "", 
                model.StateId ?? 0, model.CategoryId ?? 0);

            if (model.PriorityId.HasValue)
                tickets = tickets.Where(t => t.TicketPriorityId == model.PriorityId).ToList();
            
            if (model.DepartmentId.HasValue)
                tickets = tickets.Where(t => t.DepartmentId == model.DepartmentId).ToList();
            
            if (!string.IsNullOrEmpty(model.AssignedToId))
                tickets = tickets.Where(t => t.AssignedToId == model.AssignedToId).ToList();

            var viewModel = new CustomTicketReportViewModel
            {
                Model = tickets,
                Configuration = model
            };

            return PartialView("_CustomReportResults", viewModel);
        }
    }
}
