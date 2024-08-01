using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public UnitOfWork(ApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task SaveToDataStore()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<int> SaveToDataStoreSync()
        {
            int recordsAffected = await _context.SaveChangesAsync();
            return recordsAffected;
        }    
     }
}
