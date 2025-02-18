﻿using System.Security.Cryptography.X509Certificates;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketRepository
    {

       
        void Add(Ticket ticket);
        Ticket Exists(Ticket? ticket);
        Task<List<Ticket>?> GetTickets(CursorParams @params, Department department = null, string ticketStatus = "");
        Task<int> GetTicketsTotalFilteredAsync(CursorParams @params, Department department = null, string ticketStatus = "");
        Task<List<Ticket?>> GetMemberTickets(CursorParams @params, int memberId, string stateName = "");
        Task<List<Ticket?>> GetAssignedToTickets(CursorParams @params, string assignedToId, string status = "");
        Task<int> GetAssignedToTicketsCountAsync(CursorParams @params, string assignedToId, string status = "");
        Task<Ticket?> GetTicket(int id);
        void Remove(Ticket ticket);
        Task<int> TotalCount(string stateName = "");
        Task<int> TotalCountByMember(int memberId, string stateName = "");
        Task<int> TotalCountByAssignedTo(string assignedTo);
        Task<int> CountTicketsByStatusAssignedTo(string status, string assignedToId);
        Task<int> CountTicketsByStatusMember(string status, int memberId);
        Task<Ticket> LastTicket();
        Task<int> CountTicketsByPriority(string priority);
        Task<int> CountTicketsByCategory(string category);
        Task<int> CountTicketsByStatus(string state);
        Task<int> TotalClosedCount();
        Task<List<Ticket?>> GetClosedTickets(CursorParams @params);
        Task UnAssignedTickets();
        Task SendEscalatedTicketsReminder();
        Task SendUnassignedTicketEmail(Ticket ticket);
        Task SendTicketEscalationEmail(Ticket ticket, TicketEscalation ticketEscalation, string previousAssigneeEmail);
        Task EscalateTicket(Ticket ticket, string sender, string escalationReason);
        Task SendTicketReminders();
        Task SendTicketClosureNotifications(Ticket ticket, string reason);

        Task SendTicketReopenedNotifications(Ticket ticket, string reason);

        Task<int> CountTicketsByStatusCreatedByOrAssignedTo(string state, string identifier);
        Task<List<Ticket?>> GetTicketReports(CursorParams @params, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0);
        Task<int> GetTicketReportsCount(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0);

        Task SendTicketDeEscalationEmail(Ticket ticket, string previousAssigneeEmail);

        Task<int> GetEscalatedTicketsDataCountAsync(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0);
        Task<List<Ticket?>> GetEscalatedTicketsData(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0);
        Task<List<Ticket>> GetMemberEngagementOfficerReport(DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0, int departmentId = 0);
        Task SendTicketReassignmentEmail(string previousEmail, string newEmail, Ticket ticket);

        Task<object> GetTicketInitiator(int ticketId);
         Task<Ticket?> GetTicketWithTracking(int id);

         void SendTicketPickedEmail(string pickerEmail, Ticket ticket);
         Task<Ticket?> GetTicketByTicketNumber(string ticketNumber);
    }
}
