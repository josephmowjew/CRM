using System.Security.Claims;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IFeedbackRepository
    {
        void Add(Feedback state);
        Feedback? DefaultFeedback(string name);
        Feedback? Exists(string name);
        Task<Feedback?> GetFeedbackAsync(int id);
        Task<List<Feedback>?> GetFeedbacks();
        Task<List<Feedback>?> GetFeedbacks(CursorParams @params, ClaimsPrincipal user);
        void Remove(Feedback state);
        Task<int> TotalActiveCount(ClaimsPrincipal user);
        Task<int> TotalDeletedCount();
    }
}