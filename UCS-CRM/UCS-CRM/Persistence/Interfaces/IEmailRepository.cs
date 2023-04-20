namespace UCS_CRM.Persistence.Interfaces
{
    public interface IEmailRepository
    {
        IConfiguration _configuration { get; }

         Task<string> SendMail(string email, string subject, string HtmlMessage);
    }
}