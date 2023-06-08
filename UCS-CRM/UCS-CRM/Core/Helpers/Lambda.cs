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
        public static string Pending = "Pending";

        // roles
        public static string Administrator = "Administrator";
        public static string Member = "Member";
        public static string ExternalAdministrator = "External Administrator";
        public static string Manager = "Manager";
        public static string Clerk = "Clerk";
        public static string SeniorManager = "Senior Manager";

        // default properties
        public static string Category = "Support";
        public static string Support = "Support";
        public static string Priority = "Level One";
        public static string NewTicket = "New";

        //link
        public static string systemLink = "<a href='http://129.151.141.178/'>UCS Sacco System</a>";


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

        public static async Task<string> UploadFile(IFormFile file,string webrootPath)
        {
            string cleanFileName = string.Empty;
            string fileName = string.Empty;
            string complete_file_name = string.Empty;
            try
            {
                // Get the extension of the file
                var extension = Path.GetExtension(file.FileName);

                // Generate a file name on the spot
                fileName = Path.GetRandomFileName();

                cleanFileName = Path.GetFileNameWithoutExtension(fileName) + extension; ;
                // Generate a possible path to the file
                var pathBuilt = Path.Combine(webrootPath, "TicketAttachments");

                if (!Directory.Exists(pathBuilt))
                {
                    // Create the directory
                    await Task.Run(() => Directory.CreateDirectory(pathBuilt));
                }

                var path = Path.Combine(pathBuilt, cleanFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    // Copy the file to the path
                    await file.CopyToAsync(stream);
                }

                complete_file_name = Path.Combine("/", "TicketAttachments", cleanFileName);

                return complete_file_name;
            }
            catch (Exception ex)
            {
                return $"{complete_file_name} {ex}";
            }
        }

    }
}
