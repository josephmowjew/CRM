using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.ictofficer.Controllers
{
    [Area("ictofficer")]
    [Authorize]
    public class FailedRegistrationsController : Controller
    {
        private readonly IFailedRegistrationRepository _failedRegistrationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FailedRegistrationsController(
            IFailedRegistrationRepository failedRegistrationRepository,
            IUnitOfWork unitOfWork)
        {
            _failedRegistrationRepository = failedRegistrationRepository;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetFailedRegistrations()
        {
            var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
            var start = HttpContext.Request.Form["start"].FirstOrDefault();
            var length = HttpContext.Request.Form["length"].FirstOrDefault();
            
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int resultTotal = 0;

            resultTotal = await _failedRegistrationRepository.GetUnresolvedCountAsync();
            var result = await _failedRegistrationRepository.GetUnresolvedAsync();

            return Json(new { 
                draw = draw, 
                recordsFiltered = resultTotal, 
                recordsTotal = resultTotal, 
                data = result 
            });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsResolved(int id, string notes)
        {
            try
            {
                var userClaims = (ClaimsIdentity)User.Identity;
                var resolvedBy = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(resolvedBy))
                {
                    return Json(new { error = "error", message = "User not found" });
                }

                await _failedRegistrationRepository.MarkAsResolvedAsync(id, resolvedBy, notes);
                await _unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "Registration marked as resolved" });
            }
            catch (Exception)
            {
                return Json(new { error = "error", message = "Failed to mark registration as resolved" });
            }
        }
    }
}