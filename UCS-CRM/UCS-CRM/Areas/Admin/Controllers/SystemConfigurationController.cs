using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Models;
using UCS_CRM.ViewModel;

namespace UCS_CRM.Areas.Admin.Controllers
{
   
    [Area("Admin")]
    [Authorize]
    public class SystemConfigurationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemConfigurationController> _logger;

        public SystemConfigurationController(ApplicationDbContext context, ILogger<SystemConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dateConfig = await _context.SystemDateConfigurations.FirstOrDefaultAsync();
            var holidays = await _context.Holidays
                .Where(h => h.DeletedDate == null)
                .OrderBy(h => h.StartDate)
                .ToListAsync();

            var viewModel = new SystemConfigurationViewModel
            {
                DateConfiguration = dateConfig,
                Holidays = holidays
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDateConfiguration(SystemDateConfiguration config)
        {
            try
            {
                _logger.LogInformation($"Received config - TimeZone: {config.TimeZone}, DateFormat: {config.DateFormat}, FirstDayOfWeek: {config.FirstDayOfWeek}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var modelStateEntry in ModelState.Values)
                    {
                        foreach (var error in modelStateEntry.Errors)
                        {
                            _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                        }
                    }

                    var holidays = await _context.Holidays
                        .Where(h => h.DeletedDate == null)
                        .OrderBy(h => h.StartDate)
                        .ToListAsync();

                    return View("Index", new SystemConfigurationViewModel 
                    { 
                        DateConfiguration = config,
                        Holidays = holidays
                    });
                }

                var existing = await _context.SystemDateConfigurations.FirstOrDefaultAsync();
                if (existing == null)
                {
                    _logger.LogInformation("Creating new configuration");
                    config.CreatedDate = DateTime.Now;
                    _context.SystemDateConfigurations.Add(config);
                }
                else
                {
                    _logger.LogInformation("Updating existing configuration");
                    existing.TimeZone = config.TimeZone;
                    existing.DateFormat = config.DateFormat;
                    existing.FirstDayOfWeek = config.FirstDayOfWeek;
                    existing.UseSystemTime = config.UseSystemTime;
                    existing.CustomDateTime = config.CustomDateTime;
                    existing.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "System configuration updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating system configuration: {ex}");
                ModelState.AddModelError("", "An error occurred while saving the configuration. Please try again.");
                
                var holidays = await _context.Holidays
                    .Where(h => h.DeletedDate == null)
                    .OrderBy(h => h.StartDate)
                    .ToListAsync();

                return View("Index", new SystemConfigurationViewModel 
                { 
                    DateConfiguration = config,
                    Holidays = holidays
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHoliday([FromBody] Holiday holiday)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            holiday.CreatedDate = DateTime.Now;
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return Json(new { status = "success", message = "Holiday added successfully" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return Json(new { status = "error", message = "Holiday not found" });

            holiday.DeletedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { status = "success", message = "Holiday deleted successfully" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetHolidays()
        {
            var holidays = await _context.Holidays
                .Where(h => h.DeletedDate == null)
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    h.StartDate,
                    h.EndDate,
                    h.Description,
                    h.IsRecurring
                })
                .ToListAsync();

            return Json(new { data = holidays });
        }

        [HttpGet]
        public async Task<IActionResult> EditHoliday(int id)
        {
            var holiday = await _context.Holidays
                .Where(h => h.DeletedDate == null && h.Id == id)
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    h.StartDate,
                    h.EndDate,
                    h.Description,
                    h.IsRecurring
                })
                .FirstOrDefaultAsync();

            if (holiday == null)
                return NotFound();

            return Json(holiday);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHoliday([FromBody] Holiday holiday)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _context.Holidays.FindAsync(holiday.Id);
            if (existing == null)
                return NotFound();

            existing.Name = holiday.Name;
            existing.StartDate = holiday.StartDate;
            existing.EndDate = holiday.EndDate;
            existing.Description = holiday.Description;
            existing.IsRecurring = holiday.IsRecurring;
            existing.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { status = "success", message = "Holiday updated successfully" });
        }
    }
}
