using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class WorkingHoursController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkingHoursController> _logger;

        public WorkingHoursController(ApplicationDbContext context, ILogger<WorkingHoursController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var workingHours = await _context.WorkingHours
                .Where(w => w.DeletedDate == null)
                .OrderBy(w => w.DayOfWeek)
                .ToListAsync();

            return View(workingHours);
        }

        public IActionResult Create()
        {
            return View(new WorkingHours 
            { 
                StartTime = new TimeSpan(9, 0, 0), // 9:00 AM
                EndTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                IsWorkingDay = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkingHours workingHours)
        {
            if (!ModelState.IsValid)
                return View(workingHours);

            // Check for existing working hours for the same day
            var existing = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.DayOfWeek == workingHours.DayOfWeek && w.DeletedDate == null);

            if (existing != null)
            {
                ModelState.AddModelError("", "Working hours for this day already exist.");
                return View(workingHours);
            }

            try
            {
                _context.WorkingHours.Add(workingHours);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Working hours created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating working hours: {ex}");
                ModelState.AddModelError("", "An error occurred while saving the working hours.");
                return View(workingHours);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.Id == id && w.DeletedDate == null);

            if (workingHours == null)
                return NotFound();

            return View(workingHours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkingHours workingHours)
        {
            if (id != workingHours.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(workingHours);

            try
            {
                var existing = await _context.WorkingHours
                    .FirstOrDefaultAsync(w => w.Id == id && w.DeletedDate == null);

                if (existing == null)
                    return NotFound();

                existing.StartTime = workingHours.StartTime;
                existing.EndTime = workingHours.EndTime;
                existing.IsWorkingDay = workingHours.IsWorkingDay;
                existing.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Working hours updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating working hours: {ex}");
                ModelState.AddModelError("", "An error occurred while updating the working hours.");
                return View(workingHours);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.Id == id && w.DeletedDate == null);

            if (workingHours == null)
                return NotFound();

            try
            {
                workingHours.DeletedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Working hours deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting working hours: {ex}");
                TempData["ErrorMessage"] = "An error occurred while deleting the working hours.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 