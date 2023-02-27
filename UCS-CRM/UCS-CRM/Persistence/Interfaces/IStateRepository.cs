using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IStateRepository
    {
        void Add(State state);
        State Exists(State state);
        Task<List<State>> GetStates();
        Task<State> GetStateAsync(int id);
        void Remove(State state);
        Task<int> TotalCount();
    }
}
