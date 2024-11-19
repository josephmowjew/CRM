using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Models;
using UCS_CRM.ViewModel;

namespace UCS_CRM.Controllers
{
   
    [Area("Manager")]
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
        public async Task<IActionResult> UpdateDateConfiguration(SystemDateConfiguration config)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _context.SystemDateConfigurations.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.SystemDateConfigurations.Add(config);
            }
            else
            {
                existing.TimeZone = config.TimeZone;
                existing.DateFormat = config.DateFormat;
                existing.FirstDayOfWeek = config.FirstDayOfWeek;
                existing.UseSystemTime = config.UseSystemTime;
                existing.CustomDateTime = config.CustomDateTime;
                existing.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddHoliday(Holiday holiday)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return NotFound();

            holiday.DeletedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    }
}
