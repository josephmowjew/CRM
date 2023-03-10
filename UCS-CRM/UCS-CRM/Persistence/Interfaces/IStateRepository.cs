using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IStateRepository
    {
        void Add(State state);
        State? Exists(string name);
        Task<List<State>?> GetStates(CursorParams @params);
        Task<State?> GetStateAsync(int id);
        void Remove(State state);
        Task<int> TotalActiveCount();
        Task<int> TotalDeletedCount();
    }
}
