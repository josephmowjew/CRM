using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UCS_CRM.Core.Helpers
{
    public class Lambda
    {
        public static string Active = "Active";
        public static string Deleted = "Deleted";
        public static string Disabled = "Disabled";
        public static string Voided = "Voided";
        public static string Closed = "Closed";
        public static string IssuePrefix = "REF-0000";
        public static string RequestPrefix = "REQ-0000";

        // roles
        public static string Administrator = "Administrator";
        public static string External = "External";
        public static string ExternalAdministrator = "External Administrator";
        public static string Management = "Management";
        public static string Engineers = "Engineers";
        public static string SeniorEngineer = "Senior Engineer";

        // default properties
        public static string Category = "Support";
        public static string Priority = "Level One";
        public static string State = "Waiting For Support";


       

        // issues filters
        public static string Categories = "Categories";
        public static string Priorities = "Priorities";
        public static string States = "States";
        public static string Clients = "Clients";

        // message action
        public static string UserCreation = "User Creation";
        public static string UserActivation = "User Activation";
        public static string UserPassword = "User Password";
        public static string ContractExpiry = "Contract Expiry";
        public static string PasswordReset = "Password Reset";
        public static string TicketAssignment = "Ticket Assignment";
        public static string TicketCreate = "Ticket Created";
        public static string TicketUpdate = "Ticket Updated";
        public static string TicketClosed = "Ticket Closed";
        public static string QuotationRequest = "Quotation Request";
        public static string TicketNeedAttention = "Ticket Needing Attention";
        public static string TicketUnassigned = "Ticket Unassigned";
        public static string TicketEscalation = "Ticket Escalation";

    }
}
