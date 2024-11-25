using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;


namespace UCS_CRM.Persistence.SQLRepositories
{
    public class WorkingHoursRepository : IWorkingHoursRepository
    {
        private readonly ApplicationDbContext _context;

        public WorkingHoursRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasWorkingHours()
        {
            return await _context.WorkingHours.AnyAsync(w => !w.DeletedDate.HasValue);
        }

        public async Task<WorkingHours> GetActiveWorkingHours()
        {
            return await _context.WorkingHours
                .FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
        }

        public async void AddWorkingHours(WorkingHours workingHours)
        {
            _context.WorkingHours.Add(workingHours);
            await _context.SaveChangesAsync();
        }

        public async Task<WorkingHours> GetWorkingHours()
        {
            return await _context.WorkingHours
                .FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
        }

        public async Task UpdateWorkingHours(WorkingHours workingHours)
        {
            var existing = await GetWorkingHours();
            if (existing != null)
            {
                existing.DeletedDate = DateTime.Now;
            }
            
            workingHours.CreatedDate = DateTime.Now;
            _context.WorkingHours.Add(workingHours);
            await _context.SaveChangesAsync();
        }
    }
} 