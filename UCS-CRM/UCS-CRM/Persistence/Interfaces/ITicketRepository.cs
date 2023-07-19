using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketRepository
    {
        void Add(Ticket ticket);
        Ticket Exists(Ticket? ticket);
        Task<List<Ticket>?> GetTickets(CursorParams @params, Department department = null);
        Task<int> GetTicketsTotalFilteredAsync(CursorParams @params, Department department = null);
        Task<List<Ticket?>> GetMemberTickets(CursorParams @params, int memberId);
        Task<List<Ticket?>> GetAssignedToTickets(CursorParams @params, string assignedToId);
        Task<int> GetAssignedToTicketsCountAsync(CursorParams @params, string assignedToId);
        Task<Ticket?> GetTicket(int id);
        void Remove(Ticket ticket);
        Task<int> TotalCount();
        Task<int> TotalCountByMember(int memberId);
        Task<int> TotalCountByAssignedTo(string assignedTo);
        Task<int> CountTicketsByStatusAssignedTo(string status, string assignedToId);
        Task<int> CountTicketsByStatusMember(string status, int memberId);
        Task<Ticket> LastTicket();
        Task<int> CountTicketsByPriority(string priority);
        Task<int> CountTicketsByCategory(string category);
        Task<int> CountTicketsByStatus(string state);
        Task<int> TotalClosedCount();
        Task<List<Ticket?>> GetClosedTickets(CursorParams @params);
        Task<string> UnAssignedTickets();
        Task<string> EscalatedTickets();
        Task<string> SendUnassignedTicketEmail(Ticket ticket);
        Task<string> SendTicketEscalationEmail(Ticket ticket, TicketEscalation ticketEscalation, string previousAssigneeEmail);
        Task<string> EscalateTicket(Ticket ticket, string sender, string escalationReason);
        Task SendTicketReminders();
        Task<string> SendTicketClosureNotifications(Ticket ticket, string reason);

        Task<string> SendTicketReopenedNotifications(Ticket ticket, string reason);

        Task<int> CountTicketsByStatusCreatedByOrAssignedTo(string state, string identifier);
        Task<List<Ticket?>> GetTicketReports(CursorParams @params, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0);
        Task<int> GetTicketReportsCount(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0);

        Task<string> SendTicketDeEscalationEmail(Ticket ticket, string previousAssigneeEmail);

        Task<int> GetEscalatedTicketsDataCountAsync(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0);
        Task<List<Ticket?>> GetEscalatedTicketsData(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0);
        Task<List<Ticket>> GetMemberEngagementOfficerReport(DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0, int departmentId = 0);
        Task<string> SendTicketReassignmentEmail(string previousEmail, string newEmail, Ticket ticket);
    }
}
