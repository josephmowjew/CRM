using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using UCS_CRM.Core.Services;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class EmailRepository : IEmailRepository
    {
        public IConfiguration _configuration { get; }
        private readonly IErrorLogService _errorService;
        public EmailRepository(IConfiguration configuration, IErrorLogService errorLogService)
        {
            _configuration = configuration;
            _errorService = errorLogService;
        }


        public async Task<string> SendMail(string email, string subject, string HtmlMessage)
        {
            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress(_configuration["MailSettings:SenderName"], _configuration["MailSettings:SenderEmail"]);
            message.From.Add(from);

            MailboxAddress to = new MailboxAddress(email, email);
            message.To.Add(to);

            message.Subject = subject;

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = HtmlMessage;

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();


                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.Auto);
                client.Authenticate(_configuration["MailSettings:SenderEmail"], _configuration["MailSettings:Password"]);

                client.Send(message);
                client.Disconnect(true);
                client.Dispose();

                return "Message sent";

            }
            catch (SslHandshakeException ex)
            {
                //Log exception to data store

                await this._errorService.LogErrorAsync(ex);

                return "Message not sent due to internet issues";
            }
            catch (Exception ex)
            {
                await this._errorService.LogErrorAsync(ex);

                return "Message not sent";
            }

           
           

            
        }
        public async Task<KeyValuePair<bool,string>> SendMailWithKeyVarReturn(string email, string subject, string HtmlMessage)
        {
            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress(_configuration["MailSettings:SenderName"], _configuration["MailSettings:SenderEmail"]);
            message.From.Add(from);

            MailboxAddress to = new MailboxAddress(email, email);
            message.To.Add(to);

            message.Subject = subject;

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = HtmlMessage;

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();


                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.Auto);
                client.Authenticate(_configuration["MailSettings:SenderEmail"], _configuration["MailSettings:Password"]);

                client.Send(message);
                client.Disconnect(true);
                client.Dispose();

                return new KeyValuePair<bool, string>(true, "Message sent");

            }
            catch (SslHandshakeException ex)
            {
                //Log exception to data store

                await this._errorService.LogErrorAsync(ex);

                return new KeyValuePair<bool, string>(false, "Message not sent due to internet issues");

            }
            catch (Exception ex)
            {
                await this._errorService.LogErrorAsync(ex);

                return new KeyValuePair<bool, string>(false, "Message not sent");

            }





        }
    }
}
