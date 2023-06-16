using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IPositionRepository
    {
        void Add(Position position);
        Position? Exists(int id, string positionName, int rating);
        Task<List<Position>?> GetPositions(CursorParams @params);
        Task<List<Position>?> GetPositions();
        Task<Position?> GetPosition(int id);
        void Remove(Position position);
        Task<int> TotalCount();
        Task<int> TotalCountFiltered(CursorParams @params);

    }
}
