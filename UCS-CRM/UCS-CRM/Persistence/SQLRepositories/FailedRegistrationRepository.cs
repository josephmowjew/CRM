using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class FailedRegistrationRepository : IFailedRegistrationRepository
    {
        private readonly ApplicationDbContext _context;

        public FailedRegistrationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(FailedRegistration failedRegistration)
        {
            await _context.FailedRegistrations.AddAsync(failedRegistration);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnresolvedCountAsync()
        {
            return await _context.FailedRegistrations
                .CountAsync(f => !f.IsResolved);
        }

        public async Task<List<FailedRegistration>> GetUnresolvedAsync()
        {
            return await _context.FailedRegistrations
                .Where(f => !f.IsResolved)
                .OrderByDescending(f => f.AttemptedAt)
                .ToListAsync();
        }

        public async Task MarkAsResolvedAsync(int id, string resolvedBy, string notes)
        {
            var registration = await _context.FailedRegistrations.FindAsync(id);
            if (registration != null)
            {
                registration.IsResolved = true;
                registration.ResolvedAt = DateTime.UtcNow;
                registration.ResolvedBy = resolvedBy;
                registration.Notes = notes;
                await _context.SaveChangesAsync();
            }
        }
    }
}