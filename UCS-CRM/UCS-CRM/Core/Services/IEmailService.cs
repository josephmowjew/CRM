namespace UCS_CRM.Core.Services
{
    public interface IEmailService
    {
      

        void SendEmail(string email, string subject, string HtmlMessage);
        string SendMail(string email, string subject, string HtmlMessage);

        Task<KeyValuePair<bool, string>> SendMailWithKeyVarReturn(string email, string subject, string HtmlMessage);
    }
}
