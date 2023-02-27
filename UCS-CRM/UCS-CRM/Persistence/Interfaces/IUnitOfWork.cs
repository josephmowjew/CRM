namespace UCS_CRM.Persistence.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveToDataStore();
    }
}
