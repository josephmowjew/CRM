using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class ErrorLogRepository : IErrorLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ErrorLogRepository(ApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task AddAsync(ErrorLog errorDetails)
        {
             await this._context.ErrorLogs.AddAsync(errorDetails);
        }
    }
}
