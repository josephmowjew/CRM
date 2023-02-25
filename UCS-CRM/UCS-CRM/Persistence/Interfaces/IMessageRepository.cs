using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMessageRepository
    {
        void Add(Message message);
        Message Exists(Message message);
        Task<List<Message>> GetMessages();
        Task<Message> GetMessageAsync(int id);
        void Remove(Message message);
    }
}
